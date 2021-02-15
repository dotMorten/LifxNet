using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LifxNet {
	/// <summary>
	/// A wrapper class for a byte payload
	/// Any time the payload is read from, our pointer increments
	/// the proper number of bits until the end is reached,
	/// at which time a message will be logged. Should eventually throw an error or something...
	/// </summary>
	public class Payload {
		private byte[] Data { get; set; }
		private int Pointer { get; set; }
		
		/// <summary>
		/// Get the length of the internal byte array
		/// </summary>
		public int Length => Data.Length;

		/// <summary>
		/// Initialize with a byte array
		/// </summary>
		/// <param name="data"></param>
		public Payload(byte[] data) {
			Data = data;
		}

		/// <summary>
		/// Return our base byte array
		/// </summary>
		/// <returns></returns>
		public byte[] ToArray() {
			return Data;
		}

		/// <summary>
		/// Convert base byte array to a list
		/// </summary>
		/// <returns></returns>
		public List<byte> ToList() {
			return Data.ToList();
		}

		/// <summary>
		/// Serialize base byte array to a string
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return Data.ToString();
		}

		/// <summary>
		/// Check to see if we still have data to read
		/// </summary>
		/// <returns></returns>
		public bool HasContent() {
			return Pointer < Data.Length;
		}

		/// <summary>
		/// Rewind our pointer N bits
		/// </summary>
		/// <param name="len">How far to rewind. Default is 1.</param>
		public void Rewind(int len = 1) {
			Pointer -= len;
			if (Pointer < 0) Pointer = 0;
		}

		/// <summary>
		/// Forward our pointer N bits
		/// </summary>
		/// <param name="len">How far to advance. Default is 1.</param>
		public void Advance(int len = 1) {
			Pointer += len;
			if (Pointer >= Data.Length) Pointer = Data.Length - 1;
		}

		/// <summary>
		/// Forward the pointer to the end of the array
		/// </summary>
		public void FastForward() {
			Pointer = Data.Length - 1;
		}

		/// <summary>
		/// Reset our pointer to 0
		/// </summary>
		public void Reset() {
			Pointer = 0;
		}

		/// <summary>
		/// Read LifxColor from array and increment pointer 8 bytes
		/// </summary>
		/// <returns></returns>
		public LifxColor GetColor() {
			if (Pointer + 16 < Data.Length) {
				var h = GetUInt16();
				var s = GetUInt16();
				var b = GetUInt16();
				var k = GetUInt16();
				return new LifxColor(h, s, b, k);
			}
			FastForward();
			Debug.WriteLine($"Error getting color, pointer {Pointer} is out of range: " + Data.Length);
			return new LifxColor();
		}
		
		/// <summary>
		/// Read Uint8 from array and increment pointer 1 byte
		/// </summary>
		/// <returns>byte</returns>
		public byte GetUint8() {
			if (Pointer + 1 < Data.Length) {
				var output = Data[Pointer];
				Pointer++;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Uint8 from payload, pointer {Pointer} out of range: " + Data.Length);
			return 0;
		}

		/// <summary>
		/// Read UInt16 from array and increment pointer 2 bits
		/// </summary>
		/// <returns>ushort</returns>
		public ushort GetUInt16() {
			if (Pointer + 2 < Data.Length) {
				var output = BitConverter.ToUInt16(Data.ToArray(), Pointer);
				Pointer += 2;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Uint16 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}

		/// <summary>
		/// Read Int16 from array and increment pointer 2 bits.
		/// </summary>
		/// <returns>short</returns>
		public short GetInt16() {
			if (Pointer + 2 < Data.Length) {
				var output = BitConverter.ToInt16(Data.ToArray(), Pointer);
				Pointer += 2;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting int16 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}

		/// <summary>
		/// Read Int32 from array and increment pointer 4 bits.
		/// </summary>
		/// <returns>int</returns>
		public int GetInt32() {
			if (Pointer + 4 < Data.Length) {
				var output = BitConverter.ToInt32(Data.ToArray(), Pointer);
				Pointer += 4;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Int32 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}
		
		/// <summary>
		/// Read a UInt32 from array and increment pointer 4 bits.
		/// </summary>
		/// <returns></returns>
		public uint GetUInt32() {
			if (Pointer + 4 < Data.Length) {
				var output = BitConverter.ToUInt32(Data.ToArray(), Pointer);
				Pointer += 4;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Uint32 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}
		
		/// <summary>
		/// Read an Int64 from array and increment pointer 8 bits.
		/// </summary>
		/// <returns>long</returns>
		public long GetInt64() {
			if (Pointer + 8 < Data.Length) {
				var output = BitConverter.ToInt64(Data.ToArray(), Pointer);
				Pointer += 8;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Int64 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}
		
		/// <summary>
		/// Read a UInt64 from array and increment pointer 8 bits.
		/// </summary>
		/// <returns>ulong</returns>
		public ulong GetUInt64() {
			if (Pointer + 8 < Data.Length) {
				var output = BitConverter.ToUInt64(Data.ToArray(), Pointer);
				Pointer += 8;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Uint64 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}

		/// <summary>
		/// Read a Float32 from array and increment pointer 4 bits.
		/// </summary>
		/// <returns>float</returns>
		public float GetFloat32() {
			if (Pointer + 4 < Data.Length) {
				var output = BitConverter.ToSingle(Data.ToArray(), Pointer);
				Pointer += 4;
				return output;
			}
			FastForward();
			Debug.WriteLine($"Error getting Float32 from payload, pointer {Pointer} of range: " + Data.Length);
			return 0;
		}

		/// <summary>
		/// Read a string from our payload.
		/// </summary>
		/// <param name="length">The number of chars to read. If none specified, will read the entire payload</param>
		/// <returns>string</returns>
		public string GetString(int length = -1) {
			if (length == -1) length = Data.Length - 1 - Pointer;
			if (Pointer + length < Data.Length) {
				var output = Encoding.UTF8.GetString(Data, Pointer, length);
				Pointer += length;
				return output;
			}
			var str = Encoding.UTF8.GetString(Data, Pointer, Data.Length - 1 - Pointer);
			Debug.WriteLine($"Error getting string, pointer {Pointer} out of range: " + Data.Length);
			FastForward();
			return str;
		}
	}
}