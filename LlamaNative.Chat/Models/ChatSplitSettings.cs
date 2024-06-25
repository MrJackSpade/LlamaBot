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

        public float MessageSplitMinP { get; set; } = 0.01f;

        public int MessageMax { get; set; } = 200;

        public int MessageMin { get; set; } = 10;
    }
}
