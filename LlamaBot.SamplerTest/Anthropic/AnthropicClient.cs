using LlamaBot.SamplerTest.Anthropic.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace LlamaBot.SamplerTest.Anthropic
{
    public class AnthropicClient : IRemoteApi
    {
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";

        private const string ApiVersion = "2023-06-01";

        private readonly string _apiKey;

        private readonly HttpClient _httpClient;

        public AnthropicClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<AnthropicResponse> SendMessageAsync(
                    string systemPrompt,
            List<AnthropicMessage> messages,
            List<JObject>? tools = null)
        {
            AnthropicRequest request = new()
            {
                System = systemPrompt,
                Messages = messages,
                Tools = tools
            };

            string requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(ApiUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Anthropic API error: {response.StatusCode}\n{responseBody}");
            }

            return JsonConvert.DeserializeObject<AnthropicResponse>(responseBody)
                ?? throw new InvalidOperationException("Failed to deserialize Anthropic response");
        }
    }
}