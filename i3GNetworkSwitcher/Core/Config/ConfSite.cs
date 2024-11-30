using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config
{
    internal class ConfSite
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sources")]
        public ConfSource[] Sources { get; set; } // at least one, or exactly one if no switcher is specified

        [JsonProperty("switcher_type")]
        public string SwitcherType { get; set; } // optional - leave null

        [JsonProperty("switcher_data")]
        public JObject SwitcherData { get; set; } // optional - leave null

        [JsonProperty("codec_type")]
        public string CodecType { get; set; }

        [JsonProperty("codec_data")]
        public JObject CodecData { get; set; }
    }
}
