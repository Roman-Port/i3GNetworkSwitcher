using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    /// <summary>
    /// A studio location.
    /// </summary>
    internal class NetworkSite
    {
        public NetworkSite(string label, NetworkSource[] sources, INetworkSwitcher switcher, INetworkCodec codec)
        {
            //Validate
            if (label == null)
                throw new ArgumentNullException($"{nameof(label)} must not be null.");
            if (sources == null || sources.Length == 0)
                throw new ArgumentNullException($"{nameof(sources)} must not be null or empty.");
            if (switcher == null && sources.Length != 1)
                throw new ArgumentNullException($"{nameof(switcher)} cannot be null if more than one source is provided.");
            if (codec == null)
                throw new ArgumentNullException($"{nameof(codec)} must not be null.");

            //Set
            this.label = label;
            this.sources = sources;
            this.switcher = switcher;
            this.codec = codec;
        }

        private string label; // Must not be null
        private NetworkSource[] sources; // Must not be null, must contain at least 1 item.
        private INetworkSwitcher switcher; // MAY be null if only one source is provided.
        private INetworkCodec codec; // Must not be null

        /// <summary>
        /// Display label.
        /// </summary>
        public string Label => label;

        /// <summary>
        /// Sources available to send out.
        /// </summary>
        public NetworkSource[] Sources => sources;

        /// <summary>
        /// Switcher used to select stations to send out. MAY be null if one source is available.
        /// </summary>
        public INetworkSwitcher Switcher => switcher;

        /// <summary>
        /// Codec used to connect to other sites. Must not be null.
        /// </summary>
        public INetworkCodec Codec => codec;
    }
}
