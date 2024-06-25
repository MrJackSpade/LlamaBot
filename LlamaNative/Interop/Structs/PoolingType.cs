using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Interop.Structs
{
    public enum PoolingType
    {
        Unspecified = -1,
        None = 0,
        Mean = 1,
        Cls = 2
    }
}
