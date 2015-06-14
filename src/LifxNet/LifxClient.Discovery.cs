using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		private static Random randomizer = new Random();
		private UInt32 discoverSourceID;
		private CancellationTokenSource _DiscoverCancellationSource;
		private Dictionary<string, Device> DiscoveredBulbs = new Dictionary<string, Device>();

		public event EventHandler<DeviceDiscoveryEventArgs> DeviceDiscovered;
		public event EventHandler<DeviceDiscoveryEventArgs> DeviceLost; //TODO

		private IList<Device> devices = new List<Device>();
		
		public IEnumerable<Device> Devices { get { return devices; } }

		public sealed class DeviceDiscoveryEventArgs : EventArgs
		{
			public Device Device { get; internal set; }
		}

		private void ProcessDeviceDiscoveryMessage(HostName remoteAddress, string remotePort, LifxResponse msg)
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
				HostName = remoteAddress,
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

		public void StopDeviceDiscovery()
		{
			if (_DiscoverCancellationSource == null || _DiscoverCancellationSource.IsCancellationRequested)
				return;
			_DiscoverCancellationSource.Cancel();
			_DiscoverCancellationSource = null;
		}
	}


	public abstract class Device
	{
		internal Device() { }
		public HostName HostName { get; internal set; }
		public byte Service { get; internal set; }
		public UInt32 Port { get; internal set; }
		internal DateTime LastSeen { get; set; }
	}
	public sealed class LightBulb : Device
	{
		internal LightBulb()
		{
		}
	}
}
