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
        public static Span<float> GetLogits(this INativeContext handler)
        {
            int n_vocab = handler.VocabCount();

            Span<float> logits = NativeApi.GetLogits(handler.Handle, n_vocab);

            return logits;
        }

        public static Token GetToken(this INativeContext handler, TokenMask mask, int id)
        {
            return new(id, NativeApi.TokenToPiece(handler.ModelHandle, id), mask);
        }

        public static IEnumerable<Token> RemoveString(this INativeContext handler, Token token, string s_end)
        {
            string s_val = token.Value ?? string.Empty;

            //If this token contains the end string
            if (s_val.Contains(s_end))
            {
                //check where in the token the end is
                int e_index = s_val.IndexOf(s_end);

                //if there's text before the end string
                if (e_index > 0)
                {
                    //clip the next before the end
                    string clipped_value = s_val[..e_index];

                    //tokenize it
                    TokenCollection clipped_tokens = handler.Tokenize(token.Mask, clipped_value);

                    //then add it to the response.
                    foreach (Token clipped_token in clipped_tokens)
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
    
        public static TokenCollection Tokenize(this INativeContext context, TokenMask tokenMask, string value, bool addBos = false)
        {
            TokenCollection tokens = new();

            foreach (int id in NativeApi.Tokenize(context.ModelHandle, value, addBos))
            {
                tokens.Append(context.GetToken(tokenMask, id));
            }

            return tokens;
        }

        public static int VocabCount(this INativeContext handler)
        {
            return NativeApi.NVocab(handler.ModelHandle);
        }

        public static void Write(this INativeContext handler, TokenMask mask, params string[] inputText)
        {
            ProcessInputText(handler, mask, inputText);
        }

        public static void Write(this INativeContext context, IEnumerable<Token> tokens)
        {
            TokenCollection toWrite = new TokenCollection(tokens).Trim();

            foreach (Token token in toWrite)
            {
                context.Write(token);
            }
        }

        public static void WriteTemplate(this INativeContext handler, params string[] inputText)
        {
            ProcessInputText(handler, TokenMask.Template, inputText);
        }

        private static void ProcessInputText(this INativeContext handler, TokenMask mask, params string[] inputTexts)
        {
            foreach (string inputText in inputTexts)
            {
                Console.Write(inputText);

                TokenCollection line_inp = handler.Tokenize(mask, inputText);

                handler.Write(line_inp);
            }
        }
    }
}