namespace LifxNet {
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