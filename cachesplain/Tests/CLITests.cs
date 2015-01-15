/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// TODO [Greg 01/15/2015] : Move this into a separate testing assembly during the refactor. Was having an odd time with R#, VS and assembly arches, so this is here for now.

namespace cachesplain.Tests
{
    [TestFixture]
    public class CLITests
    {
        /// <summary>
        /// Tests the case where we're given a null port string to parse. This should return 
        /// an empty enum as there's nothing to parse.
        /// </summary>
        [Test]
        public void ParsePortsForNullInput()
        {
            var parsed = App.ParsePorts(null);

            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);
        }

        /// <summary>
        /// Tests the case where we're given a blank port string to parse. This should also return
        /// an empty enum as there's still nothing to parse.
        /// </summary>
        [Test]
        public void ParsePortsForEmptyInput()
        {
            var parsed = App.ParsePorts("");
            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);

            parsed = App.ParsePorts("        ");
            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);    
        }

        /// <summary>
        /// Tests the case where we're given a non-integer in our string to parse.
        /// </summary>
        [Test]
        public void ParsePortsForNonIntegerInput()
        {
            // If all we've got is bogus input, we should get back an empty list of ports.
            var parsed = App.ParsePorts("I,Love,Cheese");
            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);

            // If we've got integers buried in cruft, make sure we pull them out properly.
            var ordinalParsed = App.ParsePorts("some,3,stuff,4,here").ToList();
            Assert.NotNull(ordinalParsed);
            Assert.AreEqual(2, ordinalParsed.Count);

            Assert.AreEqual(3, ordinalParsed[0]);
            Assert.AreEqual(4, ordinalParsed[1]);

            // Partial fragments with numbers are not parseable.
            parsed = App.ParsePorts("99redballoons");
            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);
        }

        /// <summary>
        /// Tests the case when someone hands us the literal string of "0" as our input. Technically
        /// this is an integer, but it isn't a valid port, so we filter it. Coincidentally, this is
        /// how we're handling parse failures (Select on GetValueOrDefault).
        /// </summary>
        [Test]
        public void ParsePortsForZero()
        {
            var parsed = App.ParsePorts("0");

            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);
        }

        /// <summary>
        /// Tests the case where someone decides to be clever and passes in a string composed solely
        /// of commas. It's commas all the way down man.
        /// </summary>
        [Test]
        public void ParsePortsForOnlyCommas()
        {
            var parsed = App.ParsePorts(",,,,,");

            Assert.NotNull(parsed);
            Assert.IsEmpty(parsed);
        }

        /// <summary>
        /// Tests the case where we wind up parsing a null input. This should return null.
        /// </summary>
        [Test]
        public void TryParseNullableIntForNullInput()
        {
            Assert.IsNull(App.TryParseNullableInt(null));
        }

        /// <summary>
        /// Tests the case where we wind up parsing blank input. This should return null.
        /// </summary>
        [Test]
        public void TryParseNullableIntForEmptyInput()
        {
            Assert.IsNull(App.TryParseNullableInt(""));
            Assert.IsNull(App.TryParseNullableInt("   "));
        }

        /// <summary>
        /// Tests the case where we wind up parsing a non-numerical input. This should return null as it's not an integer.
        /// </summary>
        [Test]
        public void TryParseNullableIntForNonNumericInput()
        {
            Assert.IsNull(App.TryParseNullableInt("Hello World"));
            Assert.IsNull(App.TryParseNullableInt("Hello 1234 World"));   
        }

        /// <summary>
        /// Tests what happens when someone tries to feed us a float. Still not integers, so we'll return null. 
        /// Being that we're not John Malkovich, there's probably not a 7.5th port...
        /// </summary>
        [Test]
        public void TryParseNullableIntNonIntegerInput()
        {
            Assert.IsNull(App.TryParseNullableInt("3.14f"));
            Assert.IsNull(App.TryParseNullableInt("3.14"));
        }

        /// <summary>
        /// Tests the happy path where someone has actually given us an integer input...
        /// </summary>
        [Test]
        public void TryParseNullableIntIntegerInput()
        {
            Assert.AreEqual(400, App.TryParseNullableInt("400"));
        }

        /// <summary>
        /// Tests what happens when someone hands us something that's larger than our int max. This should
        /// still be null, since we only parse integers.
        /// </summary>
        [Test]
        public void TryParseNullableIntLongInput()
        {
            Assert.IsNull(App.TryParseNullableInt((int.MaxValue + 5L).ToString()));
        }

        /// <summary>
        /// Tests what happens when someone gives a null set of input ports of interest. We'll get back a null
        /// here because no ports of interest are in use (because there are none).
        /// </summary>
        [Test]
        public void DetermineRelevantPortForNullPorts()
        {
            Assert.IsNull(App.DetermineRelevantPort(3, 4, null));
        }

        /// <summary>
        /// Tests what happens when we get an empty set of input ports of interest. We should get back a null
        /// here since we're not using any of the non existant ports.
        /// </summary>
        [Test]
        public void DetermineRelevantPortForEmptyPorts()
        {
            Assert.IsNull(App.DetermineRelevantPort(3, 4, new List<int>()));
        }

        /// <summary>
        /// Tests the happy path of trying to find our relevant port.
        /// </summary>
        [Test]
        public void DetermineRelevantPort()
        {
            // Usually we have one port bound to something local, that no one cares about, and one which is our server port.
            // We wind up having to filter this out since we might be viewing raw traffic from a network appliance or something.
            Assert.AreEqual(11211, App.DetermineRelevantPort(32417, 11211, new List<int> {11211}));
            Assert.AreEqual(11211, App.DetermineRelevantPort(11211, 32417, new List<int> {11211}));
        }

        /// <summary>
        /// Tests what happens when we're given a relevant port, but the traffic is not on that port (source or destination).
        /// </summary>
        [Test]
        public void DetermineRelevantPortMiss()
        {
            // Pretend we're only doing traffic on 11213 and we're looking for 11211. We'll get nothing back since the traffic
            // is on the wrong port.
            Assert.IsNull(App.DetermineRelevantPort(44444, 11213, new List<int> {11211}));
        }

        /// <summary>
        /// Since port 0 isn't real, we tend to ignore it. This is also how we handle nulls (IE: if there's no match).
        /// </summary>
        [Test]
        public void DetermineRelevantPortForZeroInput()
        {
            // We matched, but we'll still get a null since this isn't real.
            Assert.IsNull(App.DetermineRelevantPort(0, 0, new List<int> {0}));
        }

        /// <summary>
        /// Tests what happens when the source and destination port are the same. This isn't particularly likely, 
        /// but let's test it anyway...
        /// </summary>
        [Test]
        public void DetermineRelevantPortForSamePorts()
        {
            Assert.AreEqual(11213, App.DetermineRelevantPort(11213, 11213, new List<int> { 11211, 11212, 11213}));
        }
    }
}
