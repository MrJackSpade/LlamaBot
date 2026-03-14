using Newtonsoft.Json;

namespace LlamaBot.SamplerTest.Results
{
    public static class ResultsWriter
    {
        public static string GenerateFileName(string modelInfo)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string safeName = string.Join("_", modelInfo.Split(Path.GetInvalidFileNameChars()));
            if (safeName.Length > 50)
            {
                safeName = safeName[..50];
            }
            return $"sampler_test_{safeName}_{timestamp}.json";
        }

        public static async Task SaveConversationAsync(Orchestration.ConversationResult result, string outputPath)
        {
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            await File.WriteAllTextAsync(outputPath, json);

            Console.WriteLine($"Results saved to: {outputPath}");
        }
    }
}