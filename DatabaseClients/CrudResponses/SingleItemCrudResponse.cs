using System.ComponentModel;

namespace DataRepositories.CrudResponses
{
    public class SingleItemCrudResponse<T> : SimpleCrudResponse where T : class, new()
    {
        public T? Item { get; set; }
    }
}
