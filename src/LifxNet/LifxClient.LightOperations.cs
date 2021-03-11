using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient {
		private readonly Dictionary<uint, Action<LifxResponse>> _taskCompletions =
			new Dictionary<uint, Action<LifxResponse>>();

		/// <summary>
		/// Turns a bulb on using the provided transition time
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		/// <seealso cref="TurnBulbOffAsync(LightBulb, TimeSpan)"/>
		/// <seealso cref="TurnDeviceOnAsync(Device)"/>
		/// <seealso cref="TurnDeviceOffAsync(Device)"/>
		/// <seealso cref="SetLightPowerAsync(LightBulb, TimeSpan, bool)"/>
		/// <seealso cref="SetDevicePowerStateAsync(Device, bool)"/>
		/// <seealso cref="GetLightPowerAsync(LightBulb)"/>
		public Task TurnBulbOnAsync(LightBulb bulb, TimeSpan transitionDuration) =>
			SetLightPowerAsync(bulb, transitionDuration, true);

		/// <summary>
		/// Turns a bulb off using the provided transition time
		/// </summary>
		/// <seealso cref="TurnBulbOnAsync(LightBulb, TimeSpan)"/>
		/// <seealso cref="TurnDeviceOnAsync(Device)"/>
		/// <seealso cref="TurnDeviceOffAsync(Device)"/>
		/// <seealso cref="SetLightPowerAsync(LightBulb, TimeSpan, bool)"/>
		/// <seealso cref="SetDevicePowerStateAsync(Device, bool)"/>
		/// <seealso cref="GetLightPowerAsync(LightBulb)"/>
		public Task TurnBulbOffAsync(LightBulb bulb, TimeSpan transitionDuration) =>
			SetLightPowerAsync(bulb, transitionDuration, false);

		/// <summary>
		/// Turns a bulb on or off using the provided transition time
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="transitionDuration"></param>
		/// <param name="isOn">True to turn on, false to turn off</param>
		/// <returns></returns>
		/// <seealso cref="TurnBulbOffAsync(LightBulb, TimeSpan)"/>
		/// <seealso cref="TurnBulbOnAsync(LightBulb, TimeSpan)"/>
		/// <seealso cref="TurnDeviceOnAsync(Device)"/>
		/// <seealso cref="TurnDeviceOffAsync(Device)"/>
		/// <seealso cref="SetDevicePowerStateAsync(Device, bool)"/>
		/// <seealso cref="GetLightPowerAsync(LightBulb)"/>
		public async Task SetLightPowerAsync(LightBulb bulb, TimeSpan transitionDuration, bool isOn) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
			    transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);

			var b = BitConverter.GetBytes((ushort) transitionDuration.TotalMilliseconds);

			Debug.WriteLine(
				$"Sending LightSetPower(on={isOn},duration={transitionDuration.TotalMilliseconds}ms) to {bulb.HostName}");

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header, MessageType.LightSetPower,
				(ushort) (isOn ? 65535 : 0), b
			).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the current power state for a light bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<bool> GetLightPowerAsync(LightBulb bulb) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			return (await BroadcastMessageAsync<LightPowerResponse>(
				bulb.HostName, header, MessageType.LightGetPower).ConfigureAwait(false)).IsOn;
		}

		/// <summary>
		/// Sets color and temperature of bulb
		/// </summary>
		/// <param name="bulb">The bulb to set</param>
		/// <param name="color">The LifxColor to set the bulb to</param>
		/// <param name="duration">An optional transition duration, in milliseconds.</param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, LifxColor color, int duration = 0) {
			return SetColorAsync(bulb, (ushort)color.LifxHue, (ushort)color.LifxSaturation, (ushort)color.LifxBrightness, (ushort)color.K,
				TimeSpan.FromMilliseconds(duration));
		}

		/// <summary>
		/// Sets color and temperature for a bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="lifxColor"></param>
		/// <param name="kelvin"></param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, LifxColor lifxColor, ushort kelvin) =>
			SetColorAsync(bulb, lifxColor, kelvin, TimeSpan.Zero);

		/// <summary>
		/// Sets color and temperature for a bulb and uses a transition time to the provided state
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="lifxColor"></param>
		/// <param name="kelvin"></param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, LifxColor lifxColor, ushort kelvin, TimeSpan transitionDuration) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			var hsl = Utilities.RgbToHsl(lifxColor);
			return SetColorAsync(bulb, hsl[0], hsl[1], hsl[2], kelvin, transitionDuration);
		}

		/// <summary>
		/// Sets color and temperature for a bulb and uses a transition time to the provided state
		/// </summary>
		/// <param name="bulb">Light bulb</param>
		/// <param name="hue">0..65535</param>
		/// <param name="saturation">0..65535</param>
		/// <param name="brightness">0..65535</param>
		/// <param name="kelvin">2700..9000</param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public async Task SetColorAsync(LightBulb bulb,
			ushort hue,
			ushort saturation,
			ushort brightness,
			ushort kelvin,
			TimeSpan transitionDuration) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
			    transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");
			if (kelvin < 2500 || kelvin > 9000) {
				throw new ArgumentOutOfRangeException("kelvin", "Kelvin must be between 2500 and 9000");
			}

			Debug.WriteLine("Setting color to {0}", bulb.HostName);
			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var duration = (uint) transitionDuration.TotalMilliseconds;

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.LightSetColor, (byte) 0x00, //reserved
				hue, saturation, brightness, kelvin, //HSBK
				duration
			);
		}


		public async Task SetBrightnessAsync(LightBulb bulb,
			ushort brightness,
			TimeSpan transitionDuration) {
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var duration = (uint) transitionDuration.TotalMilliseconds;

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetLightBrightness, brightness, duration
			);
		}

		/// <summary>
		/// Gets the current state of the bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<LightStateResponse> GetLightStateAsync(LightBulb bulb) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<LightStateResponse>(
				bulb.HostName, header, MessageType.LightGet);
		}


		/// <summary>
		/// Gets the current maximum power level of the Infrared channel
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<ushort> GetInfraredAsync(LightBulb bulb) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return (await BroadcastMessageAsync<InfraredStateResponse>(
				bulb.HostName, header, MessageType.InfraredGet).ConfigureAwait(false)).Brightness;
		}

		/// <summary>
		/// Sets the infrared brightness level
		/// </summary>
		/// <param name="device"></param>
		/// <param name="brightness"></param>
		/// <returns></returns>
		public async Task SetInfraredAsync(Device device, ushort brightness) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			Debug.WriteLine($"Sending SetInfrared({brightness}) to {device.HostName}");
			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);

			await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.InfraredSet, brightness).ConfigureAwait(false);
		}
	}
}