/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.Linq;
using NUnit.Framework;

namespace cachesplain.tests
{
    [TestFixture]
    public class CommandLineTests
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
    }
}
