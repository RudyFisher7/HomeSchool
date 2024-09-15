using DataRepositories.Attributes;
using DataRepositories.CrudResponses;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataRepositories
{
    //FIXME:: use partition keys so this can be like cosmosclient
    //TODO:: create database if not exists implementation in this class so far only might support sql server
    public class SqlDataRepository : IDataRepository
    {
        private class BuildQueryResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Query { get; set; } = string.Empty;
        }


        private static readonly string _DATABASE_NAME_PARAMETER = "@DatabaseName";
        private static readonly string _COLLECTION_NAME_PARAMETER = "@CollectionName";
        private static readonly string _COLLECTION_NAME_FORMAT_STRING = "{0}s";
        private static readonly string _VALID_DATABASE_OR_TABLE_NAME_REGEX = @"^[a-zA-Z0-9]+";
        private static readonly string _FORBIDDEN_DDL_MESSAGE_FORMAT_STRING = $"'{0}' contians forbidden characters. Only letters and numbers allowed.";


        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();


        private readonly string _connectionString;


        public SqlDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        

        /// <inheritdoc/>
        /// <remarks>
        /// WARNING! Parameters are only supported for DML operations, not DDL
        /// operations, so this function is succeptable to SQL injection for
        /// the database name.
        /// </remarks>
        public async Task<SimpleCrudResponse> CreateDatabaseIfNotExists(string databaseName)
        {
            var result = new SimpleCrudResponse();

            if (IsDdlNameAllowed(databaseName))
            {
                var command = new SqlCommand($"IF NOT EXISTS(SELECT name FROM sys.databases WHERE name = {_DATABASE_NAME_PARAMETER}) BEGIN CREATE DATABASE {databaseName} END");
                command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);

                result = await ExecuteNonQuery(command);
            }
            else
            {
                result.Message = string.Format(_FORBIDDEN_DDL_MESSAGE_FORMAT_STRING, databaseName);
            }

            return result;
        }

        public async Task<SimpleCrudResponse> DeleteDatabaseIfExists(string databaseName)
        {
            var result = new SimpleCrudResponse();

            if (IsDdlNameAllowed(databaseName))
            {
                var command = new SqlCommand($"DROP DATABASE IF EXISTS {databaseName}");
                command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);

                result = await ExecuteNonQuery(command);
            }
            else
            {
                result.Message = string.Format(_FORBIDDEN_DDL_MESSAGE_FORMAT_STRING, databaseName);
            }

            return result;
        }


        public async Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName)
        {
            var result = new SimpleCrudResponse();

            if (IsDdlNameAllowed(databaseName))
            {
                var queryResult = BuildCreateCollectionQuery(databaseName, modelType);

                if (queryResult.Success)
                {
                    var command = new SqlCommand(queryResult.Query);
                    command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                    command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(modelType));

                    result = await ExecuteNonQuery(command);
                }
                else
                {
                    result.Success = false;
                    result.Message = queryResult.Message;
                }
            }
            else
            {
                result.Message = string.Format(_FORBIDDEN_DDL_MESSAGE_FORMAT_STRING, databaseName);
            }

            return result;
        }


        public async Task<SimpleCrudResponse> DeleteCollectionIfExists(string databaseName, Type modelType)
        {
            var result = new SimpleCrudResponse();

            if (IsDdlNameAllowed(databaseName))
            {
                var command = new SqlCommand($"USE {databaseName}; DROP TABLE IF EXISTS dbo.{modelType.Name}s");
                command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(modelType));

                result = await ExecuteNonQuery(command);
            }
            else
            {
                result.Message = string.Format(_FORBIDDEN_DDL_MESSAGE_FORMAT_STRING, databaseName);
            }

            return result;
        }


        public async Task<DataCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            var result = new DataCrudResponse<T>();

            var queryResult = BuildCreateSingleItemQuery(databaseName, typeof(T));

            if (queryResult.Success)
            {
                var command = new SqlCommand(queryResult.Query);
                var populateResult = PopulateCreateSingleItemCommand(command, databaseName, item);

                var simpleResponse = await ExecuteNonQuery(command);

                result.Success = simpleResponse.Success;
                result.Message = simpleResponse.Message;
                result.DocumentsAffected = simpleResponse.DocumentsAffected;
                result.Data = item;
            }

            return result;
        }


        public Task<T?> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class
        {
            throw new NotImplementedException();
        }


        public Task<T?> ReadSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class
        {
            throw new NotImplementedException();
        }


        public Task<ItemResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class
        {
            throw new NotImplementedException();
        }


        public Task<ItemResponse<T>> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class
        {
            throw new NotImplementedException();
        }


        public Task<ItemResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            throw new NotImplementedException();
        }


        private async Task<SimpleCrudResponse> ExecuteNonQuery(SqlCommand command)
        {
            var result = new SimpleCrudResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    command.Connection = connection;
                    await command.Connection.OpenAsync();

                    result.DocumentsAffected = await command.ExecuteNonQueryAsync();
                    result.Success = true;
                }
                catch (Exception exception)
                {
                    result.Success = false;
                    result.Message = exception.Message;
                }
            }

            return result;
        }


        private bool IsDdlNameAllowed(string name)
        {
            bool result = Regex.IsMatch(name, _VALID_DATABASE_OR_TABLE_NAME_REGEX);

            return result;
        }


        private static string BuildCollectionName(Type modelType)
        {
            return string.Format(_COLLECTION_NAME_FORMAT_STRING, modelType.Name);
        }


        private static BuildQueryResult BuildCreateSingleItemQuery(string databaseName, Type modelType)
        {
            var result = new BuildQueryResult();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(modelType, t => t.GetProperties());

            var query = new StringBuilder();
            query.Append($"USE {databaseName}; INSERT INTO dbo.{BuildCollectionName(modelType)} (");

            var propertyName = string.Empty;
            var propertyType = string.Empty;

            try
            {
                foreach (var property in properties)
                {
                    propertyName = property.Name;

                    var attribute = property.GetCustomAttribute<SqlTypeAttribute>();
                    propertyType = attribute!.SqlTypeString;

                    query.Append($"{propertyName} {propertyType}, ");
                }

                query.Length -= 2;
                query.Append(") ");

                query.Append("VALUES (");

                foreach (var property in properties)
                {
                    propertyName = property.Name;
                    query.Append($"@{propertyName}, ");
                }

                query.Length -= 2;
                query.Append(")");

                result.Success = true;
                result.Query = query.ToString();
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = $"model type: {modelType.Name}, propertyName: {propertyName} - {exception.Message}";
            }

            return result;
        }


        private static bool PopulateCreateSingleItemCommand<T>(SqlCommand command, string databaseName, T model)
        {
            var result = false;

            command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
            command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(typeof(T)));

            PropertyInfo[] properties = _propertyCache.GetOrAdd(typeof(T), t => t.GetProperties());

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<SqlTypeAttribute>();
                var propertyValue = Activator.CreateInstance(attribute!.SqlType, property.GetValue(model));
                if (propertyValue != null)
                {
                    command.Parameters.AddWithValue($"@{property.Name}", propertyValue);
                }
            }

            return result;
        }


        private static BuildQueryResult BuildCreateCollectionQuery(string databaseName, Type modelType)
        {
            var result = new BuildQueryResult();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(modelType, t => t.GetProperties());

            var query = new StringBuilder();
            query.Append($"USE {databaseName}; IF NOT EXISTS (SELECT name FROM sys.tables WHERE name = {_COLLECTION_NAME_PARAMETER}) BEGIN CREATE TABLE dbo.{BuildCollectionName(modelType)} (");

            var propertyName = string.Empty;
            var propertyType = string.Empty;

            try
            {
                foreach (var property in properties)
                {
                    propertyName = property.Name;

                    var attribute = property.GetCustomAttribute<SqlTypeAttribute>();
                    propertyType = attribute!.SqlTypeString;

                    query.Append($"{propertyName} {propertyType} {attribute.SqlConstraintString}, ");
                }

                query.Length -= 2;
                query.Append(") END");

                result.Success = true;
                result.Query = query.ToString();
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = $"model type: {modelType.Name}, propertyName: {propertyName} - {exception.Message}";
            }

            return result;
        }
    }
}
