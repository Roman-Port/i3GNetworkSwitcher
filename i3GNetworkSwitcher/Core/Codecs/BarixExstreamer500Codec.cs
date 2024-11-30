using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core.Codecs
{
    /// <summary>
    /// Barix Box codec
    /// </summary>
    internal class BarixExstreamer500Codec : INetworkCodec
    {
        public BarixExstreamer500Codec(string label, string url, string publicIp, int publicPort)
        {
            this.label = label;
            this.url = url;
            this.publicIp = publicIp;
            this.publicPort = publicPort;
            client.Timeout = TimeSpan.FromSeconds(20);
        }

        private readonly string label;
        private readonly string url;
        private readonly string publicIp; // IP given to other Barixes to talk to this one
        private readonly int publicPort;

        private readonly HttpClient client = new HttpClient();

        private Dictionary<string, string> dataKeyCache = null;

        /* Public API */

        public string Name => $"Barix Exstreamer 500 ({label})";

        public TimeSpan ModifyDelay => TimeSpan.FromSeconds(15);

        public async Task ConnectTo(INetworkCodec[] codecs, NetworkSite[] network)
        {
            //Get config keys
            Dictionary<string, string> configKeys = await GetConfigKeys();

            //Count how many streams this supports
            int supportedStreams = 0;
            while (configKeys.ContainsKey($"STREAM{supportedStreams}_METHOD") && configKeys.ContainsKey($"STREAM{supportedStreams}_URL") && configKeys.ContainsKey($"STREAM{supportedStreams}_PORT"))
                supportedStreams++;

            //The system only supports up to this many
            if (codecs == null || codecs.Length > supportedStreams)
                throw new Exception($"Barix Exstreamer only supports up to {supportedStreams} streaming destinations.");

            //Find all possibly conflicting codecs on the network. This is any barix 500 that isn't us or one of the specified codecs to connect to
            List<INetworkCodec> conflicting = new List<INetworkCodec>(network.Select(x => x.Codec));
            conflicting.Remove(this);
            foreach (INetworkCodec codec in codecs)
            {
                if (conflicting.Contains(codec))
                    conflicting.Remove(codec);
            }

            //Now loop through all conflicting codecs and check if any are transmitting to any of our destination codecs or ourselves
            foreach (var c in conflicting)
            {
                //Get what this is connected to
                NetworkSite[] conflictSendingTo = await c.GetSendingTo(network);

                //See if we need to disconnect it
                bool forceDisconnect = false;
                foreach (var n in conflictSendingTo)
                {
                    if (n.Codec == this || codecs.Contains(n.Codec))
                        forceDisconnect = true;
                }

                //If we need to disconnect this, do so
                if (forceDisconnect)
                    await c.Disconnect();
            }

            //Loop through all destination codecs and set them up to recieve mode
            for (int i = 0; i < codecs.Length; i++)
            {
                if (codecs[i] == this)
                    throw new Exception("Specified loop in codecs to connect to.");
                if (codecs[i] is BarixExstreamer500Codec dst)
                {
                    await dst.SetToReceive();
                }
            }

            //Begin building form that'll be sent
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(configKeys["LOCATION"], "0"); // Set to "studio encoder"
            form.Add(configKeys["RELAY0_MODE"], "2"); // Set relays to be always OFF
            form.Add(configKeys["RELAY1_MODE"], "2"); // Set relays to be always OFF
            form.Add(configKeys["RELAY2_MODE"], "2"); // Set relays to be always OFF
            form.Add(configKeys["RELAY3_MODE"], "2"); // Set relays to be always OFF

            //Add each stream manually
            for (int i = 0; i < supportedStreams; i++)
            {
                //If this index is in-bounds, use data otherwise clear out with defaults
                string valUrl = "";
                string valPort = "3030";
                if (i < codecs.Length)
                {
                    //Convert to a Barix type
                    if (codecs[i] is BarixExstreamer500Codec dst)
                    {
                        valUrl = dst.publicIp;
                        valPort = dst.publicPort.ToString();
                    } else
                    {
                        throw new Exception($"Cannot connect Barix streamer to non-Barix: {codecs[i].Name}");
                    }
                }

                //Set
                form.Add(configKeys[$"STREAM{i}_METHOD"], "0"); // Push RTP method
                form.Add(configKeys[$"STREAM{i}_URL"], valUrl);
                form.Add(configKeys[$"STREAM{i}_PORT"], valPort);
            }

            //Send setup
            await SendSetup(form);
        }

        public async Task Disconnect()
        {
            //Just put it into rx mode
            await SetToReceive();
        }

        public async Task<NetworkSite[]> GetSendingTo(IReadOnlyCollection<NetworkSite> sites)
        {
            //Fetch the mode and abort if it's not an encoder
            int mode = await GetCurrentLocationMode();
            if (mode != 0)
                return new NetworkSite[0];

            //Get config keys
            Dictionary<string, string> configKeys = await GetConfigKeys();

            //Download and decode HTML config page. I am NOT proud of this...
            HtmlDocument doc = await LoadWebDocument(url + "stl_config.html");

            //Collect rows...for some reason the first one is special
            HtmlNode restNode = doc.GetElementbyId("stream_out_rest");
            string[] ips = new string[]
            {
                FindNamedInput(doc.GetElementbyId("stream_out_1"), configKeys["STREAM0_URL"]),
                FindNamedInput(restNode, configKeys["STREAM1_URL"]),
                FindNamedInput(restNode, configKeys["STREAM2_URL"]),
                FindNamedInput(restNode, configKeys["STREAM3_URL"]),
                FindNamedInput(restNode, configKeys["STREAM4_URL"]),
                FindNamedInput(restNode, configKeys["STREAM5_URL"]),
                FindNamedInput(restNode, configKeys["STREAM6_URL"]),
                FindNamedInput(restNode, configKeys["STREAM7_URL"])
            };

            //Now that we have all destination IPs, decode them using the site table
            List<NetworkSite> result = new List<NetworkSite>();
            foreach (var s in sites)
            {
                //If this is a Barix codec, get it's public IP and see if it exists in the IPs table
                if (s.Codec is BarixExstreamer500Codec dst)
                {
                    if (ips.Contains(dst.publicIp))
                        result.Add(s);
                }
            }
            return result.ToArray();
        }

        /* Internal API */

        /// <summary>
        /// Sets this Barix box into receive mode.
        /// </summary>
        /// <returns></returns>
        private async Task SetToReceive()
        {
            //Get config keys
            Dictionary<string, string> configKeys = await GetConfigKeys();

            //Begin building form that'll be sent
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(configKeys["LOCATION"], "1"); // Set to "studio decoder"
            form.Add(configKeys["RELAY0_MODE"], "0"); // Set relays to be on when remote is on
            form.Add(configKeys["RELAY1_MODE"], "0"); // Set relays to be on when remote is on
            form.Add(configKeys["RELAY2_MODE"], "0"); // Set relays to be on when remote is on
            form.Add(configKeys["RELAY3_MODE"], "0"); // Set relays to be on when remote is on
            form.Add(configKeys["STREAMINC_METHOD"], "0"); // Set incoming stream type to RTP Push 
            form.Add(configKeys["STREAMINC_URL"], ""); // Set incoming stream type to blank
            form.Add(configKeys["STREAMINC_PORT"], publicPort.ToString()); // Set incoming stream port

            //Send setup
            await SendSetup(form);
        }

        /* Internal methods */

        private static readonly Regex locationRegex = new Regex("var wmode *= *[0-9];");

        /// <summary>
        /// Returns the current mode of the STL.
        /// 0: Studio Encoder
        /// 1: Transmitter Decoder
        /// 2: Studio Encoder/Decoder
        /// 3: Transmitter Encoder/Decoder
        /// </summary>
        /// <returns></returns>
        private async Task<int> GetCurrentLocationMode()
        {
            //Request and read STL status JS file
            string js;
            using (var response = await client.GetAsync(url + "stl_status.js"))
            {
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Unsuccessful status code when communicating with codec at {Name}.");
                js = await response.Content.ReadAsStringAsync();
            }

            //Use regex to find a variable being set...very janky
            //This'll match to "var wmode = 0;"
            Match[] matches = locationRegex.Matches(js).ToArray();
            if (matches.Length != 1)
                throw new Exception($"Failed to get mode/location from codec {Name}: Failed to find in data.");

            //Extract the number from the match
            string foundValue = matches[0].Value;
            int foundValueOffset = foundValue.IndexOf("= ");
            if (foundValueOffset < 0)
                throw new Exception($"Failed to get mode/location from codec {Name}: Failed to decode line.");
            foundValue = foundValue.Substring(foundValueOffset + 2).TrimEnd(';');

            //Parse
            if (int.TryParse(foundValue, out int mode))
                return mode;
            throw new Exception($"Failed to get mode/location from codec {Name}: Invalid integer.");
        }

        private async Task SendSetup(Dictionary<string, string> data)
        {
            using (var response = await client.PostAsync(url + "setup.cgi", new FormUrlEncodedContent(data)))
            {
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Unsuccessful status code when communicating with codec at {Name}.");
            }
        }

        private string FindNamedInput(HtmlNode node, string name)
        {
            if (node == null)
                throw new Exception($"Search for data value \"{name}\" failed because the node to search wasn't found.");
            var dec = node.Descendants().ToArray();
            HtmlNode[] selected = node.Descendants().Where(x => x.Name == "input" && x.GetAttributeValue("name", "") == name).ToArray();
            if (selected.Length == 0)
                throw new Exception($"Failed to find data value \"{name}\".");
            if (selected.Length > 1)
                throw new Exception($"Found multiple data values \"{name}\".");
            return selected[0].GetAttributeValue("value", "");
        }

        /// <summary>
        /// Config keys differ from product to product. This is an abstraction layer that allows us to use common names. This loads that table.
        /// It only loads it once. Once loaded, it'll be cached.
        /// </summary>
        /// <returns></returns>
        private async Task<Dictionary<string, string>> GetConfigKeys()
        {
            //If the cache is valid, use it
            if (dataKeyCache != null)
                return dataKeyCache;

            //Initialize new cache
            Dictionary<string, string> keys = new Dictionary<string, string>();

            //Fetch STL config page
            var docStl = await LoadWebDocument(url + "stl_config.html");

            //Incoming stream
            HtmlNode incomingStream = docStl.GetElementbyId("stream_inc");
            if (incomingStream == null)
                throw new Exception("Failed to find stream_inc while preparing config keys.");
            ExtractStreamRowKeys(keys, "STREAMINC", incomingStream.GetElementChildren().LastOrDefault());

            //Outgoing stream #1 - This one is special
            ExtractStreamRowKeys(keys, "STREAM0", docStl.GetElementbyId("stream_out_1"));

            //Other outgoing streams are in their own group
            HtmlNode otherOutgoing = docStl.GetElementbyId("stream_out_rest");
            if (otherOutgoing == null)
                throw new Exception("Failed to find stream_out_rest while preparing config keys.");
            HtmlNode[] otherOutgoingChildren = otherOutgoing.GetElementChildren();
            for (int i = 0; i < 7; i++)
                ExtractStreamRowKeys(keys, "STREAM" + (i + 1), otherOutgoingChildren[i]);

            //Get the relay ones. These have ID sr<x> where <x> is the index, starting at 1. Find as many as we can
            int relayIndex = 0;
            while (true)
            {
                HtmlNode relayNode = docStl.GetElementbyId("sr" + (relayIndex + 1));
                if (relayNode == null)
                    break;
                keys.Add($"RELAY{relayIndex}_MODE", relayNode.GetRequiredAttribute("name"));
                relayIndex++;
            }
            if (relayIndex == 0)
                throw new Exception("Failed to find relay mode config keys.");

            //Fetch STL location page
            var docLoc = await LoadWebDocument(url + "stl_location.html");

            //Find the select for the location
            HtmlNode location = docLoc.GetElementbyId("stype");
            if (location == null)
                throw new Exception("Failed to find stype while preparing config keys.");
            keys.Add($"LOCATION", location.GetRequiredAttribute("name"));

            //Finalize
            dataKeyCache = keys;
            return keys;
        }

        /// <summary>
        /// Extracts the keys from a stream row.
        /// </summary>
        /// <param name="dst">Places extracted keys into this dict.</param>
        /// <param name="prefix">Prefix for extracted keys.</param>
        /// <param name="tr">The element to search in.</param>
        private void ExtractStreamRowKeys(Dictionary<string, string> dst, string prefix, HtmlNode tr)
        {
            if (tr == null || tr.Name.ToLower() != "tr")
                throw new Exception("Failed to find element in document for preparing config keys.");
            HtmlNode[] children = tr.GetElementChildren();
            dst.Add(prefix + "_METHOD", children[0].GetFirstElementChild().GetRequiredAttribute("name"));
            dst.Add(prefix + "_URL", children[1].GetFirstElementChild().GetRequiredAttribute("name"));
            dst.Add(prefix + "_PORT", children[2].GetFirstElementChild().GetRequiredAttribute("name"));
        }

        private async Task<HtmlDocument> LoadWebDocument(string url)
        {
            //Download
            string docHtml = await client.GetStringAsync(url);

            //Parse
            var doc = new HtmlDocument();
            doc.LoadHtml(docHtml);

            return doc;
        }

        /* Barix config keys */

        /*private const string BCONF_STEAM0_METHOD = "B856";
        private const string BCONF_STEAM0_URL = "S600";
        private const string BCONF_STEAM0_PORT = "W864";
        private const string BCONF_STEAM1_METHOD = "B857";
        private const string BCONF_STEAM1_URL = "S632";
        private const string BCONF_STEAM1_PORT = "W866";
        private const string BCONF_STEAM2_METHOD = "B858";
        private const string BCONF_STEAM2_URL = "S664";
        private const string BCONF_STEAM2_PORT = "W868";
        private const string BCONF_STEAM3_METHOD = "B859";
        private const string BCONF_STEAM3_URL = "S696";
        private const string BCONF_STEAM3_PORT = "W870";
        private const string BCONF_STEAM4_METHOD = "B860";
        private const string BCONF_STEAM4_URL = "S728";
        private const string BCONF_STEAM4_PORT = "W872";
        private const string BCONF_STEAM5_METHOD = "B861";
        private const string BCONF_STEAM5_URL = "S760";
        private const string BCONF_STEAM5_PORT = "W874";
        private const string BCONF_STEAM6_METHOD = "B862";
        private const string BCONF_STEAM6_URL = "S792";
        private const string BCONF_STEAM6_PORT = "W876";
        private const string BCONF_STEAM7_METHOD = "B863";
        private const string BCONF_STEAM7_URL = "S824";
        private const string BCONF_STEAM7_PORT = "W878";
        private const string BCONF_RELAY0_MODE = "B530";
        private const string BCONF_RELAY1_MODE = "B531";
        private const string BCONF_RELAY2_MODE = "B532";
        private const string BCONF_RELAY3_MODE = "B533";
        private const string BCONF_LOCATION = "B536";
        private const string BCONF_STEAMINC_METHOD = "B922";
        private const string BCONF_STEAMINC_URL = "S890";
        private const string BCONF_STEAMINC_PORT = "W534";*/
    }
}
