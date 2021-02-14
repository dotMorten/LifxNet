using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient {
		private static uint _identifier = 1;
		private static readonly object IdentifierLock = new object();
		private uint _discoverSourceId;
		private CancellationTokenSource? _discoverCancellationSource;
		private readonly Dictionary<string, Device> _discoveredBulbs = new Dictionary<string, Device>();

		private static uint GetNextIdentifier() {
			lock (IdentifierLock)
				return _identifier++;
		}

		/// <summary>
		/// Event fired when a LIFX bulb is discovered on the network
		/// </summary>
		public event EventHandler<DiscoveryEventArgs>? Discovered;

		/// <summary>
		/// Event fired when a LIFX bulb hasn't been seen on the network for a while (for more than 5 minutes)
		/// </summary>
		public event EventHandler<DiscoveryEventArgs>? Lost;

		private IList<Device> devices = new List<Device>();

		/// <summary>
		/// Gets a list of currently known devices
		/// </summary>
		public IEnumerable<Device> Devices {
			get { return devices; }
		}

		/// <summary>
		/// Event args for <see cref="LifxClient.Discovered"/> and <see cref="LifxClient.Lost"/> events.
		/// </summary>
		public sealed class DiscoveryEventArgs : EventArgs {
			internal DiscoveryEventArgs(Device device) => Device = device;

			/// <summary>
			/// The device the event relates to
			/// </summary>
			public Device Device { get; }
		}

		private void ProcessDeviceDiscoveryMessage(IPAddress remoteAddress, LifxResponse msg) {
			string id = msg.Header.TargetMacAddressName; //remoteAddress.ToString()
			if (_discoveredBulbs.ContainsKey(id)) //already discovered
			{
				_discoveredBulbs[id].LastSeen = DateTime.UtcNow; //Update datestamp
				_discoveredBulbs[id].HostName = remoteAddress.ToString(); //Update hostname in case IP changed

				return;
			}

			if (msg.Source != _discoverSourceId || //did we request the discovery?
			    _discoverCancellationSource == null ||
			    _discoverCancellationSource.IsCancellationRequested) //did we cancel discovery?
				return;

			var device = new LightBulb(remoteAddress.ToString(), msg.Header.TargetMacAddress, msg.Payload[0]
				, BitConverter.ToUInt32(msg.Payload, 1)) {
				LastSeen = DateTime.UtcNow
			};
			_discoveredBulbs[id] = device;
			devices.Add(device);
			Discovered?.Invoke(this, new DiscoveryEventArgs(device));
		}

		/// <summary>
		/// Begins searching for bulbs.
		/// </summary>
		/// <seealso cref="Discovered"/>
		/// <seealso cref="Lost"/>
		/// <seealso cref="StopDeviceDiscovery"/>
		public void StartDeviceDiscovery() {
			if (_discoverCancellationSource != null && !_discoverCancellationSource.IsCancellationRequested)
				return;
			_discoverCancellationSource = new CancellationTokenSource();
			var token = _discoverCancellationSource.Token;
			var source = _discoverSourceId = GetNextIdentifier();
			//Start discovery thread
			Task.Run(async () => {
				Debug.WriteLine("Sending GetServices");
				FrameHeader header = new FrameHeader {
					Identifier = source
				};
				while (!token.IsCancellationRequested) {
					try {
						await BroadcastMessageAsync<UnknownResponse>(null, header, MessageType.DeviceGetService);
					} catch {
						// ignored
					}

					await Task.Delay(5000, token);
					var lostDevices = devices.Where(d => (DateTime.UtcNow - d.LastSeen).TotalMinutes > 5).ToArray();
					if (!lostDevices.Any()) {
						continue;
					}

					foreach (var device in lostDevices) {
						devices.Remove(device);
						_discoveredBulbs.Remove(device.MacAddressName);
						Lost?.Invoke(this, new DiscoveryEventArgs(device));
					}
				}
			}, token);
		}

		/// <summary>
		/// Stops device discovery
		/// </summary>
		/// <seealso cref="StartDeviceDiscovery"/>
		public void StopDeviceDiscovery() {
			if (_discoverCancellationSource == null || _discoverCancellationSource.IsCancellationRequested)
				return;
			_discoverCancellationSource.Cancel();
			_discoverCancellationSource = null;
		}
	}

	/// <summary>
	/// LIFX Generic Device
	/// </summary>
	public abstract class Device {
		internal Device(string hostname, byte[] macAddress, byte service, UInt32 port) {
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
		public string MacAddressName {
			get { return string.Join(":", MacAddress.Take(6).Select(tb => tb.ToString("X2")).ToArray()); }
		}
	}

	/// <summary>
	/// LIFX light bulb
	/// </summary>
	public sealed class LightBulb : Device {
		/// <summary>
		/// Initializes a new instance of a bulb instead of relying on discovery. At least the host name must be provide for the device to be usable.
		/// </summary>
		/// <param name="hostname">Required</param>
		/// <param name="macAddress"></param>
		/// <param name="service"></param>
		/// <param name="port"></param>
		public LightBulb(string hostname, byte[] macAddress, byte service = 0, UInt32 port = 0) : base(hostname,
			macAddress, service, port) {
		}
	}
}