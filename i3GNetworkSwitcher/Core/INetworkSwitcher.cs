using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core
{
    /// <summary>
    /// Switcher that controls audio routing.
    /// </summary>
    internal interface INetworkSwitcher
    {
        /// <summary>
        /// Gets the static name of the switcher.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Fetches the current port routed. Returns -1 if invalid.
        /// </summary>
        /// <returns></returns>
        Task<int> GetCurrentPortAsync();

        /// <summary>
        /// Selects the current port routed.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        Task SetCurrentPortAsync(int port);
    }
}
