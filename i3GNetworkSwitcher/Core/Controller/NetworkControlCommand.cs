using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3GNetworkSwitcher.Core.Controller
{
    /// <summary>
    /// Serializable command to make changes to the network config
    /// </summary>
    class NetworkControlCommand
    {
        /// <summary>
        /// The index of the site that will originate programming.
        /// </summary>
        [JsonProperty("from_site")]
        public int FromSite { get; set; }

        /// <summary>
        /// The index of the source within the site that will originate programming.
        /// </summary>
        [JsonProperty("from_site_source")]
        public int FromSiteSrc { get; set; }

        /// <summary>
        /// Indicies of sites that this will deliver programming to.
        /// </summary>
        [JsonProperty("to_sites")]
        public int[] ToSites { get; set; }

        /// <summary>
        /// Creates label for showing info about changes.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public string CreateBlameLabel(NetworkController controller)
        {
            try
            {
                NetworkSite fromSite = controller.Sites[FromSite];
                string label = fromSite.Sources[FromSiteSrc].Name + " -> " + fromSite.Label;
                foreach (var s in ToSites)
                    label += ", " + controller.Sites[s].Label;
                return label;
            }
            catch
            {
                return "(invalid)";
            }
        }
    }
}
