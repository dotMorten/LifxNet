using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace LifxNet {
	[Serializable]
	public class Tile {
		public short AccelMeasX { get; set; }
		public short AccelMeasY { get; set; }
		public short AccelMeasZ { get; set; }
		public float UserX { get; set; }
		public float UserY { get; set; }
		public byte Width { get; set; }
		public byte Height { get; set; }
		public uint DeviceVersionVendor { get; set; }
		public uint DeviceVersionProduct { get; set; }
		public uint DeviceVersionVersion { get; set; }
		public ulong FirmwareBuild { get; set; }
		public ushort FirmwareVersionMinor { get; set; }
		public ushort FirmwareVersionMajor { get; set; }

		public Tile() {
		}

		public void CreateDefault(int index) {
			AccelMeasX = 0;
			AccelMeasY = 0;
			AccelMeasZ = 0;
			UserX = index * .5f;
			UserY = 8.06f;
			Width = 8;
			Height = 8;
			DeviceVersionProduct = 55;
			DeviceVersionVendor = 1;
			DeviceVersionVersion = 10;
			FirmwareBuild = 1532997580;
			FirmwareVersionMajor = 50;
			FirmwareVersionMinor = 3;
		}

		/// <summary>
		/// Read payload into tile
		/// </summary>
		/// <param name="payload"></param>
		public void LoadBytes(Payload payload) {
			
			AccelMeasX = payload.GetInt16();
			AccelMeasY = payload.GetInt16();
			AccelMeasZ = payload.GetInt16();
			payload.Advance(2);
			UserX = payload.GetFloat32();
			UserY = payload.GetFloat32();
			Width = payload.GetUint8();
			Height = payload.GetUint8();
			payload.Advance();
			DeviceVersionVendor = payload.GetUInt32();
			DeviceVersionProduct = payload.GetUInt32();
			DeviceVersionVersion = payload.GetUInt32();
			FirmwareBuild = payload.GetUInt64();
			payload.Advance(8);
			FirmwareVersionMajor = payload.GetUInt16();
			FirmwareVersionMinor = payload.GetUInt16();
			payload.Advance(4);
		}

		public byte[] ToBytes() {
			var output = new List<byte>();
			output.AddRange(BitConverter.GetBytes(AccelMeasX));
			output.AddRange(BitConverter.GetBytes(AccelMeasY));
			output.AddRange(BitConverter.GetBytes(AccelMeasZ));
			output.AddRange(BitConverter.GetBytes((short) 0));
			output.AddRange(BitConverter.GetBytes(UserX));
			output.AddRange(BitConverter.GetBytes(UserY));
			output.Add(Width);
			output.Add(Height);
			output.Add(50); // Reserved
			output.AddRange(BitConverter.GetBytes(DeviceVersionVendor));
			output.AddRange(BitConverter.GetBytes(DeviceVersionProduct));
			output.AddRange(BitConverter.GetBytes(DeviceVersionVersion));
			output.AddRange(BitConverter.GetBytes(FirmwareBuild));
			output.AddRange(BitConverter.GetBytes(FirmwareBuild));
			output.AddRange(BitConverter.GetBytes(FirmwareVersionMajor));
			output.AddRange(BitConverter.GetBytes(FirmwareVersionMinor));
			output.AddRange(BitConverter.GetBytes(uint.MinValue)); // Reserved

			return output.ToArray();
		}
		
	}
}