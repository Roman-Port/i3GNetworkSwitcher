using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config
{
    /// <summary>
    /// Optional automation for a site for issuing commands.
    /// </summary>
    internal class ConfAutomation
    {
        /// <summary>
        /// IP address to send commands to.
        /// </summary>
        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        /// <summary>
        /// Port to send UDP commands to.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary>
        /// Text to send to command automation to join network.
        /// </summary>
        [JsonProperty("cmd_join")]
        public string CommandJoinNetwork { get; set; }

        /// <summary>
        /// Text to send to command automation to leave network.
        /// </summary>
        [JsonProperty("cmd_exit")]
        public string CommandExitNetwork { get; set; }
    }
}
