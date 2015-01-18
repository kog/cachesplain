/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

namespace cachesplain.Engine
{
    /// <summary>
    /// Describes the the type of source we're going to capture from.
    /// </summary>
    public enum CaptureSource
    {
        /// <summary>
        /// We're going to try and capture from a live interface.
        /// </summary>
        /// 
        /// <remarks>
        /// While this mode is supported, it's generally recommended to grab a PCAP dump via
        /// something like tcpdump. Reading from a live interface may not be reproducible later,
        /// and may be hard to do: no Mono/MS CLR installed, permission issues, performance concerns.
        /// </remarks>
        Interface,

        /// <summary>
        /// We're going to read packets out of something previously captured.
        /// </summary>
        /// 
        /// <remarks>
        /// This is the recommended capture source, and should be preferred unless you're doing
        /// something like local dev on a personal machine.
        /// </remarks>
        PcapFile
    }
}
