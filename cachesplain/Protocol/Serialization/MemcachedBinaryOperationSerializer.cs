/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using Newtonsoft.Json;

namespace cachesplain.Protocol.Serialization
{
    /// <summary>
    /// Provides a manual Json.NET serializer for MemcachedBinaryOperations and their associated object hierarchy.
    /// </summary>
    public class MemcachedBinaryOperationSerializer : BaseMemcachedBinaryObjectSerializer
    {
        /// <summary>
        /// Holds our composed serializer for our headers.
        /// </summary>
        public MemcachedBinaryHeaderSerializer HeaderSerializer { get; set; }

        /// <summary>
        /// Holds our composed serializer for our extras.
        /// </summary>
        public MemcachedBinaryExtrasSerializer ExtrasSerializer { get; set; }

        /// <summary>
        /// Attempts to serialize an operation into JSON.
        /// </summary>
        /// 
        /// <param name="operation">The operation to serialize. Must not be null.</param>
        /// <param name="jsonWriter">The JsonWriter to write to - must be primed and non-null.</param>
        public void Serialize(MemcachedBinaryOperation operation, JsonWriter jsonWriter)
        {
            // Start our object.
            jsonWriter.WriteStartObject();

            // TODO [Greg 02/15/2015] : figure out null handling policy. Need to figure out the consumers first.
            if (null != operation)
            {
                // Write our flat fields.
                WriteObject("key", operation.Key, jsonWriter);

                // Write out our header data.
                jsonWriter.WritePropertyName("header");             
                HeaderSerializer.Serialize(operation.Header, jsonWriter);

                // Write out whatever extras we've got. These are optional, and may actually be null...
                jsonWriter.WritePropertyName("extras");
                ExtrasSerializer.Serialize(operation.Extras, jsonWriter); 
            }

            // End it.
            jsonWriter.WriteEndObject();
        }
    }
}
