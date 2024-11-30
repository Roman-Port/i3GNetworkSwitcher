using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config
{
    internal class ConfRoot
    {
        [JsonProperty("sites")]
        public ConfSite[] Sites { get; set; }

        [JsonProperty("listen_port")]
        public int ListenPort { get; set; }

        [JsonProperty("events_filename")]
        public string EventsFilename { get; set; }

        [JsonProperty("email")]
        public ConfEmail Email { get; set; }
    }
}
