using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LifxNet;
using Newtonsoft.Json;

namespace ColorSendTest {
	class Program {
		private static LifxClient _client;
		private static List<Device> _devicesBulb;
		private static List<Device> _devicesMulti;
		private static List<Device> _devicesMultiV2;
		private static List<Device> _devicesTile;
		private static List<Device> _devicesSwitch;
		
		static void Main(string[] args) {
			var tr1 = new TextWriterTraceListener(Console.Out);
			Trace.Listeners.Add(tr1);
			_devicesBulb = new List<Device>();
			_devicesMulti = new List<Device>();
			_devicesMultiV2 = new List<Device>();
			_devicesTile = new List<Device>();
			_devicesSwitch = new List<Device>();
			_client = LifxClient.CreateAsync().Result;
			_client.DeviceDiscovered += ClientDeviceDiscovered;
			_client.DeviceLost += ClientDeviceLost;
			_client.StartDeviceDiscovery();
			Task.Delay(15000);
			_client.StopDeviceDiscovery();
			Console.WriteLine("Please select a device type to test (Enter a number):");
			if (_devicesBulb.Count > 0) {
				Console.WriteLine("Bulbs: 1");
			}

			if (_devicesMulti.Count > 0) {
				Console.WriteLine("Multi Zone V1: 2");
			}
			
			if (_devicesMultiV2.Count > 0) {
				Console.WriteLine("Multi Zone V2: 3");
			}
			
			if (_devicesTile.Count > 0) {
				Console.WriteLine("Tiles: 4");
			}
			
			if (_devicesSwitch.Count > 0) {
				Console.WriteLine("Switch: 5");
			}

			var selection = int.Parse(Console.ReadLine() ?? "0");
			switch (selection) {
				case 1:
					Console.WriteLine("Flashing bulbs on and off.");
					FlashBulbs().ConfigureAwait(true);
					break;
				case 2:
					Console.WriteLine("Flashing multizone v1 devices on and off.");
					FlashMultizone().ConfigureAwait(true);
					break;
				case 3:
					Console.WriteLine("Flashing multizone v2 devices on and off.");
					FlashMultizoneV2().ConfigureAwait(true);
					break;
				case 4:
					Console.WriteLine("Flashing tile devices on and off.");
					FlashTiles().ConfigureAwait(true);
					break;
				case 5:
					Console.WriteLine("Toggling switches is not enabled yet.");
					FlashSwitches();
					break;
			}

			Console.WriteLine("All done!");
			Console.ReadKey();
		}

		private static async Task FlashBulbs() {
			// Save our existing states
			var stateList = new List<LightStateResponse>();
			var red = new LifxColor(255, 0, 0);
			var black = new LifxColor(0, 0, 0);
			foreach (var b in _devicesBulb) {
				var bulb = (LightBulb) b;
				var state = await _client.GetLightStateAsync(bulb);
				stateList.Add(state);
				await _client.SetPowerAsync(b, 1);
				await _client.SetBrightnessAsync(bulb, 255, TimeSpan.Zero);
			}

			Console.WriteLine($"Flashing {_devicesBulb.Count} bulbs.");
			foreach (var bulb in _devicesBulb.Cast<LightBulb>()) {
				_client.SetColorAsync(bulb, red, 2700).ConfigureAwait(false);
			}

			await Task.Delay(1000);
			foreach (var bulb in _devicesBulb.Cast<LightBulb>()) {
				_client.SetColorAsync(bulb, black, 2700).ConfigureAwait(false);
			}

			await Task.Delay(500);
			
			foreach (var bulb in _devicesBulb.Cast<LightBulb>()) {
				_client.SetColorAsync(bulb, red, 2700).ConfigureAwait(false);
			}

			await Task.Delay(1000);
			foreach (var bulb in _devicesBulb.Cast<LightBulb>()) {
				_client.SetColorAsync(bulb, black, 2700).ConfigureAwait(false);
			}

			await Task.Delay(500);
			// Set them to red
			var idx = 0;
			Console.WriteLine("Restoring bulb states.");
			foreach (var b in _devicesBulb) {
				var bulb = (LightBulb) b;
				var state = stateList[idx];
				await _client.SetBrightnessAsync(bulb, state.Brightness, TimeSpan.Zero);
				await _client.SetPowerAsync(bulb, state.IsOn ? 1 : 0);
				idx++;
			}
		}

		private static async Task FlashMultizone() {
			var stateList = new List<int>();
			var responses = new List<StateMultiZoneResponse>();
			foreach (var m in _devicesMulti) {
				var state = await _client.GetPowerAsync(m);
				stateList.Add(state);
				var zoneState = await _client.GetColorZonesAsync(m,0,8);
				responses.Add(zoneState);
				await _client.SetPowerAsync(m, 1);
			}

			var idx = 0;
			foreach (var m in _devicesMulti) {
				var state = responses[idx];
				var count = state.Count;
				var start = state.Index;
				var total = start - count;
				for (var i = start; i < count; i++) {
					var progress = (start - i) / total;
					var apply = i == count - 1; 
					_client.SetColorZonesAsync(m, i, i, Rainbow(progress), TimeSpan.Zero, apply);
				}
				idx++;
			}
			
			await Task.Delay(2000);
			
			idx = 0;
			Debug.WriteLine("Setting v1 multi to rainbow!");
			var black = new LifxColor(0, 0, 0);
			foreach (var m in _devicesMulti) {
				var state = responses[idx];
				var count = state.Count;
				var start = state.Index;
				var total = start - count;
				for (var i = start; i < count; i++) {
					await _client.SetColorZonesAsync(m, i, i, black, TimeSpan.Zero, true);
				}
				idx++;
			}
			
			idx = 0;
			Debug.WriteLine("Setting v1 multi to black/disabling.");
			foreach (var m in _devicesMulti) {
				var power = stateList[idx];
				if (power == 0) {
					await _client.SetPowerAsync(m, power);
				}
			}
		}

		private static async Task FlashMultizoneV2() {
			var stateList = new List<int>();
			var responses = new List<StateExtendedColorZonesResponse>();
			foreach (var m in _devicesMulti) {
				var state = await _client.GetPowerAsync(m);
				stateList.Add(state);
				var zoneState = await _client.GetExtendedColorZonesAsync(m);
				responses.Add(zoneState);
				await _client.SetPowerAsync(m, 1);
			}
			Debug.WriteLine("Setting devices to rainbow!");
			var idx = 0;
			foreach (var m in _devicesMulti) {
				var state = responses[idx];
				var count = state.Count;
				var start = state.Index;
				var total = start - count;
				var colors = new List<LifxColor>();
				
				for (var i = start; i < count; i++) {
					var progress = (start - i) / total;
					colors.Add(Rainbow(progress));
				}
				_client.SetExtendedColorZonesAsync(m, TimeSpan.Zero, start, colors, true);
				idx++;
			}
			
			await Task.Delay(2000);
			Debug.WriteLine("Setting v2 to black.");

			idx = 0;
			var black = new LifxColor(0, 0, 0);
			foreach (var m in _devicesMulti) {
				var state = responses[idx];
				var count = state.Count;
				var start = state.Index;
				var colors = new List<LifxColor>();
				for (var i = start; i < count; i++) {
					colors.Add(black);
				}
				_client.SetExtendedColorZonesAsync(m, TimeSpan.Zero, start, colors,true);
				idx++;
			}
			
			idx = 0;
			Debug.WriteLine("Resetting v2 multizone.");

			foreach (var m in _devicesMulti) {
				var power = stateList[idx];
				if (power == 0) {
					await _client.SetPowerAsync(m, power);
				}
			}
		}

		private static async Task FlashTiles() {
			var chains = new List<StateDeviceChainResponse>();
			foreach (var t in _devicesTile) {
				var state = await _client.GetDeviceChainAsync(t);
				chains.Add(state);
				await _client.SetPowerAsync(t, 1);
			}

			var idx = 0;
			Debug.WriteLine("Rainbowing tiles!");

			foreach (var t in _devicesTile) {
				var state = chains[idx];
				var tidx = 0;
				var colors = new List<LifxColor>();
				for (var c = 0; c < 64; c++) {
					var progress = c / 64;
					colors.Add(Rainbow(progress));
				}
				for (var i = state.StartIndex; i < state.TotalCount; i++) {
					_client.SetTileState64Async(t, i, 64, 1000, colors.ToArray());
				}
				idx++;
			}

			await Task.Delay(2000);
			
			idx = 0;
			Debug.WriteLine("Turning off tiles.");
			foreach (var t in _devicesTile) {
				var state = chains[idx];
				var colors = new List<LifxColor>();
				for (var c = 0; c < 64; c++) {
					colors.Add(new LifxColor(0,0,0));
				}
				for (var i = state.StartIndex; i < state.TotalCount; i++) {
					_client.SetTileState64Async(t, i, 64, 1000, colors.ToArray());
				}

				_client.SetPowerAsync(t, 0);
				idx++;
			}
		}

		private static void FlashSwitches() {
			
		}
		
		public static LifxColor Rainbow(float progress) {
			var div = Math.Abs(progress % 1) * 6;
			var ascending = (int) (div % 1 * 255);
			var descending = 255 - ascending;
			var alpha = 0;
			var output = (int) div switch {
				0 => Color.FromArgb(alpha, 255, ascending, 0),
				1 => Color.FromArgb(alpha, descending, 255, 0),
				2 => Color.FromArgb(alpha, 0, 255, ascending),
				3 => Color.FromArgb(alpha, 0, descending, 255),
				4 => Color.FromArgb(alpha, ascending, 0, 255),
				_ => Color.FromArgb(alpha, 255, 0, descending)
			};
			return new LifxColor(output);
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
            var added = false;
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
	                added = true;
	                _devicesMultiV2.Add(e.Device);
                } else {
	                added = true;
                    _devicesMulti.Add(e.Device);
                }
            }
            
            // Tile
            if (version.Product == 55) {
	            added = true;
                _devicesTile.Add(e.Device);
            }
            // Switch
            if (version.Product == 70) {
	            added = true;
                _devicesSwitch.Add(e.Device);
            }

            if (!added) {
	            _devicesBulb.Add(e.Device);
            }
        }
	}
}