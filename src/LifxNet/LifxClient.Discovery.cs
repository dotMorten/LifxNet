using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
    public partial class LifxClient : IDisposable
    {
        private static int sequence = 1;
        private CancellationTokenSource? _DiscoverCancellationSource;
        private Dictionary<string, Device> DiscoveredBulbs = new Dictionary<string, Device>();

        private static byte GetNextSequence()
        {
            unchecked
            {
                return (byte)Interlocked.Increment(ref sequence);
            }
        }

        /// <summary>
        /// Event fired when a LIFX bulb is discovered on the network
        /// </summary>
        public event EventHandler<DeviceDiscoveryEventArgs>? DeviceDiscovered;
        /// <summary>
        /// Event fired when a LIFX bulb hasn't been seen on the network for a while (for more than 5 minutes)
        /// </summary>
        public event EventHandler<DeviceDiscoveryEventArgs>? DeviceLost;

        private IList<Device> devices = new List<Device>();

        /// <summary>
        /// Gets a list of currently known devices
        /// </summary>
        public IEnumerable<Device> Devices { get { return devices; } }

        /// <summary>
        /// Event args for <see cref="DeviceDiscovered"/> and <see cref="DeviceLost"/> events.
        /// </summary>
        public sealed class DeviceDiscoveryEventArgs : EventArgs
        {
            internal DeviceDiscoveryEventArgs(Device device) => Device = device;
            /// <summary>
            /// The device the event relates to
            /// </summary>
            public Device Device { get; }
        }

        private void ProcessDeviceDiscoveryMessage(System.Net.IPAddress remoteAddress, int remotePort, LifxResponse msg)
        {
            string id = msg.Header.TargetMacAddressName; //remoteAddress.ToString()
            if (DiscoveredBulbs.ContainsKey(id))  //already discovered
            {
                DiscoveredBulbs[id].LastSeen = DateTime.UtcNow; //Update datestamp
                DiscoveredBulbs[id].HostName = remoteAddress.ToString(); //Update hostname in case IP changed

                return;
            }
            if (msg.Source != ClientSource || //did we request the discovery?
                _DiscoverCancellationSource == null ||
                _DiscoverCancellationSource.IsCancellationRequested) //did we cancel discovery?
                return;

            var device = new LightBulb(remoteAddress.ToString(), msg.Header.TargetMacAddress, msg.Payload[0]
                , BitConverter.ToUInt32(msg.Payload, 1))
            {
                LastSeen = DateTime.UtcNow
            };
            DiscoveredBulbs[id] = device;
            devices.Add(device);
            if (DeviceDiscovered != null)
            {
                DeviceDiscovered(this, new DeviceDiscoveryEventArgs(device));
            }
        }

        /// <summary>
        /// Begins searching for bulbs.
        /// </summary>
        /// <seealso cref="DeviceDiscovered"/>
        /// <seealso cref="DeviceLost"/>
        /// <seealso cref="StopDeviceDiscovery"/>
        public void StartDeviceDiscovery()
        {
            if (_DiscoverCancellationSource != null && !_DiscoverCancellationSource.IsCancellationRequested)
                return;
            _DiscoverCancellationSource = new CancellationTokenSource();
            var token = _DiscoverCancellationSource.Token;
            //Start discovery thread
            Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine("Sending GetServices");
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        FrameHeader header = new FrameHeader()
                        {
                            Sequence = GetNextSequence()
                        };
                        await BroadcastMessageAsync<UnknownResponse>(null, header, MessageType.DeviceGetService);
                    }
                    catch { }
                    await Task.Delay(5000);
                    var lostDevices = devices.Where(d => (DateTime.UtcNow - d.LastSeen).TotalMinutes > 5).ToArray();
                    if (lostDevices.Any())
                    {
                        foreach (var device in lostDevices)
                        {
                            devices.Remove(device);
                            DiscoveredBulbs.Remove(device.MacAddressName);
                            if (DeviceLost != null)
                                DeviceLost(this, new DeviceDiscoveryEventArgs(device));
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Stops device discovery
        /// </summary>
        /// <seealso cref="StartDeviceDiscovery"/>
        public void StopDeviceDiscovery()
        {
            if (_DiscoverCancellationSource == null || _DiscoverCancellationSource.IsCancellationRequested)
                return;
            _DiscoverCancellationSource.Cancel();
            _DiscoverCancellationSource = null;
        }
    }

    /// <summary>
    /// LIFX Generic Device
    /// </summary>
    public abstract class Device
    {
        internal Device(string hostname, byte[] macAddress, byte service, UInt32 port)
        {
            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException(nameof(hostname));
            HostName = hostname;
            MacAddress = macAddress;
            Service = service;
            Port = port;
            LastSeen = DateTime.MinValue;
        }

        /// <summary>
        /// Hostname for the device
        /// </summary>
        public string HostName { get; internal set; }

        /// <summary>
        /// Service ID
        /// </summary>
        public byte Service { get; }

        /// <summary>
        /// Service port
        /// </summary>
        public UInt32 Port { get; }

        internal DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets the MAC address
        /// </summary>
        public byte[] MacAddress { get; }

        /// <summary>
        /// Gets the MAC address
        /// </summary>
        public string MacAddressName
        {
            get
            {
                if (MacAddress == null) return string.Empty;
                return string.Join(":", MacAddress.Take(6).Select(tb => tb.ToString("X2")).ToArray());
            }
        }
    }
    /// <summary>
    /// LIFX light bulb
    /// </summary>
    public sealed class LightBulb : Device
    {
        /// <summary>
        /// Initializes a new instance of a bulb instead of relying on discovery. At least the host name must be provide for the device to be usable.
        /// </summary>
        /// <param name="hostname">Required</param>
        /// <param name="macAddress"></param>
        /// <param name="service"></param>
        /// <param name="port"></param>
        public LightBulb(string hostname, byte[] macAddress, byte service = 0, UInt32 port = 0) : base(hostname, macAddress, service, port)
        {
        }
    }
}
