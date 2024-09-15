using DataRepositories.CrudResponses;
using Microsoft.Azure.Cosmos;

namespace DataRepositories
{
    public interface IDataRepository
    {
        Task<SimpleCrudResponse> CreateDatabaseIfNotExists(string databaseName);
        Task<SimpleCrudResponse> DeleteDatabaseIfExists(string databaseName);

        Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName);
        Task<SimpleCrudResponse> DeleteCollectionIfExists(string databaseName, Type modelType);

        //FIXME:: return a custom interface type
        Task<DataCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class;
        Task<T?> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class;
        Task<T?> ReadSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class;
        Task<ItemResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class;
        Task<ItemResponse<T>> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class;
        Task<ItemResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class;
    }
}
