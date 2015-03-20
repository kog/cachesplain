/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using cachesplain.Protocol;
using cachesplain.Protocol.Serialization;
using NUnit.Framework;

namespace cachesplain.tests.Protocol.Serialization
{
    [TestFixture]
    public class MemcachedBinaryExtrasSerializerTests : BaseMemcachedJsonSerializerTest<MemcachedBinaryExtrasSerializer>
    {
        /// <summary>
        /// Tests what happens when we're asked to serialize a null set of extras. We should return an empty object here. 
        /// </summary>
        [Test]
        public void TestSerializeNullExtras()
        {
            Serializer.Serialize(null, JsonWriter);
            Assert.That("{}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Make sure that we handle null values properly.
        /// </summary>
        [Test]
        public void TestSerializeEmptyObject()
        {
            var extras = new MemcachedBinaryExtras();
            Serializer.Serialize(extras, JsonWriter);

            Assert.That("[Extras]", Is.EqualTo(extras.ToString()));
            Assert.That("{\"flags\":null,\"expiration\":null,\"amount\":null,\"initialValue\":null,\"verbosity\":null}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Tests the happy path of all properties being set. This actually should never happen during operation in
        /// the real world, but it's good to cover nonetheless as it tells us we can serialize everything properly.
        /// </summary>
        [Test]
        public void TestSerialize()
        {
            var extras = new MemcachedBinaryExtras
            {
                Flags = 42,
                Expiration = 99,
                Amount = 8675309,
                InitialValue = 314159265,
                Verbosity = 86
            };

            Serializer.Serialize(extras, JsonWriter);

            Assert.That("[Extras :: Flags: 42 :: Expiration: 99 :: Amount: 8675309 :: InitialValue: 314159265 :: Verbosity: 86]", Is.EqualTo(extras.ToString()));
            Assert.That("{\"flags\":42,\"expiration\":99,\"amount\":8675309,\"initialValue\":314159265,\"verbosity\":86}", Is.EqualTo(GetSerializedJson()));
        }
    }
}