using Newtonsoft.Json.Linq;

namespace LlamaBot.SamplerTest.Errors
{
    public static class ErrorToolDefinition
    {
        public const string ToolName = "categorize_errors";

        public static JObject GetToolDefinition()
        {
            return new JObject
            {
                ["name"] = ToolName,
                ["description"] = "Categorize any errors or issues found in the model's response. Call this after every response, even if there are no errors (pass an empty array).",
                ["input_schema"] = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["errors"] = new JObject
                        {
                            ["type"] = "array",
                            ["description"] = "List of errors found in the response. Pass an empty array if no errors.",
                            ["items"] = new JObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JObject
                                {
                                    ["category"] = new JObject
                                    {
                                        ["type"] = "string",
                                        ["enum"] = new JArray(
                                            "Typos",
                                            "GrammarIssues",
                                            "FormattingIssues",
                                            "LogicalInconsistencies",
                                            "CharacterBreaks",
                                            "RepetitionIssues",
                                            "CoherenceIssues",
                                            "ToneInconsistencies"
                                        ),
                                        ["description"] = "The category of the error"
                                    },
                                    ["description"] = new JObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "Detailed description of the error. Note if this is a NEW issue or part of a PATTERN developing over the conversation."
                                    },
                                    ["quote"] = new JObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "The problematic text from the response, if applicable"
                                    }
                                },
                                ["required"] = new JArray("category", "description")
                            }
                        }
                    },
                    ["required"] = new JArray("errors")
                }
            };
        }
    }
}