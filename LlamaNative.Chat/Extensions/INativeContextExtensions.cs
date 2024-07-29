using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LlamaNative.Extensions;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Extensions
{
    public static class INativeContextExtensions
    {
        public static void Write(this INativeContext context, MaskedString maskedString)
        {
            context.Write(maskedString.Mask, maskedString.Value);
        }
    }
}