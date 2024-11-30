using i3GNetworkSwitcher.Core.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Scheduler
{
    class ScheduledCommandEvent
    {
        /// <summary>
        /// ID of this event
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Time this event is set to be ready by.
        /// </summary>
        [JsonProperty("time")]
        public DateTime Time { get; set; }

        /// <summary>
        /// User description of the event, shown in the UI.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Command to be executed.
        /// </summary>
        [JsonProperty("command")]
        public NetworkControlCommand Command { get; set; }

        public string CreateInfo(NetworkController controller)
        {
            DateTime local = Time.ToLocalTime();
            return $"NAME: {Description}\r\nTIME: {local.ToShortDateString()} {local.ToLongTimeString()}\r\nCOMMAND: {Command.CreateBlameLabel(controller)}";
        }
    }
}
