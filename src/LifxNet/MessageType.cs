using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	internal enum MessageType : ushort
	{
		//Device Messages
		DeviceGetService = 0x02,
		DeviceStateService = 0x03,
		DeviceGetTime = 0x04,
		DeviceSetTime = 0x05,
		DeviceStateTime = 0x06,
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
		DeviceEchoRequest = 58,
		DeviceEchoResponse = 59,
		//Light messages
		LightGet = 101,
		LightSetColor = 102,
		LightState = 107,
		LightGetPower = 116,
		LightSetPower = 117,
		LightStatePower = 118,
		//Infrared
		InfraredGet = 120,
		InfraredState = 121,
		InfraredSet = 122,

		//Unofficial
		LightGetTemperature = 0x6E,
        //LightStateTemperature = 0x6f,
		SetLightBrightness = 0x68
	}
}
