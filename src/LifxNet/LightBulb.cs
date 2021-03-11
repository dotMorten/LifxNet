﻿namespace LifxNet {
	/// <summary>
	/// LIFX light bulb
	/// </summary>
	public sealed class LightBulb : Device {
		/// <summary>
		/// Initializes a new instance of a bulb instead of relying on discovery. At least the host name must be provide for the device to be usable.
		/// </summary>
		/// <param name="hostname">Required</param>
		/// <param name="macAddress"></param>
		/// <param name="service"></param>
		/// <param name="port"></param>
		/// <param name="productId"></param>
		public LightBulb(string hostname, byte[] macAddress, byte service = 0, uint port = 0, uint productId = 0) :
			base(hostname,
				macAddress, service, port) {
		}
	}
}