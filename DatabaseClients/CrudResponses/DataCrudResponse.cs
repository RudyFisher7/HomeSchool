using System.ComponentModel;

namespace DataRepositories.CrudResponses
{
    public class DataCrudResponse<T> : SimpleCrudResponse where T : class
    {
        public T? Data { get; set; }
    }
}
