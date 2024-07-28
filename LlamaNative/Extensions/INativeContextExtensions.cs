using Llama.Core;
using LlamaNative.Apis;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Logit.Collections;
using LlamaNative.Models;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Extensions
{
    public static class INativeContextExtensions
    {
        public static void Ensure(this INativeContext context)
        {
            //if (context.Buffer[0].Id != Token.Bos)
            //{
            //    throw new Exception("First buffer token is not BOS");
            //}
        }

        public static float[] GetEmbeddings(this INativeContext handler) => handler.Handle.GetEmbeddings();

        public static Span<float> GetLogits(this INativeContext handler)
        {
            int n_vocab = handler.VocabCount();

            Span<float> logits = NativeApi.GetLogits(handler.Handle, n_vocab);

            return logits;
        }

        public static Token GetToken(this INativeContext handler, int id) => new(id, NativeApi.TokenToPiece(handler.ModelHandle, id));

        public static Token Predict(this INativeContext handler, LogitRuleCollection logitRules)
        {
            handler.Evaluate();

            return handler.SelectToken(logitRules);
        }

        public static IEnumerable<Token> RemoveString(this INativeContext handler, Token token, string s_end)
        {
            string s_val = token.Value ?? string.Empty;

            //If this token contains the end string
            if (s_val.Contains(s_end))
            {
                //check where in the token the end is
                int e_index = s_val.IndexOf(s_end);

                //if theres text before the end string
                if (e_index > 0)
                {
                    //clip the next before the end
                    string clipped_value = s_val[..e_index];

                    //tokenize it
                    TokenCollection clipped_tokens = handler.Tokenize(clipped_value);

                    //then add it to the response.
                    foreach (var clipped_token in clipped_tokens)
                    {
                        yield return clipped_token;
                    }
                }
            }
        }

        public static Token SelectToken(this INativeContext handler)
        {
            return handler.SelectToken(null, out _);
        }

        public static Token SelectToken(this INativeContext handler, LogitRuleCollection logitBias)
        {
            return handler.SelectToken(logitBias, out _);
        }

        public static Token SelectToken(this INativeContext handler, LogitRuleCollection logitBias, out SampleContext context)
        {
            return handler.SelectToken(logitBias, out context);
        }

        public static Token SelectToken(this INativeContext handler, out SampleContext context)
        {
            return handler.SelectToken(null, out context);
        }

        public static void SetBuffer(this INativeContext context, TokenCollection Tokens)
        {
            context.Clear(false);

            context.Write(Tokens);
        }

        public static void SetBuffer(this INativeContext context, IEnumerable<Token> tokens)
        {
            Token[] toSet = tokens.ToArray();

            if (toSet.Length > context.Size)
            {
                throw new InvalidOperationException("Generated context state is larger than context size");
            }

            context.Clear(false);

            context.Write(toSet);

            context.Ensure();
        }

        public static TokenCollection Tokenize(this INativeContext context, string value, bool addBos = false)
        {
            TokenCollection tokens = new();

            foreach (int id in NativeApi.Tokenize(context.ModelHandle, value, addBos))
            {
                tokens.Append(context.GetToken(id));
            }

            return tokens;
        }

        public static TokenCollection Tokenize(this INativeContext context, IEnumerable<int> value)
        {
            TokenCollection tokens = new();

            foreach (int id in value)
            {
                tokens.Append(context.GetToken(id));
            }

            return tokens;
        }

        public static int VocabCount(this INativeContext handler) => NativeApi.NVocab(handler.ModelHandle);

        public static void Write(this INativeContext handler, params string[] inputText) => ProcessInputText(handler, inputText);

        public static void Write(this INativeContext context, IEnumerable<Token> tokens)
        {
            TokenCollection toWrite = new TokenCollection(tokens).Trim();

            foreach (Token token in toWrite)
            {
                context.Write(token);
            }
        }

        private static void ProcessInputText(this INativeContext handler, params string[] inputTexts)
        {
            foreach (string inputText in inputTexts)
            {
                Console.Write(inputText);

                if (inputText.Length > 1)
                {
                    TokenCollection line_inp = handler.Tokenize(inputText);

                    handler.Write(line_inp);
                }
            }
        }
    }
}