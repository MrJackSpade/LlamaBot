using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Models
{
    public class ReadResponseSettings
    {
        public ChatSplitSettings? ChatSplitSettings { get; set; }

        public bool ContinueLast { get; set; }

        public string RespondingUser { get; set; }
    }
}