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
		private Dictionary<UInt32, Action<LifxResponse>> taskCompletions = new Dictionary<uint, Action<LifxResponse>>();

		public Task TurnBulbOnAsync(LightBulb bulb, TimeSpan transitionDuration)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnBulbOn to {0}", bulb.HostName);
			return SetLightPowerAsync(bulb, transitionDuration, true);
		}
		public Task TurnBulbOffAsync(LightBulb bulb, TimeSpan transitionDuration)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnBulbOff to {0}", bulb.HostName);
			return SetLightPowerAsync(bulb, transitionDuration, false);
		}
		private async Task SetLightPowerAsync(LightBulb bulb, TimeSpan transitionDuration, bool isOn)
		{
			if (bulb == null)
				throw new ArgumentNullException("bulb");
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");

			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};

			var b = BitConverter.GetBytes((UInt16)transitionDuration.TotalMilliseconds);

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header, MessageType.LightSetPower,
				(UInt16)(isOn ? 65535 : 0), b
			).ConfigureAwait(false);
		}

		public async Task<bool> GetLightPowerAsync(LightBulb bulb)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};
			return (await BroadcastMessageAsync<LightPowerResponse>(
				bulb.HostName, header, MessageType.LightGetPower).ConfigureAwait(false)).IsOn;
		}


		public Task SetColorAsync(LightBulb bulb, Windows.UI.Color color, UInt16 kelvin)
		{
			return SetColorAsync(bulb, color, kelvin, TimeSpan.Zero);
		}
		public Task SetColorAsync(LightBulb bulb, Windows.UI.Color color, UInt16 kelvin, TimeSpan transitionDuration)
		{
			var hsl = Utilities.RgbToHsl(color);
			return SetColorAsync(bulb, hsl[0], hsl[1], hsl[2], kelvin, transitionDuration);
		}

		public async Task SetColorAsync(LightBulb bulb,
			UInt16 hue,
			UInt16 saturation,
			UInt16 brightness,
			UInt16 kelvin,
			TimeSpan transitionDuration)
		{
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");
			if (kelvin < 2500 || kelvin > 9000)
			{
				throw new ArgumentOutOfRangeException("kelvin", "Kelvin must be between 2500 and 9000");
			}

				System.Diagnostics.Debug.WriteLine("Setting color to {0}", bulb.HostName);
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = true
			};
			UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;
			var durationBytes = BitConverter.GetBytes(duration);
			var h = BitConverter.GetBytes(hue);
			var s = BitConverter.GetBytes(saturation);
			var b = BitConverter.GetBytes(brightness);
			var k = BitConverter.GetBytes(kelvin);

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.LightSetColor, (byte)0x00, //reserved
					hue, saturation, brightness, kelvin, //HSBK
					duration
			);
		}

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
		}

		public Task<LightStateResponse> GetLightStateAsync(LightBulb bulb)
		{
			FrameHeader header = new FrameHeader()
			{
				Identifier = (uint)randomizer.Next(),
				AcknowledgeRequired = false
			};
			return BroadcastMessageAsync<LightStateResponse>(
				bulb.HostName, header, MessageType.LightGet);
		}
	}
}
