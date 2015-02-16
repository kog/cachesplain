/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using cachesplain.Protocol;
using cachesplain.Protocol.Serialization;
using NUnit.Framework;

namespace cachesplain.tests.Protocol.Serialization
{
    /// <summary>
    /// Tests our MemcachedBinaryPacketSerializer at the unit level.
    /// </summary>
    public class MemcachedBinaryPacketSerializerTests
    {
        /// <summary>
        /// Holds an instance of the class under test.
        /// </summary>
        private readonly MemcachedBinaryPacketSerializer _serializer = new MemcachedBinaryPacketSerializer();

        [SetUp]
        public void SetUp()
        {
            _serializer.OperationSerializer = new MemcachedBinaryOperationSerializer
            {
                ExtrasSerializer = new MemcachedBinaryExtrasSerializer(),
                HeaderSerializer = new MemcachedBinaryHeaderSerializer()
            };
        }

        /// <summary>
        /// Tests the case where someone hands us a null packet. We should get back an empty object here.
        /// </summary>
        [Test]
        public void TestSerializeNullPacket()
        {
            Assert.That("{}", Is.EqualTo(_serializer.Serialize(null)));           
        }

        /// <summary>
        /// Tests the case where someone hands us an otherwise empty packet. We'll do some quick defaulting.
        /// This is another one of those "this should never happen, but..." cases.
        /// </summary>
        /// 
        /// <remarks>
        /// There seems to be a difference of opinion between the MS CLR and Mono as to what a new DateTime should give you...
        /// The former gives you -62135568000, which is actually not actually the value quoted at 
        /// https://msdn.microsoft.com/en-us/library/system.datetime.minvalue(v=vs.110).aspx. Mono does the right thing, which 
        /// means this test is somewhat fragile with respect to being cross platform...
        /// </remarks>
        [Test]
        public void TestSerializeEmptyPacket()
        {
            // Set the packet date to a known value to prevent breaking when moving between the MS CLR and Mono.
            var packet = new MemcachedBinaryPacket {PacketTime = new DateTime(1970, 2, 1, 0, 0, 0, DateTimeKind.Utc)};
            Assert.That("{\"opCount\":0,\"time\":2678400,\"source\":null,\"destination\":null,\"size\":0,\"port\":0,\"operations\":[]}", Is.EqualTo(_serializer.Serialize(packet)));            
        }

        /// <summary>
        /// Tests the happy path, where we've got multiple operations in our packet.
        /// </summary>
        [Test]
        public void TestSerialize()
        {           
            var extrasOne = new MemcachedBinaryExtras
            {
                Flags = 42, Expiration = 99, Amount = 8675309, InitialValue = 314159265, Verbosity = 86
            };

            var headerOne = new MemcachedBinaryHeader(new ArraySegment<byte>(new byte[]
            {
                (byte)MagicValue.Received, (byte)Opcode.Set, 
                0x15, 0x15, 
                0xAA, 
                0xFF, 
                0x00, 0x85,                 
                0x45, 0x45, 0x45, 0x45, 
                0x12, 0x12, 0x12, 0x12,                 
                0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99
            }));

            var extrasTwo = new MemcachedBinaryExtras
            {
                Flags = 77,
                Expiration = 88,
                Amount = 4012,
                InitialValue = 2015,
                Verbosity = 444412
            };

            var headerTwo = new MemcachedBinaryHeader(new ArraySegment<byte>(new byte[]
            {
                (byte)MagicValue.Requested, (byte)Opcode.Touch, 
                0x14, 0x14,                
                0xAB,                
                0xFE, 
                0x00, 0x82, 
                0x33, 0x33, 0x33, 0x33, 
                0x11, 0x11, 0x11, 0x11, 
                0x98, 0x98, 0x98, 0x98, 0x98, 0x98, 0x98, 0x98
            }));


            var packet = new MemcachedBinaryPacket
            {
                OperationCount = 99,
                PacketTime = new DateTime(2015, 10, 21, 19, 28, 00, DateTimeKind.Utc),
                SourceAddress = "127.0.0.1",
                DestinationAddress = "255.255.255.255",
                PacketSize = 640,
                Port = 11211
            };

            var operationOne = new MemcachedBinaryOperation(headerOne, extrasOne, "key1");
            var operationTwo = new MemcachedBinaryOperation(headerTwo, extrasTwo, "key2");

            packet.Operations = new List<MemcachedBinaryOperation> {operationOne, operationTwo};

            var serialized = _serializer.Serialize(packet);
            Assert.That("{\"opCount\":99,\"time\":1445455680,\"source\":\"127.0.0.1\",\"destination\":\"255.255.255.255\",\"size\":640,\"port\":11211,\"operations\":[{\"key\":\"key1\",\"header\":{\"magic\":\"Received\",\"opCode\":\"Set\",\"keyLength\":5397,\"extrasLength\":170,\"dataType\":255,\"status\":\"Busy\",\"totalBodyLength\":1162167621,\"opaque\":303174162,\"cas\":11068046444225730969},\"extras\":{\"flags\":42,\"expiration\":99,\"amount\":8675309,\"initialValue\":314159265,\"verbosity\":86}},{\"key\":\"key2\",\"header\":{\"magic\":\"Requested\",\"opCode\":\"Touch\",\"keyLength\":5140,\"extrasLength\":171,\"dataType\":254,\"status\":130,\"totalBodyLength\":858993459,\"opaque\":286331153,\"cas\":10995706271387654296},\"extras\":{\"flags\":77,\"expiration\":88,\"amount\":4012,\"initialValue\":2015,\"verbosity\":444412}}]}", Is.EqualTo(serialized));
        }
    }
}
