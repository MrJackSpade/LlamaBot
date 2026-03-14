using LlamaBot.SamplerTest.Anthropic;
using LlamaBot.SamplerTest.Anthropic.Models;
using LlamaBot.SamplerTest.Errors;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace LlamaBot.SamplerTest.Orchestration
{
    public class ConversationOrchestrator
    {
        private readonly string _botName;

        private readonly IChatContext _chatContext;

        private readonly object _defaultSamplerSettings;

        private readonly int _messageCount;

        private readonly string _modelInfo;

        private readonly IRemoteApi _remoteApi;

        public ConversationOrchestrator(
            IRemoteApi remoteApi,
            IChatContext chatContext,
            int messageCount,
            string modelInfo,
            string botName,
            object defaultSamplerSettings)
        {
            _remoteApi = remoteApi;
            _chatContext = chatContext;
            _messageCount = messageCount;
            _modelInfo = modelInfo;
            _botName = botName;
            _defaultSamplerSettings = defaultSamplerSettings;
        }

        public async Task<ConversationResult> RunConversationAsync()
        {
            ConversationResult result = new()
            {
                ModelInfo = _modelInfo,
                StartTime = DateTime.UtcNow
            };

            try
            {
                Console.WriteLine("=== Phase 1: Character Creation ===");
                Console.WriteLine($"Asking Claude to create a roleplay character named '{_botName}'...");

                // Phase 1: Get character from Claude
                AnthropicResponse characterResponse = await _remoteApi.SendMessageAsync(
                    ClaudePrompts.GetCharacterCreationPrompt(_botName),
                    [AnthropicMessage.User($"Please create a roleplay character named {_botName} for testing.")]);

                string fullResponse = characterResponse.GetTextContent();
                Console.WriteLine($"Claude's response:\n{fullResponse}\n");

                // Extract system prompt from <system_prompt> tags
                Match systemPromptMatch = Regex.Match(fullResponse, @"<system_prompt>\s*(.*?)\s*</system_prompt>", RegexOptions.Singleline);
                if (!systemPromptMatch.Success)
                {
                    throw new InvalidOperationException("Claude did not return a system prompt in the expected format");
                }

                result.CharacterSystemPrompt = systemPromptMatch.Groups[1].Value.Trim();
                Console.WriteLine($"Extracted system prompt:\n{result.CharacterSystemPrompt}\n");

                // Extract opening message from <opening_message> tags
                Match openingMessageMatch = Regex.Match(fullResponse, @"<opening_message>\s*(.*?)\s*</opening_message>", RegexOptions.Singleline);
                if (openingMessageMatch.Success)
                {
                    result.OpeningMessage = openingMessageMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // Fallback: try to get text after </system_prompt> if no tag
                    int endTagIndex = fullResponse.IndexOf("</system_prompt>");
                    if (endTagIndex >= 0)
                    {
                        result.OpeningMessage = fullResponse[(endTagIndex + "</system_prompt>".Length)..].Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(result.OpeningMessage))
                {
                    result.OpeningMessage = "Hello! Nice to meet you.";
                }

                Console.WriteLine($"Opening message: {result.OpeningMessage}\n");

                // Set up the local model with the character system prompt
                // Append format instructions that we don't want Claude to generate
                const string formatInstructions = "Write actions and narration as plain prose. Enclose all spoken dialog in quotation marks.";
                string fullSystemPrompt = result.CharacterSystemPrompt + "\n\n" + formatInstructions;

                ChatMessage systemMessage = new(TokenMask.System, "System", fullSystemPrompt);
                _chatContext.SendMessage(systemMessage);

                Console.WriteLine($"=== Phase 2: Testing Loop ({_messageCount} exchanges) ===\n");

                // Initialize Claude's conversation history for the testing phase
                List<AnthropicMessage> claudeMessages = new();
                List<JObject> tools = new()
                { ErrorToolDefinition.GetToolDefinition() };

                string currentClaudeMessage = result.OpeningMessage;

                for (int i = 1; i <= _messageCount; i++)
                {
                    Console.WriteLine($"--- Exchange {i}/{_messageCount} ---");

                    ConversationExchange exchange = new()
                    {
                        ExchangeNumber = i,
                        Timestamp = DateTime.UtcNow,
                        ClaudeMessage = currentClaudeMessage
                    };

                    Console.WriteLine($"Claude (User): {currentClaudeMessage}");

                    // Send Claude's message to local model
                    ChatMessage userMessage = new(TokenMask.User, "Claude", currentClaudeMessage);
                    _chatContext.SendMessage(userMessage);

                    // Read model response
                    ReadResponseSettings responseSettings = new()
                    {
                        RespondingUser = _botName,
                        SamplerSettings = _defaultSamplerSettings
                    };

                    string modelResponse = this.ReadModelResponse(responseSettings);
                    exchange.ModelResponse = modelResponse;

                    Console.WriteLine($"Model ({_botName}): {modelResponse}");

                    // Send model's response to Claude for evaluation
                    claudeMessages.Add(AnthropicMessage.User($"The character responded:\n\n{modelResponse}\n\nFirst use the categorize_errors tool to document any issues, then provide your next message to continue the roleplay."));

                    AnthropicResponse claudeResponse = await _remoteApi.SendMessageAsync(
                        ClaudePrompts.TestingLoopPrompt,
                        claudeMessages,
                        tools);

                    // Process tool call if present
                    AnthropicResponseContent? toolUse = claudeResponse.GetToolUse();
                    if (toolUse != null && toolUse.Name == ErrorToolDefinition.ToolName)
                    {
                        exchange.Errors = this.ParseErrors(toolUse.Input);
                        Console.WriteLine($"Errors found: {exchange.Errors.Count}");

                        foreach (CategorizedError error in exchange.Errors)
                        {
                            Console.WriteLine($"  - [{error.Category}] {error.Description}");
                        }

                        // Add assistant message with tool use to history
                        claudeMessages.Add(AnthropicMessage.AssistantWithToolUse(
                            claudeResponse.GetTextContent(),
                            toolUse.Id!,
                            toolUse.Name,
                            toolUse.Input!));

                        // Add tool result
                        claudeMessages.Add(AnthropicMessage.ToolResult(toolUse.Id!, "Errors recorded. Continue with your next message."));

                        // If Claude didn't include text with the tool use, get the next message
                        string nextMessage = claudeResponse.GetTextContent();
                        if (string.IsNullOrWhiteSpace(nextMessage))
                        {
                            AnthropicResponse continuationResponse = await _remoteApi.SendMessageAsync(
                                ClaudePrompts.TestingLoopPrompt,
                                claudeMessages,
                                tools);
                            nextMessage = continuationResponse.GetTextContent();
                            claudeMessages.Add(AnthropicMessage.Assistant(nextMessage));
                        }

                        currentClaudeMessage = nextMessage;
                    }
                    else
                    {
                        // No tool use, just use the text response
                        currentClaudeMessage = claudeResponse.GetTextContent();
                        claudeMessages.Add(AnthropicMessage.Assistant(currentClaudeMessage));
                        Console.WriteLine("Warning: Claude did not use the error categorization tool");
                    }

                    result.Exchanges.Add(exchange);
                    Console.WriteLine();
                }

                result.Completed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! Error during conversation: {ex.Message} !!!\n");
                Console.WriteLine(ex.StackTrace);
                result.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                // Always finalize results even if we failed partway through
                result.EndTime = DateTime.UtcNow;
                result.TotalExchanges = result.Exchanges.Count;

                // Calculate error summary
                foreach (ConversationExchange exchange in result.Exchanges)
                {
                    foreach (CategorizedError error in exchange.Errors)
                    {
                        string category = error.Category.ToString();
                        if (!result.ErrorSummary.ContainsKey(category))
                        {
                            result.ErrorSummary[category] = 0;
                        }
                        result.ErrorSummary[category]++;
                    }
                }

                string status = result.Completed ? "Test Complete" : "Test Interrupted";
                Console.WriteLine($"=== {status} ===");
                Console.WriteLine($"Total exchanges: {result.TotalExchanges}");
                Console.WriteLine($"Duration: {result.EndTime - result.StartTime}");
                if (result.ErrorSummary.Count > 0)
                {
                    Console.WriteLine("Error summary:");
                    foreach (KeyValuePair<string, int> kvp in result.ErrorSummary.OrderByDescending(k => k.Value))
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }
            }

            return result;
        }

        private List<CategorizedError> ParseErrors(JObject? input)
        {
            if (input == null)
            {
                return [];
            }

            List<CategorizedError> errors = new();

            if (input.TryGetValue("errors", out JToken? errorsToken) && errorsToken is JArray errorsArray)
            {
                foreach (JToken errorObj in errorsArray)
                {
                    CategorizedError error = new();

                    if (errorObj["category"] is JValue categoryValue)
                    {
                        string categoryStr = categoryValue.ToString();
                        if (Enum.TryParse<ErrorCategory>(categoryStr, out ErrorCategory category))
                        {
                            error.Category = category;
                        }
                    }

                    if (errorObj["description"] is JValue descValue)
                    {
                        error.Description = descValue.ToString();
                    }

                    if (errorObj["quote"] is JValue quoteValue)
                    {
                        error.Quote = quoteValue.ToString();
                    }

                    errors.Add(error);
                }
            }

            return errors;
        }

        private string ReadModelResponse(ReadResponseSettings responseSettings)
        {
            List<ChatMessage> responses = _chatContext.ReadResponse(responseSettings, CancellationToken.None);

            if (responses.Count == 0)
            {
                return "[No response generated]";
            }

            // Combine all response messages
            string response = string.Join("\n", responses.Select(r => r.Content?.Trim()).Where(c => !string.IsNullOrEmpty(c)));
            return response;
        }
    }
}