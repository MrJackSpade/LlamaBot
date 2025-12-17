namespace LlamaBot.Shared.Models
{
    /// <summary>
    /// Platform-agnostic representation of a file attachment from a command.
    /// </summary>
    public class CommandAttachment
    {
        /// <summary>
        /// The original filename of the attachment.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// The file content as raw bytes.
        /// </summary>
        public required byte[] Data { get; init; }

        /// <summary>
        /// The MIME content type of the file (e.g., "text/plain").
        /// </summary>
        public string? ContentType { get; init; }
    }
}
