using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web
{
    class NetworkWebServer
    {
        public NetworkWebServer(int port)
        {
            server = new HttpListener();
            server.Prefixes.Add($"http://+:{port}/");
        }

        private readonly HttpListener server;
        private readonly Dictionary<string, INetworkWebHandler> handlers = new Dictionary<string, INetworkWebHandler>();

        public void AddHandler(string path, INetworkWebHandler handler)
        {
            handlers.Add(path, handler);
        }

        public void Start()
        {
            server.Start();
        }

        private async Task HandleRequestAsync(HttpListenerContext e)
        {
            try
            {
                //Attempt to resolve to a service
                if (handlers.TryGetValue(e.Request.Url.AbsolutePath, out INetworkWebHandler handler))
                {
                    //Allow service to handle it
                    await handler.HandleRequest(e);
                }
                else
                {
                    //Handler not found
                    e.Response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                //Try to send
                try
                {
                    e.Response.StatusCode = 500;
                }
                catch
                {

                }

                //Write error to log
                Console.WriteLine($"Unhandled web exception: {ex.Message}{ex.StackTrace}");
            }
            e.Response.Close();
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext e = await server.GetContextAsync();
                HandleRequestAsync(e);
            }
        }
    }
}
