using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	internal abstract class LifxPacket
	{
		private byte[] _payload;
		private ushort _type;
		protected LifxPacket(ushort type, byte[] payload)
		{
			_type = type;
			_payload = payload;
		}
		internal byte[] Payload { get { return _payload; } }
		internal ushort Type { get { return _type; } }

		protected LifxPacket(ushort type, object[] data)
		{
			_type = type;
			using (var ms = new MemoryStream())
			{
				StreamWriter bw = new StreamWriter(ms);
				foreach (var obj in data)
				{
					if (obj is byte)
					{
						bw.Write((byte)obj);
					}
					else if (obj is byte[])
					{
						bw.Write((byte[])obj);
					}
					else if (obj is UInt16)
						bw.Write((UInt16)obj);
					else if (obj is UInt32)
						bw.Write((UInt32)obj);
					else
						throw new NotImplementedException();
				}
				_payload = ms.ToArray();
			}
		}

		public static LifxPacket FromByteArray(byte[] data)
		{
			//			preambleFields = [
			//				{ name: "size"       , type:type.uint16_le },
			//				{ name: "protocol"   , type:type.uint16_le },
			//				{ name: "reserved1"  , type:type.byte4 }    ,
			//				{ name: "bulbAddress", type:type.byte6 }    ,
			//				{ name: "reserved2"  , type:type.byte2 }    ,
			//				{ name: "site"       , type:type.byte6 }    ,
			//				{ name: "reserved3"  , type:type.byte2 }    ,
			//				{ name: "timestamp"  , type:type.uint64 }   ,
			//				{ name: "packetType" , type:type.uint16_le },
			//				{ name: "reserved4"  , type:type.byte2 }    ,
			//			];
			MemoryStream ms = new MemoryStream(data);
			var br = new BinaryReader(ms);
			//Header
			ushort len = br.ReadUInt16(); //ReverseBytes(br.ReadUInt16()); //size uint16
			ushort protocol = br.ReadUInt16(); // ReverseBytes(br.ReadUInt16()); //origin = 0
			var identifier = br.ReadUInt32();
			byte[] bulbAddress = br.ReadBytes(6);
			byte[] reserved2 = br.ReadBytes(2);
			byte[] site = br.ReadBytes(6);
			byte[] reserved3 = br.ReadBytes(2);
			ulong timestamp = br.ReadUInt64();
			ushort packetType = br.ReadUInt16(); // ReverseBytes(br.ReadUInt16());
			byte[] reserved4 = br.ReadBytes(2);
			byte[] payload = new byte[] { };
			if (len > 0)
			{
				payload = br.ReadBytes(len);
			}
			LifxPacket packet = new UnknownPacket(packetType, payload)
			{
				BulbAddress = bulbAddress,
				TimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp),
				Site = site
			};
			//packet.Identifier = identifier;
			return packet;
		}

		private class UnknownPacket : LifxPacket
		{
			public UnknownPacket(ushort packetType, byte[] payload) : base(packetType, payload)
			{
			}
			public byte[] BulbAddress { get; set; }
			public DateTime TimeStamp { get; set; }
			public byte[] Site { get; set; }
		}
	}
}