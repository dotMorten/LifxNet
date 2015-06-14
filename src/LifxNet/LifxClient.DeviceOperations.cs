using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		public Task TurnDeviceOnAsync(Device device)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOn to {0}", device.HostName);
			return SetDevicePowerStateAsync(device, true);
		}
		public Task TurnDeviceOffAsync(Device device)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOff to {0}", device.HostName);
			return SetDevicePowerStateAsync(device, false);
		}
		public async Task SetDevicePowerStateAsync(Device device, bool isOn)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOff to {0}", device.HostName);
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};

			await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.DeviceSetPower, (UInt16)(isOn ? 65535 : 0));
		}

		public async Task<string> GetDeviceLabelAsync(Device device)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = false
			};
			var resp = await BroadcastMessageAsync<StateLabelResponse>(device.HostName, header, MessageType.DeviceGetLabel);
			return resp.Label;
		}

		public async Task SetDeviceLabelAsync(Device device, string label)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};
			var resp = await BroadcastMessageAsync<AcknowledgementResponse>(
				device.HostName, header, MessageType.DeviceSetLabel, label);
		}

		public async Task<StateVersionResponse> GetDeviceVersionAsync(Device device)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = false
			};
			var resp = await BroadcastMessageAsync<StateVersionResponse>(device.HostName, header, MessageType.DeviceGetVersion);
			return resp;
		}
		public async Task<StateHostFirmwareResponse> GetDeviceHostFirmwareAsync(Device device)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = false
			};
			var resp = await BroadcastMessageAsync<StateHostFirmwareResponse>(device.HostName, header, MessageType.DeviceGetHostFirmware);
			return resp;
		}
	}
}
