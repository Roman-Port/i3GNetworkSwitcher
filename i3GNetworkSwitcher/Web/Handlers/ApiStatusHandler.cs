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

namespace i3GNetworkSwitcher.Web.Handlers
{
    class ApiStatusHandler : INetworkWebHandler
    {
        public ApiStatusHandler(NetworkController controller)
        {
            this.controller = controller;
        }

        private readonly NetworkController controller;

        public async Task HandleRequest(HttpListenerContext e)
        {
            //Make sure to disable cache
            e.Response.Headers.Add("Cache-Control", "no-store");

            //Get info for sites. If the "only" parameter is specified, read only one site
            Response_Site[] sites;
            var onlyParam = e.Request.QueryString.GetValues("only");
            if (onlyParam != null && onlyParam.Length == 1)
            {
                //Parse as int and validate
                if (int.TryParse(onlyParam[0], out int index) && index >= 0 && index < controller.Sites.Length)
                {
                    //Get only this
                    sites = new Response_Site[1];
                    sites[0] = await GetSiteStatus(controller.Sites[index], index);
                } else
                {
                    //Invalid
                    await e.Response.RespondText("Invalid only parameter.", 400);
                    return;
                }
            } else
            {
                //Start all tasks
                Task<Response_Site>[] tasks = new Task<Response_Site>[controller.Sites.Length];
                for (int i = 0; i < tasks.Length; i++)
                    tasks[i] = GetSiteStatus(controller.Sites[i], i);

                //Get all
                sites = new Response_Site[controller.Sites.Length];
                for (int i = 0; i < sites.Length; i++)
                    sites[i] = await tasks[i];
            }

            //Respond
            await e.Response.RespondJson(new Response
            {
                Sites = sites
            });
        }

        private async Task<Response_Site> GetSiteStatus(NetworkSite site, int index)
        {
            //Start both at the same time
            Task<Response_Site_Codec> codecTask = GetCodecStatus(site);
            Task<Response_Site_Switcher> switcherTask = GetSwitcherStatus(site);

            return new Response_Site
            {
                Name = site.Label,
                Index = index,
                Switcher = await switcherTask,
                Codec = await codecTask
            };
        }

        private async Task<Response_Site_Switcher> GetSwitcherStatus(NetworkSite site)
        {
            //If has no switcher, return null
            if (site.Switcher == null)
                return null;

            //Request port from the switcher
            int port;
            try
            {
                port = await site.Switcher.GetCurrentPortAsync();
            } catch
            {
                return new Response_Site_Switcher
                {
                    Success = false,
                    SelectedSource = -1,
                    SelectedPort = -1
                };
            }

            //Resolve to source
            int source = -1;
            if (port != -1)
            {
                for (int i = 0; i < site.Sources.Length; i++)
                {
                    if (site.Sources[i].Port == port)
                        source = i;
                }
            }

            return new Response_Site_Switcher
            {
                Success = true,
                SelectedPort = port,
                SelectedSource = source
            };
        }

        private async Task<Response_Site_Codec> GetCodecStatus(NetworkSite site)
        {
            //Request status from codec
            NetworkSite[] txTo;
            try
            {
                txTo = await site.Codec.GetSendingTo(controller.Sites);
            } catch
            {
                return new Response_Site_Codec
                {
                    Success = false,
                    TransmittingTo = new int[0]
                };
            }

            //Resolve sites to indicies
            List<int> txToIndex = new List<int>();
            for (int i = 0; i < controller.Sites.Length; i++)
            {
                if (txTo.Contains(controller.Sites[i]))
                    txToIndex.Add(i);
            }

            return new Response_Site_Codec
            {
                Success = true,
                TransmittingTo = txToIndex.ToArray()
            };
        }

        class Response
        {
            [JsonProperty("sites")]
            public Response_Site[] Sites { get; set; }
        }

        class Response_Site
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("switcher")]
            public Response_Site_Switcher Switcher { get; set; } // May be null
           
            [JsonProperty("codec")]
            public Response_Site_Codec Codec { get; set; }
        }

        class Response_Site_Switcher
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("selected_port")]
            public int SelectedPort { get; set; } // -1 on invalid

            [JsonProperty("selected_source")]
            public int SelectedSource { get; set; } // -1 on non-matching
        }

        class Response_Site_Codec
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("transmitting_to")]
            public int[] TransmittingTo { get; set; }
        }
    }
}
