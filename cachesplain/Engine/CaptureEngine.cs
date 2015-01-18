/**
 * CacheSplain: A tool for helping with Memcached
 *
 * This source is licensed under the MIT license. Please see the distributed LICENSE.TXT for details.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using cachesplain.Protocol;
using NLog;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using Solenoid.Expressions;

namespace cachesplain.Engine
{
    // TODO [Greg 01/18/2015] : Pardon the dust, starting the refactoring...
    
    public class CaptureEngine : IDisposable
    {
        /// <summary>
        /// Holds the set of options to use during our capture.
        /// </summary>
        public CaptureOptions Options { get; set; }

        /// <summary>
        /// Holds the device we're going to try and capture from.
        /// </summary>
        public ICaptureDevice Device { get; set; }

        /// <summary>
        /// Holds a <see cref="NLog.Logger"/> to assist with debugging etc.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public CaptureEngine(CaptureOptions options)
        {
            Options = options;
        }

        public void Start()
        {
            var filterExpression = CompileFilterExpression();
            Device = GetDevice(Options);

            StartCapture(Device, Options, filterExpression);
        }

        public void Stop()
        {
            Device.StopCapture();
            Device.Close();    
        }

        /// <summary>
        /// Starts capture on the selected input device.
        /// </summary>
        /// 
        /// <param name="device">The device to start capturing on. Must not be null.</param>
        /// <param name="captureOptions">The capture options to use. Must not be null.</param>
        /// <param name="filterExpression">The optional application-level filter expression to use on parsed packets. May be null, indicating no filtering.</param>
        public void StartCapture(ICaptureDevice device, CaptureOptions captureOptions, IExpression filterExpression)
        {
            OpenDevice(device, captureOptions);

            // TODO [Greg 12/31/2014] : Move handling of the packets elsewhere.
            device.OnPacketArrival += (sender, e) =>
            {
                HandlePacket(e, filterExpression, Options);
            };

            Console.WriteLine("Starting capture... SIGTERM to quit.");
            device.StartCapture();
        }

        /// <summary>
        /// Compiles the filter expression we'll use to perform application-level filtering of parsed Memcached binary packets.
        /// </summary>
        /// 
        /// <returns>The <see cref="IExpression"/> that is parsed out of the user input filter. If no filter, or an invalid filter,
        /// is specified, this will return null.
        /// </returns>
        public IExpression CompileFilterExpression()
        {
            IExpression filterExpression = null;
            if (!String.IsNullOrEmpty(Options.RawFilterExpression))
            {
                try
                {
                    filterExpression = Expression.Parse(Options.RawFilterExpression);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to parse expression {0}: {1}. Ignoring...", Options.RawFilterExpression, ex.Message);
                }
            }

            return filterExpression;
        }

        // TODO [Greg 01/18/2015] : Try and abstract this portion. Maybe networking portion should be moved into a collaborating class?

        /// <summary>
        /// Opens the given device for capture.
        /// </summary>
        /// 
        /// <param name="device">The device to open. Must not be null, must be valid.</param>
        /// <param name="captureOptions">The capture options to use during capture.</param>
        public void OpenDevice(ICaptureDevice device, CaptureOptions captureOptions)
        {
            if (captureOptions.Source == CaptureSource.Interface)
            {
                //TODO [Greg 12/30/2014] : Maybe add an advanced configuration option/something into app.config to use a non-promiscuous mode?

                // If we're a live interface, we're going to drop into promiscuous mode. Most people will be in a switched environment, so this won't be a problem.
                Logger.Info("Configuring PCAP");
                device.Open(DeviceMode.Promiscuous, 10000);
                Logger.Info("Listening to interface {0} on port(s) {1}", captureOptions.DeviceName, String.Join(",", captureOptions.Ports));
            }
            else
            {
                Logger.Info("Reading input PCAP file");
                device.Open();
            }
        }

        /// <summary>
        /// Provides a convenience method for finding our device by name.
        /// </summary>
        /// 
        /// <returns>The device, if known, else Environment.Exit.</returns>
        public ICaptureDevice GetDevice(CaptureOptions captureOptions)
        {
            ICaptureDevice device;
            var descriptor = captureOptions.DeviceName;

            // If no one specified a PCAP file, default to a device.
            if (captureOptions.Source == CaptureSource.Interface)
            {
                Logger.Info("Looking for target device {0}...", captureOptions.DeviceName);
                device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Equals(descriptor));

                // Make sure we've got the device.
                if (null == device)
                {
                    Console.WriteLine("Unable to find device {0}, exiting...", descriptor);
                    Environment.Exit(1);
                }
            }
            else
            {
                // Try and read PCAP dump.
                Logger.Info("Attempting to use PCAP file {0}...", descriptor);

                if (!File.Exists(descriptor))
                {
                    Console.WriteLine("Unable to find PCAP file {0}, exiting...", descriptor);
                    Environment.Exit(1);
                }

                device = new CaptureFileReaderDevice(descriptor);
            }

            return device;
        }

        // TODO: [Greg 01/02/2015] - This is a fairly low level networking thing... Should move all the actual packet abstractions into something dedicated to them.

        // TODO [Greg 01/15/2015] : Probably want to perf test this too. Probably worth moving the port filtering back into the LibPCAP filter, though it did
        // TODO [Greg 01/15/2015] : seem to cause artifical misses on PCAP files instead of live interfaces...

        /// <summary>
        /// Handles an incoming packet, attempting to parse it into something useful.
        /// </summary>
        /// 
        /// <param name="eventArgs">The <see cref="CaptureEventArgs"/> from the packet capture event. Must not be null.</param>
        /// <param name="filterExpression">An optional, user-specified filter expression. May be null if no filter is to be applied.</param>
        /// <param name="captureOptions">The user-input capture options to use.</param>
        public void HandlePacket(CaptureEventArgs eventArgs, IExpression filterExpression, CaptureOptions captureOptions)
        {
            if (eventArgs.Packet.LinkLayerType != LinkLayers.Null)
            {
                var parsed = Packet.ParsePacket(eventArgs.Packet.LinkLayerType, eventArgs.Packet.Data);
                var tcpPacket = (TcpPacket)parsed.Extract(typeof(TcpPacket));

                if (null != tcpPacket && null != tcpPacket.PayloadData && tcpPacket.PayloadData.Length > 0)
                {
                    var relevantPort = DetermineRelevantPort(tcpPacket.DestinationPort, tcpPacket.SourcePort, captureOptions.Ports);

                    if (null != relevantPort)
                    {
                        var ipPacket = (IpPacket)tcpPacket.ParentPacket;
                        var packet = new MemcachedBinaryPacket(tcpPacket.PayloadData)
                        {
                            PacketTime = eventArgs.Packet.Timeval.Date,
                            SourceAddress = ipPacket.SourceAddress.ToString(),
                            DestinationAddress = ipPacket.DestinationAddress.ToString(),
                            PacketSize = tcpPacket.PayloadData.LongLength,
                            Port = relevantPort.Value
                        };

                        var i = 0;

                        foreach (var operation in packet.Operations)
                        {
                            // If we've got a filter expression, see what it does...
                            try
                            {
                                if (filterExpression == null || (bool)filterExpression.GetValue(operation, new Dictionary<string, object> { { "packet", packet } }))
                                {
                                    // TODO: [Greg 01/02/2015] - Figure out something better to do with the packets.
                                    LogPacket(++i, packet.OperationCount, packet, operation);
                                }
                            }
                            catch (Exception ex)
                            {
                                // TODO [Greg 12/27/2014] : Need to come back and re-think this. If we've got an invalid expression, could lead to a lot of log spam.
                                Logger.Warn("Ignoring invalid expression \"{0}\": {1}...", Options.RawFilterExpression, ex.Message);
                                LogPacket(++i, packet.Operations.Count(), packet, operation);
                            }
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
        public void LogPacket(int index, int count, MemcachedBinaryPacket packet, MemcachedBinaryOperation operation)
        {
            Logger.Debug("{0} -> {1} {2} {3} UTC ({5}/{6}: {4} bytes) Packet {7}: {8} {9} {10} {11}",
                packet.SourceAddress, packet.DestinationAddress, packet.Port, packet.PacketTime.ToUniversalTime(), packet.PacketSize,
                index, count, operation.Magic, operation.Opcode,
                (String.IsNullOrWhiteSpace(operation.Key)) ? "<key omitted>" : operation.Key.ToString(CultureInfo.InvariantCulture),
                (operation.Extras != null) ? operation.Extras.ToString() : "",
                (operation.Magic == MagicValue.Received) ? "= " + (ResponseStatus)operation.Header.StatusOrVbucketId : "");
        }


        /// <summary>
        /// Provides a utility method to try and figure out which of the ports we're listening to a packet came from. If Any.
        /// </summary>
        /// 
        /// <param name="sourcePort">The source port of the TCP packet.</param>
        /// <param name="destinationPort">The destination port of the TCP packet.</param>
        /// <param name="portsOfInterest">The list of ports we're listening on. Must not be null.</param>
        /// 
        /// <returns>The relevant source or destination port for the packet.</returns>
        public int? DetermineRelevantPort(ushort sourcePort, ushort destinationPort, IEnumerable<int> portsOfInterest)
        {
            var port = 0;

            if (null != portsOfInterest)
            {
                port = portsOfInterest.FirstOrDefault(x => x == sourcePort || x == destinationPort);
            }

            return (0 == port) ? (int?)null : port;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
