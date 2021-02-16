using System;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient {
		/// <summary>
		/// Get the power state of a relay on a switch device.
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="relayIndex">The relay on the switch starting from 0</param>
		/// <returns>A StateRelayPower message.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<StateRelayPowerResponse> GetRelayPowerAsync(LightBulb bulb, int relayIndex) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateRelayPowerResponse>(
				bulb.HostName, header, MessageType.GetRelayPower, (byte) relayIndex);
		}

		/// <summary>
		/// Set the power state of a relay on a switch device.
		/// Current models of the LIFX switch do not have dimming capability,
		/// so the two valid values are 0 for off and 65535 for on.
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="relayIndex">The relay on the switch starting from 0</param>
		/// <param name="enable">Whether to turn the device on or not.</param>
		/// <returns>A StateRelayPower message.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<StateRelayPowerResponse> SetRelayPowerAsync(LightBulb bulb, int relayIndex, bool enable) {
			if (bulb == null)
				throw new ArgumentNullException(nameof(bulb));
			var level = enable ? 65535 : 0;

			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return await BroadcastMessageAsync<StateRelayPowerResponse>(
				bulb.HostName, header, MessageType.SetRelayPower, (byte) relayIndex, (ushort) level);
		}
	}
}