# LifxNet

A .NET library for LIFX

Tested with LIFX 2.0 Firmware.

Based on the official [LIFX protocol docs](https://github.com/LIFX/lifx-protocol-docs)

####Usage

```csharp
	client = await LifxNet.LifxClient.CreateAsync();
	client.DeviceDiscovered += Client_DeviceDiscovered;
	client.DeviceLost += Client_DeviceLost;
	client.StartDeviceDiscovery();

...

	private void Client_DeviceDiscovered(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
	{
		var bulb = e.Device as LifxNet.LightBulb;
		await client.SetDevicePowerStateAsync(bulb, true); //Turn bulb on
  }

```

Note: Be careful with sending too many messages to your bulbs - LIFX recommends a max of 20 messages pr second pr bulb. 
This is especially important when using sliders to change properties of the bulb - make sure you use a throttling
mechanism to avoid issues with your bulbs.
