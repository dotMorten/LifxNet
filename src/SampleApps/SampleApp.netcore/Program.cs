using System;
using LifxNet;

namespace SampleApp.NET462
{
    class Program
    {
        static LifxClient client;
        static void Main(string[] args)
        {
            var task = LifxClient.CreateAsync();
            task.Wait();
            client = task.Result;
            client.Discovered += Client_DeviceDiscovered;
            client.Lost += Client_DeviceLost;
            client.StartDeviceDiscovery();
            Console.ReadKey();
        }

        private static void Client_DeviceLost(object sender, LifxClient.DiscoveryEventArgs e)
        {
            Console.WriteLine("Device lost");
        }

        private static async void Client_DeviceDiscovered(object sender, LifxClient.DiscoveryEventArgs e)
        {
            Console.WriteLine($"Device {e.Device.MacAddressName} found @ {e.Device.HostName}");
            var version = await client.GetDeviceVersionAsync(e.Device);
            //var label = await client.GetDeviceLabelAsync(e.Device);
            var state = await client.GetLightStateAsync(e.Device as LightBulb);
            Console.WriteLine($"{state.Label}\n\tIs on: {state.IsOn}\n\tHue: {state.Hue}\n\tSaturation: {state.Saturation}\n\tBrightness: {state.Brightness}\n\tTemperature: {state.Kelvin}");
        }
    }
}
