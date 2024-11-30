using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web.Handlers
{
    class ResourceHandler : INetworkWebHandler
    {
        public ResourceHandler(string resource, string mimeType, TimeSpan cacheAge)
        {
            //Attempt to find resource to make sure it exists
            assembly = Assembly.GetCallingAssembly();
            if (assembly.GetManifestResourceInfo(resource) == null)
                throw new Exception($"Failed to find web resource: {resource}");

            //Set
            this.resource = resource;
            this.mimeType = mimeType;
            this.cacheAge = cacheAge;
        }

        private readonly Assembly assembly;
        private readonly string resource;
        private readonly string mimeType;
        private readonly TimeSpan cacheAge;

        public async Task HandleRequest(HttpListenerContext e)
        {
            //Open assembly
            using (Stream src = assembly.GetManifestResourceStream(resource))
            {
                //Set info
                e.Response.ContentType = mimeType;
                e.Response.ContentLength64 = src.Length;
                e.Response.StatusCode = 200;
                e.Response.Headers.Add("Cache-Control", "max-age=" + ((int)cacheAge.TotalSeconds).ToString());

                //Copy stream data
                using (Stream dst = e.Response.OutputStream)
                    await src.CopyToAsync(dst);
            }
        }
    }
}
