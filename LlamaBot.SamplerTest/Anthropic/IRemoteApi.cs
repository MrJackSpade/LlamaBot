using LlamaBot.SamplerTest.Anthropic.Models;
using Newtonsoft.Json.Linq;

namespace LlamaBot.SamplerTest.Anthropic
{
    public interface IRemoteApi : IDisposable
    {
        Task<AnthropicResponse> SendMessageAsync(
            string systemPrompt,
            List<AnthropicMessage> messages,
            List<JObject>? tools = null);
    }
}