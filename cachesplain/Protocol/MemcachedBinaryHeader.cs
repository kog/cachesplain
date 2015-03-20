/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Text;

// TODO [Greg 01/06/2014] : Clean me. Needs refactoring and testing.

namespace cachesplain.Protocol
{
    /// <summary>
    /// All Memcached messages must include at least a 24 byte header. The header includes various information for parsing the
    /// of the packet.
    /// </summary>
    /// 
    /// <remarks>
    /// It's worth noting that any multi-byte numerical value is in network order and bit twiddling must be performed.
    /// </remarks>
    public class MemcachedBinaryHeader
    {
        /// <summary>
        /// The magic indicates what manner of operation is underway: request to a Memcached server, or a response to a client.
        /// </summary>
        /// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Magic_Byte"/>.
        public readonly MagicValue Magic;

        /// <summary>
        /// Identifies which operation is being performed.
        /// </summary>
        /// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Command_Opcodes"/>.
        public readonly Opcode Opcode;

        /// <summary>
        /// Tells us how many bytes are in the key.
        /// </summary>
        public readonly ushort KeyLength;

        /// <summary>
        /// Tells us many bytes are in the optional extras. 
        /// </summary>
        /// 
        /// <remarks>
        /// If this value is greater than zero, the packet/operation is assumed to have extras.
        /// </remarks>
        public readonly byte ExtrasLength;

        /// <summary>
        /// This is a placeholder field and can be ignored. 
        /// </summary>
        /// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Data_Types"/>.
        public readonly byte DataType;

        /// <summary>
        /// This field will be one of two things depending on whether this is a response or a request:
        /// 
        ///  * Requests will have a a virtual bucket ID. Unfortunately this isn't particularly well documented.
        ///  * Responses will have a status, where the server will tell us the result of the command. 
        /// </summary>
        /// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Response_Status"/>
        public readonly ushort StatusOrVbucketId;

        /// <summary>
        /// The length, in bytes, of the extras field, the key field and the actual value associated with the key.
        /// In short, the length of everything after the key.
        /// </summary>
        public readonly int TotalBodyLength;

        /// <summary>
        /// As the name implies, this value is opaque to the server, which merely copies whatever the request
        /// contained into the response. Some clients use this for correlation.
        /// </summary>
        /// 
        /// <remarks>
        /// It is worth noting that if you have a cluster of multiple Memcached clients, the opaque will not 
        /// be unique across said cluster.
        /// </remarks>
        public readonly int Opaque;

        /// <summary>
        /// A 64-bit unique number associated with versioning of data. 
        /// </summary>
        /// 
        /// <see cref="http://neopythonic.blogspot.com/2011/08/compare-and-set-in-memcache.html"/>
        public readonly ulong Cas;

        // TODO [Greg 01/06/2015] : Move the parsing out of the constructor.

        public MemcachedBinaryHeader(ArraySegment<Byte> segment)
        {
            var rawData = segment.Array;
            var offset = segment.Offset;

            Magic = (MagicValue) rawData[offset];
            Opcode = (Opcode) rawData[++offset];
            KeyLength = BinaryHelper.DecodeUInt16(rawData, ++offset);
            ExtrasLength = rawData[offset += 2];
            DataType = rawData[++offset];
            StatusOrVbucketId = BinaryHelper.DecodeUInt16(rawData, ++offset);
            TotalBodyLength = BinaryHelper.DecodeInt32(rawData, offset += 2);
            Opaque = BinaryHelper.DecodeInt32(rawData, offset += 4);
            Cas = BinaryHelper.DecodeUInt64(rawData, offset + 4);
        }

        public override string ToString()
        {
            // TODO [Greg 12/27/2014] : Figure out a better way of handling optional raw binary in format string.
            return new StringBuilder().Append(Magic == MagicValue.Requested ? "TX## " : "RX## ")
                .Append(String.Format("{0} (0x{1:X2}) ", Opcode, (byte) Opcode))
                .Append("KeyLen: ").Append(KeyLength)
                .Append(", ExtLen: ").Append(ExtrasLength)
                .Append(", DataType: ").Append(DataType)
                .Append(", Status/VBucket: ").Append(StatusOrVbucketId)
                .Append(", BodyLen: ").Append(TotalBodyLength)
                .Append(", Opaque: ").Append(Opaque)
                .Append(", CAS: ").Append(Cas)
                .ToString();
        }
    }
}