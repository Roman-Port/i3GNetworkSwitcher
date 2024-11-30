using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config
{
    internal class ConfSource
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("data")]
        public int Data { get; set; }

        [JsonProperty("type")] // NORMAL, EXTERNAL, SPECIAL
        public string Type { get; set; }
    }
}
