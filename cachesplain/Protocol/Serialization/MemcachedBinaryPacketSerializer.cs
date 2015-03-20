/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace cachesplain.Protocol.Serialization
{
    /// <summary>
    /// Provides a manual Json.NET serializer for MemcachedBinaryPackets and the underlying object hierarchy.
    /// </summary>
    /// 
    /// <remarks>
    /// Taking a look at the Json.NET performance tips page at http://www.newtonsoft.com/json/help/html/Performance.htm, it looks like 
    /// there is enough of a difference between the reflection-based serializer and a manual serializer. Since we're going to be doing
    /// this a lot, it's worth hand-crafting. The downside is that this is going to be a tad bit brittle: if anything new gets added
    /// it'll have to also be added to the serializer. 
    /// </remarks>
    /// 
    /// <see cref="MemcachedBinaryPacket"/>
    /// <seealso cref="http://www.newtonsoft.com/json/help/html/Performance.htm"/>
    public class MemcachedBinaryPacketSerializer : BaseMemcachedBinaryObjectSerializer
    {
        /// <summary>
        /// Provides a property for the serializer we're going to use to serialize our collection of operations.
        /// </summary>
        public MemcachedBinaryOperationSerializer OperationSerializer { get; set; }

        /// <summary>
        /// Attempts to serialize a MemcachedBinaryPacket into JSON.
        /// </summary>
        /// 
        /// <param name="packet">The packet to serialize. Must not be null.</param>
        /// 
        /// <returns>A JSON representation of the packet.</returns>
        public string Serialize(MemcachedBinaryPacket packet)
        {
            var builder = new StringBuilder();

            using (var stringWriter = new StringWriter(builder))
            {
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    // TODO [Greg 02/15/2015] : Figure out a null handling policy. Need to figure out consumers first.
                    jsonWriter.WriteStartObject();

                    if (null != packet)
                    {
                        // Take care of all our flat attributes.
                        WriteObject("opCount", packet.OperationCount, jsonWriter);
                        WriteAsTimestamp("time", packet.PacketTime, jsonWriter);
                        WriteObject("source", packet.SourceAddress, jsonWriter);
                        WriteObject("destination", packet.DestinationAddress, jsonWriter);
                        WriteObject("size", packet.PacketSize, jsonWriter);
                        WriteObject("port", packet.Port, jsonWriter);

                        // Serialize our operations.
                        jsonWriter.WritePropertyName("operations");
                        jsonWriter.WriteStartArray();

                        if (null != packet.Operations)
                        {
                            foreach (var operation in packet.Operations)
                            {
                                OperationSerializer.Serialize(operation, jsonWriter);
                            }
                        }

                        jsonWriter.WriteEndArray();
                    }

                    jsonWriter.WriteEndObject();
                }
            }

            return builder.ToString();
        }
    }
}