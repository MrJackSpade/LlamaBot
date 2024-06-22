using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Models
{
    public class ChatMessage
    {
        public string? ExternalId { get; set; }  
        public string? Content { get; set; }
        public required string User { get; set; }
    }
}
