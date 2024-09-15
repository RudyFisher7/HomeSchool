using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRepositories.CrudResponses
{
    public interface ICrudResponse
    {
        bool Success { get; set; }
        string Message { get; set; }
    }
}
