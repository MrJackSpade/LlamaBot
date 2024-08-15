using LlamaNative;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Models;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Samplers.Temperature;
using LlamaNative.Tokens.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LlamaBot
{
    internal partial class Program
    {
        private const string KEY_FOLDER = "Keys";

        private const string MODEL_NAME = "/home/mrjackspade/Meta-Llama-3.1-8B-Q4_K_M.gguf";

        private const int RUNS = 5;

        private const decimal STEP = 0.1m;

        private const decimal MIN_TEMP = 0;

        private const decimal MAX_TEMP = 1.2m;

        private const int CONTEXT = 4096;

	private const int NGPU = 0;

	private const bool SWAP = false;

        public static async Task Main()
        {
            try
            {
                LogitRuleCollection logitRuleCollection = [];

                for(int i = 128000; i < 128003; i++)
                {
                    logitRuleCollection.Add(new LogitBias(i, float.NegativeInfinity, LogitRuleLifetime.Context, LogitBiasType.Multiplicative));
                }

                TemperatureSamplerSettings temperatureSamplerSettings = new()
                {
                    PreserveWords = false
                };

                INativeContext context = LlamaClient.LoadContext(new ModelSettings()
                {
                    ModelPath = MODEL_NAME,
                    GpuLayerCount = NGPU,
                    UseMemoryLock = !SWAP,
                    UseMemoryMap = SWAP
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

                            Token token = context.SelectToken(logitRuleCollection, out SampleContext sampleContext);

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
            } catch (Exception ex)
            {
                Console.Write(ex);
                Console.ReadKey();
            }
        }
    }
}
