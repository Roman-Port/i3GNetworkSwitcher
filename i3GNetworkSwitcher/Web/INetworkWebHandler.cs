using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web
{
    interface INetworkWebHandler
    {
        Task HandleRequest(HttpListenerContext e);
    }
}
