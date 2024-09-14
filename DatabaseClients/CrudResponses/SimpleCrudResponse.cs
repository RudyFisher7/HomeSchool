using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseClients.CrudResponses
{
    public class SimpleCrudResponse : ICrudResponse
    {
        public bool Success { get; set; }
        public int DocumentsAffected { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
