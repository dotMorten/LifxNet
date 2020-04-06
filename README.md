
# LifxNet

A .NET Standard 1.3 library for LIFX.
Supports .NET, UWP, Xamarin iOS, Xamarin Android, and any other .NET Platform that has implemented .NET Standard 1.3+.

For Cloud Protocol based implementation, check out [isaacrlevin's repo](https://github.com/isaacrlevin/LifxCloudClient)

## Sponsoring

If you like this library and use it a lot, consider sponsoring me. Anything helps and encourages me to keep going.

See here for details: https://github.com/sponsors/dotMorten


### NuGet

Get the [Nuget package here](http://www.nuget.org/packages/LifxNet/):
```
PM> Install-Package LifxNet 
```

Tested with LIFX 2.0 Firmware.

Based on the official [LIFX protocol docs](https://github.com/LIFX/lifx-protocol-docs)

####Usage

```csharp
	client = await LifxNet.LifxClient.CreateAsync();
	client.DeviceDiscovered += Client_DeviceDiscovered;
	client.DeviceLost += Client_DeviceLost;
	client.StartDeviceDiscovery();

...

	private async void Client_DeviceDiscovered(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
	{
		var bulb = e.Device as LifxNet.LightBulb;
		await client.SetDevicePowerStateAsync(bulb, true); //Turn bulb on
		await client.SetColorAsync(bulb, Colors.Red, 2700); //Set color to Red and 2700K Temperature			
	}

```
See the sample apps for more examples.

Note: Be careful with sending too many messages to your bulbs - LIFX recommends a max of 20 messages pr second pr bulb. 
This is especially important when using sliders to change properties of the bulb - make sure you use a throttling
mechanism to avoid issues with your bulbs. See the sample app for one way to handle this.
