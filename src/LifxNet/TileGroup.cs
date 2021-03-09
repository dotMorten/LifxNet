using System;

namespace LifxNet {
	[Serializable]
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

		public void CreateDefault(int index) {
			AccelMeasX = 0;
			AccelMeasY = 0;
			AccelMeasZ = 0;
			UserX = index * .5f;
			UserY = index * 1;
			Width = 8;
			Height = 8;
			DeviceVersionProduct = 55;
			DeviceVersionVendor = 1;
			FirmwareBuild = 1532997580;
			FirmwareVersionMajor = 1;
			FirmwareVersionMajor = 1;
		}

	}
}