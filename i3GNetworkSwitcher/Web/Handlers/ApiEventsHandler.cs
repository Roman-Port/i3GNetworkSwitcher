using i3GNetworkSwitcher.Core.Controller;
using i3GNetworkSwitcher.Core.Scheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web.Handlers
{
    class ApiEventsHandler : INetworkWebHandler
    {
        public ApiEventsHandler(NetworkController controller, NetworkScheduler scheduler)
        {
            this.controller = controller;
            this.scheduler = scheduler;
        }

        private readonly NetworkController controller;
        private readonly NetworkScheduler scheduler;

        public Task HandleRequest(HttpListenerContext e)
        {
            //Make sure to disable cache
            e.Response.Headers.Add("Cache-Control", "no-store");

            //Switch on the method
            switch (e.Request.HttpMethod.ToUpper())
            {
                case "GET":
                    return HandleGet(e);
                case "PATCH":
                case "PUT":
                case "DELETE":
                    return HandleModification(e);
                default:
                    return e.Response.RespondText("Invalid method.", 400);
            }
        }

        private Task HandleGet(HttpListenerContext e)
        {
            //Get all events and wrap them
            WebEventList result = new WebEventList
            {
                Events = scheduler.Events.OrderBy(x => x.Time).Select(x => WrapEvent(x)).ToArray()
            };

            //Send
            return e.Response.RespondJson(result);
        }

        private async Task HandleModification(HttpListenerContext e)
        {
            //Get the body
            WebEvent request = await e.Request.ReadRequestBodyJson<WebEvent>();
            if (request == null)
            {
                await e.Response.RespondText("Invalid request body.", 400);
                return;
            }

            //Switch on method again
            ScheduledCommandEvent evt;
            switch (e.Request.HttpMethod.ToUpper())
            {
                case "PUT":
                    evt = await HandleCreate(e, request);
                    break;
                case "PATCH":
                    evt = await HandleUpdate(e, request);
                    break;
                case "DELETE":
                    evt = await HandleDelete(e, request);
                    break;
                default:
                    await e.Response.RespondText("Invalid method.", 400);
                    return;
            }

            //If returned null, bail out. We should've already handled it.
            if (evt == null)
                return;

            //Write the event back
            await e.Response.RespondJson(WrapEvent(evt));
        }

        private async Task<ScheduledCommandEvent> HandleCreate(HttpListenerContext e, WebEvent request)
        {
            //Check request
            try
            {
                ValidateEventRequestData(request, false);
            } catch (Exception ex)
            {
                await e.Response.RespondText(ex.Message, 400);
                return null;
            }

            //Add event
            return scheduler.AddEvent(request.Time.Value, request.Description, request.Command);
        }

        private async Task<ScheduledCommandEvent> HandleUpdate(HttpListenerContext e, WebEvent request)
        {
            try
            {
                //Check request
                ValidateEventRequestData(request, true);

                //Update event
                return scheduler.UpdateEvent(request.Id.Value, request.Time.Value, request.Description, request.Command);
            }
            catch (Exception ex)
            {
                await e.Response.RespondText(ex.Message, 400);
                return null;
            }
        }

        private async Task<ScheduledCommandEvent> HandleDelete(HttpListenerContext e, WebEvent request)
        {
            try
            {
                //Check request
                if (request.Id == null)
                    throw new Exception("ID field is missing.");

                //Update event
                return scheduler.DeleteEvent(request.Id.Value);
            }
            catch (Exception ex)
            {
                await e.Response.RespondText(ex.Message, 400);
                return null;
            }
        }

        /// <summary>
        /// Checks if everything is valid.
        /// </summary>
        /// <param name="request"></param>
        /// <exception cref="Exception"></exception>
        private void ValidateEventRequestData(WebEvent request, bool checkId)
        {
            if (request.Description == null)
                throw new Exception("Description field is missing.");
            if (request.Time == null)
                throw new Exception("Time field is missing.");
            if (request.Command == null)
                throw new Exception("Command field is missing.");
            if (request.Id == null && checkId)
                throw new Exception("ID field is missing.");
            controller.ValidateCommand(request.Command);
        }

        private WebEvent WrapEvent(ScheduledCommandEvent evt)
        {
            return new WebEvent
            {
                Id = evt.Id,
                Time = evt.Time,
                Description = evt.Description,
                Command = evt.Command
            };
        }

        class WebEvent
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("time")]
            public DateTime? Time { get; set; }

            [JsonProperty("command")]
            public NetworkControlCommand Command { get; set; }
        }

        class WebEventList
        {
            [JsonProperty("events")]
            public WebEvent[] Events { get; set; }
        }
    }
}
