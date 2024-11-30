using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Notifier
{
    internal class ConsoleNotifier : INetworkNotifier
    {
        public void SendAlert(AlertLevel level, string caption, string message)
        {
            Console.WriteLine($"[{level}] {caption} - {message}");
        }
    }
}
