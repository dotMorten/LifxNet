using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient : IDisposable {

		/// <summary>
		/// This message is used for changing the color of either a single or multiple zones.
		/// </summary>
		/// <param name="bulb">Target bulb</param>
		/// <param name="startIndex">Start index to target</param>
		/// <param name="endIndex">End index to target</param>
		/// <param name="Color">LifxColor to use</param>
		/// <param name="transitionDuration">How long to fade</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task SetColorZones(LightBulb bulb, int startIndex, int endIndex, LifxColor Color,
			TimeSpan transitionDuration) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0) {throw new ArgumentOutOfRangeException(nameof(transitionDuration));}
			if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));
			
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;
			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetColorZones, startIndex, endIndex, Color, duration, 0x01);
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
		public async Task SetExtendedColorZonesAsync(LightBulb bulb, TimeSpan transitionDuration, int index, List<LifxColor> colors) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0) {throw new ArgumentOutOfRangeException(nameof(transitionDuration));}
			
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			uint duration = (uint)transitionDuration.TotalMilliseconds;
			var cArgs = new List<byte>();
			foreach (var color in colors) {
				cArgs.AddRange(color.ToBytes());
			}
			
			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetExtendedColorZones, duration, (byte) 0x01, index, colors.Count, cArgs);
		}
		
		/// <summary>
		/// Try to get the color zones from our device.
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<StateExtendedColorZonesResponse> GetExtendedColorZonesAsync(LightBulb bulb)
		{
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
		/// <returns>Either a "StateZone" response for single-zone devices, or "StateMultiZone" response.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<LifxResponse> GetColorZonesAsync(LightBulb bulb)
		{
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = false
			};
			return BroadcastMessageAsync<LifxResponse>(
				bulb.HostName, header, MessageType.GetColorZones);
		}
			
	}
}