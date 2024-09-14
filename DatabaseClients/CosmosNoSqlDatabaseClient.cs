namespace DatabaseClients
{
    using Microsoft.Azure.Cosmos;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class CosmosNoSqlDatabaseClient : IDatabaseClient
    {
        private readonly string _databaseNameFormatString = "{0}s";
        private readonly string _partitionKeyPathFormatString = "/{0}";

        private readonly CosmosClient _client;


        public CosmosNoSqlDatabaseClient(string connectionString)
        {
            _client = new CosmosClient(connectionString);
        }


        public async Task<bool> CreateDatabaseIfNotExists(string databaseName)
        {
            bool result = false;

            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(databaseName);

            result = databaseResponse.StatusCode == System.Net.HttpStatusCode.Created;

            return result;
        }


        public async Task<bool> DeleteDatabase(string databaseName)
        {
            bool result = false;

            DatabaseResponse response = await _client.GetDatabase(databaseName).DeleteAsync();

            result = response.StatusCode == System.Net.HttpStatusCode.OK;

            return result;
        }


        public async Task<bool> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName)
        {
            bool result = false;

            string collectionName = BuildCollectionName(modelType);

            Database database = _client.GetDatabase(databaseName);

            ContainerProperties properties = new ContainerProperties()
            {
                Id = collectionName,
                PartitionKeyPath = BuildPartitionKeyPath(partitionKeyPropertyName),
            };

            ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(properties);

            result = containerResponse.StatusCode == System.Net.HttpStatusCode.Created;

            return result;
        }


        public async Task<bool> DeleteCollection(string databaseName, Type modelType)
        {
            bool result = false;

            string collectionName = BuildCollectionName(modelType);

            var response = await _client.GetContainer(databaseName, collectionName).DeleteContainerAsync();

            result = response.StatusCode == System.Net.HttpStatusCode.OK;

            return result;
        }


        public async Task<ItemResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            var key = BuildPartitionKey(partitionKeyValue);

            return await _client.GetContainer(databaseName, collectionName).CreateItemAsync(item, key);
        }


        public async Task<T?> GetSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class
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


        public async Task<T?> GetSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class
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


        private string BuildCollectionName(Type modelType)
        {
            return string.Format(_databaseNameFormatString, modelType.Name);
        }


        private string BuildPartitionKeyPath(string propertyName)
        {
            return string.Format(_partitionKeyPathFormatString, propertyName);
        }


        private PartitionKey BuildPartitionKey<V>(V value)
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
