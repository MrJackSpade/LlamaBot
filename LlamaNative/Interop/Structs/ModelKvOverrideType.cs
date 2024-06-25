using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Interop.Structs
{
    // Define the enum for the override types
    public enum ModelKvOverrideType
    {
        LlamaKvOverrideInt,

        LlamaKvOverrideFloat,

        LlamaKvOverrideBool
    }
}