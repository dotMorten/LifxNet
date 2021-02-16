using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace LifxNet {
	/// <summary>
	/// Base class for LIFX response types
	/// </summary>
	public abstract class LifxResponse {
		internal static LifxResponse Create(FrameHeader header, MessageType type, uint source, Payload payload) {
			payload.Reset();
			switch (type) {
				case MessageType.DeviceAcknowledgement:
					return new AcknowledgementResponse(header, type, payload, source);
				case MessageType.DeviceStateLabel:
					return new StateLabelResponse(header, type, payload, source);
				case MessageType.LightState:
					return new LightStateResponse(header, type, payload, source);
				case MessageType.LightStatePower:
					return new LightPowerResponse(header, type, payload, source);
				case MessageType.InfraredState:
					return new InfraredStateResponse(header, type, payload, source);
				case MessageType.DeviceStateVersion:
					return new StateVersionResponse(header, type, payload, source);
				case MessageType.DeviceStateHostFirmware:
					return new StateHostFirmwareResponse(header, type, payload, source);
				case MessageType.DeviceStateService:
					return new StateServiceResponse(header, type, payload, source);
				case MessageType.StateExtendedColorZones:
					return new StateExtendedColorZonesResponse(header, type, payload, source);
				case MessageType.StateZone:
					return new StateZoneResponse(header, type, payload, source);
				case MessageType.StateMultiZone:
					return new StateMultiZoneResponse(header, type, payload, source);
				case MessageType.StateDeviceChain:
					return new StateDeviceChainResponse(header, type, payload, source);
				case MessageType.StateTileState64:
					return new StateTileState64Response(header, type, payload, source);
				case MessageType.StateRelayPower:
					return new StateRelayPowerResponse(header, type, payload, source);
				case MessageType.DeviceStateHostInfo:
					return new StateHostInfoResponse(header, type, payload, source);
				case MessageType.DeviceStateWifiInfo:
					return new StateWifiInfoResponse(header, type, payload, source);
				case MessageType.DeviceStateWifiFirmware:
					return new StateWifiFirmwareResponse(header, type, payload, source);
				case MessageType.DeviceStatePower:
					return new StatePowerResponse(header, type, payload, source);
				case MessageType.DeviceStateInfo:
					return new StateInfoResponse(header, type, payload, source);
				case MessageType.DeviceStateLocation:
					return new StateLocationResponse(header, type, payload, source);
				case MessageType.DeviceStateGroup:
					return new StateGroupResponse(header, type, payload, source);
				case MessageType.DeviceEchoResponse:
					return new EchoResponse(header, type, payload, source);
				default:
					return new UnknownResponse(header, type, payload, source);
			}
		}

		internal LifxResponse(FrameHeader header, MessageType type, Payload payload, uint source) {
			Header = header;
			Type = type;
			Payload = payload;
			Source = source;
		}

		internal FrameHeader Header { get; }
		internal Payload Payload { get; }
		internal MessageType Type { get; }
		internal uint Source { get; }
	}

	/// <summary>
	/// Response to any message sent with ack_required set to 1. 
	/// </summary>
	internal class AcknowledgementResponse : LifxResponse {
		internal AcknowledgementResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
		}
	}

	/// <summary>
	/// The StateZone message represents the state of a single zone with the index field indicating which zone is represented. The count field contains the count of the total number of zones available on the device.
	/// </summary>
	public class StateZoneResponse : LifxResponse {
		internal StateZoneResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Count = payload.GetUInt16();
			Index = payload.GetUInt16();
			var h = payload.GetInt16();
			var s = payload.GetInt16();
			var b = payload.GetInt16();
			var k = payload.GetInt16();
			Color = new LifxColor(h, s, b, k);
			payload.Reset();
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
		public LifxColor Color { get; }
	}
	
	
	/// <summary>
	/// Response to GetHostInfo message.
	/// Provides host MCU information.
	/// </summary>
	public class StateHostInfoResponse : LifxResponse {
		internal StateHostInfoResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Signal = payload.GetFloat32();
			Tx = payload.GetUInt32();
			Rx = payload.GetUInt32();
			payload.Reset();
		}

		/// <summary>
		/// Bytes received since power on
		/// </summary>
		public uint Rx { get; set; }

		/// <summary>
		/// Bytes transmitted since power on
		/// </summary>
		public uint Tx { get; set; }

		/// <summary>
		/// Radio receive signal strength in milliWatts
		/// </summary>
		public float Signal { get; set; }
	}


	/// <summary>
	/// Response to GetWifiInfo message.
	/// Provides host Wifi information.
	/// </summary>
	public class StateWifiInfoResponse : LifxResponse {
		internal StateWifiInfoResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Signal = payload.GetFloat32();
			Tx = payload.GetUInt32();
			Rx = payload.GetUInt32();
			payload.Reset();
		}

		/// <summary>
		/// Bytes received since power on
		/// </summary>
		public uint Rx { get; set; }

		/// <summary>
		/// Bytes transmitted since power on
		/// </summary>
		public uint Tx { get; set; }

		/// <summary>
		/// Radio receive signal strength in milliWatts
		/// </summary>
		public float Signal { get; set; }
	}
	
	/// <summary>
	/// Response to GetWifiFirmware message.
	/// Provides Wifi subsystem information.
	/// </summary>
	public class StateWifiFirmwareResponse : LifxResponse {
		internal StateWifiFirmwareResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Build = payload.GetUInt64();
			// Skip 64-bit reserved
			payload.Advance(8);
			VersionMinor = payload.GetUInt16();
			VersionMajor = payload.GetUInt16();
			payload.Reset();
		}
		
		/// <summary>
		/// Firmware build time (epoch time)
		/// </summary>
		public ulong Build { get; set; }
		/// <summary>
		/// Minor firmware version number
		/// </summary>
		public ushort VersionMinor { get; set; }
		/// <summary>
		/// Major firmware version number
		/// </summary>
		public ushort VersionMajor { get; set; }


	}
	
	
	/// <summary>
	/// Provides device power level.
	/// </summary>
	public class StatePowerResponse : LifxResponse {
		internal StatePowerResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Level = payload.GetUInt16();
			payload.Reset();
		}
		
		/// <summary>
		/// Zero implies standby and non-zero sets a corresponding power draw level. Currently only 0 and 65535 are supported.
		/// </summary>
		public ulong Level { get; set; }
		
	}
	
	
	/// <summary>
	/// Provides run-time information of device.
	/// </summary>
	public class StateInfoResponse : LifxResponse {
		internal StateInfoResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Time = DateTimeOffset.FromUnixTimeSeconds(payload.GetInt64()).DateTime;
			Uptime = payload.GetInt64();
			Downtime = payload.GetInt64();
			payload.Reset();
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
	/// Device location.
	/// </summary>
	public class StateLocationResponse : LifxResponse {
		internal StateLocationResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Location = payload.GetBytes(16);
			Label = payload.GetString(32);
			Updated = payload.GetUInt64();
			payload.Reset();
		}

		public byte[] Location { get; set; }

		public string Label { get; set; }

		public ulong Updated { get; set; }
	}
	
	/// <summary>
	/// Device group.
	/// </summary>
	public class StateGroupResponse : LifxResponse {
		internal StateGroupResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Group = payload.GetBytes(16);
			Label = payload.GetString(32);
			Updated = payload.GetUInt64();
			payload.Reset();
		}

		public byte[] Group { get; set; }

		public string Label { get; set; }

		public ulong Updated { get; set; }
	}
	
	/// <summary>
	/// Echo response with payload sent in the EchoRequest.
	/// </summary>
	public class EchoResponse : LifxResponse {
		internal EchoResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			RequestPayload = payload.ToArray();
			payload.Reset();
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
		internal StateDeviceChainResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header,
			type, payload, source) {
			Tiles = new List<Tile>();
			StartIndex = payload.GetUint8();
			while (payload.HasContent()) {
				var tile = new Tile();
				tile.LoadPayload(payload);
				Tiles.Add(tile);
			}

			TotalCount = payload.GetUint8();
			if (TotalCount != Tiles.Count)
				Debug.WriteLine($"Warning, tile count doesn't match: {TotalCount} : {Tiles.Count}");
			payload.Reset();
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

	/// <summary>
	/// Get the list of colors currently being displayed by zones
	/// </summary>
	public class StateMultiZoneResponse : LifxResponse {
		internal StateMultiZoneResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			Colors = new List<LifxColor>();
			Count = payload.GetUInt16();
			Index = payload.GetUInt16();
			while (payload.HasContent()) {
				Colors.Add(payload.GetColor());
			}

			payload.Reset();
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
		public List<LifxColor> Colors { get; }
	}


	/// <summary>
	/// Get the list of colors currently being displayed by zones
	/// </summary>
	public class StateExtendedColorZonesResponse : LifxResponse {
		internal StateExtendedColorZonesResponse(FrameHeader header, MessageType type, Payload payload, uint source) :
			base(header, type, payload, source) {
			Colors = new List<LifxColor>();
			Count = payload.GetUInt16();
			Index = payload.GetUInt16();
			while (payload.HasContent()) {
				Colors.Add(payload.GetColor());
			}

			payload.Reset();
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
	/// Response to GetService message.
	/// Provides the device Service and port.
	/// If the Service is temporarily unavailable, then the port value will be 0.
	/// </summary>
	internal class StateServiceResponse : LifxResponse {
		internal StateServiceResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			Service = payload.GetUint8();
			Port = payload.GetUInt32();
			payload.Reset();
		}

		private byte Service { get; }
		private uint Port { get; }
	}

	/// <summary>
	/// Response to any message sent with ack_required set to 1. 
	/// </summary>
	public class StateTileState64Response : LifxResponse {
		internal StateTileState64Response(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			TileIndex = payload.GetUint8();
			// Skip one byte for reserved
			payload.Advance();
			X = payload.GetUint8();
			Y = payload.GetUint8();
			Width = payload.GetUint8();
			Colors = new LifxColor[64];
			for (var i = 0; i < Colors.Length; i++) {
				if (payload.HasContent()) {
					Colors[i] = payload.GetColor();
				} else {
					Debug.WriteLine($"Content size mismatch fetching colors: {i}/64: ");
				}
			}
		}

		public uint TileIndex { get; }
		public uint X { get; }
		public uint Y { get; }
		public uint Width { get; }
		public LifxColor[] Colors { get; }
	}

	/// <summary>
	/// Response to GetLabel message. Provides device label.
	/// </summary>
	internal class StateLabelResponse : LifxResponse {
		internal StateLabelResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Label = payload.GetString().Replace("\0", "");
			payload.Reset();
		}

		public string? Label { get; }
	}

	/// <summary>
	/// Sent by a device to provide the current light state
	/// </summary>
	public class LightStateResponse : LifxResponse {
		internal LightStateResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			Hue = payload.GetUInt16();
			Saturation = payload.GetUInt16();
			Brightness = payload.GetUInt16();
			Kelvin = payload.GetUInt16();
			IsOn = payload.GetUInt16() > 0;
			Label = payload.GetString(32).Replace("\\0", "");
			payload.Reset();
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

	internal class LightPowerResponse : LifxResponse {
		internal LightPowerResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
			IsOn = payload.GetUInt16() > 0;
			payload.Reset();
		}

		public bool IsOn { get; }
	}

	internal class InfraredStateResponse : LifxResponse {
		internal InfraredStateResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			Brightness = payload.GetUInt16();
			payload.Reset();
		}

		public ushort Brightness { get; }
	}

	/// <summary>
	/// Response to GetVersion message.	Provides the hardware version of the device.
	/// </summary>
	public class StateVersionResponse : LifxResponse {
		internal StateVersionResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			Vendor = Payload.GetUInt32();
			Product = Payload.GetUInt32();
			Version = Payload.GetUInt32();
			payload.Reset();
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
		internal StateHostFirmwareResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			var nanoseconds = payload.GetUInt64();
			Build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
			//8..15 UInt64 is reserved
			Version = payload.GetUInt32();
			payload.Reset();
		}

		/// <summary>
		/// Firmware build time
		/// </summary>
		public DateTime Build { get; }

		/// <summary>
		/// Firmware version
		/// </summary>
		public uint Version { get; }
	}

	/// <summary>
	/// Response to GetVersion message.	Provides the hardware version of the device.
	/// </summary>
	public class StateRelayPowerResponse : LifxResponse {
		internal StateRelayPowerResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(
			header, type, payload, source) {
			RelayIndex = payload.GetUint8();
			Level = payload.GetUInt16();
			payload.Reset();
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

	internal class UnknownResponse : LifxResponse {
		internal UnknownResponse(FrameHeader header, MessageType type, Payload payload, uint source) : base(header,
			type, payload, source) {
		}
	}
}