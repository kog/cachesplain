/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

// TODO [Greg 01/06/2015] : docs, enum value explanation

namespace cachesplain.Protocol
{
    public enum MagicValue : byte
    {
        Requested = 0x80,
        Received = 0x81
    }
}