using DatabaseClients.Attributes;
using DatabaseClients.CrudResponses;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace DatabaseClients
{
    //FIXME:: use partition keys so this can be like cosmosclient
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


        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();


        private readonly string _connectionString;


        public SqlDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        

        public async Task<SimpleCrudResponse> CreateDatabaseIfNotExists(string databaseName)
        {
            var command = new SqlCommand($"CREATE DATABASE IF NOT EXISTS {_DATABASE_NAME_PARAMETER}");
            command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);

            var result = await ExecuteNonQuery(command);

            return result;
        }

        public async Task<SimpleCrudResponse> DeleteDatabase(string databaseName)
        {
            var command = new SqlCommand($"DROP DATABASE IF EXISTS {_DATABASE_NAME_PARAMETER}");
            command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);

            var result = await ExecuteNonQuery(command);

            return result;
        }


        public async Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName)
        {
            var result = new SimpleCrudResponse();

            var queryResult = BuildCreateCollectionQuery(modelType);

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

            return result;
        }


        public async Task<SimpleCrudResponse> DeleteCollection(string databaseName, Type modelType)
        {
            var command = new SqlCommand($"USE {_DATABASE_NAME_PARAMETER}; DROP TABLE IF EXISTS dbo.{_COLLECTION_NAME_PARAMETER}");
            command.Parameters.AddWithValue(_DATABASE_NAME_PARAMETER, databaseName);
            command.Parameters.AddWithValue(_COLLECTION_NAME_PARAMETER, BuildCollectionName(modelType));

            var result = await ExecuteNonQuery(command);

            return result;
        }


        public async Task<DataCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            var result = new DataCrudResponse<T>();

            var queryResult = BuildCreateSingleItemQuery(typeof(T));

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


        private static string BuildCollectionName(Type modelType)
        {
            return string.Format(_COLLECTION_NAME_FORMAT_STRING, modelType.Name);
        }


        private static BuildQueryResult BuildCreateSingleItemQuery(Type modelType)
        {
            var result = new BuildQueryResult();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(modelType, t => t.GetProperties());

            var query = new StringBuilder();
            query.Append($"USE {_DATABASE_NAME_PARAMETER}; INSERT INTO dbo.{_COLLECTION_NAME_PARAMETER} (");

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


        private static BuildQueryResult BuildCreateCollectionQuery(Type modelType)
        {
            var result = new BuildQueryResult();

            PropertyInfo[] properties = _propertyCache.GetOrAdd(modelType, t => t.GetProperties());

            var query = new StringBuilder();
            query.Append($"USE {_DATABASE_NAME_PARAMETER}; CREATE TABLE IF NOT EXISTS dbo.{_COLLECTION_NAME_PARAMETER} (");

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
    }
}
