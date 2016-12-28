using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		private static Random randomizer = new Random();
		private UInt32 discoverSourceID;
		private CancellationTokenSource _DiscoverCancellationSource;
		private Dictionary<string, Device> DiscoveredBulbs = new Dictionary<string, Device>();

		/// <summary>
		/// Event fired when a LIFX bulb is discovered on the network
		/// </summary>
		public event EventHandler<DeviceDiscoveryEventArgs> DeviceDiscovered;
		/// <summary>
		/// Event fired when a LIFX bulb hasn't been seen on the network for a while (for more than 5 minutes)
		/// </summary>
		public event EventHandler<DeviceDiscoveryEventArgs> DeviceLost;

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
			/// <summary>
			/// The device the event relates to
			/// </summary>
			public Device Device { get; internal set; }
		}

		private void ProcessDeviceDiscoveryMessage(System.Net.IPAddress remoteAddress, int remotePort, LifxResponse msg)
		{
			if (DiscoveredBulbs.ContainsKey(remoteAddress.ToString()))  //already discovered
            {
				DiscoveredBulbs[remoteAddress.ToString()].LastSeen = DateTime.UtcNow; //Update datestamp
				return;
			}
			if (msg.Source != discoverSourceID || //did we request the discovery?
				_DiscoverCancellationSource == null ||
				_DiscoverCancellationSource.IsCancellationRequested) //did we cancel discovery?
				return;

			var device = new LightBulb()
			{
				HostName = remoteAddress.ToString(),
				Service = msg.Payload[0],
				Port = BitConverter.ToUInt32(msg.Payload, 1),
				LastSeen = DateTime.UtcNow
			};
			DiscoveredBulbs[remoteAddress.ToString()] = device;
			devices.Add(device);
			if (DeviceDiscovered != null)
			{
				DeviceDiscovered(this, new DeviceDiscoveryEventArgs() { Device = device });
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
			var source = discoverSourceID = (uint)randomizer.Next(int.MaxValue);
			//Start discovery thread
            Task.Run(async () =>
			{
				System.Diagnostics.Debug.WriteLine("Sending GetServices");
				FrameHeader header = new FrameHeader()
				{
					Identifier = source
				};
				while (!token.IsCancellationRequested)
				{
					try
					{
						await BroadcastMessageAsync<UnknownResponse>(null, header, MessageType.DeviceGetService, null);
					}
					catch { }
					await Task.Delay(5000);
					var lostDevices = devices.Where(d => (DateTime.UtcNow - d.LastSeen).TotalMinutes > 5).ToArray();
					if(lostDevices.Any())
					{
						foreach(var device in lostDevices)
						{
							devices.Remove(device);
							DiscoveredBulbs.Remove(device.HostName.ToString());
							if (DeviceLost != null)
								DeviceLost(this, new DeviceDiscoveryEventArgs() { Device = device });
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
		internal Device() { }
		/// <summary>
		/// Hostname for the device
		/// </summary>
		public string HostName { get; internal set; }
		/// <summary>
		/// Service ID
		/// </summary>
		public byte Service { get; internal set; }
		/// <summary>
		/// Service port
		/// </summary>
		public UInt32 Port { get; internal set; }
		internal DateTime LastSeen { get; set; }
	}
	/// <summary>
	/// LIFX light bulb
	/// </summary>
	public sealed class LightBulb : Device
	{
		internal LightBulb()
		{
		}
	}
}
