using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaBot
{
    public class MetaData
    {
        public Dictionary<ulong, DateTime> ClearValues {  get; set; }  = new Dictionary<ulong, DateTime>();
    }
}