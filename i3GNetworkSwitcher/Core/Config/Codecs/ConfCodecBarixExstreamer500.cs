using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config.Codecs
{
    internal class ConfCodecBarixExstreamer500
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("public_ip")]
        public string PublicIp { get; set; }

        [JsonProperty("public_port")]
        public int PublicPort { get; set; }
    }
}
