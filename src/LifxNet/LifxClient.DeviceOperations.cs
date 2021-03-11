using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient {
		/// <summary>
		/// Turns the device on
		/// </summary>
		public Task TurnDeviceOnAsync(Device device) => SetDevicePowerStateAsync(device, true);

		/// <summary>
		/// Turns the device off
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		public Task TurnDeviceOffAsync(Device device) => SetDevicePowerStateAsync(device, false);

		/// <summary>
		/// Sets the device power state
		/// </summary>
		/// <param name="device"></param>
		/// <param name="isOn"></param>
		/// <returns></returns>
		public async Task SetDevicePowerStateAsync(Device device, bool isOn) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			Debug.WriteLine($"Sending DeviceSetPower({isOn}) to {device.HostName}");
			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);

			_ = await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.DeviceSetPower, (ushort) (isOn ? 65535 : 0)).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the label for the device
		/// </summary>
		/// <param name="device"></param>
		/// <returns>The device label</returns>
		public async Task<string?> GetDeviceLabelAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			var resp = await BroadcastMessageAsync<StateLabelResponse>(device.HostName, header,
				MessageType.DeviceGetLabel).ConfigureAwait(false);
			return resp.Label;
		}

		/// <summary>
		/// Sets the label on the device
		/// </summary>
		/// <param name="device"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public async Task SetDeviceLabelAsync(Device device, string label) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			_ = await BroadcastMessageAsync<AcknowledgementResponse>(
				device.HostName, header, MessageType.DeviceSetLabel, label).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the device version
		/// </summary>
		public Task<StateVersionResponse> GetDeviceVersionAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return BroadcastMessageAsync<StateVersionResponse>(device.HostName, header, MessageType.DeviceGetVersion);
		}

		/// <summary>
		/// Gets Host MCU firmware information.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateHostFirmwareResponse"/></returns>
		public Task<StateHostFirmwareResponse> GetDeviceHostFirmwareAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return BroadcastMessageAsync<StateHostFirmwareResponse>(device.HostName, header,
				MessageType.DeviceGetHostFirmware);
		}

		/// <summary>
		/// Get Host MCU information.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateHostInfoResponse"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<StateHostInfoResponse> GetHostInfoAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateHostInfoResponse>(device.HostName, header,
				MessageType.DeviceGetHostInfo);
		}
		
		/// <summary>
		/// Get Host Wifi information.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateWifiInfoResponse"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<StateWifiInfoResponse> GetWifiInfoAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateWifiInfoResponse>(device.HostName, header,
				MessageType.DeviceGetWifiInfo);
		}
		
		/// <summary>
		/// Get Host Wifi firmware information.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateWifiFirmwareResponse"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<StateWifiFirmwareResponse> GetWifiFirmwareAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateWifiFirmwareResponse>(device.HostName, header,
				MessageType.DeviceGetWifiFirmware);
		}

		/// <summary>
		/// Get device power level
		/// Zero implies standby and non-zero sets a corresponding power draw level. Currently only 0 and 65535 are supported.
		/// </summary>
		/// <param name="device"></param>
		/// <returns>0 for off, 1 for on</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<int> GetPowerAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			var level = await BroadcastMessageAsync<StatePowerResponse>(device.HostName, header,
				MessageType.DeviceGetPower);
			return level.Level == 0 ? 0 : 1;
		}
		
		/// <summary>
		/// Set Device power level.
		/// Internally, Lifx offers a range from 0-65535, but actually only responds to 0 and 65535.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="level">0 for off, 1 for on</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task SetPowerAsync(Device device, int level) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			if (level != 0) level = 65535;
			await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.DeviceSetPower, level);
		}

		/// <summary>
		/// Get run-time information. 
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateInfoResponse"/></returns>
		/// <exception cref="ArrayTypeMismatchException"></exception>
		public async Task<StateInfoResponse> GetInfoAsync(Device device) {
			if (device == null)
				throw new ArrayTypeMismatchException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateInfoResponse>(device.HostName, header,
				MessageType.DeviceGetInfo);
		}
		
		/// <summary>
		/// Set the device location label
		/// </summary>
		/// <param name="device"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task SetLocationAsync(Device device, string label) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var rand = new Random();
			var location = new byte[16];
			rand.NextBytes(location);
			var updated = DateTimeOffset.Now.ToUnixTimeSeconds();
			await BroadcastMessageAsync<StatePowerResponse>(device.HostName, header,
				MessageType.DeviceSetLocation, location, label, updated);
		}

		/// <summary>
		/// Ask the device to return its location information.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateLocationResponse"/></returns>
		/// <exception cref="ArrayTypeMismatchException"></exception>
		public async Task<StateLocationResponse> GetLocationAsync(Device device) {
			if (device == null)
				throw new ArrayTypeMismatchException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateLocationResponse>(device.HostName, header,
				MessageType.DeviceGetLocation);
		}
		
		/// <summary>
		/// Set the device group.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="label">The new group name</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task SetGroupAsync(Device device, string label) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var rand = new Random();
			var group = new byte[16];
			rand.NextBytes(group);
			var updated = DateTimeOffset.Now.ToUnixTimeSeconds();
			await BroadcastMessageAsync<StatePowerResponse>(device.HostName, header,
				MessageType.DeviceSetGroup, group, label, updated);
		}

		/// <summary>
		/// Get the device group.
		/// </summary>
		/// <param name="device"></param>
		/// <returns><see cref="StateGroupResponse"/></returns>
		/// <exception cref="ArrayTypeMismatchException"></exception>
		public async Task<StateGroupResponse> GetGroupAsync(Device device) {
			if (device == null)
				throw new ArrayTypeMismatchException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateGroupResponse>(device.HostName, header,
				MessageType.DeviceGetGroup);
		}

		/// <summary>
		/// Request an arbitrary payload be echoed back. 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="payload"></param>
		/// <returns><see cref="EchoResponse"/></returns>
		/// <exception cref="ArrayTypeMismatchException"></exception>
		public async Task<EchoResponse> RequestEcho(Device device, byte[] payload) {
			if (device == null)
				throw new ArrayTypeMismatchException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			// Truncate our input payload to be 64 bits exactly
			var realPayload = new byte[64];
			for (var i = 0; i < realPayload.Length; i++) {
				if (i < payload.Length) {
					realPayload[i] = payload[i];
				} else {
					realPayload[i] = 0;
				}
			} 
			return await BroadcastMessageAsync<EchoResponse>(device.HostName, header,
				MessageType.DeviceEchoRequest, realPayload);
		}
	}
}