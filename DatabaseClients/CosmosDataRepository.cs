namespace DataRepositories
{
    using Azure;
    using DataRepositories.CrudResponses;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading.Tasks;


    //FIXME:: better error handling throughout
    public class CosmosDataRepository : IDataRepository
    {
        private static readonly string _COLLECTION_NAME_FORMAT_STRING = "{0}s";
        private static readonly string _PARTITION_KEY_PATH_FORMAT_STRING = "/{0}";

        private readonly CosmosClient _client;


        public CosmosDataRepository(string connectionString)
        {
            _client = new CosmosClient(connectionString);
        }


        public async Task<SimpleCrudResponse> CreateDatabaseIfNotExists(string databaseName)
        {
            var response = await _client.CreateDatabaseIfNotExistsAsync(databaseName);

            var crudResponse = new SimpleCrudResponse()
            {
                Success = response.StatusCode == HttpStatusCode.Created,
                Message = response.StatusCode.ToString(),
            };

            return crudResponse;
        }


        public async Task<SimpleCrudResponse> DeleteDatabaseIfExists(string databaseName)
        {
            var response = await _client.GetDatabase(databaseName).DeleteAsync();

            var crudResponse = new SimpleCrudResponse()
            {
                Success = response.StatusCode == HttpStatusCode.OK,
                Message = response.StatusCode.ToString(),
            };

            return crudResponse;
        }


        public async Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName)
        {
            string collectionName = BuildCollectionName(modelType);

            Database database = _client.GetDatabase(databaseName);

            ContainerProperties properties = new ContainerProperties()
            {
                Id = collectionName,
                PartitionKeyPath = BuildPartitionKeyPath(partitionKeyPropertyName),
            };

            var response = await database.CreateContainerIfNotExistsAsync(properties);

            var crudResponse = new SimpleCrudResponse()
            {
                Success = response.StatusCode == HttpStatusCode.Created,
                Message = response.StatusCode.ToString(),
            };

            return crudResponse;
        }


        public async Task<SimpleCrudResponse> DeleteCollectionIfExists(string databaseName, Type modelType)
        {
            string collectionName = BuildCollectionName(modelType);

            var response = await _client.GetContainer(databaseName, collectionName).DeleteContainerAsync();

            var crudResponse = new SimpleCrudResponse()
            {
                Success = response.StatusCode == HttpStatusCode.OK,
                Message = response.StatusCode.ToString(),
            };

            return crudResponse;
        }


        public async Task<SingleItemCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).CreateItemAsync(item, key);

            var crudResponse = new SingleItemCrudResponse<T>()
            {
                Success = response.StatusCode == HttpStatusCode.Created,
                Message = response.StatusCode.ToString(),
                Item = response.Resource,
            };

            return crudResponse;
        }


        public async Task<SingleItemCrudResponse<T>> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).ReadItemAsync<T>(itemId, key);

            var crudResponse = new SingleItemCrudResponse<T>()
            {
                Success = response.StatusCode == HttpStatusCode.OK,
                Message = response.StatusCode.ToString(),
                Item = response.Resource,
            };

            return crudResponse;
        }


        public async Task<SingleItemCrudResponse<T>> ReadSingleItem<T>(string databaseName, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));

            var queryable = _client.GetContainer(databaseName, collectionName).GetItemLinqQueryable<T>();

            var item = queryable.FirstOrDefault(predicate);

            var crudResponse = new SingleItemCrudResponse<T>()
            {
                Success = true,
                Message = "Warning! Implemented with Linq query, so this will block. FIXME:: Implement using QueryDefinition and asynchronous operations.",
                Item = item,
            };

            return crudResponse;
        }


        public async Task<SingleItemCrudResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).ReplaceItemAsync(item, id, key);

            var crudResponse = new SingleItemCrudResponse<T>()
            {
                Success = response.StatusCode == HttpStatusCode.OK,
                Message = response.StatusCode.ToString(),
                Item = response.Resource,
            };

            return crudResponse;
        }


        public async Task<SimpleCrudResponse> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            Container container = _client.GetContainer(databaseName, collectionName);
            var response = await container.DeleteItemAsync<T>(id, key);

            var simpleResponse = new SimpleCrudResponse()
            {
                Success = response.StatusCode == HttpStatusCode.NoContent,
                Message = response.StatusCode.ToString(),
            };

            return simpleResponse;
        }


        public async Task<SingleItemCrudResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new()
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).UpsertItemAsync(item, key);

            var crudResponse = new SingleItemCrudResponse<T>()
            {
                Success = response.StatusCode == HttpStatusCode.OK,
                Message = response.StatusCode.ToString(),
                Item = response.Resource,
            };

            return crudResponse;
        }


        private static string BuildCollectionName(Type modelType)
        {
            return string.Format(_COLLECTION_NAME_FORMAT_STRING, modelType.Name);
        }


        private static string BuildPartitionKeyPath(string propertyName)
        {
            return string.Format(_PARTITION_KEY_PATH_FORMAT_STRING, propertyName);
        }


        private static PartitionKey BuildPartitionKey<V>(V value)
        {
            PartitionKey result = new PartitionKey();

            switch (value)
            {
                case bool _value:
                    result = new PartitionKey(_value);
                    break;
                case double _value:
                    result = new PartitionKey(_value);
                    break;
                case string _value:
                    result = new PartitionKey(_value);
                    break;
                default:
                    Console.WriteLine($"V cannot be of type {typeof(V)}");//FIXME:: use logging
                    break;
            }

            return result;
        }
    }
}
