using System;

namespace LifxNet {
	/// <summary>
	/// LIFX tile
	/// </summary>
	public sealed class TileGroup : Device {
		/// <summary>
		/// Initializes a new instance of a bulb instead of relying on discovery. At least the host name must be provide for the device to be usable.
		/// </summary>
		/// <param name="hostname">Required</param>
		/// <param name="macAddress"></param>
		/// <param name="service"></param>
		/// <param name="port"></param>
		/// <param name="productId"></param>
		public TileGroup(string hostname, byte[] macAddress, byte service = 0, uint port = 0, uint productId = 0) : base(hostname,
			macAddress, service, port, productId) {
		}

		public void LoadPayload() {
			 
		}
	}

	public class Tile {
		public int AccelMeasX { get; set; }
		public int AccelMeasY { get; set; }
		public int AccelMeasZ { get; set; }
		public float UserX { get; set; }
		public float UserY { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public uint DeviceVersionVendor { get; set; }
		public uint DeviceVersionProduct { get; set; }
		public uint DeviceVersionVersion { get; set; }
		public long FirmwareBuild { get; set; }
		public short FirmwareVersionMinor { get; set; }
		public short FirmwareVersionMajor { get; set; }

		public Tile() {
			
		}

		public void LoadPayload(Payload payload) {
			AccelMeasX = payload.GetInt16();
			AccelMeasY = payload.GetInt16();
			AccelMeasZ = payload.GetInt16();
			// Skip 2 bytes for reserved
			payload.Advance(2);
			UserX = payload.GetFloat32();
			UserY = payload.GetFloat32();
			Width = payload.GetUint8();
			Height = payload.GetUint8();
			// Skip 2 bytes for reserved
			payload.Advance(2);
			DeviceVersionVendor = payload.GetUInt32();
			DeviceVersionProduct = payload.GetUInt32();
			DeviceVersionVendor = payload.GetUInt32();
			FirmwareBuild = payload.GetInt64();
			// Skip 8 bytes for reserved
			payload.Advance(8);
			FirmwareVersionMinor = payload.GetInt16();
			FirmwareVersionMajor = payload.GetInt16();
		}
	}
}