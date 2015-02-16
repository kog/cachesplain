/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using cachesplain.Protocol;
using cachesplain.Protocol.Serialization;
using NUnit.Framework;

// TODO [Greg 02/15/2015] : clean this when the constructor is fixed for the class being serialized...

namespace cachesplain.tests.Protocol.Serialization
{
    [TestFixture]
    public class MemcachedBinaryHeaderSerializerTests : BaseMemcachedJsonSerializerTest<MemcachedBinaryHeaderSerializer>
    {
        /// <summary>
        /// Tests the case where we're asked to serialize a null header. In theory this should never happen,
        /// but if it does we want to return an empty object.
        /// </summary>
        [Test]
        public void TestSerializeNullHeader()
        {
            Serializer.Serialize(null, JsonWriter);
            Assert.That("{}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Tests the happy path for the case we've recieved something.
        /// </summary>
        [Test]
        public void TestSerializeForReceived()
        {
            var payload = new byte[]
            {
                // Magic
                (byte)MagicValue.Received, 

                // OpCode
                (byte)Opcode.Set, 

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

            var header = new MemcachedBinaryHeader(new ArraySegment<byte>(payload));
            Assert.That("RX## Set (0x01) KeyLen: 5397, ExtLen: 170, DataType: 255, Status/VBucket: 133, BodyLen: 1162167621, Opaque: 303174162, CAS: 11068046444225730969", Is.EqualTo(header.ToString()));

            Serializer.Serialize(header, JsonWriter);
            Assert.That("{\"magic\":\"Received\",\"opCode\":\"Set\",\"keyLength\":5397,\"extrasLength\":170,\"dataType\":255,\"status\":\"Busy\",\"totalBodyLength\":1162167621,\"opaque\":303174162,\"cas\":11068046444225730969}", Is.EqualTo(GetSerializedJson()));
        }

        /// <summary>
        /// Tests the happy path for the case we've requested something.
        /// </summary>
        [Test]
        public void TestSerializedForRequested()
        {
            var payload = new byte[]
            {
                // Magic
                (byte)MagicValue.Requested, 

                // OpCode
                (byte)Opcode.Touch, 

                // Key length
                0x14, 0x14, 
                
                // Extras length
                0xAB, 
                
                // Data type
                0xFE, 
                
                // Status/VBucket
                0x00, 0x82, 
                
                // Total Body
                0x33, 0x33, 0x33, 0x33, 
                
                // Opaque
                0x11, 0x11, 0x11, 0x11, 
                
                // CAS
                0x98, 0x98, 0x98, 0x98, 0x98, 0x98, 0x98, 0x98
            };

            var header = new MemcachedBinaryHeader(new ArraySegment<byte>(payload));
            Assert.That("TX## Touch (0x1C) KeyLen: 5140, ExtLen: 171, DataType: 254, Status/VBucket: 130, BodyLen: 858993459, Opaque: 286331153, CAS: 10995706271387654296", Is.EqualTo(header.ToString()));

            Serializer.Serialize(header, JsonWriter);
            Assert.That("{\"magic\":\"Requested\",\"opCode\":\"Touch\",\"keyLength\":5140,\"extrasLength\":171,\"dataType\":254,\"status\":130,\"totalBodyLength\":858993459,\"opaque\":286331153,\"cas\":10995706271387654296}", Is.EqualTo(GetSerializedJson()));           
        }
    }
}
