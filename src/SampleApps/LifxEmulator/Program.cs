using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LifxNet;
using Console = Colorful.Console;

namespace LifxEmulator {
	internal static class Program {
		private static bool _quitFlag;
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static int _deviceVersion;
		
		public static void Main(string[] args) {
			var tr1 = new TextWriterTraceListener(Console.Out);
			Trace.Listeners.Add(tr1);
			Console.CancelKeyPress += HandleClose;
			Console.WriteLine("What device would you like to emulate? 1-4");
			Console.WriteLine("(0) - Bulb");
			Console.WriteLine("(1) - Z-LED Gen 1");
			Console.WriteLine("(2) - Z-LED Gen 2");
			Console.WriteLine("(3) - Beam");
			Console.WriteLine("(4) - Tile");
			Console.WriteLine("(5) - Switch");

			_deviceVersion = int.Parse(Console.ReadLine() ?? "0");
			Console.WriteLine("Emulation mode: " + _deviceVersion);
			StartListener().Wait();
		}

		private static void HandleClose(object sender, ConsoleCancelEventArgs args) {
			_quitFlag = true;
		}

		private static async Task StartListener() {
			
			var end = new IPEndPoint(IPAddress.Any, 56700);
			var client = new UdpClient(end) {Client = {Blocking = false}};
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			client.Client.SendBufferSize = 4096;
			client.Client.ReceiveBufferSize = 4096;
			Console.WriteLine("Starting listener...");
			while (_quitFlag == false) {
				Console.WriteLine("Loop.");
				try {
					var result = await client.ReceiveAsync();
					Console.WriteLine($"Message received from {result.RemoteEndPoint.Address}.");
					if (result.Buffer.Length <= 0) {
						continue;
					}

					Console.WriteLine("Got something...");
					await HandleIncomingMessages(result.Buffer, result.RemoteEndPoint, client);
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
				}
			}
			Console.WriteLine("Canceled.");
		}

		
		private static async Task HandleIncomingMessages(byte[] data, IPEndPoint endpoint, UdpClient client) {
			var remote = endpoint;
			var msg = await ParseMessage(data);
			
			if (msg.GetType() != typeof(AcknowledgementResponse) || msg.Header.AcknowledgeRequired) {
				Debug.WriteLine("Responding to " + remote.Address);
				await BroadcastMessageAsync(remote, msg, client);
			}
		}
		
		private static async Task BroadcastMessageAsync(IPEndPoint target, LifxResponse message, UdpClient client) {
			
			using var stream = new MemoryStream();
			WritePacketToStream(stream, message.Header, (ushort) message.Type, message.Payload);
			var msg = stream.ToArray();
			var text = string.Join(",", (from a in msg select a.ToString("X2")).ToArray());
			Debug.WriteLine($"Sending message to {target.Address}: " + text);

			await client.SendAsync(msg, msg.Length, target);
		}
		
		private static void WritePacketToStream(Stream outStream, FrameHeader header, ushort type, Payload payload) {
			if (payload == null) {
				Console.WriteLine("No payload, creating new...");
				payload = new Payload();
			}
			using var dw = new BinaryWriter(outStream);

			#region Frame

			Console.WriteLine($"Sending {payload.Length + 36} length message...");
			//size uint16
			dw.Write((ushort) (payload.ToArray().Length + 36)); //length
			// origin (2 bits, must be 0), reserved (1 bit, must be 0), addressable (1 bit, must be 1), protocol 12 bits must be 0x400) = 0x1400
			dw.Write((ushort) 0x3400); //protocol
			dw.Write(header.Identifier); //source identifier - unique value set by the client, used by responses. If 0, responses are broadcast instead

			#endregion Frame

			#region Frame address

			//The target device address is 8 bytes long, when using the 6 byte MAC address then left - 
			//justify the value and zero-fill the last two bytes. A target device address of all zeroes effectively addresses all devices on the local network
			dw.Write(header.TargetMacAddress); // target mac address - 0 means all devices
			dw.Write(new byte[] {0, 0, 0, 0, 0, 0}); //reserved 1

			//The client can use acknowledgements to determine that the LIFX device has received a message. 
			//However, when using acknowledgements to ensure reliability in an over-burdened lossy network ... 
			//causing additional network packets may make the problem worse. 
			//Client that don't need to track the updated state of a LIFX device can choose not to request a 
			//response, which will reduce the network burden and may provide some performance advantage. In
			//some cases, a device may choose to send a state update response independent of whether res_required is set.
			if (header.AcknowledgeRequired && header.ResponseRequired)
				dw.Write((byte) 0x03);
			else if (header.AcknowledgeRequired)
				dw.Write((byte) 0x02);
			else if (header.ResponseRequired)
				dw.Write((byte) 0x01);
			else
				dw.Write((byte) 0x00);
			//The sequence number allows the client to provide a unique value, which will be included by the LIFX 
			//device in any message that is sent in response to a message sent by the client. This allows the client
			//to distinguish between different messages sent with the same source identifier in the Frame. See
			//ack_required and res_required fields in the Frame Address.
			dw.Write(header.Sequence);

			#endregion Frame address

			#region Protocol Header

			//The at_time value should be zero for Set and Get messages sent by a client.
			//For State messages sent by a device, the at_time will either be the device
			//current time when the message was received or zero. StateColor is an example
			//of a message that will return a non-zero at_time value
			if (header.AtTime > DateTime.MinValue) {
				var time = header.AtTime.ToUniversalTime();
				dw.Write((ulong) (time - new DateTime(1970, 01, 01)).TotalMilliseconds * 10); //timestamp
			} else {
				dw.Write((ulong) 0);
			}

			#endregion Protocol Header

			dw.Write(type); //packet _type
			dw.Write((ushort) 0); //reserved
			dw.Write(payload.ToArray());
			dw.Flush();
		}
		
		private static async Task<LifxResponse> ParseMessage(byte[] packet) {
			using MemoryStream ms = new MemoryStream(packet);
			BinaryReader br = new BinaryReader(ms);
			//frame
			var size = br.ReadUInt16();
			if (packet.Length != size || size < 36)
				throw new Exception("Invalid packet");
			br.ReadUInt16(); //origin:2, reserved:1, addressable:1, protocol:12
			var source = br.ReadUInt32();
			var header = new FrameHeader(source);
			//frame address
			byte[] target = br.ReadBytes(8);
			  
			ms.Seek(6, SeekOrigin.Current); //skip reserved
			br.ReadByte(); //reserved:6, ack_required:1, res_required:1, 
			header.Sequence = br.ReadByte();
			//protocol header
			var nanoseconds = br.ReadUInt64();
			header.AtTime = Epoch.AddMilliseconds(nanoseconds * 0.000001);
			var type = (MessageType) br.ReadUInt16();
			Console.WriteLine($"Incoming type is {type}");
			ms.Seek(2, SeekOrigin.Current); //skip reserved
			var payload = new Payload(size > 36 ? br.ReadBytes(size - 36) : new byte[] { });
			if (type == MessageType.SetColorZones) {
				var start = payload.GetUint8();
				var end = payload.GetUint8();
				var color = payload.GetColor();
				Debug.WriteLine($"Setting zones {start} - {end} to {color}");
			}

			if (type == MessageType.SetTileState64) {
				var idx = payload.GetUint8();
				var len = payload.GetUint8();
				payload.Advance(); // reserved
				var x = payload.GetUint8();
				var y = payload.GetUint8();
				var width = payload.GetUint8();
				var duration = payload.GetUInt32();
				var colors = new List<LifxColor>();
				Console.WriteLine("Colors: ");
				for (var i = 0; i < 64; i++) {
					var color = payload.GetColor();
					Console.Write(i.ToString(),Color.FromArgb(color.R, color.G, color.B));
				}
				Console.WriteLine("");
			}
			var res = LifxResponse.Create(header, type, source,
				payload,_deviceVersion);
			await Task.FromResult(true);
			return res;
		}
	}
	
	internal class FrameHeader {
		public uint Identifier;
		public byte Sequence;
		public bool AcknowledgeRequired;
		public bool ResponseRequired;
		public byte[] TargetMacAddress = {0, 0, 0, 0, 0, 0, 0, 0};
		public DateTime AtTime = DateTime.MinValue;

		public FrameHeader() {
		}

		public FrameHeader(uint id, bool acknowledgeRequired = false) {
			Identifier = id;
			AcknowledgeRequired = acknowledgeRequired;
		}

		public string TargetMacAddressName {
			get { return string.Join(":", TargetMacAddress.Take(6).Select(tb => tb.ToString("X2")).ToArray()); }
		}
	}
	
	internal enum MessageType : ushort {
		//Device Messages
		DeviceGetService = 0x02,
		DeviceStateService = 0x03,
		//Undocumented?
		DeviceGetTime = 0x04,
		DeviceSetTime = 0x05,
		DeviceStateTime = 0x06,
		// Documented
		DeviceGetHostInfo = 12,
		DeviceStateHostInfo = 13,
		DeviceGetHostFirmware = 14,
		DeviceStateHostFirmware = 15,
		DeviceGetWifiInfo = 16,
		DeviceStateWifiInfo = 17,
		DeviceGetWifiFirmware = 18,
		DeviceStateWifiFirmware = 19,
		DeviceGetPower = 20,
		DeviceSetPower = 21,
		DeviceStatePower = 22,
		DeviceGetLabel = 23,
		DeviceSetLabel = 24,
		DeviceStateLabel = 25,
		DeviceGetVersion = 32,
		DeviceStateVersion = 33,
		DeviceGetInfo = 34,
		DeviceStateInfo = 35,
		DeviceAcknowledgement = 45,
		DeviceGetLocation = 48,
		DeviceSetLocation = 49,
		DeviceStateLocation = 50,
		DeviceGetGroup = 51,
		DeviceSetGroup = 52,
		DeviceStateGroup = 53,
		DeviceEchoRequest = 58,
		DeviceEchoResponse = 59,

		//Light messages
		LightGet = 101,
		LightSetColor = 102,
		LightSetWaveform = 103,
		LightState = 107,
		LightGetPower = 116,
		LightSetPower = 117,
		LightStatePower = 118,
		LightSetWaveformOptional = 119,

		//Infrared
		InfraredGet = 120,
		InfraredState = 121,
		InfraredSet = 122,

		//Multi zone
		SetColorZones = 501,
		GetColorZones = 502,
		StateZone = 503,
		StateMultiZone = 506,
		SetExtendedColorZones = 510,
		GetExtendedColorZones = 511,
		StateExtendedColorZones = 512,

		//Tile
		GetDeviceChain = 701,
		StateDeviceChain = 702,
		SetUserPosition = 703,
		GetTileState64 = 707,
		StateTileState64 = 711,
		SetTileState64 = 715,

		//Switch 
		GetRelayPower = 816,
		SetRelayPower = 817,
		StateRelayPower = 818,

		//Unofficial
		LightGetTemperature = 0x6E,

		//LightStateTemperature = 0x6f,
		SetLightBrightness = 0x68
	}
}