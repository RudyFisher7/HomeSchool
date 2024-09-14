using System.ComponentModel;

namespace DatabaseClients.CrudResponses
{
    public class DataCrudResponse<T> : SimpleCrudResponse where T : class
    {
        public T? Data { get; set; }
    }
}
