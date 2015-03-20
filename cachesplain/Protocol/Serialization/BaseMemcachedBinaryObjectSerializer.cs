/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using Newtonsoft.Json;

namespace cachesplain.Protocol.Serialization
{
    /// <summary>
    /// Provides a base set of functionality for serializing our Memcached objects.
    /// </summary>
    public abstract class BaseMemcachedBinaryObjectSerializer
    {
        /// <summary>
        /// Provides a convenience method to write an object with a preceeding field name.
        /// </summary>
        /// 
        /// <param name="fieldName">The field name to write. Must not be blank, must be a valid JSON field name.</param>
        /// <param name="value">The value to write. May be null.</param>
        /// <param name="writer">The JsonWriter to write with. Must not be null, must be valid and fully primed.</param>
        public void WriteObject(string fieldName, object value, JsonWriter writer)
        {
            if (null != writer)
            {
                writer.WritePropertyName(fieldName);
                writer.WriteValue(value);
            }
        }

        /// <summary>
        /// Provides a conveneince method to write an object via ToString, with a preceeding field name.
        /// </summary>
        /// 
        /// <param name="fieldName">The field name to write. Must not be blank, must be a valid JSON field name.</param>
        /// <param name="value">The value to write. May be null.</param>
        /// <param name="writer">The JsonWriter to write with. Must not be null, must be valid and fully primed.</param>
        public void WriteAsString(string fieldName, object value, JsonWriter writer)
        {
            if (null != writer)
            {
                writer.WritePropertyName(fieldName);

                if (null == value)
                {
                    writer.WriteNull();
                }
                else
                {
                    writer.WriteValue(value.ToString());
                }
            }
        }

        /// <summary>
        /// Provides a convenience method to write a DateTime as a Timestamp.
        /// </summary>
        /// 
        /// <param name="fieldName">The field name to write. Must not be blank, must be a valid JSON field name.</param>
        /// <param name="date">The date to write.</param>
        /// <param name="writer">The JsonWriter to write with. Must not be null, must be valid and fully primed.</param>
        /// 
        /// <remarks>
        /// <para>
        /// Json.NET provides handling behavior that either uses ISO-8601 (default) or the Microsoft Date DOM object.
        /// Traditionally people tend to use "timestamps" in JSON (seconds since epoch), so this method is a quick
        /// implementation. 
        /// </para>
        /// 
        /// <para>
        /// This implementation is lifted wholesale from http://stackoverflow.com/questions/13547394/convert-human-readable-date-into-an-epoch-time-stamp.
        /// It will become substantially prettier when upgrading to .NET 4.6: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/announcing-net-2015-preview-a-new-era-for-net.aspx
        /// (support for "Unix Time").  
        /// </para>
        /// </remarks>
        public void WriteAsTimestamp(string fieldName, DateTime date, JsonWriter writer)
        {
            if (null != writer)
            {
                writer.WritePropertyName(fieldName);

                // TODO [Greg 02/15/2015] : Move to DateTimeOffset when upgrading to 4.6.
                writer.WriteValue((date.ToUniversalTime().Ticks - 621355968000000000)/10000000);
            }
        }
    }
}