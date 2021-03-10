using System;
using LifxNet;
using Newtonsoft.Json;

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
            Console.WriteLine("Version info: " + JsonConvert.SerializeObject(version));
            Console.WriteLine("State info: " + JsonConvert.SerializeObject(state));
            
            // Multi-zone devices
            if (version.Product == 31 || version.Product == 32 || version.Product == 38) {
                Console.WriteLine("Device is multi-zone, enumerating data.");
                var extended = false;
                // If new Z-LED or Beam, check if FW supports "extended" commands.
                if (version.Product == 32 || version.Product == 38) {
                    if (version.Version >= 1532997580) {
                        extended = true;
                        Console.WriteLine("Enabling extended firmware features.");
                    }
                }

                if (extended) {
                    var zones = await _client.GetExtendedColorZonesAsync(e.Device);
                    Console.WriteLine("Zones: " + JsonConvert.SerializeObject(zones));
                } else {
                    // Original device only supports eight zones?
                    var zones = await _client.GetColorZonesAsync(e.Device, 0, 8);
                    Console.WriteLine("Zones: " + JsonConvert.SerializeObject(zones));
                }
            }
            
            // Tile
            if (version.Product == 55) {
                Console.WriteLine("Device is a tile group, enumerating data.");
                var chain = await _client.GetDeviceChainAsync(e.Device);
                Console.WriteLine("Tile chain: " + JsonConvert.SerializeObject(chain));
            }
            // Switch
            if (version.Product == 70) {
                Console.WriteLine("Device is a switch, enumerating data.");
                var switchState = await _client.GetRelayPowerAsync(e.Device, 0);
                Console.WriteLine($"Switch State: {switchState.Level}");
            }
        }
    }
}
