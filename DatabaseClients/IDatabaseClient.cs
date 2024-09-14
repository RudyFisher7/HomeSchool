using Microsoft.Azure.Cosmos;

namespace DatabaseClients
{
    public interface IDatabaseClient
    {
        Task<bool> CreateDatabaseIfNotExists(string databaseName);
        Task<bool> DeleteDatabase(string databaseName);

        Task<bool> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName);
        Task<bool> DeleteCollection(string databaseName, Type modelType);

        //FIXME:: return a custom interface type
        Task<ItemResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class;
        Task<T?> GetSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class;
        Task<T?> GetSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class;
        Task<ItemResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class;
        Task<ItemResponse<T>> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class;
        Task<ItemResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class;
    }
}
