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
		private readonly int[] _stripIds = {31,32,38};
		private readonly int[] _tileIds = {55};
		private readonly int[] _switchIds = {70};

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

			var address = remoteAddress.ToString();
			var mac = msg.Header.TargetMacAddress;
			var svc = msg.Payload.GetUint8();
			var port = msg.Payload.GetUInt32();
			var lastSeen = DateTime.UtcNow;
			var device = new LightBulb(address, mac, svc, port) {
				LastSeen = lastSeen
			};
			var ver = GetDeviceVersionAsync(device).Result;
			
			if (_stripIds.Contains((int) ver.Product)) {
				var dev = new Strip(address, mac, svc, port, ver.Product){LastSeen = lastSeen};
				_discoveredBulbs[id] = dev;
				devices.Add(dev);
			} else if (_switchIds.Contains((int) ver.Product)) {
				var dev = new Switch(address, mac, svc, port, ver.Product){LastSeen = lastSeen};
				_discoveredBulbs[id] = dev;
				devices.Add(dev);
			} else if (_tileIds.Contains((int) ver.Product)) {
				var dev = new TileGroup(address, mac, svc, port, ver.Product){LastSeen = lastSeen};
				_discoveredBulbs[id] = dev;
				devices.Add(dev);
			} else {
				_discoveredBulbs[id] = device;
				devices.Add(device);
			}

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
}