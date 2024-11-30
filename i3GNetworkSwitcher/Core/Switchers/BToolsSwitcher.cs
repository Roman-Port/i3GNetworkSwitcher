using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core.Switchers
{
    /// <summary>
    /// BTools switcher, communicating via the BTools Switcher Daemon
    /// </summary>
    internal class BToolsSwitcher : INetworkSwitcher
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="label">Display label.</param>
        /// <param name="url">URL prefix to connect to. For example, "http://127.0.0.1/"</param>
        /// <param name="outputIndex">The zero-based output index for switchers with multiple outputs.</param>
        public BToolsSwitcher(string label, string url, int outputIndex)
        {
            this.label = label;
            this.url = url;
            this.outputIndex = outputIndex;
            client.Timeout = TimeSpan.FromSeconds(10);
        }

        private readonly string label;
        private readonly string url;
        private readonly int outputIndex;

        public string Label => $"BTools ({label})";

        private readonly HttpClient client = new HttpClient();

        public async Task<int> GetCurrentPortAsync()
        {
            //Request the info from the switcher
            HttpResponseMessage response = await client.GetAsync(url + "audio");
            response.EnsureSuccessStatusCode();

            //Deserialize and verify
            SwitcherData res = JsonConvert.DeserializeObject<SwitcherData>(await response.Content.ReadAsStringAsync());
            if (res == null || res.Outputs == null)
                throw new Exception("Invalid switcher response.");
            if (res.Outputs.Length <= outputIndex)
                throw new Exception($"Switcher does not have {outputIndex+1} outputs.");

            //Check bitmask for set bits, setting flag if multiple were found
            int setBit = -1;
            int bitsFound = 0;
            for (int i = 0; i < 31; i++)
            {
                if (((res.Outputs[outputIndex].Bitmask >> i) & 1) == 1)
                {
                    setBit = i;
                    bitsFound++;
                }
            }

            //If exactly one bit was set, return it. Otherwise, return -1 for failure
            if (bitsFound == 1)
                return setBit;
            else
                return -1;
        }

        public async Task SetCurrentPortAsync(int port)
        {
            //The port for this is the 0-based bitmask shift for the input. For example input 0 is 1<<0 while input 1 is 1<<1.
            int bitmask = 1 << port;

            //Create the set command
            SwitcherData req = new SwitcherData
            {
                Outputs = new SwitcherData_Output[]
                {
                    new SwitcherData_Output
                    {
                        Index = outputIndex,
                        Bitmask = bitmask
                    }
                }
            };

            //Send
            string reqSer = JsonConvert.SerializeObject(req);
            StringContent reqContent = new StringContent(reqSer, Encoding.UTF8);
            reqContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(url + "audio", reqContent);
            response.EnsureSuccessStatusCode();

            //Check response
            SwitcherData res = JsonConvert.DeserializeObject<SwitcherData>(await response.Content.ReadAsStringAsync());
            if (res == null || res.Outputs == null || res.Outputs.Length <= outputIndex || res.Outputs[outputIndex].Bitmask != bitmask)
                throw new Exception("Switcher response did not match what was requested.");
        }

        class SwitcherData
        {
            [JsonProperty("outputs")]
            public SwitcherData_Output[] Outputs { get; set; }
        }

        class SwitcherData_Output
        {
            /// <summary>
            /// Index of the output. Only useful for requests.
            /// </summary>
            [JsonProperty("index")]
            public int Index { get; set; }

            /// <summary>
            /// Bitmask of the currenly selected sources.
            /// </summary>
            [JsonProperty("bitmask")]
            public int Bitmask { get; set; }
        }
    }
}
