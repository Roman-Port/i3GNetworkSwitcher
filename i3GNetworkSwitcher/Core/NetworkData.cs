using i3GNetworkSwitcher.Core.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    internal class NetworkData
    {
        public NetworkData(NetworkSite[] sites, ConfRoot config)
        {
            if (sites == null || sites.Length == 0)
                throw new Exception("At least one site must be specified.");
            if (config.EventsFilename == null)
                throw new Exception("Config events filename cannot be blank.");
            this.sites = sites;
            this.config = config;
        }

        private readonly NetworkSite[] sites;
        private readonly ConfRoot config;

        public NetworkSite[] Sites => sites;
        public int ListenPort => config.ListenPort;
        public ConfEmail Email => config.Email;
        public string EventsFilename => config.EventsFilename;
    }
}
