namespace DatabaseClients
{
    using Microsoft.Azure.Cosmos;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class CosmosNoSqlDatabaseClient : IDatabaseClient
    {
        private readonly string _databaseNameFormatString = "{0}s";
        private readonly string _partitionKeyPathFormatString = "/{0}s";

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

        public async Task<bool> CreateCollectionIfNotExists(string databaseName, Type modelType)
        {
            bool result = false;

            string collectionName = BuildCollectionName(modelType);

            Database database = _client.GetDatabase(databaseName);

            ContainerProperties properties = new ContainerProperties()
            {
                Id = collectionName,
                PartitionKeyPath = BuildPartitionKeyPath(modelType),
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

        public async Task<ItemResponse<T>> InsertSingleItem<T>(string databaseName, T item) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));

            return await _client.GetContainer(databaseName, collectionName).UpsertItemAsync(item);
        }

        public async Task<T?> GetSingleItem<T>(string databaseName, string itemId) where T : class
        {
            T? result = null;
            string collectionName = BuildCollectionName(typeof(T));
            string partitionKeyPath = BuildPartitionKeyPath(typeof(T));

            var key = new PartitionKey(partitionKeyPath);

            var response = await _client.GetContainer(databaseName, collectionName).ReadItemAsync<T>(itemId, new PartitionKey(partitionKeyPath));

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

        public async Task<ItemResponse<T>> UpdateSingleItem<T>(string databaseName, string id, T item) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            string partitionKeyPath = BuildPartitionKeyPath(typeof(T));

            return await _client.GetContainer(databaseName, collectionName).ReplaceItemAsync(item, id, new PartitionKey(partitionKeyPath));
        }

        public async Task<ItemResponse<T>> DeleteSingleItem<T>(string databaseName, string id) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));
            string partitionKeyPath = BuildPartitionKeyPath(typeof(T));

            var key = new PartitionKey(partitionKeyPath);

            var container = _client.GetContainer(databaseName, collectionName);
            var response = container.DeleteItemAsync<T>(id, new PartitionKey(partitionKeyPath)).Result;

            return response;
        }

        public async Task<ItemResponse<T>> UpsertSingleItem<T>(string databaseName, T item) where T : class
        {
            string collectionName = BuildCollectionName(typeof(T));

            return await _client.GetContainer(databaseName, collectionName).UpsertItemAsync(item);
        }

        private string BuildCollectionName(Type modelType)
        {
            return string.Format(_databaseNameFormatString, modelType.Name);
        }

        private string BuildPartitionKeyPath(Type modelType)
        {
            return string.Format(_partitionKeyPathFormatString, modelType.Name);
        }
    }
}
