using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Controller.Exceptions
{
    /// <summary>
    /// Exception raised when there was an error connecting codecs
    /// </summary>
    class ControlCodecConnectException : Exception
    {
        public ControlCodecConnectException(NetworkSite sourceSite, Exception innerException) : base($"Failed to connect codecs.", innerException)
        {
            this.sourceSite = sourceSite;
        }

        private readonly NetworkSite sourceSite;

        public NetworkSite SourceSite => sourceSite;
    }
}
