using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient : IDisposable {
		private const byte Apply = 0x01;


		/// <summary>
		/// This message is used for changing the color of either a single or multiple zones.
		/// </summary>
		/// <param name="bulb">Target bulb</param>
		/// <param name="startIndex">Start index to target</param>
		/// <param name="endIndex">End index to target</param>
		/// <param name="color">LifxColor to use</param>
		/// <param name="transitionDuration">How long to fade</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task SetColorZones(LightBulb bulb, int startIndex, int endIndex, LifxColor color,
			TimeSpan transitionDuration) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0) {
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));
			}

			if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));

			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			var duration = (uint) transitionDuration.TotalMilliseconds;
			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetColorZones, (byte) startIndex, (byte) endIndex, color, duration, Apply);
		}

		/// <summary>
		/// Set a zone of colors
		/// </summary>
		/// <param name="bulb">The device to set</param>
		/// <param name="transitionDuration">Duration in ms</param>
		/// <param name="index">Start index of the zone. Should probably just be 0 for most cases.</param>
		/// <param name="colors">An array of system.drawing.colors. For completeness, I should probably make an
		/// overload for this that accepts HSB values, but that's kind of a pain. :P</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">Thrown if the bulb is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the duration is longer than the max</exception>
		/// 
		public async Task SetExtendedColorZonesAsync(LightBulb bulb, TimeSpan transitionDuration, uint index,
			List<LifxColor> colors) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0) {
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));
			}

			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			var duration = (uint) transitionDuration.TotalMilliseconds;
			var count = (byte) colors.Count;
			var colorBytes = new List<byte>();
			foreach (var color in colors) {
				colorBytes.AddRange(color.ToBytes());
			}

			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetExtendedColorZones, duration, Apply, index, count, colorBytes);
		}

		/// <summary>
		/// Try to get the color zones from our device.
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<StateExtendedColorZonesResponse> GetExtendedColorZonesAsync(LightBulb bulb) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = false
			};
			return BroadcastMessageAsync<StateExtendedColorZonesResponse>(
				bulb.HostName, header, MessageType.GetExtendedColorZones);
		}

		/// <summary>
		/// Try to get the color zones from our device, non-extended.
		/// </summary>
		/// <param name="bulb">Target bulb</param>
		/// <param name="startIndex">Start index of requested zones</param>
		/// <param name="endIndex">End index of requested zones</param>
		/// <returns>Either a "StateZone" response for single-zone devices, or "StateMultiZone" response.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<LifxResponse> GetColorZonesAsync(LightBulb bulb, int startIndex, int endIndex) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = false
			};
			return BroadcastMessageAsync<LifxResponse>(
				bulb.HostName, header, MessageType.GetColorZones, (byte) startIndex, (byte) endIndex);
		}
	}
}