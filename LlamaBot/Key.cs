using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;
using System.Text;

namespace LlamaBot
{
    internal partial class Program
    {
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
    }
}