using Microsoft.Azure.Cosmos;

namespace DatabaseClients
{
    public interface IDatabaseClient
    {
        Task<bool> CreateDatabaseIfNotExists(Type modelType);
        Task<ItemResponse<T>> UpsertSingleItem<T>(T item) where T : class;
    }
}
