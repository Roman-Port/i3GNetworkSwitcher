using i3GNetworkSwitcher.Core.Controller;
using i3GNetworkSwitcher.Core.Scheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web.Handlers
{
    class ApiModifyHandler : INetworkWebHandler
    {
        public ApiModifyHandler(NetworkController controller)
        {
            this.controller = controller;
        }

        private readonly NetworkController controller;

        public async Task HandleRequest(HttpListenerContext e)
        {
            //Read the request
            Request req = await e.Request.ReadRequestBodyJson<Request>();

            //Validate
            if (req == null || req.Command == null || req.Command.ToSites == null)
            {
                await e.Response.RespondText("Request is invalid.", 400);
                return;
            }

            //Make sure to disable cache
            e.Response.Headers.Add("Cache-Control", "no-store");

            //Execute command
            bool success;
            string message;
            TimeSpan delay;
            try
            {
                //Run
                delay = await controller.ExecuteCommand(req.Command);

                //Set
                success = true;
                message = "OK";
            } catch (Exception ex)
            {
                //Set error info
                success = false;
                message = ex.Message;
                delay = TimeSpan.Zero;
            }

            //Create response
            await e.Response.RespondJson(new Response(success, message, delay));
        }

        class Request
        {
            [JsonProperty("command")]
            public NetworkControlCommand Command { get; set; }
        }

        class Response
        {
            public Response(bool success, string message, TimeSpan delay)
            {
                this.success = success;
                this.message = message;
                this.delay = delay;
            }

            private readonly bool success;
            private readonly string message;
            private readonly TimeSpan delay;

            [JsonProperty("success")]
            public bool Success => success;

            [JsonProperty("message")]
            public string Message => message;

            [JsonProperty("delay")]
            public int Delay => (int)delay.TotalMilliseconds;
        }
    }
}
