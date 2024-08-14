using LlamaNative;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Samplers.Temperature;
using LlamaNative.Tokens.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace LlamaBot
{
    internal class Program
    {
        private const string KEY_FOLDER = "Keys";

        private const string MODEL_NAME = "D:\\Chie\\Models\\Meta-Llama-3.1-8B.gguf";

        private const int RUNS = 5;

        private const decimal STEP = 0.1m;

        private const decimal MIN_TEMP = 0;

        private const decimal MAX_TEMP = 1.2m;

        private const int CONTEXT = 4096;

        public static async Task Main()
        {
            TemperatureSamplerSettings temperatureSamplerSettings = new()
            {
                PreserveWords = false
            };

            INativeContext context = LlamaClient.LoadContext(new ModelSettings()
            {
                ModelPath = MODEL_NAME,
                GpuLayerCount = 33
            },
            new ContextSettings()
            {
                ContextSize = CONTEXT
            },
            new TemperatureSampler(temperatureSamplerSettings),
            []);

            int keyIndex = 1;

            if (!Directory.Exists(KEY_FOLDER))
            {
                Directory.CreateDirectory(KEY_FOLDER);
            }
            else if (Directory.EnumerateFiles(KEY_FOLDER).Any())
            {
                keyIndex = Directory.EnumerateFiles(KEY_FOLDER).Select(s => int.Parse(Path.GetFileNameWithoutExtension(s))).Max() + 1;
            }

            ConcurrentQueue<Key> writeQueue = new();

            ManualResetEvent writeComplete = new(true);
            ManualResetEvent readComplete = new(false);

            Thread writeThread = new(() =>
            {
                do
                {
                    readComplete.WaitOne();

                    if (writeQueue.TryDequeue(out var key))
                    {
                        string toWrite = key.Serialize();

                        string keysPath = Path.Combine(KEY_FOLDER, $"{key.Id}.json");

                        File.WriteAllText(keysPath, toWrite);
                    }

                    writeComplete.Set();
                } while (true);
            });

            writeThread.Start();

            int expected_steps = (int)(CONTEXT * ((MAX_TEMP - MIN_TEMP) / STEP + 1)) * RUNS;

            DateTime startTime = DateTime.Now;

            for (int i = 0; i < RUNS; i++)
            {
                for (decimal temp = MIN_TEMP; temp <= MAX_TEMP; temp += STEP)
                {
                    temperatureSamplerSettings.Temperature = (float)temp;

                    context.Clear(true);

                    context.Write(new MaskedString("Once upon a time", TokenMask.Undefined));

                    context.Evaluate();

                    Console.Write($"----- RUN: {i} -- TEMP: {temp:0.00} ");

                    int index = 0;

                    while (context.AvailableBuffer > 0)
                    {

                        if (context.AvailableBuffer % 10 == 0)
                        {
                            DateTime currentTime = DateTime.Now;
                            double ms = (currentTime - startTime).TotalMilliseconds;
                            int completed = keyIndex;
                            int remaining = expected_steps - completed;
                            float ms_per_step = (float)(ms / completed);
                            ulong ms_remaining = (ulong)(remaining * ms_per_step);
                            ulong s_remaining = ms_remaining / 1000;
                            DateTime endTime = DateTime.Now.AddSeconds(s_remaining);
                            Debug.WriteLine($"ETA: {endTime:yyyy-MM-dd HH:mm:ss}");
                        }

                        Token token = context.SelectToken(null, out SampleContext sampleContext);

                        writeComplete.WaitOne();

                        context.Write(token);

                        context.Evaluate();

                        Console.Write(token.Value);

                        Key key = new()
                        {
                            Id = keyIndex++,
                            Index = index++,
                            Run = i,
                            Sampler = "T",
                            SelectedToken = token,
                            Temperature = temp,
                        };

                        foreach (TokenData t in sampleContext.OriginalCandidates)
                        {
                            if (t.P > 0.01f)
                            {
                                key.Values.Add(new KeyTokenData(t));
                            }
                        }

                        writeQueue.Enqueue(key);

                        readComplete.Set();
                    }
                }
            }

            writeComplete.WaitOne();
        }

        private class Key
        {
            public int Id { get; set; }

            public int Index { get; set; }

            public int Run { get; set; }

            public string Sampler { get; set; }

            public Token SelectedToken { get; set; }

            public decimal Temperature { get; set; }

            public List<KeyTokenData> Values { get; set; } = [];

            public static Key Deserialize(string data)
            {
                var parts = data.Split('\0');
                var key = new Key
                {
                    // Deserialize the basic properties
                    Id = int.Parse(parts[0]),
                    Index = int.Parse(parts[1]),
                    Run = int.Parse(parts[2]),
                    Sampler = parts[3]
                };

                var selectedToken = new Token(int.Parse(parts[4]), parts[5], TokenMask.Undefined);
                key.SelectedToken = selectedToken;

                key.Temperature = decimal.Parse(parts[6]);

                // Deserialize the List<KeyTokenData>
                for (int i = 7; i < parts.Length; i += 3)
                {
                    var keyTokenData = new KeyTokenData(new TokenData()
                    {
                        Id = int.Parse(parts[i]),
                        Logit = float.Parse(parts[i + 1]),
                        P = float.Parse(parts[i + 2])
                    });

                    key.Values.Add(keyTokenData);
                }

                return key;
            }

            public string Serialize()
            {
                var sb = new StringBuilder();
                Key key = this;

                // Serialize the basic properties
                sb.Append(key.Id).Append('\0');
                sb.Append(key.Index).Append('\0');
                sb.Append(key.Run).Append('\0');
                sb.Append(key.Sampler).Append('\0');
                sb.Append(key.SelectedToken.Id).Append('\0');
                sb.Append(key.SelectedToken.Value).Append('\0');
                sb.Append(key.Temperature);

                // Serialize the List<KeyTokenData>
                foreach (var value in key.Values)
                {
                    sb.Append('\0').Append(value.Id);
                    sb.Append('\0').Append(value.Logit);
                    sb.Append('\0').Append(value.P);
                }

                return sb.ToString();
            }
        }

        private class KeyTokenData(TokenData t)
        {
            /// <summary>
            /// token id
            /// </summary>
            public int Id { get; set; } = t.Id;

            /// <summary>
            /// log-odds of the token
            /// </summary>
            public float Logit { get; set; } = t.Logit;

            /// <summary>
            /// probability of the token
            /// </summary>
            public float P { get; set; } = t.P;
        }
    }
}