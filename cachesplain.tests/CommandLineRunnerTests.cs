/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

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
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);
        }

        /// <summary>
        /// Tests the case where we're given a blank port string to parse. This should also return
        /// an empty enum as there's still nothing to parse.
        /// </summary>
        [Test]
        public void ParsePortsForEmptyInput()
        {
            var parsed = App.ParsePorts("");
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);

            parsed = App.ParsePorts("        ");
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);
        }

        /// <summary>
        /// Tests the case where we're given a non-integer in our string to parse.
        /// </summary>
        [Test]
        public void ParsePortsForNonIntegerInput()
        {
            // If all we've got is bogus input, we should get back an empty list of ports.
            var parsed = App.ParsePorts("I,Love,Cheese");
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);

            // If we've got integers buried in cruft, make sure we pull them out properly.
            var ordinalParsed = App.ParsePorts("some,3,stuff,4,here").ToList();
            Assert.That(ordinalParsed, Is.Not.Null);
            Assert.That(2, new EqualConstraint(ordinalParsed.Count));

            Assert.That(3, new EqualConstraint(ordinalParsed[0]));
            Assert.That(4, new EqualConstraint(ordinalParsed[1]));

            // Partial fragments with numbers are not parseable.
            parsed = App.ParsePorts("99redballoons");
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);
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

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);
        }

        /// <summary>
        /// Tests the case where someone decides to be clever and passes in a string composed solely
        /// of commas. It's commas all the way down man.
        /// </summary>
        [Test]
        public void ParsePortsForOnlyCommas()
        {
            var parsed = App.ParsePorts(",,,,,");

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.Empty);
        }

        /// <summary>
        /// Tests the case where we're handed a port range.
        /// </summary>
        [Test]
        public void ParsePortsForPortRange()
        {
            var parsed = App.ParsePorts("11211,11212,11213...11216,11217,11218").ToList();

            Assert.That(parsed.Count(), new EqualConstraint(8));
            Assert.That(string.Join(",", parsed), new EqualConstraint("11211,11212,11213,11214,11215,11216,11217,11218"));
        }

        /// <summary>
        /// Tests the case where we're handed a port range, but someone gets the order backwards.
        /// </summary>
        [Test]
        public void ParsePortsForBackwardsRange()
        {
            var parsed = App.ParsePorts("11211,11212,11216...11213,11217,11218").ToList();

            Assert.That(parsed.Count(), new EqualConstraint(8));
            Assert.That(string.Join(",", parsed), new EqualConstraint("11211,11212,11213,11214,11215,11216,11217,11218"));
        }

        /// <summary>
        /// Tests the case where we're handed an invalid port range.
        /// </summary>
        [Test]
        public void ParsePortsForInvalidRange()
        {
            var parsed = App.ParsePorts("11211,11212,11216...abcd,11217,11218").ToList();

            Assert.That(parsed.Count(), new EqualConstraint(4));
            Assert.That(string.Join(",", parsed), new EqualConstraint("11211,11212,11217,11218"));
        }

        /// <summary>
        /// Tests the case where we wind up parsing a null input. This should return null.
        /// </summary>
        [Test]
        public void TryParseNullableIntForNullInput()
        {
            Assert.That(App.TryParseNullableInt(null), Is.Null);
        }

        /// <summary>
        /// Tests the case where we wind up parsing blank input. This should return null.
        /// </summary>
        [Test]
        public void TryParseNullableIntForEmptyInput()
        {
            Assert.That(App.TryParseNullableInt(""), Is.Null);
            Assert.That(App.TryParseNullableInt("   "), Is.Null);
        }

        /// <summary>
        /// Tests the case where we wind up parsing a non-numerical input. This should return null as it's not an integer.
        /// </summary>
        [Test]
        public void TryParseNullableIntForNonNumericInput()
        {
            Assert.That(App.TryParseNullableInt("Hello World"), Is.Null);
            Assert.That(App.TryParseNullableInt("Hello 1234 World"), Is.Null);
        }

        /// <summary>
        /// Tests what happens when someone tries to feed us a float. Still not integers, so we'll return null. 
        /// Being that we're not John Malkovich, there's probably not a 7.5th port...
        /// </summary>
        [Test]
        public void TryParseNullableIntNonIntegerInput()
        {
            Assert.That(App.TryParseNullableInt("3.14f"), Is.Null);
            Assert.That(App.TryParseNullableInt("3.14"), Is.Null);
        }

        /// <summary>
        /// Tests the happy path where someone has actually given us an integer input...
        /// </summary>
        [Test]
        public void TryParseNullableIntIntegerInput()
        {
            Assert.That(400, new EqualConstraint(App.TryParseNullableInt("400")));
        }

        /// <summary>
        /// Tests what happens when someone hands us something that's larger than our int max. This should
        /// still be null, since we only parse integers.
        /// </summary>
        [Test]
        public void TryParseNullableIntLongInput()
        {
            Assert.That(App.TryParseNullableInt((int.MaxValue + 5L).ToString()), Is.Null);
        }
    }
}