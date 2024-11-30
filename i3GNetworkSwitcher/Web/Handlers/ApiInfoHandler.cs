using i3GNetworkSwitcher.Core;
using i3GNetworkSwitcher.Core.Controller;
using i3GNetworkSwitcher.Core.Scheduler;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace i3GNetworkSwitcher.Web.Handlers
{
    class ApiInfoHandler : INetworkWebHandler
    {
        public ApiInfoHandler(NetworkController controller)
        {
            this.controller = controller;
        }

        private readonly NetworkController controller;

        public Task HandleRequest(HttpListenerContext e)
        {
            //Make sure to disable cache
            e.Response.Headers.Add("Cache-Control", "no-store");

            return e.Response.RespondJson(new Response(controller));
        }

        class Response
        {
            public Response(NetworkController controller)
            {
                this.controller = controller;
            }

            private readonly NetworkController controller;

            [JsonProperty("sites")]
            public Response_Site[] Sites => controller.Sites.Select((x, i) => new Response_Site(x, i)).ToArray();
        }

        class Response_Site
        {
            public Response_Site(NetworkSite site, int index)
            {
                this.site = site;
                this.index = index;
            }

            private readonly NetworkSite site;
            private readonly int index;

            [JsonProperty("name")]
            public string Name => site.Label;

            [JsonProperty("index")]
            public int Index => index;

            [JsonProperty("sources")]
            public Response_Site_Source[] Sources => site.Sources.Select((x, i) => new Response_Site_Source(x, i)).ToArray();
        }

        class Response_Site_Source
        {
            public Response_Site_Source(NetworkSource source, int index)
            {
                this.source = source;
                this.index = index;
            }

            private readonly NetworkSource source;
            private readonly int index;

            [JsonProperty("name")]
            public string Name => source.Name;

            [JsonProperty("index")]
            public int Index => index;

            [JsonProperty("type")]
            public string Type => source.Type.ToString().ToUpper();
        }
    }
}
