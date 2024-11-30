using i3GNetworkSwitcher.Core.Controller.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core.Controller
{
    /// <summary>
    /// The class that will actually make changes to the network.
    /// </summary>
    class NetworkController
    {
        public delegate Task CommandSuccessEventArgs(NetworkController controller, NetworkControlCommand cmd);

        public NetworkController(NetworkSite[] sites)
        {
            this.sites = sites;
        }

        private readonly NetworkSite[] sites;

        /// <summary>
        /// Sites associated with the controller
        /// </summary>
        public NetworkSite[] Sites => sites;

        /// <summary>
        /// Event raised when a command is executed successfully.
        /// </summary>
        public event CommandSuccessEventArgs OnCommandSuccess;

        /// <summary>
        /// Executes a command, making changes to the network. Returns the recommended delay before checking status while codecs reboot.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public async Task<TimeSpan> ExecuteCommand(NetworkControlCommand cmd)
        {
            //Get all associated sites
            NetworkSite fromSite = sites[cmd.FromSite];
            NetworkSource fromSrc = fromSite.Sources[cmd.FromSiteSrc];
            NetworkSite[] toSites = new NetworkSite[cmd.ToSites.Length];
            for (int i = 0; i < toSites.Length; i++)
                toSites[i] = sites[cmd.ToSites[i]];

            //First, connect codecs (even if there are none)
            TimeSpan codecDelay = fromSite.Codec.ModifyDelay;
            try
            {
                //Resolve sites to their codecs
                INetworkCodec[] dstCodecs = new INetworkCodec[toSites.Length];
                for (int i = 0; i < dstCodecs.Length; i++)
                    dstCodecs[i] = toSites[i].Codec;

                //Connect
                await fromSite.Codec.ConnectTo(dstCodecs, sites);
            }
            catch (Exception ex)
            {
                throw new ControlCodecConnectException(fromSite, ex);
            }

            //Update originator site's switcher to the source that this is originating from, if set
            if (fromSite.Switcher != null)
            {
                try
                {
                    await fromSite.Switcher.SetCurrentPortAsync(fromSrc.Port);
                } catch (Exception ex)
                {
                    throw new ControlSwitcherChangeException(fromSite, ex);
                }
            }

            //Update destination sites' switchers to change to external source
            foreach (var site in toSites)
            {
                try
                {
                    //Find the external site
                    bool hasExternal = false;
                    int externalPort = 0;
                    foreach (var src in site.Sources)
                    {
                        if (src.Type == NetworkSourceType.EXTERNAL)
                        {
                            hasExternal = true;
                            externalPort = src.Port;
                        }
                    }

                    //If the external port was found, update the switcher
                    if (site.Switcher != null && hasExternal)
                        await site.Switcher.SetCurrentPortAsync(externalPort);
                } catch (Exception ex)
                {
                    throw new ControlSwitcherChangeException(site, ex);
                }
            }

            //Finally, send event
            OnCommandSuccess?.Invoke(this, cmd);

            return codecDelay;
        }

        /// <summary>
        /// Utility function that'll throw an exception if a command is invalid
        /// </summary>
        /// <param name="command"></param>
        public void ValidateCommand(NetworkControlCommand command)
        {
            //Check from site and site source
            if (command.FromSite < 0 || command.FromSite >= sites.Length)
                throw new Exception("Command from_site is invalid.");
            if (command.FromSiteSrc < 0 || command.FromSiteSrc >= sites[command.FromSite].Sources.Length)
                throw new Exception("Command from_site_source is invalid.");

            //Check to sites
            if (command.ToSites == null)
                throw new Exception("Command to_sites is null.");
            if (command.ToSites.Contains(command.FromSite))
                throw new Exception("Command to_sites cannot contain the originating site.");

            //Scan to sites for out-of-range sites
            foreach (int to in command.ToSites)
            {
                if (to < 0 || to >= sites.Length)
                    throw new Exception($"Command to_sites contains an invalid site: {to}.");
            }

            //Scan to sites for duplicates
            foreach (int to in command.ToSites)
            {
                bool found = false;
                foreach (int c in command.ToSites)
                {
                    if (c == to)
                    {
                        if (found)
                            throw new Exception("Command to_sites cannot contain duplicates.");
                        found = true;
                    }
                }
            }
        }
    }
}
