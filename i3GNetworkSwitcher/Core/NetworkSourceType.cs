using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    /// <summary>
    /// The type of network source. Typically only one of special types exist per site.
    /// </summary>
    internal enum NetworkSourceType
    {
        NORMAL, // A normal station
        EXTERNAL, // The external codec used to recieve from the network
        SPECIAL // An otherwise different station, hidden unless in expert mode
    }
}
