using System;
using LifxNet;

namespace SampleApp.netcore
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
            
            // Multi-zone devices
            if (version.Product == 31 || version.Product == 32 || version.Product == 38) {
                var extended = false;
                // If new Z-LED or Beam, check if FW supports "extended" commands.
                if (version.Product == 32 || version.Product == 38) {
                    var fwVersion = await _client.GetDeviceHostFirmwareAsync(e.Device);
                    if (fwVersion.Version >= 1532997580) extended = true;
                }

                int zoneCount;
                if (extended) {
                    var zones = await _client.GetExtendedColorZonesAsync((e.Device as LightBulb)!);
                    zoneCount = zones.Count;
                } else {
                    // Original device only supports eight zones?
                    var zones = await _client.GetColorZonesAsync((e.Device as LightBulb)!, 0, 8);
                    zoneCount = zones.Count;
                }
                Console.WriteLine($"Device is multi-zone.\r\nExtended Support: {extended}\r\nZone Count: {zoneCount}");
            }
            
            // Tile
            if (version.Product == 55) {
                var chain = await _client.GetDeviceChainAsync((e.Device as LightBulb)!);
                Console.WriteLine($"Device is a tile group.\r\nTile count: {chain.TotalCount}");
            }
            // Switch
            if (version.Product == 70) {
                var switchState = await _client.GetRelayPowerAsync((e.Device as LightBulb)!, 0);
                Console.WriteLine($"Device is a switch. \r\nSwitch State: {switchState.Level}");
            }
        }
    }
}
