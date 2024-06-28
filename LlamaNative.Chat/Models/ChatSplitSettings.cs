using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Models
{
    public class ChatSplitSettings
    {
        public int MessageSplitId { get; set; } = 0;

        public int MessageMaxCharacters { get; set; } = 500;

        public int MessageMinTokens { get; set; } = 10;

        public bool DoubleNewlineSplit { get; set; } = true;
    }
}
