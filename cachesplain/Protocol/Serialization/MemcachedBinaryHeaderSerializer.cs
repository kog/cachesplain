/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using Newtonsoft.Json;

namespace cachesplain.Protocol.Serialization
{
    /// <summary>
    /// Provides a manual Json.NET serializer for Memcached binary operation headers.
    /// </summary>
    public class MemcachedBinaryHeaderSerializer : BaseMemcachedBinaryObjectSerializer
    {
        /// <summary>
        /// Attempts to serialize a header into JSON.
        /// </summary>
        /// 
        /// <param name="header">The header to serialize. Must not be null.</param>
        /// <param name="jsonWriter">The JsonWriter to write to - must be primed and non-null.</param>
        public void Serialize(MemcachedBinaryHeader header, JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();

            // TODO [Greg 02/15/2015] : figure out null handling policy. Need to figure out the consumers first.
            if (null != header)
            {
                WriteAsString("magic", header.Magic, jsonWriter);
                WriteAsString("opCode", header.Opcode, jsonWriter);
                WriteObject("keyLength", header.KeyLength, jsonWriter);
                WriteObject("extrasLength", header.ExtrasLength, jsonWriter);
                WriteObject("dataType", header.DataType, jsonWriter);

                // We have to handle our status differently, depending on whether we're sending or reciving.
                if (header.Magic == MagicValue.Received)
                {
                    // If we've received this packet, then this is actually an enum value.
                    WriteAsString("status", (ResponseStatus) header.StatusOrVbucketId, jsonWriter);
                    WriteObject("vbucketId", null, jsonWriter);
                }
                else
                {
                    // If we've sent it, however it's probably just a VBucket ID.
                    WriteObject("vbucketId", header.StatusOrVbucketId, jsonWriter);
                    WriteObject("status", null, jsonWriter);
                }

                WriteObject("totalBodyLength", header.TotalBodyLength, jsonWriter);
                WriteObject("opaque", header.Opaque, jsonWriter);
                WriteObject("cas", header.Cas, jsonWriter);
            }

            jsonWriter.WriteEndObject();
        }
    }
}