using DataRepositories.CrudResponses;
using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace DataRepositories
{
    public interface IDataRepository
    {
        Task<SimpleCrudResponse> CreateDatabaseIfNotExists(string databaseName);
        Task<SimpleCrudResponse> DeleteDatabaseIfExists(string databaseName);

        Task<SimpleCrudResponse> CreateCollectionIfNotExists(string databaseName, Type modelType, string partitionKeyPropertyName);
        Task<SimpleCrudResponse> DeleteCollectionIfExists(string databaseName, Type modelType);

        //FIXME:: return a custom interface type
        Task<SingleItemCrudResponse<T>> CreateSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new();
        Task<SingleItemCrudResponse<T>> ReadSingleItem<T, K>(string databaseName, string itemId, K partitionKeyValue) where T : class, new();
        Task<SingleItemCrudResponse<T>> ReadSingleItem<T>(string databaseName, Expression<Func<T, bool>> predicate) where T : class, new();
        Task<ItemResponse<T>> UpdateSingleItem<T, K>(string databaseName, string id, T item, K partitionKeyValue) where T : class, new();
        Task<SimpleCrudResponse> DeleteSingleItem<T, K>(string databaseName, string id, K partitionKeyValue) where T : class, new();
        Task<ItemResponse<T>> UpsertSingleItem<T, K>(string databaseName, T item, K partitionKeyValue) where T : class, new();
    }
}
