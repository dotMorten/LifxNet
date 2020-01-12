using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	/// <summary>
	/// Base class for LIFX response types
	/// </summary>
	public abstract class LifxResponse
	{
		internal static LifxResponse Create(FrameHeader header, MessageType type, UInt32 source, byte[] payload)
		{
			switch (type)
			{
				case MessageType.DeviceAcknowledgement:
					return new AcknowledgementResponse(header, type, payload, source);
				case MessageType.DeviceStateLabel:
					return new StateLabelResponse(header, type, payload, source);
				case MessageType.LightState:
					return new LightStateResponse(header, type, payload, source);
				case MessageType.LightStatePower:
					return new LightPowerResponse(header, type, payload, source);
				case MessageType.DeviceStateVersion:
					return new StateVersionResponse(header, type, payload, source);
				case MessageType.DeviceStateHostFirmware:
					return new StateHostFirmwareResponse(header, type, payload, source);
				case MessageType.DeviceStateService:
					return new StateServiceResponse(header, type, payload, source);
				default:
					return new UnknownResponse(header, type, payload, source);
			}
		}

		internal LifxResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) 
		{
			Header = header;
			Type = type;
			Payload = payload;
			Source = source;
		}

		internal FrameHeader Header { get; }
		internal byte[] Payload { get; }
		internal MessageType Type { get; }
		internal UInt32 Source { get; }
	}
	/// <summary>
	/// Response to any message sent with ack_required set to 1. 
	/// </summary>
	internal class AcknowledgementResponse: LifxResponse
	{
		internal AcknowledgementResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source) { }
	}
	/// <summary>
	/// Response to GetService message.
	/// Provides the device Service and port.
	/// If the Service is temporarily unavailable, then the port value will be 0.
	/// </summary>
	internal class StateServiceResponse : LifxResponse
	{
		internal StateServiceResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{
			Service = payload[0];
			Port = BitConverter.ToUInt32(payload, 1);
		}
		public Byte Service { get; }
		public UInt32 Port { get; }
	}
	/// <summary>
	/// Response to GetLabel message. Provides device label.
	/// </summary>
	internal class StateLabelResponse : LifxResponse
	{
		internal StateLabelResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{ 
			if (payload != null)
				Label = Encoding.UTF8.GetString(payload, 0, payload.Length).Replace("\0", "");
		}
		public string? Label { get; private set; }
	}
	/// <summary>
	/// Sent by a device to provide the current light state
	/// </summary>
	public class LightStateResponse : LifxResponse
	{
		internal LightStateResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{
			Hue = BitConverter.ToUInt16(payload, 0);
			Saturation = BitConverter.ToUInt16(payload, 2);
			Brightness = BitConverter.ToUInt16(payload, 4);
			Kelvin = BitConverter.ToUInt16(payload, 6);
			IsOn = BitConverter.ToUInt16(payload, 10) > 0;
			Label = Encoding.UTF8.GetString(payload, 12, 32).Replace("\0","");
		}
		/// <summary>
		/// Hue
		/// </summary>
		public UInt16 Hue { get; private set; }
		/// <summary>
		/// Saturation (0=desaturated, 65535 = fully saturated)
		/// </summary>
		public UInt16 Saturation { get; private set; }
		/// <summary>
		/// Brightness (0=off, 65535=full brightness)
		/// </summary>
		public UInt16 Brightness { get; private set; }
		/// <summary>
		/// Bulb color temperature
		/// </summary>
		public UInt16 Kelvin { get; private set; }
		/// <summary>
		/// Power state
		/// </summary>
		public bool IsOn { get; private set; }
		/// <summary>
		/// Light label
		/// </summary>
		public string Label { get; private set; }
	}
	internal class LightPowerResponse : LifxResponse
	{
		internal LightPowerResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{
			IsOn = BitConverter.ToUInt16(payload, 0) > 0;
		}
		public bool IsOn { get; private set; }
	}

	/// <summary>
	/// Response to GetVersion message.	Provides the hardware version of the device.
	/// </summary>
	public class StateVersionResponse : LifxResponse
	{
		internal StateVersionResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{
			Vendor = BitConverter.ToUInt32(payload, 0);
			Product = BitConverter.ToUInt32(payload, 4);
			Version = BitConverter.ToUInt32(payload, 8);
		}
		/// <summary>
		/// Vendor ID
		/// </summary>
		public UInt32 Vendor { get; private set; }
		/// <summary>
		/// Product ID
		/// </summary>
		public UInt32 Product { get; private set; }
		/// <summary>
		/// Hardware version
		/// </summary>
		public UInt32 Version { get; private set; }
	}
	/// <summary>
	/// Response to GetHostFirmware message. Provides host firmware information.
	/// </summary>
	public class StateHostFirmwareResponse : LifxResponse
	{
		internal StateHostFirmwareResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source)
		{
			var nanoseconds = BitConverter.ToUInt64(payload, 0);
			Build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
			//8..15 UInt64 is reserved
			Version = BitConverter.ToUInt32(payload, 16);
		}
		/// <summary>
		/// Firmware build time
		/// </summary>
		public DateTime Build { get; private set; }
		/// <summary>
		/// Firmware version
		/// </summary>
		public UInt32 Version { get; private set; }
	}

	internal class UnknownResponse : LifxResponse
	{
		internal UnknownResponse(FrameHeader header, MessageType type, byte[] payload, UInt32 source) : base(header, type, payload, source) { }
	}
}
