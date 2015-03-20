/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;

// TODO [Greg 01/06/2014] : Clean me. Needs refactoring and testing.

namespace cachesplain.Protocol
{
    /// <summary>
    /// Provides a read-only structure that contains all the data for a given Memcached binary protocol operation. The format is as follows:
    /// 
    /// Bytes 0 - 24: Header
    /// Bytes 25 - N (header defined: keylen, &lt;= 65k bytes): Key
    /// Bytes N+1 - M (header defined): Value.
    /// 
    /// Where the value will be further subdivided into:
    /// 
    /// Bytes 0 - N (header defined: extlen, &lt;= 255 bytes): optional extras 
    /// Bytes N+1 - M (header defined: total body length - key length - extras length): value for key
    /// </summary>
    /// 
    /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped"/>
    public class MemcachedBinaryOperation
    {
        /// <summary>
        /// Holds the packet header. See the <see cref="MemcachedBinaryHeader"/> class for further details.
        /// </summary>
        public readonly MemcachedBinaryHeader Header;

        /// <summary>
        /// Holds the extras for the packet. May be null if the command/command direction does not allow
        /// for any extra fields (such as the delete command).
        /// </summary>
        /// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped"/>
        public readonly MemcachedBinaryExtras Extras;

        /// <summary>
        /// Holds the key associated with the packet. May be null if the command/command direction does
        /// not allow for the key to be passed.
        /// </summary>
        /// 
        /// <remarks>
        /// https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped notes that all binary operation
        /// responses not contain a key, with the exception of GetK and GetKQ. 
        /// </remarks>
        public readonly String Key;

        /// <summary>
        /// Provides a convenience property to get the <see cref="MagicValue"/> for the operation, allowing
        /// easy determination in which direction the op is going.
        /// </summary>
        public MagicValue Magic
        {
            get { return Header.Magic; }
        }

        /// <summary>
        /// Provides a convenience property to get the <see cref="Opcode"/> for the operation, allowing
        /// easy determination of what is going on. 
        /// </summary>
        public Opcode Opcode
        {
            get { return Header.Opcode; }
        }

        public MemcachedBinaryOperation(MemcachedBinaryHeader header, MemcachedBinaryExtras extras, string key)
        {
            Header = header;
            Extras = extras;
            Key = key;
        }

        public override string ToString()
        {
            return String.Format("{0} -> {1}{2}", Header, Key, null == Extras ? " %% " + Extras : "");
        }
    }
}