using System;
using LifxNet;

namespace SampleApp.NET462
{
    class Program
    {
        static LifxClient _client;
        static void Main(string[] args)
        {
            _client = new LifxClient();
            _client.Discovered += Client_DeviceDiscovered;
            _client.Lost += Client_DeviceLost;
            _client.StartDeviceDiscovery();
            Console.ReadKey();
        }

        private static void Client_DeviceLost(object sender, LifxClient.DiscoveryEventArgs e)
        {
            Console.WriteLine("Device lost");
        }

        private static async void Client_DeviceDiscovered(object sender, LifxClient.DiscoveryEventArgs e)
        {
            Console.WriteLine($"Device {e.Device.MacAddressName} found @ {e.Device.HostName}");
            var version = await _client.GetDeviceVersionAsync(e.Device);
            //var label = await client.GetDeviceLabelAsync(e.Device);
            var state = await _client.GetLightStateAsync(e.Device as LightBulb);
            Console.WriteLine($"{state.Label}\n\tIs on: {state.IsOn}\n\tHue: {state.Hue}\n\tSaturation: {state.Saturation}\n\tBrightness: {state.Brightness}\n\tTemperature: {state.Kelvin}");
        }
    }
}
