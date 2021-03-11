using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using LifxNet;
using Newtonsoft.Json;

namespace LifxEmulator {
	/// <summary>
	/// Base class for LIFX response types
	/// </summary>
	public abstract class LifxResponse {
		internal static LifxResponse Create(FrameHeader header, MessageType type, uint source, Payload payload, int deviceVersion) {
			payload.Reset();
			switch (type) {
				case MessageType.DeviceGetService:
					type = MessageType.DeviceStateService;
					return new StateServiceResponse(header, type, source);
				case MessageType.DeviceEchoRequest:
					type = MessageType.DeviceEchoResponse;
					return new EchoResponse(header, type, payload, source);
				case MessageType.DeviceGetInfo:
					type = MessageType.DeviceStateInfo;
					return new StateInfoResponse(header, type, source);
				case MessageType.LightGet:
					type = MessageType.LightState;
					return new LightStateResponse(header, type, source);
				case MessageType.DeviceGetVersion:
					type = MessageType.DeviceStateVersion;
					return new StateVersionResponse(header, type, source, deviceVersion);
				case MessageType.DeviceGetHostFirmware:
					type = MessageType.DeviceStateHostFirmware;
					return new StateHostFirmwareResponse(header, type, source, deviceVersion);
				case MessageType.GetExtendedColorZones:
					type = MessageType.StateExtendedColorZones;
					return new StateExtendedColorZonesResponse(header, type, source);
				case MessageType.GetColorZones:
					type = MessageType.StateMultiZone;
					return new StateMultiZoneResponse(header, type, source);
				case MessageType.GetDeviceChain:
					type = MessageType.StateDeviceChain;
					return new StateDeviceChainResponse(header, type, source);
				case MessageType.GetRelayPower:
					type = MessageType.StateRelayPower;
					return new StateRelayPowerResponse(header, type, source);
				case MessageType.DeviceGetPower:
					type = MessageType.DeviceStatePower;
					return new StatePowerResponse(header, type, source);
				case MessageType.DeviceSetPower:
					type = MessageType.DeviceAcknowledgement;
					return new AcknowledgementResponse(header, type, source);
				default:
					type = MessageType.DeviceAcknowledgement;
					return new AcknowledgementResponse(header, type, source);
			}
		}

		internal LifxResponse(FrameHeader header, MessageType type, uint source) {
			Header = header;
			Type = type;
			Source = source;
		}

		internal FrameHeader Header { get; }
		internal Payload Payload { get; set; }
		internal MessageType Type { get; }
		internal uint Source { get; }
	}
	
	/// <summary>
	/// Response to GetService message.
	/// Provides the device Service and port.
	/// If the Service is temporarily unavailable, then the port value will be 0.
	/// </summary>
	internal class StateServiceResponse : LifxResponse {
		internal StateServiceResponse(FrameHeader header, MessageType type, uint source) : base(
			header, type, source) {
			Service = 1;
			Port = 56700;
			Payload = new Payload(new object[]{Service, Port});
		}

		private byte Service { get; }
		private ulong Port { get; }
	}

	/// <summary>
	/// Response to any message sent with ack_required set to 1. 
	/// </summary>
	internal class AcknowledgementResponse : LifxResponse {
		internal AcknowledgementResponse(FrameHeader header, MessageType type, uint source) : base(
			header, type, source) {
		}
	}

	/// <summary>
	/// Get the list of colors currently being displayed by zones
	/// </summary>
	public class StateMultiZoneResponse : LifxResponse {
		internal StateMultiZoneResponse(FrameHeader header, MessageType type, uint source) : base(
			header, type, source) {
			Count = 8;
			Index = 0;
			Colors = new LifxColor[Count];
			for (var i = Index; i < Count; i++) {
				Colors[i] = new LifxColor(255,0,0);
			}

			var args = new List<object> {(byte)Count, (byte)Index};
			args.AddRange(Colors);
			Payload = new Payload(args.ToArray());
		}

		/// <summary>
		/// Count - total number of zones on the device
		/// </summary>
		public ushort Count { get; }

		/// <summary>
		/// Index - Zone the message starts from
		/// </summary>
		public ushort Index { get; }

		/// <summary>
		/// The list of colors returned by the message
		/// </summary>
		public LifxColor[] Colors { get; }
	}
	
	
	/// <summary>
	/// Provides run-time information of device.
	/// </summary>
	public class StateInfoResponse : LifxResponse {
		internal StateInfoResponse(FrameHeader header, MessageType type, uint source) : base(header,
			type, source) {
			Time = DateTime.Now;
			Uptime = 5000;
			Downtime = 100000;
			var args = new List<object> {Time, Uptime, Downtime};
			Payload = new Payload(args.ToArray() );
		}

		/// <summary>
		/// Current time
		/// </summary>
		public DateTime Time { get; set; }

		/// <summary>
		/// Time since last power on (relative time in nanoseconds)
		/// </summary>
		public long Uptime { get; set; }

		/// <summary>
		/// Last power off period, 5 second accuracy (in nanoseconds)
		/// </summary>
		public long Downtime { get; set; }
	}
	

	/// <summary>
	/// Echo response with payload sent in the EchoRequest.
	/// </summary>
	public class EchoResponse : LifxResponse {
		internal EchoResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, source) {
			RequestPayload = payload.ToArray();
			Payload = payload;
		}

		/// <summary>
		/// Payload sent in the EchoRequest.
		/// </summary>
		public byte[] RequestPayload { get; set; }
	}
	

	/// <summary>
	/// The StateZone message represents the state of a single zone with the index field indicating which zone is represented. The count field contains the count of the total number of zones available on the device.
	/// </summary>
	public class StateDeviceChainResponse : LifxResponse {
		internal StateDeviceChainResponse(FrameHeader header, MessageType type, uint source) : base(
			header,
			type, source) {
			Tiles = new List<Tile>();
			TotalCount = 16;
			StartIndex = 0;
			var args = new List<object>();
			args.Add(StartIndex);
			for (var i = StartIndex; i < TotalCount; i++) {
				var tile = new Tile();
				tile.CreateDefault(i);
				Tiles.Add(tile);
				args.Add(tile.ToBytes());
			}
			args.Add(TotalCount);
			Payload = new Payload(args.ToArray());
			Payload.Rewind();
		}

		/// <summary>
		/// Count - total number of zones on the device
		/// </summary>
		public byte TotalCount { get; }

		/// <summary>
		/// Start Index - Zone the message starts from
		/// </summary>
		public byte StartIndex { get; }

		/// <summary>
		/// The list of colors returned by the message
		/// </summary>
		public List<Tile> Tiles { get; }
	}
	
	public class StatePowerResponse : LifxResponse {
		internal StatePowerResponse(FrameHeader header, MessageType type,  uint source) : base(header,
			type, source) {
			Level = 65535;
			var args = new List<object> {Level};
			Payload = new Payload(args.ToArray());
		}
		
		/// <summary>
		/// Zero implies standby and non-zero sets a corresponding power draw level. Currently only 0 and 65535 are supported.
		/// </summary>
		public ulong Level { get; set; }
		
	}

	/// <summary>
	/// Get the list of colors currently being displayed by zones
	/// </summary>
	public class StateExtendedColorZonesResponse : LifxResponse {
		internal StateExtendedColorZonesResponse(FrameHeader header, MessageType type, uint source) :
			base(header, type, source) {
			Colors = new List<LifxColor>();
			Count = 8;
			Index = 0;
			for (var i = Index; i < Count; i++) {
				Colors.Add(new LifxColor(255,0,0));
			}

			var args = new List<object> {Count, Index, (byte) Colors.Count};
			args.AddRange(Colors);
			Payload = new Payload(args.ToArray());
		}

		/// <summary>
		/// Count - total number of zones on the device
		/// </summary>
		public ushort Count { get; private set; }

		/// <summary>
		/// Index - Zone the message starts from
		/// </summary>
		public ushort Index { get; private set; }

		/// <summary>
		/// The list of colors returned by the message
		/// </summary>
		public List<LifxColor> Colors { get; private set; }
	}

	/// <summary>
	/// Sent by a device to provide the current light state
	/// </summary>
	public class LightStateResponse : LifxResponse {
		internal LightStateResponse(FrameHeader header, MessageType type, uint source) : base(header,
			type, source) {

			var args = new List<object> {
				new LifxColor(255, 0, 0),
				0,
				(uint) 65535,
				"Test Light",
				(ulong) 0
			};
			Payload = new Payload(args.ToArray());
		}

		/// <summary>
		/// Hue
		/// </summary>
		public ushort Hue { get; }

		/// <summary>
		/// Saturation (0=desaturated, 65535 = fully saturated)
		/// </summary>
		public ushort Saturation { get; }

		/// <summary>
		/// Brightness (0=off, 65535=full brightness)
		/// </summary>
		public ushort Brightness { get; }

		/// <summary>
		/// Bulb color temperature
		/// </summary>
		public ushort Kelvin { get; }

		/// <summary>
		/// Power state
		/// </summary>
		public bool IsOn { get; }

		/// <summary>
		/// Light label
		/// </summary>
		public string Label { get; }
	}

	/// <summary>
	/// Response to GetVersion message.	Provides the hardware version of the device.
	/// </summary>
	public class StateVersionResponse : LifxResponse {
		internal StateVersionResponse(FrameHeader header, MessageType type, uint source, int deviceVersion) : base(
			header, type, source) {
			Product = 32;

			switch (deviceVersion) {
				case 0:
					Product = 1;
					break;
				case 1:
					Product = 31;
					break;
				case 2:
					Product = 32;
					break;
				case 3:
					Product = 38;
					break;
				case 4:
					Product = 55;
					break;
				case 5:
					Product = 70;
					break;
			}
			Vendor = 1;
			Version = 1;
			var args = new List<object> {Vendor, Product, Version};
			Payload = new Payload(args.ToArray());
		}

		/// <summary>
		/// Vendor ID
		/// </summary>
		public uint Vendor { get; }

		/// <summary>
		/// Product ID
		/// </summary>
		public uint Product { get; }

		/// <summary>
		/// Hardware version
		/// </summary>
		public uint Version { get; }
	}

	/// <summary>
	/// Response to GetHostFirmware message. Provides host firmware information.
	/// </summary>
	public class StateHostFirmwareResponse : LifxResponse {
		internal StateHostFirmwareResponse(FrameHeader header, MessageType type, uint source, int deviceVersion) : base(
			header, type, source) {
			Build = DateTime.Now;
			ulong reserved = 0;
			ulong version = 1532997580;
			var args = new List<object> {Build, reserved, version};
			Payload = new Payload(args.ToArray());
		}

		/// <summary>
		/// Firmware build time
		/// </summary>
		public DateTime Build { get; }

		/// <summary>
		/// Firmware version
		/// </summary>
		public uint VersionMinor { get; }
		public uint VersionMajor { get; }
		
	}

	/// <summary>
	/// Response to GetVersion message.	Provides the hardware version of the device.
	/// </summary>
	public class StateRelayPowerResponse : LifxResponse {
		internal StateRelayPowerResponse(FrameHeader header, MessageType type, uint source) : base(
			header, type, source) {
			RelayIndex = 0;
			Level = 65536;
			Payload = new Payload(new object[]{RelayIndex, Level});
		}

		/// <summary>
		/// The relay on the switch starting from 0
		/// </summary>
		public int RelayIndex { get; }

		/// <summary>
		/// The value of the relay
		/// </summary>
		public int Level { get; }
	}

	
}