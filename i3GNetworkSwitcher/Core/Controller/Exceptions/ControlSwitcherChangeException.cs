using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Controller.Exceptions
{
    /// <summary>
    /// Exception raised when there was an error changing switchers
    /// </summary>
    class ControlSwitcherChangeException : Exception
    {
        public ControlSwitcherChangeException(NetworkSite sourceSite, Exception innerException) : base($"Failed to update site switcher: {innerException.Message}{innerException.StackTrace}", innerException)
        {
            this.sourceSite = sourceSite;
        }

        private readonly NetworkSite sourceSite;

        public NetworkSite SourceSite => sourceSite;
    }
}
