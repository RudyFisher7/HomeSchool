namespace DataRepositories
{
    using DataRepositories.CrudResponses;
    using Microsoft.Azure.Cosmos;
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
            var result = new SimpleCrudResponse();

            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(databaseName);

            result.Success = databaseResponse.StatusCode == System.Net.HttpStatusCode.Created;

            return result;
        }


        public async Task<SimpleCrudResponse> DeleteDatabaseIfExists(string databaseName)
        {
            var result = new SimpleCrudResponse();

            DatabaseResponse response = await _client.GetDatabase(databaseName).DeleteAsync();

            result.Success = response.StatusCode == System.Net.HttpStatusCode.OK;

            return result;
        }


        public async Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName)
        {
            var result = new SimpleCrudResponse();

            string collectionName = BuildCollectionName(modelType);

            Database database = _client.GetDatabase(databaseName);

            ContainerProperties properties = new ContainerProperties()
            {
                Id = collectionName,
                PartitionKeyPath = BuildPartitionKeyPath(partitionKeyPropertyName),
            };

            ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(properties);

            result.Success = containerResponse.StatusCode == System.Net.HttpStatusCode.Created;

            return result;
        }


        public async Task<SimpleCrudResponse> DeleteCollectionIfExists(string databaseName, Type modelType)
        {
            var result = new SimpleCrudResponse();

            string collectionName = BuildCollectionName(modelType);

            var response = await _client.GetContainer(databaseName, collectionName).DeleteContainerAsync();

            result.Success = response.StatusCode == System.Net.HttpStatusCode.OK;

            return result;
        }


        public async Task<DataCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            var result = new DataCrudResponse<T>();

            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).CreateItemAsync(item, key);

            result.Success = response.StatusCode == System.Net.HttpStatusCode.Created;
            result.Data = response.Resource;

            return result;
        }


        public async Task<T?> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class
        {
            T? result = null;

            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            var response = await _client.GetContainer(databaseName, collectionName).ReadItemAsync<T>(itemId, key);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = response.Resource;
            }

            return result;
        }


        public async Task<T?> ReadSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));

            var result = _client.GetContainer(databaseName, collectionName).GetItemLinqQueryable<T>();

            return result.SingleOrDefault(predicate);
        }


        public async Task<ItemResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            return await _client.GetContainer(databaseName, collectionName).ReplaceItemAsync(item, id, key);
        }


        public async Task<ItemResponse<T>> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            Container container = _client.GetContainer(databaseName, collectionName);
            var response = await container.DeleteItemAsync<T>(id, key);

            return response;
        }


        public async Task<ItemResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            return await _client.GetContainer(databaseName, collectionName).UpsertItemAsync(item, key);
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
