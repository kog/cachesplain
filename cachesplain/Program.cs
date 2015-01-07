/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SharpPcap;
using cachesplain.Protocol;
using PacketDotNet;
using NLog;
using Mono.Options;
using SharpPcap.LibPcap;
using System.IO;
using Solenoid.Expressions;

// TODO [Greg 12/31/2014] : Needs some refactoring love... Better namespacing/separation etc. 

namespace cachesplain 
{
	class App
	{
		/// <summary>
		/// Holds the name of the device we want to listen on. This is specified via the starting options.
		/// </summary>
		/// 
		/// <remarks>
		/// While it's possible to analyze a pre-recorded PCAP file, a user may opt to capture live traffic from a device. 
		/// This can be handy for developing/debugging applications in a pre-production environment, or merely for the sake
		/// of curiosity (IE: what is my code doing?). Please note that this is mutually exclusive of reading an input PCAP file.
		/// </remarks>
		private static string DeviceName;

		/// <summary>
		/// Holds the port we want to listen on. This defaults to 11211 (the usual Memcached port), but can be specified via the starting options.
		/// </summary>
		private static int Port = 11211;

		/// <summary>
		/// Holds whether or not the user needs some help running the app - either requested, or due to missing parameters.
		/// </summary>
		private static bool NeedsHelp;

        /// <summary>
        /// Holds whether or not the user would like to enumerate the devices available for sniffing.
        /// </summary>
        /// 
        /// <remarks>
        /// Please note that this takes precedence over both PCAP and live capture modes.
        /// </remarks>
        private static bool EnumerateDevices;

		/// <summary>
		/// Holds an optional path to an input PCAP file.
		/// </summary>
		/// 
		/// <remarks>
		/// A user may choose to analyze a PCAP file instead of sniffing traffic from a live device for a variety of reasons. They
		/// may not have access to run captures, they may not have .NET available on the source machine/appliance or they may be
		/// doing something like historical research.
		/// 
		/// Providing an input PCAP file allows this app to analyze that instead, subject to the same caveats you'd see from
		/// a live device (IE: NULL on BSD/OSX is still not supported).
		/// </remarks>
		private static String PCAPFile;

		/// <summary>
		/// Holds the user specified, application level filter for packets. 
		/// </summary>
		/// 
		/// <remarks>
		/// The given expression is run against packets after they're passed to see if they meet a given criteria (IE: opcode is x, is a request). If no 
		/// expression is given, any packet that is parseable will be logged. If the expression cannot be evaluated for some reason, it will be ignored.
		/// </remarks>
		private static String RawFilterExpression;

        /// <summary>
        /// Holds a <see cref="NLog.Logger"/> to assist with debugging etc.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void Main(string[] args)
		{
			ParseArgs(args);

			var device = GetDevice();
	
			SetSigtermHook(device);
			StartCapture(device);
		}

		/// <summary>
		/// Starts capture on the selected input device.
		/// </summary>
		/// 
		/// <param name="device">The device to start capturing on. Must not be null.</param>
		static void StartCapture(ICaptureDevice device)
		{			
			if (String.IsNullOrEmpty(PCAPFile))
			{
				//TODO [Greg 12/30/2014] : Maybe add an advanced configuration option/something into app.config to use a non-promiscuous mode?

				// If we're a live interface, we're going to drop into promiscuous mode. Most people will be in a switched environment, so this won't be a problem.
				Logger.Info("Configuring PCAP");
				device.Open(DeviceMode.Promiscuous, 10000);
				Logger.Info("Listening to interface {0} on port {1}", DeviceName, Port);
			}
			else
			{
				Logger.Info("Reading input PCAP file");
				device.Open();
			}				

			// Try and compile our expression.
			IExpression filterExpression = null;
			if (!String.IsNullOrEmpty(RawFilterExpression))
			{ 
				try
				{
					filterExpression = Expression.Parse(RawFilterExpression);
				}
				catch(Exception ex)
				{
					Logger.Warn("Failed to parse expression {0}: {1}. Ignoring...", RawFilterExpression, ex.Message);
				}
			}

			// TODO [Greg 12/31/2014] : Move handling of the packets elsewhere.
			device.OnPacketArrival += (sender, e) =>  
			{
				HandlePacket(e, filterExpression);
			};

			Console.WriteLine("Starting capture... SIGTERM to quit.");
			device.StartCapture();
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
				if(NeedsHelp)
				{
					options.WriteOptionDescriptions(Console.Out);
					Environment.Exit(0);
				}

                if (EnumerateDevices)
                {
                    foreach (var device in CaptureDeviceList.Instance)
                    {
                        Console.WriteLine(device);
                    }

                    Environment.Exit(0);
                }

				if (String.IsNullOrEmpty(DeviceName) && String.IsNullOrEmpty(PCAPFile))
				{
					Console.WriteLine("Error: no interface name or PCAP file has been provided.");
					Console.WriteLine("");
					options.WriteOptionDescriptions(Console.Out);

					Environment.Exit(1);
				}
			}
			catch(Exception ex) 
			{
				// If they've given us bogus command line options, tell them what happened and bail.
				Console.WriteLine("Looks like some of the parameters are invalid: ");
				Console.WriteLine(ex.Message);
				Console.WriteLine("Please try --help for more information.");

				Environment.Exit(1);
			}
		}

		/// <summary>
		/// Provides a convenience method for finding our device by name.
		/// </summary>
		/// 
		/// <returns>The device, if known, else Environment.Exit.</returns>
		public static ICaptureDevice GetDevice()
		{
			ICaptureDevice device;

			// If no one specified a PCAP file, default to a device.
			if (String.IsNullOrEmpty(PCAPFile)) 
			{
				Logger.Info("Looking for target device {0}...", DeviceName);
				device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Equals (DeviceName));

				// Make sure we've got the device.
				if (null == device) 
				{
					Console.WriteLine("Unable to find device {0}, exiting...", DeviceName);
					Environment.Exit(1);
				}
			} 
			else 
			{
				// Try and read PCAP dump.
				Logger.Info("Attempting to use PCAP file {0}...", PCAPFile);

				if(!File.Exists(PCAPFile))
				{
					Console.WriteLine("Unable to find PCAP file {0}, exiting...", PCAPFile);
					Environment.Exit(1);
				}

				device = new CaptureFileReaderDevice(PCAPFile);
			}
				
			return device;
		}

		/// <summary>
		/// DWISOTT. Sets a SIGTERM hook for great justice. Shutting down the capture loop may take a while,
		/// so let the users know what's going on to prevent hammering ctrl+c.
		/// </summary>
		/// 
		/// <param name="device">The device we're attempting to capture from.</param>
		public static void SetSigtermHook(ICaptureDevice device)
		{
			Console.CancelKeyPress += (sender, eventArgs) =>  
			{
				Console.WriteLine("Caught SIGTERM, shutting down capture. This may take a moment, please be patient...");
				Logger.Info("Caught SIGTERM, stopping packet capture...");
				eventArgs.Cancel = true;

				device.StopCapture();
				device.Close();
				Logger.Info("Packet capture stopped. Process shut down.");
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
                { "d", "enumerate the network devices available for listening.", v => EnumerateDevices = (v != null) },
				{ "i:", "the {NAME} of the interface to listen on. Will be ignored if an input PCAP file is specified.", v => DeviceName = v },
				{ "p:",  "the {PORT} to listen on.\n" +  "this must be an integer. Defaults to 11211 if not otherwise specified.", (int v) => Port = v },
				{ "h|help",  "show this message and exit",  v => NeedsHelp = (v != null) },
				{ "f:", "a PCAP file to use instead of a device. If specified, will be used as the input device instead of specified interface.", v => PCAPFile = v},
				{ "x:", "An optional app-level filter expression to filter out packets (IE: opcode, magic, flags etc). Please note this is run across a parsed MemcachedBinaryOperation.", v => RawFilterExpression = v }
			};
		}

		// TODO: [Greg 01/02/2015] - This is a fairly low level networking thing... Should move all the actual packet abstractions into something dedicated to them.

		/// <summary>
		/// Handles an incoming packet, attempting to parse it into something useful.
		/// </summary>
		/// 
		/// <param name="eventArgs">The <see cref="CaptureEventArgs"/> from the packet capture event. Must not be null.</param>
		/// <param name="filterExpression">An optional, user-specified filter expression. May be null if no filter is to be applied.</param>
		/// 
		/// <remarks>
		/// 
		/// </remarks>
		public static void HandlePacket(CaptureEventArgs eventArgs, IExpression filterExpression)
		{
			if(eventArgs.Packet.LinkLayerType != LinkLayers.Null) 
			{
				var parsed = Packet.ParsePacket(eventArgs.Packet.LinkLayerType, eventArgs.Packet.Data);
				var tcpPacket = (TcpPacket)parsed.Extract(typeof(TcpPacket));

				if(null != tcpPacket && null != tcpPacket.PayloadData && tcpPacket.PayloadData.Length > 0 && (tcpPacket.DestinationPort == Port || tcpPacket.SourcePort == Port)) 
				{
					var ipPacket = (IpPacket)tcpPacket.ParentPacket;
				    var packet = new MemcachedBinaryPacket(tcpPacket.PayloadData)
				    {
				        PacketTime = eventArgs.Packet.Timeval.Date,
				        SourceAddress = ipPacket.SourceAddress.ToString(),
				        DestinationAddress = ipPacket.DestinationAddress.ToString(),
				        PacketSize = tcpPacket.PayloadData.LongLength,
				        Port = Port
				    };


				    var i = 0;

					foreach(var operation in packet.Operations) 
					{

						// If we've got a filter expression, see what it does...
						try
						{	
							if (RawFilterExpression == null || (bool)filterExpression.GetValue(operation))
							{
								// TODO: [Greg 01/02/2015] - Figure out something better to do with the packets.
								LogPacket(++i, packet.Operations.Count(), packet, operation);
							}
						}
						catch (Exception ex)
						{
							// TODO [Greg 12/27/2014] : Need to come back and re-think this. If we've got an invalid expression, could lead to a lot of log spam.
							Logger.Warn("Ignoring invalid expression \"{0}\": {1}...", RawFilterExpression, ex.Message);
							LogPacket(++i, packet.Operations.Count(), packet, operation);
						}
					}
				}
			}
			else 
			{
				Logger.Warn("Received packet on LinkLayerType Null, which is not currently supported. Dropping.");
			}
		}

		// TODO [Greg 01/02/2015] - This should go away when we actually do something with our packets...

		/// <summary>
		/// Provides a convenience method to log a given packet.
		/// </summary>
		/// 
        /// <param name="index">The index of the command within the packet: Memcached tends to pipeline operations, stuffing multiple operations into a single packet.</param>
        /// <param name="count">The total number of operations in the packet.</param>
        /// <param name="packet">The packet, containing one or more operations. Must not be null. Used for aggregate stats (packet size, operation count etc).</param>
        /// <param name="operation">The specific operation to log. Must not be null.</param>
		/// 
		/// <remarks>
		/// At present we're not really doing much with packets - merely logging them via NLog. Future versions will most likely provide persistence.
		/// </remarks>
		public static void LogPacket(int index, int count, MemcachedBinaryPacket packet, MemcachedBinaryOperation operation)
		{
			Logger.Debug("{0} -> {1} {2} {3} ({5}/{6}: {4}k) Packet {7}: {8} {9} {10} {11}", 
				packet.SourceAddress, packet.DestinationAddress, packet.Port, packet.PacketTime.ToUniversalTime(), packet.PacketSize, 
				index, count, operation.Magic, operation.Opcode, 
				(String.IsNullOrWhiteSpace(operation.Key)) ? "<key omitted>" : operation.Key.ToString(CultureInfo.InvariantCulture), 
				(operation.Extras != null) ? operation.Extras.ToString() : "", 
				(operation.Magic == MagicValue.Received) ? "= " + (ResponseStatus)operation.Header.StatusOrVbucketId : "");
		}
	}
}