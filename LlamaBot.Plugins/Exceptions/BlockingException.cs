using System.Runtime.Serialization;

namespace LlamaBot.Plugins.Exceptions
{
    public abstract class BlockingException : Exception
    {
        protected BlockingException()
        {
        }

        protected BlockingException(string? message) : base(message)
        {
        }

        protected BlockingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected BlockingException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}