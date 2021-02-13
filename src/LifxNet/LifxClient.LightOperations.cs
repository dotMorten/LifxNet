using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LifxNet
{
	public partial class LifxClient {
		private readonly Dictionary<UInt32, Action<LifxResponse>> _taskCompletions = new Dictionary<uint, Action<LifxResponse>>();

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
		public Task TurnBulbOnAsync(LightBulb bulb, TimeSpan transitionDuration) => SetLightPowerAsync(bulb, transitionDuration, true);

		/// <summary>
		/// Turns a bulb off using the provided transition time
		/// </summary>
		/// <seealso cref="TurnBulbOnAsync(LightBulb, TimeSpan)"/>
		/// <seealso cref="TurnDeviceOnAsync(Device)"/>
		/// <seealso cref="TurnDeviceOffAsync(Device)"/>
		/// <seealso cref="SetLightPowerAsync(LightBulb, TimeSpan, bool)"/>
		/// <seealso cref="SetDevicePowerStateAsync(Device, bool)"/>
		/// <seealso cref="GetLightPowerAsync(LightBulb)"/>
		public Task TurnBulbOffAsync(LightBulb bulb, TimeSpan transitionDuration) => SetLightPowerAsync(bulb, transitionDuration, false);

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
		public async Task SetLightPowerAsync(LightBulb bulb, TimeSpan transitionDuration, bool isOn)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));

			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};

			var b = BitConverter.GetBytes((UInt16)transitionDuration.TotalMilliseconds);

			Debug.WriteLine($"Sending LightSetPower(on={isOn},duration={transitionDuration.TotalMilliseconds}ms) to {bulb.HostName}");

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header, MessageType.LightSetPower,
				(UInt16)(isOn ? 65535 : 0), b
			).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the current power state for a light bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<bool> GetLightPowerAsync(LightBulb bulb)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));

			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			return (await BroadcastMessageAsync<LightPowerResponse>(
				bulb.HostName, header, MessageType.LightGetPower).ConfigureAwait(false)).IsOn;
		}

		/// <summary>
		/// Sets color and temperature for a bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="lifxColor"></param>
		/// <param name="kelvin"></param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, LifxColor lifxColor, UInt16 kelvin) => SetColorAsync(bulb, lifxColor, kelvin, TimeSpan.Zero);

		/// <summary>
		/// Sets color and temperature for a bulb and uses a transition time to the provided state
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="lifxColor"></param>
		/// <param name="kelvin"></param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, LifxColor lifxColor, UInt16 kelvin, TimeSpan transitionDuration)
		{
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
			UInt16 hue,
			UInt16 saturation,
			UInt16 brightness,
			UInt16 kelvin,
			TimeSpan transitionDuration)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");
			if (kelvin < 2500 || kelvin > 9000)
			{
				throw new ArgumentOutOfRangeException("kelvin", "Kelvin must be between 2500 and 9000");
			}

			Debug.WriteLine("Setting color to {0}", bulb.HostName);
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			var duration = (UInt32)transitionDuration.TotalMilliseconds;
			
			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.LightSetColor, (byte)0x00, //reserved
					hue, saturation, brightness, kelvin, //HSBK
					duration
			);
		}

		/*
		public async Task SetBrightnessAsync(LightBulb bulb,
			UInt16 brightness,
			TimeSpan transitionDuration)
		{
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");

			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};
			UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;
			var durationBytes = BitConverter.GetBytes(duration);
			var b = BitConverter.GetBytes(brightness);

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetLightBrightness, brightness, duration
			);
		}*/

			/// <summary>
			/// Gets the current state of the bulb
			/// </summary>
			/// <param name="bulb"></param>
			/// <returns></returns>
		public Task<LightStateResponse> GetLightStateAsync(LightBulb bulb)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = false
			};
			return BroadcastMessageAsync<LightStateResponse>(
				bulb.HostName, header, MessageType.LightGet);
		}


		/// <summary>
		/// Gets the current maximum power level of the Infrared channel
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<UInt16> GetInfraredAsync(LightBulb bulb)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));

			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};	
			return (await BroadcastMessageAsync<InfraredStateResponse>(
				bulb.HostName, header, MessageType.InfraredGet).ConfigureAwait(false)).Brightness;
		}

		/// <summary>
		/// Sets the infrared brightness level
		/// </summary>
		/// <param name="device"></param>
		/// <param name="brightness"></param>
		/// <returns></returns>
		public async Task SetInfraredAsync(Device device, UInt16 brightness)
		{
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			Debug.WriteLine($"Sending SetInfrared({brightness}) to {device.HostName}");
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};

			_ = await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.InfraredSet, brightness).ConfigureAwait(false);
		}
	}
}
