using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlamaNative.Sampling.Models
{
    public class SamplerSetting
    {
        [JsonPropertyName("settings")]
        public JsonObject Settings { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
