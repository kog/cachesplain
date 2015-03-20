/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;

namespace cachesplain.Protocol
{
    /// <summary>
    /// Provides a set of helper methods to deal with the fact that several values in our packet are actually network order.
    /// </summary>
    /// 
    /// <remarks>
    /// These methods are lifted (mostly) wholesale from https://github.com/enyim/EnyimMemcached/blob/master/Enyim.Caching/Memcached/Protocol/Binary/BinaryConverter.cs,
    /// which is part of a Memcached client written in C#. The license at the time (https://github.com/enyim/EnyimMemcached/blob/master/LICENSE) was listed as 
    /// Apache 2.0, which allows such usage with attribution. 
    /// </remarks>
    public class BinaryHelper
    {
        /// <summary>
        /// Given a byte buffer and an index, attempts to read an unsigned 16 bit int in network order.
        /// </summary>
        /// 
        /// <returns>An unsigned 16 bit integer, comprising the 2 bytes immediately following the given offset.</returns>
        /// 
        /// <param name="buffer">The byte buffer to read. Must not be null, must have at least 2 bytes.</param>
        /// <param name="offset">The start index within the byte buffer to start reading. Must be a valid index.</param>
        public static UInt16 DecodeUInt16(byte[] buffer, int offset)
        {
            return (UInt16) ((buffer[offset] << 8) + buffer[offset + 1]);
        }

        /// <summary>
        /// Given a byte buffer and an index, attempts to read a signed 32 bit int in network order.
        /// </summary>
        /// 
        /// <returns>A signed 32 bit integer, comprising the 4 bytes immediately following the given offset.</returns>
        /// 
        /// <param name="buffer">The byte buffer to read. Must not be null, must have at least 4 bytes.</param>
        /// <param name="offset">The start index within the byte buffer to start reading. Must be a valid index.</param>
        public static Int32 DecodeInt32(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        /// <summary>
        /// Given a byte buffer and an index, attempts to read an unsigned 64 bit int in network order.
        /// </summary>
        /// 
        /// <returns>An unsigned 64 bit integer, comprising the 8 bytes immediately following the given offset.</returns>
        /// 
        /// <param name="buffer">The byte buffer to read. Must not be null, must have at least 8 bytes.</param>
        /// <param name="offset">The start index within the byte buffer to start reading. Must be a valid index.</param>
        public static unsafe UInt64 DecodeUInt64(byte[] buffer, int offset)
        {
            fixed (byte* ptr = buffer)
            {
                return DecodeUInt64(ptr, offset);
            }
        }

        /// <summary>
        /// Given a pointer to a byte buffer and an offset, attempts to read an unsigned 64 bit in network order.
        /// </summary>
        /// 
        /// <returns>An unsigned 64 bit integer, comprising the 8 bytes immediately following the given offset.</returns>
        /// 
        /// <param name="buffer">A pointer to a given byte buffer, for pointer arithmetic. Must be valid.</param>
        /// <param name="offset">The start index within the byte buffer to start reading. Must be a valid index.</param>
        private static unsafe UInt64 DecodeUInt64(byte* buffer, int offset)
        {
            buffer += offset;

            var part1 = (UInt32) ((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
            var part2 = (UInt32) ((buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7]);

            return ((UInt64) part1 << 32) | part2;
        }
    }
}