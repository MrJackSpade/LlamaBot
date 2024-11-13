using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaBot.Extensions
{
    internal static class IChannelExtensions
    {
        public static ulong GetChannelId(this IChannel channel)
        {
            if (channel is SocketDMChannel socketDMChannel)
            {
                return socketDMChannel.Users.ToArray()[1].Id;
            }

            return channel.Id;
        }
    }
}