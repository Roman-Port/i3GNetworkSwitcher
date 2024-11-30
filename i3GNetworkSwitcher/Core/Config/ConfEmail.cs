using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace i3GNetworkSwitcher.Core.Config
{
    internal class ConfEmail
    {
        [JsonProperty("smtp_host")]
        public string SmtpHost { get; set; }

        [JsonProperty("smtp_use_ssl")]
        public bool UseSsl { get; set; }

        [JsonProperty("smtp_username")]
        public string SmtpUsername { get; set; }

        [JsonProperty("smtp_password")]
        public string SmtpPassword { get; set; }

        [JsonProperty("from_email")]
        public string FromEmail { get; set; }

        [JsonProperty("from_name")]
        public string FromName { get; set; }

        [JsonProperty("lists")]
        public Dictionary<string, string[]> Lists { get; set; }
    }
}
