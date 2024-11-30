using i3GNetworkSwitcher.Core.Codecs;
using i3GNetworkSwitcher.Core.Config;
using i3GNetworkSwitcher.Core.Config.Codecs;
using i3GNetworkSwitcher.Core.Config.Switchers;
using i3GNetworkSwitcher.Core.Switchers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    internal static class ConfigInflater
    {
        /// <summary>
        /// Inflates the config to a usable object, loading from filename.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static NetworkData InflateConfig(string filename)
        {
            return InflateConfig(JsonConvert.DeserializeObject<ConfRoot>(File.ReadAllText(filename)));
        }

        /// <summary>
        /// Inflates the config to a usable object.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static NetworkData InflateConfig(ConfRoot conf)
        {
            List<NetworkSite> sites = new List<NetworkSite>();
            foreach (var s in conf.Sites)
            {
                //Validate
                if (s.Name == null)
                    throw new Exception("Site is missing name.");
                if (s.Sources == null || s.Sources.Length == 0 || (s.SwitcherType == null && s.Sources.Length > 1))
                    throw new Exception($"Site {s.Name}: Sources are either null, empty, or have more then one item with no switcher specified.");
                if (s.CodecType == null || s.CodecData == null)
                    throw new Exception($"Site {s.Name}: Codec type or codec data is null.");

                //Inflate each source
                List<NetworkSource> sources = new List<NetworkSource>();
                foreach (var src in s.Sources)
                {
                    //Validate
                    if (src.Name == null)
                        throw new Exception($"Site {s.Name}: Source is missing name.");

                    //Get type
                    NetworkSourceType type;
                    if (src.Type == null || !Enum.TryParse(src.Type, out type))
                        throw new Exception($"Site {s.Name}: Source is missing type or it is invalid.");

                    //Add
                    sources.Add(new NetworkSource(src.Name, src.Data, type));
                }

                //Inflate switcher if one is needed
                INetworkSwitcher switcher = null;
                if (s.SwitcherType != null)
                    switcher = InflateSwitcher(s.SwitcherType, s.SwitcherData);

                //Inflate codec
                INetworkCodec codec = InflateCodec(s.CodecType, s.CodecData);

                //Create the site and add it
                sites.Add(new NetworkSite(s.Name, sources.ToArray(), switcher, codec));
            }
            return new NetworkData(sites.ToArray(), conf);
        }

        private static INetworkSwitcher InflateSwitcher(string switcherType, JObject switcherData)
        {
            switch (switcherType)
            {
                case "BTools": return InflateSwitcherBTools(switcherData);
                case "Livewire": return InflateSwitcherLivewire(switcherData);
            }
            throw new Exception($"Unknown switcher type \"{switcherType}\".");
        }

        private static BToolsSwitcher InflateSwitcherBTools(JObject data)
        {
            //Validate
            if (data == null)
                throw new Exception($"Switcher data is null.");
            ConfSwitcherBTools serData = data.ToObject<ConfSwitcherBTools>();
            if (serData.Url == null)
                throw new Exception($"Codec data url is missing.");

            return new BToolsSwitcher(serData.Url, serData.Url, serData.OutputIndex);
        }

        private static LivewireSwitcher InflateSwitcherLivewire(JObject data)
        {
            //Validate
            if (data == null)
                throw new Exception($"Switcher data is null.");
            ConfSwitcherLivewire serData = data.ToObject<ConfSwitcherLivewire>();
            if (serData.Ip == null)
                throw new Exception($"Codec data ip is missing.");

            return new LivewireSwitcher(serData.Ip, serData.DstIndex);
        }

        private static INetworkCodec InflateCodec(string codecType, JObject codecData)
        {
            switch (codecType)
            {
                case "BarixExstreamer500": return InflateCodecBarixExstreamer500(codecData);
            }
            throw new Exception($"Unknown codec type \"{codecType}\".");
        }

        private static BarixExstreamer500Codec InflateCodecBarixExstreamer500(JObject data)
        {
            //Validate
            if (data == null)
                throw new Exception($"Codec data is null.");
            ConfCodecBarixExstreamer500 serData = data.ToObject<ConfCodecBarixExstreamer500>();
            if (serData.Url == null)
                throw new Exception($"Codec data url is missing.");
            if (serData.PublicIp == null)
                throw new Exception($"Codec data public_ip is missing.");

            return new BarixExstreamer500Codec(serData.Url, serData.Url, serData.PublicIp, serData.PublicPort);
        }
    }
}
