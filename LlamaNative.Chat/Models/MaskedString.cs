using LlamaNative.Tokens.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Models
{
    public class MaskedString(string value, TokenMask mask)
    {
        public string Value { get; private set; } = value ?? throw new ArgumentNullException(nameof(value));

        public TokenMask Mask { get; private set; } = mask;
    }
}
