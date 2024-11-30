using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config.Switchers
{
    internal class ConfSwitcherBTools
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("output_index")]
        public int OutputIndex { get; set; }
    }
}
