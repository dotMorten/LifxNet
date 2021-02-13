using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient : IDisposable {
		
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
		public async Task SetExtendedColorZoneAsync(LightBulb bulb, TimeSpan transitionDuration, int index, List<LifxColor> colors) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
			    transitionDuration.Ticks < 0) {throw new ArgumentOutOfRangeException(nameof(transitionDuration));}
			
			FrameHeader header = new FrameHeader {
				Identifier = GetNextIdentifier(),
				AcknowledgeRequired = true
			};
			UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;
			var cArgs = new List<Object>();
			foreach (var color in colors) {
				var hsl = Utilities.RgbToHsl(color);
				cArgs.Add(hsl[0]);
				cArgs.Add(hsl[1]);
				cArgs.Add(hsl[2]);
				cArgs.Add(2700);
			}
			
			await BroadcastMessageAsync<AcknowledgementResponse>(bulb.HostName, header,
				MessageType.SetExtendedColorZones, duration, (byte) 0x01, index, colors.Count, cArgs);
		}

		
			
	}
}