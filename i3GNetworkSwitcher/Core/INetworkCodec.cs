using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core
{
    /// <summary>
    /// Interfaces for sending audio between sites.
    /// </summary>
    internal interface INetworkCodec
    {
        /// <summary>
        /// Label for the codec.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The estimated time it takes for a codec to make a change. Sent to the web clients.
        /// </summary>
        TimeSpan ModifyDelay { get; }

        /// <summary>
        /// Connect this codec to other codecs, changing their settings. Typically only works with same-type codecs. Sources must not be null.
        /// </summary>
        /// <param name="codecs">The codecs to connect to.</param>
        /// <param name="network">All sites on the network. Used to check for conflicts.</param>
        /// <returns></returns>
        Task ConnectTo(INetworkCodec[] codecs, NetworkSite[] network);

        /// <summary>
        /// Disconnects any outgoing connections.
        /// </summary>
        /// <returns></returns>
        Task Disconnect();

        /// <summary>
        /// Gets the current sources sending from this, if any. Null if nothing is sending.
        /// </summary>
        /// <param name="sites">List of sites used to resolve to.</param>
        /// <returns></returns>
        Task<NetworkSite[]> GetSendingTo(IReadOnlyCollection<NetworkSite> sites);
    }
}
