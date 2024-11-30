using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config.Switchers
{
    internal class ConfSwitcherLivewire
    {
        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("dst_index")]
        public int DstIndex { get; set; }
    }
}
