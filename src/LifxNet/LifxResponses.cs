using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	public abstract class LifxResponse
	{
		internal static LifxResponse Create(FrameHeader header, MessageType type, UInt32 source, byte[] payload)
		{
			LifxResponse response = null;
			switch(type)
			{
				case MessageType.DeviceAcknowledgement:
					response = new AcknowledgementResponse(payload);
					break;
				case MessageType.DeviceStateLabel:
					response = new StateLabelResponse(payload);
					break;
				case MessageType.LightState:
					response = new LightStateResponse(payload);
					break;
				case MessageType.LightStatePower:
					response = new LightPowerResponse(payload);
					break;
				case MessageType.DeviceStateVersion:
					response = new StateVersionResponse(payload);
					break;
				case MessageType.DeviceStateHostFirmware:
					response = new StateHostFirmwareResponse(payload);
					break;
				case MessageType.DeviceStateService:
					response = new StateServiceResponse(payload);
					break;
				default:
					response = new UnknownResponse(payload);
					break;
			}
			response.Header = header;
			response.Type = type;
			response.Payload = payload;
			response.Source = source;
			return response;
		}
		internal LifxResponse() { }
		internal FrameHeader Header { get; private set; }
		internal byte[] Payload { get; private set; }
		internal MessageType Type { get; private set; }
		internal UInt32 Source { get; private set; }
	}
	public class AcknowledgementResponse: LifxResponse
	{
		internal AcknowledgementResponse(byte[] payload) : base() { }
	}
	public class StateServiceResponse : LifxResponse
	{
		internal StateServiceResponse(byte[] payload) : base()
		{
			Service = payload[0];
			Port = BitConverter.ToUInt32(payload, 1);
		}
		public Byte Service { get; set; }
		public UInt32 Port { get; private set; }
	}
	
	public class StateLabelResponse : LifxResponse
	{
		internal StateLabelResponse(byte[] payload) : base() {

			if (payload != null)
				Label = Encoding.UTF8.GetString(payload, 0, payload.Length).Replace("\0", "");
		}
		public string Label { get; private set; }
	}
	public class LightStateResponse : LifxResponse
	{
		internal LightStateResponse(byte[] payload) : base()
		{
			Hue = BitConverter.ToUInt16(payload, 0);
			Saturation = BitConverter.ToUInt16(payload, 2);
			Brightness = BitConverter.ToUInt16(payload, 4);
			Kelvin = BitConverter.ToUInt16(payload, 6);
			IsOn = BitConverter.ToUInt16(payload, 10) > 0;
			Label = Encoding.UTF8.GetString(payload, 12, 32).Replace("\0","");
		}
		public UInt16 Hue { get; private set; }
		public UInt16 Saturation { get; private set; }
		public UInt16 Brightness { get; private set; }
		public UInt16 Kelvin { get; private set; }
		public bool IsOn { get; private set; }
		public string Label { get; private set; }
	}
	internal class LightPowerResponse : LifxResponse
	{
		internal LightPowerResponse(byte[] payload) : base()
		{
			IsOn = BitConverter.ToUInt16(payload, 0) > 0;
		}
		public bool IsOn { get; private set; }
	}

	public class StateVersionResponse : LifxResponse
	{
		internal StateVersionResponse(byte[] payload) : base()
		{
			Vendor = BitConverter.ToUInt32(payload, 0);
			Product = BitConverter.ToUInt32(payload, 4);
			Version = BitConverter.ToUInt32(payload, 8);
		}
		public UInt32 Vendor { get; private set; }
		public UInt32 Product { get; private set; }
		public UInt32 Version { get; private set; }
	}
	public class StateHostFirmwareResponse : LifxResponse
	{
		internal StateHostFirmwareResponse(byte[] payload) : base()
		{
			var nanoseconds = BitConverter.ToUInt64(payload, 0);
			Build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
			//8..15 UInt64 is reserved
			Version = BitConverter.ToUInt32(payload, 16);
		}
		public DateTime Build { get; private set; }
		public UInt32 Version { get; private set; }
	}

	internal class UnknownResponse : LifxResponse
	{
		internal UnknownResponse(byte[] payload) : base() {
		}
	}
}
