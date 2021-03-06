﻿/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using cachesplain.Engine;
using Mono.Options;
using NLog;
using SharpPcap;

namespace cachesplain
{
    // TODO [Greg 01/18/2015] : Slap under test

    public class App
    {
        /// <summary>
        /// Holds whether or not the user needs some help running the app - either requested, or due to missing parameters.
        /// </summary>
        private static bool _needsHelp;

        /// <summary>
        /// Holds whether or not the user would like to enumerate the devices available for sniffing.
        /// </summary>
        /// 
        /// <remarks>
        /// Please note that this takes precedence over both PCAP and live capture modes.
        /// </remarks>
        private static bool _enumerateDevices;

        /// <summary>
        /// Holds the options we want to use when doing our capture.
        /// </summary>
        private static readonly CaptureOptions Options = new CaptureOptions();

        /// <summary>
        /// Holds a <see cref="NLog.Logger"/> to assist with debugging etc.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Holds the regex we use for parsing our port range pattern.
        /// </summary>
        private static readonly Regex PortRegex = new Regex(@"(\d+)\.\.\.(\d+)");

        public static void Main(string[] args)
        {
            using (var shutdownManualResetEvent = new ManualResetEvent(false))
            {
                ParseArgs(args);

                using (var captureEngine = new CaptureEngine {CaptureOptions = Options})
                {
                    // Start ze engine.
                    captureEngine.Start();

                    // Wire up our shutdown.
                    SetSigtermHook(captureEngine, shutdownManualResetEvent);
                    shutdownManualResetEvent.WaitOne();
                }
            }
        }

        /// <summary>
        /// Provides a convenience method to parse our inbound command line args.
        /// </summary>
        /// 
        /// <param name="args">The args string from the main entry point.</param>
        public static void ParseArgs(IEnumerable<string> args)
        {
            var options = GetOptions();
            try
            {
                options.Parse(args);

                // If they've asked for help, give it to them...
                if (_needsHelp)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                }

                if (_enumerateDevices)
                {
                    foreach (var device in CaptureDeviceList.Instance)
                    {
                        Console.WriteLine(device);
                    }

                    Environment.Exit(0);
                }

                if (String.IsNullOrEmpty(Options.DeviceName))
                {
                    Console.WriteLine("Error: no interface name or PCAP file has been provided.");
                    Console.WriteLine("");
                    options.WriteOptionDescriptions(Console.Out);

                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                // If they've given us bogus command line options, tell them what happened and bail.
                Console.WriteLine("Looks like some of the parameters are invalid: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Please try --help for more information.");

                Environment.Exit(1);
            }
        }

        /// <summary>
        /// DWISOTT. Sets a SIGTERM hook for great justice. Shutting down the capture loop may take a while,
        /// so let the users know what's going on to prevent hammering ctrl+c.
        /// </summary>
        /// 
        /// <param name="captureEngine">The capture engine to shut down. Must not be null.</param>
        /// <param name="resetEvent">The manual reset event to signal when we've completed shutdown. Must not be null.</param>
        public static void SetSigtermHook(CaptureEngine captureEngine, ManualResetEvent resetEvent)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Caught SIGTERM, shutting down capture. This may take a moment, please be patient...");
                Logger.Info("Caught SIGTERM, stopping packet capture...");
                eventArgs.Cancel = true;


                captureEngine.Stop();
                Logger.Info("Packet capture stopped. Process shut down.");

                resetEvent.Set();
            };
        }

        /// <summary>
        /// Provides a convenience method to get our <see cref="OptionSet"/>.
        /// </summary>
        /// 
        /// <returns>The command line options for this application.</returns>
        public static OptionSet GetOptions()
        {
            return new OptionSet
            {
                "Usage: cachesplain [OPTIONS]",
                "Start listening for packets on a specific port for a given interface.",
                "",
                "Options:", 
                {"d", "enumerate the network devices available for listening.", v => _enumerateDevices = (v != null)},
                {"i:", "the {NAME} of the interface to listen on. Will be ignored if an input PCAP file is specified.", v => Options.DeviceName = v},
                {"p:", "the {PORT} to listen on. This must be an integer or a range as specified by port1...portN (inclusive). Defaults to 11211 if not otherwise specified. To specify multiple ports or ranges, separate them via commas.", v => Options.Ports = ParsePorts(v)},
                {"h|help", "show this message and exit", v => _needsHelp = (v != null)},
                {"f:", "a PCAP file to use instead of a device. If specified, will be used as the input device instead of specified interface.", v => { Options.Source = (null == v ? CaptureSource.Interface : CaptureSource.PcapFile); Options.DeviceName = v; }},
                {"x:", "An optional app-level filter expression to filter out packets (IE: opcode, magic, flags etc). Please note this is run across a parsed MemcachedBinaryOperation.", v => Options.RawFilterExpression = v}
            };
        }

        /// <summary>
        /// Provides a convenience method to parse the command-line given string of port(s) into an enumable of integers.
        /// </summary>
        /// 
        /// <param name="rawPorts">The raw command-line string of ports to use. Should not be null, should have at least one parseable integer.</param>
        /// 
        /// <returns>An enumerable of zero or more ports that we were able to parse. May be empty, will never be null.</returns>
        public static IEnumerable<int> ParsePorts(string rawPorts)
        {
            var ports = new HashSet<int>();

            if (!String.IsNullOrWhiteSpace(rawPorts))
            {
                ports.UnionWith(rawPorts.Split(',').SelectMany(value => ParsePortItem(value)).Where(x => x > 0));
            }

            return ports;
        }

        /// <summary>
        /// Parses a given potential port in our comma-separated list of ports, performing explosions of ranges as well. 
        /// </summary>
        /// 
        /// <param name="raw"> The raw port item to try and parse.</param>
        /// <returns>An enumerable of the ports we were able to parse. Invalid entries will be denoted as 0 and can be filtered out upstream.</returns>
        /// 
        /// <remarks>
        /// Please note that if a range is detected, this will parse both sides of the expression (v1...v2) and try and use the lower value as
        /// the bottom of the range, and the higher value as the top of the range. Further note that this is inclusive of the range parameters, so
        /// if you specify 11211...11213 it will resolve to three ports: 11211, 11212 and 11213.
        /// </remarks>
        public static IEnumerable<int> ParsePortItem(string raw)
        {
            var expressionMatch = PortRegex.Match(raw);

            if (expressionMatch.Success)
            {
                var lhs = int.Parse(expressionMatch.Groups[1].Value);
                var rhs = int.Parse(expressionMatch.Groups[2].Value);

                return Enumerable.Range(Math.Min(lhs, rhs), Math.Abs(lhs - rhs) + 1);
            }

            return new[] {TryParseNullableInt(raw).GetValueOrDefault()};
        }

        /// <summary>
        /// Provides a utility method to try and parse a given string into an integer. This is mostly a cheap hack to allow for LINQ to be used.
        /// </summary>
        /// 
        /// <param name="raw">The raw string to parse. Should not be null, should be a numerical value.</param>
        /// <returns>The numerical value of the string, if possible, else null.</returns>
        /// 
        /// <remarks>
        /// If this looks familiar, yes it is from http://stackoverflow.com/questions/4961675/select-parsed-int-if-string-was-parseable-to-int.
        /// </remarks>
        public static int? TryParseNullableInt(string raw)
        {
            int value;
            return int.TryParse(raw, out value) ? (int?) value : null;
        }
    }
}