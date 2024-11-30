using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    /// <summary>
    /// A source, living at a site, that can be selected to simulcast.
    /// </summary>
    internal class NetworkSource
    {
        public NetworkSource(string name, int port, NetworkSourceType type)
        {
            this.name = name;
            this.port = port;
            this.type = type;
        }

        private string name;
        private int port;
        private NetworkSourceType type;

        /// <summary>
        /// Display name of the source.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Opaque port provided to the switcher.
        /// </summary>
        public int Port => port;

        /// <summary>
        /// The type of source.
        /// </summary>
        public NetworkSourceType Type => type;
    }
}
