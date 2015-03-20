/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using Solenoid.Expressions;

namespace cachesplain.Engine
{
    /// <summary>
    /// Provides a first class, value holding class for our capture options.
    /// </summary>
    public class CaptureOptions
    {
        /// <summary>
        /// Holds the backing member for our Ports property, allowing us to default.
        /// </summary>
        private IEnumerable<int> _ports = new[] {11211};

        /// <summary>
        /// Holds the name of the device we'd like to capture from. This must not
        /// be null. In the event our source type is a PCAP file, this should be
        /// the absolute path to said file.
        /// </summary>
        public String DeviceName { get; set; }

        /// <summary>
        /// Holds the set of ports we'd like to capture from. This will never be
        /// null or empty.
        /// </summary>
        /// 
        /// <remarks>
        /// If not otherwise specified, will be the port 11211.
        /// </remarks>
        public IEnumerable<int> Ports
        {
            get { return _ports; }
            set { _ports = value; }
        }

        /// <summary>
        /// Holds the source of the capture. Must not be null, will not be
        /// defaulted.
        /// </summary>
        public CaptureSource Source { get; set; }

        /// <summary>
        /// Holds the "raw" (unparsed) version of the user specified application-level filter expression.
        /// May be null if no filtering is configured.
        /// </summary>
        public String RawFilterExpression { get; set; }

        // TODO [Greg 01/18/2015] : This will probably need to be removed in later refactoring.

        /// <summary>
        /// Holds the parsed version of the user specified application-level filter expression. This
        /// may be null if no filtering is configured, of if the given expression could not be compiled.
        /// </summary>
        /// 
        /// <remarks>
        /// The given expression is run against packets after they're passed to see if they meet a given 
        /// criteria (IE: opcode is x, is a request). If no  expression is given, any packet that is 
        /// parseable will be logged. If the expression cannot be evaluated for some reason, it will be ignored.
        /// </remarks>
        public IExpression FilterExpression { get; set; }
    }
}