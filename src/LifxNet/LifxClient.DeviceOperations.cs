using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		/// <summary>
		/// Turns the device on
		/// </summary>
		public Task TurnDeviceOnAsync(Device device)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOn to {0}", device.HostName);
			return SetDevicePowerStateAsync(device, true);
		}
		/// <summary>
		/// Turns the device off
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		public Task TurnDeviceOffAsync(Device device)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOff to {0}", device.HostName);
			return SetDevicePowerStateAsync(device, false);
		}
		/// <summary>
		/// Sets the device power state
		/// </summary>
		/// <param name="device"></param>
		/// <param name="isOn"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Gets the label for the device
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Sets the label on the device
		/// </summary>
		/// <param name="device"></param>
		/// <param name="label"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Gets the device version
		/// </summary>
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
		/// <summary>
		/// Gets the device's host firmware
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
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
