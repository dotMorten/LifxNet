using System;
using LifxNet;

namespace SampleApp.NET462
{
    class Program
    {
        static LifxClient _client;
        static void Main(string[] args) {
            _client = LifxClient.CreateAsync().Result;
            _client.DeviceDiscovered += ClientDeviceDiscovered;
            _client.DeviceLost += ClientDeviceLost;
            _client.StartDeviceDiscovery();
            Console.ReadKey();
        }

        private static void ClientDeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            Console.WriteLine("Device lost");
        }

        private static async void ClientDeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            Console.WriteLine($"Device {e.Device.MacAddressName} found @ {e.Device.HostName}");
            var version = await _client.GetDeviceVersionAsync(e.Device);
            var state = await _client.GetLightStateAsync((e.Device as LightBulb)!);
            Console.WriteLine($"{state.Label}\n\tIs on: {state.IsOn}\n\tHue: {state.Hue}\n\tSaturation: {state.Saturation}\n\tBrightness: {state.Brightness}\n\tTemperature: {state.Kelvin}");
            Console.WriteLine($"Product: {version.Product}\n\tVendor: {version.Vendor}\n\tVersion: {version.Version} ");
        }
    }
}
