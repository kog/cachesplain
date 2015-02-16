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
	/// Extras are optional, depending on both the operation underway, as well as the direction (sent, received).
    /// </summary>
    public class MemcachedBinaryExtras
    {
        /// <summary>
        /// A 32 bit, server-opaque integer that is stored with a given value. 
        /// </summary>
        /// 
        /// <remarks>
        /// This is different than the opaque in the header in that the flags have meaning to the client, where the opaque is mostly for correlation purposes. That is,
        /// the opaque is opaque to both client and server, whereas the flags are only opaque to the server.
        /// </remarks>
        public int? Flags;

        /// <summary>
        /// The number of seconds before an item will expire. If 0, the item will not expire and will only be evicted from Memcache due to memory pressure.
        /// If the value is greater than 30 seconds in days (2,592,000), it will be treated as a Unix timestamp literal at which the item will expire. 
        /// </summary>
        /// 
        /// <remarks>
        /// It is also worth noting that the expiration extra has special meaning for both increment and decrement operations as
        /// listed here: https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Increment,_Decrement. 
        /// 
        ///  * If the counter does not exist, and all bytes in expiration value are 1 (0xffffffff), the operation will fail with NOT_FOUND.
        ///  * IF the counter does not exist, and not all bytes are 1, seed the counter with the initial value, expiration date and flags of 0.
        /// </remarks>
        public int? Expiration;

        /// <summary>
        /// For increment/decrement calls, the amount to either add or subtract.   
        /// </summary>
        /// 
        /// <remarks>
        /// https://code.google.com/p/memcached/wiki/BinaryProtocolRevamped#Increment,_Decrement notes that you may not decrement a counter to less than zero.
        /// </remarks>
        public ulong? Amount;

        /// <summary>
        /// For increment/decrement calls, the amount to set for the key if not known and the expiration time is not set to 0xffffffff.
        /// </summary>
        public ulong? InitialValue;

        /// <summary>
        /// The document calls this the verbosity level, but does not provide details.
        /// </summary>
        public int? Verbosity;

        // TODO [Greg 02/15/2015] : Added a temporary constructor to aid testing. Need to come back and fix unpacking in constructor for object graph.
        public MemcachedBinaryExtras()
        {
            
        }

        // TODO [Greg 01/06/2015] : Move the parsing out of the constructor.
		public MemcachedBinaryExtras(ArraySegment<Byte> segment, MagicValue magic, Opcode opcode)
        {
            // Extras are optional, in fact, sometimes even disallowed. Let's slap together some defaults.
            Flags = null;
            Expiration = null;
            Amount = null;
            InitialValue = null;
            Verbosity = null;
            
			if (segment.Count > 0)
            {
                // Different opcodes have different flag requirements depending on message direction.
                if (magic == MagicValue.Received)
                {
					ParseReceived(segment, opcode);
                }
                else
                {
					ParseRequest(segment, opcode);
                }
            }
        }

		private void ParseRequest(ArraySegment<Byte> segment, Opcode opcode)
        {
			var rawData = segment.Array; 
			var offset = segment.Offset;

            // Set/Add/Replace support both flags (first 4 bytes) and expiration (second 4).
            if ((opcode == Opcode.Set || opcode == Opcode.SetQ || opcode == Opcode.Add || opcode == Opcode.AddQ ||
				opcode == Opcode.Replace || opcode == Opcode.ReplaceQ) && segment.Count >= 4)
            {
				Flags = BinaryHelper.DecodeInt32(rawData, offset);

				if (segment.Count >= 8)
                {
					Expiration = BinaryHelper.DecodeInt32(rawData, offset+=4);
                }
            }
            
            // Increment supports an amount (first 8 bytes), initial value (second 8) and an expiration (last 4 bytes). 
            if ((opcode == Opcode.Increment || opcode == Opcode.IncrementQ || opcode == Opcode.Decrement ||
				opcode == Opcode.DecrementQ) && segment.Count >= 8)
            {
				Amount = BinaryHelper.DecodeUInt64(rawData, offset);

				if (segment.Count >= 16)
                {
					InitialValue = BinaryHelper.DecodeUInt64(rawData, offset+=8);
                }

				if (segment.Count >= 20)
                {
					Expiration = BinaryHelper.DecodeInt32(rawData, offset+=8);
                }
            }

            // Flush supports an optional 4 byte expiration time. This is when the flush will occur.
			if ((opcode == Opcode.Flush || opcode == Opcode.FlushQ) && segment.Count >= 4)
            {
				Expiration = BinaryHelper.DecodeInt32(rawData, offset);
            }
            
            // Verbosity supports an optional 4 byte verbosity argument. This is undocumented.
			if (opcode == Opcode.Verbosity && segment.Count >= 4)
            {
				Verbosity = BinaryHelper.DecodeInt32(rawData, offset);
            }

            // Get and Touch supports an expiration time.
			if ((opcode == Opcode.Gat || opcode == Opcode.Gatq) && segment.Count >= 4)
            {
				Expiration = BinaryHelper.DecodeInt32(rawData, offset);
            }
        }

		private void ParseReceived(ArraySegment<Byte> segment, Opcode opcode)
        {
			var rawData = segment.Array;

            // Get commands support the "flags" value - this is opaque to servers.
			if ((opcode == Opcode.Get || opcode == Opcode.GetQ || opcode == Opcode.GetK || opcode == Opcode.GetKq) && segment.Count >= 4)
            {
				Flags = BinaryHelper.DecodeInt32(rawData, segment.Offset);
            }
        }

        public override string ToString()
        {
			var stringBuilder = new StringBuilder().Append("[Extras");

            if (Flags.HasValue)
            {
				stringBuilder.Append(" :: Flags: ")
                             .Append(Flags);
            }

            if (Expiration.HasValue)
            {
				stringBuilder.Append(" :: Expiration: ")
                             .Append(Expiration);
            }

            if (Amount.HasValue)
            {
				stringBuilder.Append(" :: Amount: ")
                             .Append(Amount);
            }

            if (InitialValue.HasValue)
            {
				stringBuilder.Append(" :: InitialValue: ")
                             .Append(InitialValue);
            }

            if (Verbosity.HasValue)
            {
				stringBuilder.Append(" :: Verbosity: ")
                             .Append(Verbosity);
            }

            return stringBuilder.Append("]").ToString();
        }
    }
}