namespace LlamaBot.Shared.Exceptions
{
    public class MissingChannelException : Exception
    {
        public MissingChannelException(ulong channelId) : base($"Channel {channelId} does not exist")
        {
            ChannelId = channelId;
        }

        public ulong ChannelId { get; private set; }
    }
}