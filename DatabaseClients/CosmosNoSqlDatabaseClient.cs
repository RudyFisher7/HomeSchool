namespace DatabaseClients
{
    using Microsoft.Azure.Cosmos;
    using System.Threading.Tasks;

    public class CosmosNoSqlDatabaseClient : IDatabaseClient
    {
        private readonly string _databaseNameFormatString = "{0}s";
        private readonly CosmosClient _client;
        public CosmosNoSqlDatabaseClient(string connectionString)
        {
            _client = new CosmosClient(connectionString);
        }

        public async Task<bool> CreateDatabaseIfNotExists(Type modelType)
        {
            bool result = false;

            string databaseName = BuildDatabaseName(modelType);

            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(databaseName);

            ContainerProperties properties = new ContainerProperties()
            {
                Id = databaseName,
                PartitionKeyPath = $"/{databaseName}",
            };

            ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(properties);

            result = (
                    databaseResponse.StatusCode == System.Net.HttpStatusCode.Created 
                    || containerResponse.StatusCode == System.Net.HttpStatusCode.Created
            );

            return result;
        }

        public async Task<ItemResponse<T>> UpsertSingleItem<T>(T item) where T : class//FIXME:: return a custom interface type
        {
            string databaseName = BuildDatabaseName(typeof(T));

            return await _client.GetContainer(databaseName, databaseName).UpsertItemAsync(item);
        }

        private string BuildDatabaseName(Type modelType)
        {
            return string.Format(_databaseNameFormatString, modelType.Name);
        }
    }
}
