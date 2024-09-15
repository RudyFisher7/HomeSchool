using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRepositories.Attributes
{
    public enum SqlConstraintEnum
    {
        MIN = 0,
        NONE = MIN,
        PRIMARY_KEY,
        NOT_NULL,
        UNIQUE,
        FOREIGN_KEY,
        CHECK,
        DEFAULT,
        INDEX,
        SIZE,
    }
}
