/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using cachesplain.Protocol;
using cachesplain.Protocol.Serialization;
using NUnit.Framework;

namespace cachesplain.tests.Protocol.Serialization
{
    [TestFixture]
    public class MemcachedBinaryOperationSerializerTests : BaseMemcachedJsonSerializerTest<MemcachedBinaryOperationSerializer>
    {
        protected override void DoWiring()
        {
            base.DoWiring();

            // We'll need an extra pair of serializers here...
            Serializer.ExtrasSerializer = new MemcachedBinaryExtrasSerializer();
            Serializer.HeaderSerializer = new MemcachedBinaryHeaderSerializer();
        }

        /// <summary>
        /// Tests what happens when we're asked to serialize a null operation. We should return an empty object here.
        /// </summary>
        [Test]
        public void TestSerializeNullOperation()
        {
            Serializer.Serialize(null, JsonWriter);
            Assert.That("{}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Tests the case when we're asked to serialize an empty operation. This is another one of those famous
        /// "this should never happen" scenarios... 
        /// </summary>
        [Test]
        public void TestSerializeEmptyObject()
        {
            var operation = new MemcachedBinaryOperation(null, null, null);

            Serializer.Serialize(operation, JsonWriter);

            // The writeline here is going to look a liiiitle funky...
            Console.WriteLine(" ->  %% ");
            Assert.That("{\"key\":null,\"header\":{},\"extras\":{}}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Tests serialization for the happy path.
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

            var header = new byte[]
            {
                // Magic
                (byte) MagicValue.Received,

                // OpCode
                (byte) Opcode.Set,

                // Key length
                0x15, 0x15,

                // Extras length
                0xAA,

                // Data type
                0xFF,

                // Status/VBucket
                0x00, 0x85,

                // Total Body
                0x45, 0x45, 0x45, 0x45,

                // Opaque
                0x12, 0x12, 0x12, 0x12,

                // CAS
                0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99
            };

            var operation = new MemcachedBinaryOperation(new MemcachedBinaryHeader(new ArraySegment<byte>(header)), extras, "SomeKey");
            Serializer.Serialize(operation, JsonWriter);

            Assert.That("RX## Set (0x01) KeyLen: 5397, ExtLen: 170, DataType: 255, Status/VBucket: 133, BodyLen: 1162167621, Opaque: 303174162, CAS: 11068046444225730969 -> SomeKey", Is.EqualTo(operation.ToString()));
            Assert.That("{\"key\":\"SomeKey\",\"header\":{\"magic\":\"Received\",\"opCode\":\"Set\",\"keyLength\":5397,\"extrasLength\":170,\"dataType\":255,\"status\":\"Busy\",\"vbucketId\":null,\"totalBodyLength\":1162167621,\"opaque\":303174162,\"cas\":11068046444225730969},\"extras\":{\"flags\":42,\"expiration\":99,\"amount\":8675309,\"initialValue\":314159265,\"verbosity\":86}}", Is.EqualTo(GetSerializedJson()));
        }
    }
}