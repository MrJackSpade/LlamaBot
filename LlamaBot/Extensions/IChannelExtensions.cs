using Discord;
using Discord.WebSocket;

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