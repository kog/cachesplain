/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.Collections.Generic;
using cachesplain.Engine;
using NUnit.Framework;

namespace cachesplain.tests
{
    /// <summary>
    /// Tests our CaptureEngine at the unit level.
    /// </summary>
    public class CaptureEngineTests
    {
        /// <summary>
        /// Holds an instance of the class under test.
        /// </summary>
        private CaptureEngine _captureEngine;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _captureEngine = new CaptureEngine(null);
        }

        /// <summary>
        /// Tests what happens when someone gives a null set of input ports of interest. We'll get back a null
        /// here because no ports of interest are in use (because there are none).
        /// </summary>
        [Test]
        public void DetermineRelevantPortForNullPorts()
        {
            Assert.IsNull(_captureEngine.DetermineRelevantPort(3, 4, null));
        }

        /// <summary>
        /// Tests what happens when we get an empty set of input ports of interest. We should get back a null
        /// here since we're not using any of the non existant ports.
        /// </summary>
        [Test]
        public void DetermineRelevantPortForEmptyPorts()
        {
            Assert.IsNull(_captureEngine.DetermineRelevantPort(3, 4, new List<int>()));
        }

        /// <summary>
        /// Tests the happy path of trying to find our relevant port.
        /// </summary>
        [Test]
        public void DetermineRelevantPort()
        {
            // Usually we have one port bound to something local, that no one cares about, and one which is our server port.
            // We wind up having to filter this out since we might be viewing raw traffic from a network appliance or something.
            Assert.AreEqual(11211, _captureEngine.DetermineRelevantPort(32417, 11211, new List<int> { 11211 }));
            Assert.AreEqual(11211, _captureEngine.DetermineRelevantPort(11211, 32417, new List<int> { 11211 }));
        }

        /// <summary>
        /// Tests what happens when we're given a relevant port, but the traffic is not on that port (source or destination).
        /// </summary>
        [Test]
        public void DetermineRelevantPortMiss()
        {
            // Pretend we're only doing traffic on 11213 and we're looking for 11211. We'll get nothing back since the traffic
            // is on the wrong port.
            Assert.IsNull(_captureEngine.DetermineRelevantPort(44444, 11213, new List<int> { 11211 }));
        }

        /// <summary>
        /// Since port 0 isn't real, we tend to ignore it. This is also how we handle nulls (IE: if there's no match).
        /// </summary>
        [Test]
        public void DetermineRelevantPortForZeroInput()
        {
            // We matched, but we'll still get a null since this isn't real.
            Assert.IsNull(_captureEngine.DetermineRelevantPort(0, 0, new List<int> { 0 }));
        }

        /// <summary>
        /// Tests what happens when the source and destination port are the same. This isn't particularly likely, 
        /// but let's test it anyway...
        /// </summary>
        [Test]
        public void DetermineRelevantPortForSamePorts()
        {
            Assert.AreEqual(11213, _captureEngine.DetermineRelevantPort(11213, 11213, new List<int> { 11211, 11212, 11213 }));
        }
    }
}
