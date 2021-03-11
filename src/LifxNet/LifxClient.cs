﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LifxNet {
	/// <summary>
	/// LIFX Client for communicating with bulbs
	/// </summary>
	public partial class LifxClient {
		private const int Port = 56700;
		private readonly UdpClient _socket;
		private bool _isRunning;

		private LifxClient() {
			IPEndPoint end = new IPEndPoint(IPAddress.Any, Port);
			_socket = new UdpClient(end) {Client = {Blocking = false}, DontFragment = true};
			_socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		}

		/// <summary>
		/// Creates a new LIFX client.
		/// </summary>
		/// <returns>client</returns>
		public static Task<LifxClient> CreateAsync() {
			LifxClient client = new LifxClient();
			client.Initialize();
			return Task.FromResult(client);
		}

		private void Initialize() {
			_isRunning = true;
			StartReceiveLoop();
		}


		private void StartReceiveLoop() {
			Task.Run(async () => {
				while (_isRunning)
					try {
						var result = await _socket.ReceiveAsync();
						if (result.Buffer.Length > 0) {
							HandleIncomingMessages(result.Buffer, result.RemoteEndPoint);
						}
					} catch {
						// ignored
					}
			});
		}

		private void HandleIncomingMessages(byte[] data, IPEndPoint endpoint) {
			var remote = endpoint;
			var msg = ParseMessage(data);
			if (remote.Port == 56700) Debug.WriteLine("Incoming Message Type: " + msg.Type);
			switch (msg.Type) {
				case MessageType.DeviceStateService:
					ProcessDeviceDiscoveryMessage(remote.Address, msg);
					break;
				default:
					if (_taskCompletions.ContainsKey(msg.Source)) {
						var tcs = _taskCompletions[msg.Source];
						tcs(msg);
					}

					break;
			}

			if (remote.Port == 56700)
				Debug.WriteLine("Received from {0}:{1}", remote,
					string.Join(",", (from a in data select a.ToString("X2")).ToArray()));
		}

		/// <summary>
		/// Disposes the client
		/// </summary>
		public void Dispose() {
			_isRunning = false;
			_socket.Dispose();
		}

		private Task<T> BroadcastMessageAsync<T>(string hostName, FrameHeader header, MessageType type,
			params object[] args)
			where T : LifxResponse {
			Debug.WriteLine("Broadcasting " + type + " to " + hostName);
			var payload = new Payload(args);

			return BroadcastPayloadAsync<T>(hostName, header, type, payload);
		}

		private async Task<T> BroadcastPayloadAsync<T>(string hostName, FrameHeader header, MessageType type,
			Payload payload)
			where T : LifxResponse {
			if (_socket == null)
				throw new InvalidOperationException("No valid socket");

			MemoryStream ms = new MemoryStream();
			WritePacketToStream(ms, header, (UInt16) type, payload);
			var data = ms.ToArray();
			Debug.WriteLine(
				string.Join(",", (from a in data select a.ToString("X2")).ToArray()));


			TaskCompletionSource<T>? tcs = null;
			if ( //header.AcknowledgeRequired && 
				header.Identifier > 0 &&
				typeof(T) != typeof(UnknownResponse)) {
				tcs = new TaskCompletionSource<T>();
				Action<LifxResponse> action = (r) => {
					if (r.GetType() == typeof(T))
						tcs.TrySetResult((T) r);
				};
				_taskCompletions[header.Identifier] = action;
			}

			using (MemoryStream stream = new MemoryStream()) {
				WritePacketToStream(stream, header, (UInt16) type, payload);
				var msg = stream.ToArray();
				await _socket.SendAsync(msg, msg.Length, hostName, Port);
			}

			//{
			//	await WritePacketToStreamAsync(stream, header, (UInt16)type, payload).ConfigureAwait(false);
			//}
			T result = default(T);
			if (tcs != null) {
				var _ = Task.Delay(1000).ContinueWith((t) => {
					if (!t.IsCompleted)
						tcs.TrySetException(new TimeoutException());
				});
				try {
					result = await tcs.Task.ConfigureAwait(false);
				} finally {
					_taskCompletions.Remove(header.Identifier);
				}
			}

			return result;
		}

		private static LifxResponse ParseMessage(byte[] packet) {
			using MemoryStream ms = new MemoryStream(packet);
			var header = new FrameHeader();
			BinaryReader br = new BinaryReader(ms);
			//frame
			var size = br.ReadUInt16();
			if (packet.Length != size || size < 36)
				throw new Exception("Invalid packet");
			br.ReadUInt16(); //origin:2, reserved:1, addressable:1, protocol:12
			var source = br.ReadUInt32();
			//frame address
			byte[] target = br.ReadBytes(8);
			header.TargetMacAddress = target;
			ms.Seek(6, SeekOrigin.Current); //skip reserved
			br.ReadByte(); //reserved:6, ack_required:1, res_required:1, 
			header.Sequence = br.ReadByte();
			//protocol header
			var nanoseconds = br.ReadUInt64();
			header.AtTime = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
			var type = (MessageType) br.ReadUInt16();
			ms.Seek(2, SeekOrigin.Current); //skip reserved
			return LifxResponse.Create(header, type, source,
				new Payload(size > 36 ? br.ReadBytes(size - 36) : new byte[] { }));
		}

		private static void WritePacketToStream(Stream outStream, FrameHeader header, ushort type, Payload payload) {
			using var dw = new BinaryWriter(outStream);

			#region Frame

			//size uint16
			dw.Write((ushort) (payload.Length + 36)); //length
			// origin (2 bits, must be 0), reserved (1 bit, must be 0), addressable (1 bit, must be 1), protocol 12 bits must be 0x400) = 0x1400
			dw.Write((ushort) 0x3400); //protocol
			dw.Write(header
				.Identifier); //source identifier - unique value set by the client, used by responses. If 0, responses are broadcast instead

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
}