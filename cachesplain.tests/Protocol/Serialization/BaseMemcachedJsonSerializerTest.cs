/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.IO;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace cachesplain.tests.Protocol.Serialization
{
    /// <summary>
    /// Provides a bit of reusable scaffolding for most of our tests. 
    /// </summary>
    /// 
    /// <typeparam name="T">The type of the serializer to generate. Must have a parameterless constructor.</typeparam>
    public abstract class BaseMemcachedJsonSerializerTest<T> where T : new()
    {
        /// <summary>
        /// Holds a JsonWriter we construct anew for each test.
        /// </summary>
        protected JsonWriter JsonWriter { get; set; }

        /// <summary>
        /// Holds the StringWriter our JsonWriter composes. We need this to dispose at the end of each test.
        /// </summary>
        protected StringWriter StringWriter { get; set; }

        /// <summary>
        /// Holds the StringBuilder we're actually going to write to.
        /// </summary>
        protected StringBuilder StringBuilder { get; set; }

        /// <summary>
        /// Holds the serializer under test.
        /// </summary>
        protected T Serializer { get; set; }

        [SetUp]
        public void SetUp()
        {
            StringBuilder = new StringBuilder();
            StringWriter = new StringWriter(StringBuilder);
            JsonWriter = new JsonTextWriter(StringWriter);   
 
            DoWiring();
        }

        [TearDown]
        public void TearDown()
        {
            JsonWriter.Close();
            StringWriter.Close();
        }

        /// <summary>
        /// Provides an optional method that can be overridden if more complicated wiring is desired.
        /// </summary>
        protected virtual void DoWiring()
        {
            Serializer = new T();
        }

        /// <summary>
        /// Grabs the serialized JSON.
        /// </summary>
        /// 
        /// <returns>The serialized JSON.</returns>
        /// 
        /// <remarks>
        /// This must be called after calling serialize on your serializer: doing so will write the
        /// serialized object to the output buffer used under the hood of this method.
        /// </remarks>
        protected string GetSerializedJson()
        {
            return StringBuilder.ToString();
        }
    }
}
