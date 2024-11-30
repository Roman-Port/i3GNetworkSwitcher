using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Notifier
{
    internal interface INetworkNotifier
    {
        void SendAlert(AlertLevel level, string caption, string message);
    }
}
