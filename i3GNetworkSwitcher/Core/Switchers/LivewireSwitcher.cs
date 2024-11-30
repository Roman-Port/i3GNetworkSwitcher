using LWRPClient;
using LWRPClient.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core.Switchers
{
    internal class LivewireSwitcher : INetworkSwitcher
    {
        public LivewireSwitcher(string ip, int dst_index)
        {
            this.ip = IPAddress.Parse(ip);
            this.dst_index = dst_index;
        }

        private readonly IPAddress ip;
        private readonly int dst_index;

        public string Label => $"{ip}#DST_{dst_index}";

        public async Task<int> GetCurrentPortAsync()
        {
            //Connect and fetch
            LwChannel channel;
            using (LWRPConnection conn = new LWRPConnection(ip))
            {
                //Initialize
                conn.Initialize();

                //Wait for ready signal
                await conn.WaitForReadyAsync(TimeSpan.FromSeconds(5));

                //Get
                channel = conn.Destinations[dst_index].Channel;
            }

            //Decode
            if (channel.Type != LwChannelType.FROM_SOURCE)
                return -1;
            return channel.Channel;
        }

        public async Task SetCurrentPortAsync(int port)
        {
            using (LWRPConnection conn = new LWRPConnection(ip))
            {
                //Initialize
                conn.Initialize();

                //Wait for ready signal
                await conn.WaitForReadyAsync(TimeSpan.FromSeconds(5));

                //Set
                conn.Destinations[dst_index].Channel = new LwChannel(LwChannelType.FROM_SOURCE, (ushort)port);

                //Apply
                await conn.SendUpdatesAsync(TimeSpan.FromSeconds(5));
            }
        }
    }
}
