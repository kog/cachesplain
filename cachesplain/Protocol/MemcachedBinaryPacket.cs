/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

// TODO [Greg 01/06/2014] : Clean me. Needs refactoring and testing.

namespace cachesplain.Protocol
{
	/// <summary>
	/// Memcached allows for multiple binary operations within a given logical TCP packet. This class represents the
	/// application level portion of a given TCP packet, containing metadata about the given operations that were
	/// contained therein.
	/// </summary>
    public class MemcachedBinaryPacket
    {
		/// <summary>
		/// Holds the collection of associated binary operations.
		/// </summary>
		/// 
		/// <value>The operations that have been parsed out from a given packet. May be empty, will never be null.</value>
		private IEnumerable<MemcachedBinaryOperation> _operations = new List<MemcachedBinaryOperation>();

		/// <summary>
		/// Gets or sets the associated binary operations.
		/// </summary>
		/// 
		/// <value>The operations associated with the logical TCP packet. May be empty, but must not be null.</value>
		public IEnumerable<MemcachedBinaryOperation> Operations {get { return _operations; }
																 set { _operations = value; }}

        /// <summary>
        /// Gets or sets the number of operations contained in the given packet.
        /// </summary>
        /// 
        /// <remarks>
        /// This is encapsulated as a property, as opposed to calling Count is so that we don't need to walk the <see cref="IEnumerable"/>
        /// every time we're trying to figure out how many operations occurred.
        /// </remarks>
        public int OperationCount { get; set; }

		/// <summary>
		/// Gets or sets the time at which this packet was sent or received. 
		/// </summary>
		/// 
		/// <value>The time at which the packet was received.</value>
		/// 
		/// <remarks>
		/// We pull the "arrival time" of the packet straight from the packet itself, so even for the case of historical PCAP files this should be accurate.
		/// </remarks>
		public DateTime PacketTime { get; set; }

		/// <summary>
		/// Holds the IP address that is the source of the packet.
		/// </summary>
		/// 
		/// <value>The source IP address of the packet. Must not be null.</value>
		/// 
		/// <remarks>
		/// This value is pulled directly from the IP packet frame, and can be quite useful in the case where you either have multiple
		/// Memcached instances (IE: distributed cache) or if you're looking at something like traffic from a load balancer.
		/// </remarks>
		public String SourceAddress { get; set; }

		/// <summary>
		/// Holds the IP address that is the destination of the packet.
		/// </summary>
		/// 
		/// <value>The destination IP address of the packet. Must not be null.</value>
		/// 
		/// <remarks>
		/// This value is pulled directly from the IP packet frame, and can be quite useful in the case where you either have multiple
		/// Memcached instances (IE: distributed cache) or if you're looking at something like traffic from a load balancer.
		/// </remarks>
		public String DestinationAddress { get; set; }

		/// <summary>
		/// Holds the size of the packet.
		/// </summary>
		/// 
		/// <value>The size of the packet. Must be a positive number.</value>
		/// 
		/// <remarks>
		/// This value is the size of the portion of the logical packet that is application level. That is, minus all the other, lower level
		/// protocols such as TCP, IP, Ethernet etc.
		/// </remarks>
		public long PacketSize { get; set; }

		/// <summary>
		/// Holds the relevant port information for the packet.
		/// </summary>
		/// 
		/// <value>The relevant port information for the packet. Must be a positive number.</value>
		/// 
		/// <remarks>
		/// Given that when using TCP you wind up with both a source port, and a destination port, in this case "relevant" means the
		/// port actually associated with the Memcached instance. This will be sussed out based on the initial filtering options during
		/// capture/PCAP analysis.
		/// 
		/// Knowing the port, as with the source/destination IP address, can be incredibly helpful in the case where you're either analyzing
		/// traffic from a single node towards a distributed cache, or when you're analyzing traffic on some sort of load balancer.
		/// </remarks>
		public int Port { get; set; }

		/// <summary>
		/// Holds the length of a Memcached packet header. This is defined by the binary protocol spec to be exactly 24 bytes.
		/// </summary>
		private const int HeaderLength = 24;

		/// <summary>
		/// Holds a <see cref="Logger"/> to assist with debugging etc.
		/// </summary>
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();

	    // TODO [Greg 01/06/2015] : Move this parsing outside of the packet. 

		/// <summary>
		/// Given a raw byte array payload from a network interface, attempts to parse one or more <see cref="MemcachedBinaryPacket"/> instances. 
		/// </summary>
		/// 
		/// <param name="payload">A raw byte array to parse. Must not be null.</param>
		/// 
		/// <returns>An array of zero or more <see cref="MemcachedBinaryPacket"/> instances.</returns>
		/// 
		/// <remarks>
		/// Both the get and set commands support multi- or bulk- features, in which a packet may have multiple 
		/// operations. The spec suggests that pipelining usually happens in one of two ways:
		/// 
		///  * N-1 GetQ/GetKQ + 1 Get/GetK
		///  * N GetQ/GetKQ + 1 NoOp.
		/// 
		/// While not explicitly called out for set operations, it seems to work the same way. We attempt to de-mux the packets
		/// by parsing each and then checking the buffer for any other valid packets.
		/// </remarks>
		/// 
        /// <see cref="https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Get,_Get_Quietly,_Get_Key,_Get_Key_Quietly"/>
		public MemcachedBinaryPacket(byte[] payload)
		{
			var packets = new List<MemcachedBinaryOperation>();
			var offset = 0;

			try
			{
			    // TODO [Greg 01/06/2015] : Probably want to do a valid opcode check here too.

				// In order to have another packet, we must at least have a header and the first byte must be a correct magic value.
				while (offset + HeaderLength <= payload.Length && IsMagic(payload[offset]))
				{
					// Parse out the next packet.
					var packet = ParsePacket(payload, offset);
                    packets.Add(packet);

					// Bump our offset to see if anything is left in this request.
					offset += (HeaderLength + packet.Header.TotalBodyLength);					
				}
			}
			catch (Exception ex)
			{
			    // TODO [Greg 01/06/2015] : Improve logging of *why* this is a mangled packet. This is a temporary stopgap for dirty PCAPs.
				_logger.Warn("Caught a mangled packet, discarding", ex);
			}

		    OperationCount = packets.Count;
			_operations = packets;
		}

		/// <summary>
		/// Provides the meat of our packet processing: grabs the header and whatever else is in the packet. 
		/// </summary>
		/// 
		/// <returns>A <see cref="MemcachedBinaryPacket"/> representing the packet.</returns>
		/// 
		/// <param name="payload">The raw packet payload to parse. Must not be null or empty.</param>
		/// <param name="startIndex">The index within the raw payload to start parsing. This value is assumed to be the magic for the packet.</param>
		/// 
		/// <remarks>
		/// A valid Memcached command will always have at least a header, although it may not have either extras or a key (for example: No-Op).
		/// Further, you cannot know the size of the operation in the packet without first parsing the header fields, which means that ArraySegments are out the door.
		/// </remarks>
		MemcachedBinaryOperation ParsePacket(byte[] payload, int startIndex)
		{
			var offset = startIndex;

			MemcachedBinaryExtras extras = null;
			var key = String.Empty;

			// Parse out our header first, that should tell us what to do next.
			var header = new MemcachedBinaryHeader(new ArraySegment<Byte>(payload, offset, HeaderLength));

			// Now that we've got our headers, let's get the rest...
			if (header.TotalBodyLength > 0 && payload.Length > HeaderLength)
			{
				// Bump our offset in case we don't have any extras.
				offset += HeaderLength;

				// Grab 'em if we got 'em...
				if (header.ExtrasLength > 0)
				{
					extras = new MemcachedBinaryExtras(new ArraySegment<byte>(payload, offset, header.ExtrasLength), header.Magic, header.Opcode);
				}

				// Since we don't care about the value, let's grab our key and get out of here.
				key = ParseKey(new ArraySegment<byte> (payload, offset + header.ExtrasLength, header.KeyLength));
			}

			return new MemcachedBinaryOperation(header, extras, key);		
		}

		/// <summary>
		/// Attempts to parse a key from a Memcached packet. The text protocol defines this as any string sans spaces
		/// (as they're used for delimiters), but there doesn't seem to be anything else concrete. Given the ASCII
		/// roots, it's probably safe to assume these are UTF-8 compatible.
		/// </summary>
		/// 
		/// <returns>The key associated with the packet, if any, else String.Empty.</returns>        
		string ParseKey(ArraySegment<Byte> segment)
		{
			var key = String.Empty;

			try
			{
				key = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
			}
			catch (Exception ex)
			{
				_logger.Warn("Failed to parse key {0}, exception follows.", String.Join(" ", segment.Array.Skip(segment.Offset).Take(segment.Count), ex));
			}

			return key;
		}

		/// <summary>
		/// Tells us whether or not a given start byte is actually a Memcached magic value.
		/// </summary>
		/// 
		/// <returns><c>true</c> if the given byte is a Memcached magic value; otherwise, <c>false</c>.</returns>
		/// 
		/// <param name="firstByte">The first byte of a given packet.</param>
		/// 
		/// <remarks>
		/// It seems to be the case that when we get multiple reassembled TCP segments, we will get a packet arrival event
		/// for each segment - most of which will not actually be Memcached packets. In this case we discard the packet 
		/// and keep looking for something we know how to parse.
		/// </remarks>
		static bool IsMagic(byte firstByte)
		{
			return (byte)MagicValue.Requested == firstByte || (byte)MagicValue.Received == firstByte;
		}
    }
}