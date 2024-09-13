using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace DatabaseClients
{
    public interface IDatabaseClient
    {
        Task<bool> CreateDatabaseIfNotExists(string databaseName);
        Task<bool> DeleteDatabase(string databaseName);

        Task<bool> CreateCollectionIfNotExists(string databaseName, Type modelType);
        Task<bool> DeleteCollection(string databaseName, Type modelType);

        //FIXME:: return a custom interface type
        Task<ItemResponse<T>> InsertSingleItem<T>(string databaseName, T item) where T : class;
        Task<T?> GetSingleItem<T>(string databaseName, string itemId) where T : class;
        Task<T?> GetSingleItem<T>(string databaseName, Func<T, bool> predicate) where T : class;
        Task<ItemResponse<T>> UpdateSingleItem<T>(string databaseName, string id, T item) where T : class;
        Task<ItemResponse<T>> DeleteSingleItem<T>(string databaseName, string id) where T : class;
        Task<ItemResponse<T>> UpsertSingleItem<T>(string databaseName, T item) where T : class;
    }
}
