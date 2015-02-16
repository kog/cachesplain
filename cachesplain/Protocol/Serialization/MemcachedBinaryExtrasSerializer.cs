/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using Newtonsoft.Json;

namespace cachesplain.Protocol.Serialization
{
    /// <summary>
    /// Provides a manual Json.NET serializer for Memcached binary extras.
    /// </summary>
    public class MemcachedBinaryExtrasSerializer : BaseMemcachedBinaryObjectSerializer
    {
        /// <summary>
        /// Attempts to serialize extras into JSON.
        /// </summary>
        /// 
        /// <param name="extras">The extras to serialize. Must not be null.</param>
        /// <param name="jsonWriter">The JsonWriter to write to - must be primed and non-null.</param>
        public void Serialize(MemcachedBinaryExtras extras, JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();

            // TODO [Greg 02/15/2015] : figure out null handling policy. Need to figure out the consumers first.
            if (null != extras)
            {
                WriteObject("flags", extras.Flags, jsonWriter);
                WriteObject("expiration", extras.Expiration, jsonWriter);
                WriteObject("amount", extras.Amount, jsonWriter);
                WriteObject("initialValue", extras.InitialValue, jsonWriter);
                WriteObject("verbosity", extras.Verbosity, jsonWriter);
            }
            
            jsonWriter.WriteEndObject();
        }
    }
}