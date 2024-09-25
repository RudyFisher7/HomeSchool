using DataRepositories.Attributes;
using DataRepositories.CrudResponses;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataRepositories
{
    //FIXME:: use partition keys so this can be like cosmosclient
    //TODO:: create database if not exists implementation in this class so far only might support sql server
    public class SqlDataRepository : IDataRepository
    {
        private delegate void BuildSqlCommandDelegate(SqlCommand command);
        private class BuildQueryResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Query { get; set; } = string.Empty;
        }


        private class ReadSingleItemResult<T> where T : class, new()
        {
            public T? Item { get; set; }
            public string Message { get; set; } = string.Empty;
        }


        private static readonly string _DATABASE_NAME_PARAMETER = "@DatabaseName";
        private static readonly string _COLLECTION_NAME_PARAMETER = "@CollectionName";
        private static readonly string _ITEM_ID_PARAMETER = "@Id";
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
                BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                {
                    command.CommandText = $"IF NOT EXISTS(SELECT name FROM sys.databases WHERE name = {_DATABASE_NAME_PARAMETER}) BEGIN CREATE DATABASE {databaseName} END";
                    command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                };

                result = await ExecuteNonQuery(buildSqlCommandDelegate);
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
                BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                {
                    command.CommandText = $"DROP DATABASE IF EXISTS {databaseName}";
                    command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                };

                result = await ExecuteNonQuery(buildSqlCommandDelegate);
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
                    BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                    {
                        command.CommandText = queryResult.Query;
                        command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                        command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(modelType));
                    };

                    result = await ExecuteNonQuery(buildSqlCommandDelegate);
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
                BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                {
                    command.CommandText = $"USE {databaseName}; DROP TABLE IF EXISTS dbo.{modelType.Name}s";
                    command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
                    command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(modelType));
                };

                result = await ExecuteNonQuery(buildSqlCommandDelegate);
            }
            else
            {
                result.Message = string.Format(_FORBIDDEN_DDL_MESSAGE_FORMAT_STRING, databaseName);
            }

            return result;
        }


        public async Task<SingleItemCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new()
        {
            var result = new SingleItemCrudResponse<T>();

            var queryResult = BuildCreateSingleItemQuery(databaseName, typeof(T));

            if (queryResult.Success)
            {
                BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                {
                    command.CommandText = queryResult.Query;
                    var populateResult = PopulateCreateSingleItemCommand(command, databaseName, item);
                };

                var simpleResponse = await ExecuteNonQuery(buildSqlCommandDelegate);

                result.Success = simpleResponse.Success;
                result.Message = simpleResponse.Message;
                result.DocumentsAffected = simpleResponse.DocumentsAffected;
                result.Item = item;
            }

            return result;
        }


        public async Task<SingleItemCrudResponse<T>> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class, new()
        {
            BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
            {
                command.CommandText = $"USE {databaseName}; SELECT * FROM {BuildCollectionName(typeof(T))} WHERE Id = {_ITEM_ID_PARAMETER}";
                command.Parameters.AddWithValue(_ITEM_ID_PARAMETER, itemId);
            };

            return await ExecuteReadSingleItemQuery<T>(buildSqlCommandDelegate);
        }


        public async Task<SingleItemCrudResponse<T>> ReadSingleItem<T>(string databaseName, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var predicateBody = predicate.Body as BinaryExpression;

            if (predicateBody != null )
            {
                //TODO:: implement
            }

            return new SingleItemCrudResponse<T>()
            {
                Success = false,
                Message = "Method not yet implemented.",
            };
        }


        public async Task<SingleItemCrudResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class, new()
        {
            var response = new SingleItemCrudResponse<T>();

            var queryResult = BuildUpdateSingleItemQuery(databaseName, item);

            if (queryResult.Success)
            {
                BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
                {
                    command.CommandText = queryResult.Query;
                    var populateResult = PopulateUpdateSingleItemCommand(command, databaseName, item);
                };

                var executeResult = await ExecuteNonQuery(buildSqlCommandDelegate);

                response.Success = executeResult.Success;
                response.Message = executeResult.Message;
                response.Item = item;
            }
            else
            {
                response.Success = queryResult.Success;
                response.Message = queryResult.Message;
            }

            return response;
        }


        public async Task<SimpleCrudResponse> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class, new()
        {
            BuildSqlCommandDelegate buildSqlCommandDelegate = command =>
            {
                command.CommandText = $"USE {databaseName}; DELETE FROM {BuildCollectionName(typeof(T))} WHERE Id = {_ITEM_ID_PARAMETER}";
                command.Parameters.AddWithValue(_ITEM_ID_PARAMETER, id);
            };

            return await ExecuteNonQuery(buildSqlCommandDelegate);
        }


        public async Task<SingleItemCrudResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new()
        {
            return new SingleItemCrudResponse<T>()
            {
                Success = false,
                Message = "Method not implemented.",
            };
        }


        private async Task<SimpleCrudResponse> ExecuteNonQuery(BuildSqlCommandDelegate buildSqlCommandDelegate)
        {
            var result = new SimpleCrudResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = connection.CreateCommand())
                {
                    buildSqlCommandDelegate(command);
                    await command.Connection.OpenAsync();

                    result.DocumentsAffected = await command.ExecuteNonQueryAsync();
                    result.Success = true;
                }
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = exception.Message;
            }

            return result;
        }

        private async Task<SingleItemCrudResponse<T>> ExecuteReadSingleItemQuery<T>(BuildSqlCommandDelegate buildSqlCommandDelegate) where T : class, new()
        {
            var result = new SingleItemCrudResponse<T>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = connection.CreateCommand())
                {
                    buildSqlCommandDelegate(command);
                    await command.Connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var readResult = await TryReadSingleItem<T>(reader);

                        result.Success = readResult.Item != null;
                        result.Item = readResult.Item;
                        result.Message = readResult.Message;
                    }
                }
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = exception.Message;
            }

            return result;
        }


        private async Task<ReadSingleItemResult<T>> TryReadSingleItem<T>(SqlDataReader reader) where T : class, new()
        {
            var result = new ReadSingleItemResult<T>();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(typeof(T), t => t.GetProperties());

            if (await reader.ReadAsync())
            {
                var item = new T();
                foreach (var property in properties)
                {
                    if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                    {
                        var ordinal = reader.GetOrdinal(property.Name);
                        var value = reader.GetValue(ordinal);
                        property.SetValue(item, value);
                    }
                }

                result.Item = item;
            }

            if (await reader.ReadAsync())
            {
                result.Message = "Query resulted in more than 1 item. Just read the first one.";
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

            try
            {
                foreach (var property in properties)
                {
                    propertyName = property.Name;

                    var attribute = property.GetCustomAttribute<SqlTypeAttribute>();

                    query.Append($"{propertyName}, ");
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


        private static bool PopulateCreateSingleItemCommand<T>(SqlCommand command, string databaseName, T model) where T : class, new()
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
                    propertyType = attribute!.GetSqlTypeString();

                    query.Append($"{propertyName} {propertyType} {attribute.GetSqlConstraintString()}, ");
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

        private static BuildQueryResult BuildUpdateSingleItemQuery<T>(string databaseName, T model) where T : class, new()
        {
            var result = new BuildQueryResult();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(typeof(T), t => t.GetProperties());

            var query = new StringBuilder();
            query.Append($"USE {databaseName}; UPDATE dbo.{BuildCollectionName(typeof(T))} SET ");

            var propertyName = string.Empty;

            var primaryKeyPropertyName = string.Empty;

            try
            {
                foreach (var property in properties)
                {
                    propertyName = property.Name;

                    var attribute = property.GetCustomAttribute<SqlTypeAttribute>();

                    if (attribute!.SqlConstraintEnum != SqlConstraintEnum.PRIMARY_KEY)
                    {
                        query.Append($"{propertyName} = @{propertyName}, ");
                    }
                    else
                    {
                        primaryKeyPropertyName = property.Name;
                    }
                }

                query.Length -= 2;
                query.Append(" ");

                query.Append($"WHERE {primaryKeyPropertyName} = @{primaryKeyPropertyName}");

                result.Success = true;
                result.Query = query.ToString();
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Message = $"model type: {typeof(T).Name}, propertyName: {propertyName} - {exception.Message}";
            }

            return result;
        }

        private static bool PopulateUpdateSingleItemCommand<T>(SqlCommand command, string databaseName, T model) where T : class, new()
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
    }
}
