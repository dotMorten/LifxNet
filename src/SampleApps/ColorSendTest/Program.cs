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
		
		static async Task Main(string[] args) {
			var tr1 = new TextWriterTraceListener(Console.Out);
			_devicesBulb = new List<Device>();
			_devicesMulti = new List<Device>();
			_devicesMultiV2 = new List<Device>();
			_devicesTile = new List<Device>();
			_devicesSwitch = new List<Device>();
			_client = LifxClient.CreateAsync().Result;
			_client.DeviceDiscovered += ClientDeviceDiscovered;
			_client.DeviceLost += ClientDeviceLost;
			Console.WriteLine("Enumerating devices, please wait 15 seconds...");
			_client.StartDeviceDiscovery();
			await Task.Delay(5000);
			_client.StopDeviceDiscovery();
			Trace.Listeners.Add(tr1);
			Console.WriteLine("Please select a device type to test (Enter a number):");
			if (_devicesBulb.Count > 0) {
				Console.WriteLine("1: Bulbs");
			}

			if (_devicesMulti.Count > 0) {
				Console.WriteLine("2: Multi Zone V1");
			}
			
			if (_devicesMultiV2.Count > 0) {
				Console.WriteLine("3: Multi Zone V2");
			}
			
			if (_devicesTile.Count > 0) {
				Console.WriteLine("4: Tiles");
			}
			
			if (_devicesSwitch.Count > 0) {
				Console.WriteLine("5: Switch");
			}

			var selection = int.Parse(Console.ReadLine() ?? "0");
			switch (selection) {
				case 1:
					Console.WriteLine("Flashing bulbs on and off.");
					await FlashBulbs();
					break;
				case 2:
					Console.WriteLine("Flashing multizone v1 devices on and off.");
					await FlashMultizone();
					break;
				case 3:
					Console.WriteLine("Flashing multizone v2 devices on and off.");
					await FlashMultizoneV2();
					break;
				case 4:
					Console.WriteLine("Flashing tile devices on and off.");
					await FlashTiles();
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
				_client.SetPowerAsync(m, 1).ConfigureAwait(false);
			}

			var idx = 0;
			foreach (var m in _devicesMulti) {
				var state = responses[idx];
				var count = state.Count;
				var start = state.Index;
				var total = start - count;
				for (var i = start; i < count; i++) {
					var pi = i * 1.0f;
					var progress = (start - pi) / total;
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
					_client.SetColorZonesAsync(m, i, i, black, TimeSpan.Zero, true);
				}
				idx++;
			}
			
			idx = 0;
			Debug.WriteLine("Setting v1 multi to black/disabling.");
			foreach (var m in _devicesMulti) {
				var power = stateList[idx];
				if (power == 0) {
					_client.SetPowerAsync(m, power);
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
				_client.SetPowerAsync(m, 1);
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
					var pi = i * 1.0f;
					var progress = (start - pi) / total;
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
					_client.SetPowerAsync(m, power);
				}
			}
		}

		private static async Task FlashTiles() {
			var chains = new List<StateDeviceChainResponse>();
			foreach (var t in _devicesTile) {
				var state = _client.GetDeviceChainAsync(t).Result;
				chains.Add(state);
				_client.SetPowerAsync(t, 1);
			}

			var idx = 0;
			Debug.WriteLine("Rainbowing tiles!");

			foreach (var t in _devicesTile) {
				var state = chains[idx];
				var tidx = 0;
				var colors = new List<LifxColor>();
				for (var c = 0; c < 64; c++) {
					var pi = c * 1.0f;
					var progress = pi / 64;
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

		private static LifxColor Rainbow(float progress) {
			Console.WriteLine("Progress is " + progress);
			var div = Math.Abs(progress % 1) * 6;
			var ascending = (int) (div % 1 * 255);
			var descending = 255 - ascending;
			var output = (int) div switch {
				0 => Color.FromArgb(255, ascending, 0),
				1 => Color.FromArgb(descending, 255, 0),
				2 => Color.FromArgb(0, 255, ascending),
				3 => Color.FromArgb(0, descending, 255),
				4 => Color.FromArgb(ascending, 0, 255),
				_ => Color.FromArgb(255, 0, descending)
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
            var added = false;
            // Multi-zone devices
            if (version.Product == 31 || version.Product == 32 || version.Product == 38) {
                var extended = false;
                // If new Z-LED or Beam, check if FW supports "extended" commands.
                if (version.Product == 32 || version.Product == 38) {
                    if (version.Version >= 1532997580) {
                        extended = true;
                    }
                }

                if (extended) {
	                added = true;
	                Console.WriteLine("Adding V2 Multi zone Device.");
	                _devicesMultiV2.Add(e.Device);
                } else {
	                added = true;
	                Console.WriteLine("Adding V1 Multi zone Device.");
                    _devicesMulti.Add(e.Device);
                }
            }
            
            // Tile
            if (version.Product == 55) {
	            added = true;
	            Console.WriteLine("Adding Tile Device");
                _devicesTile.Add(e.Device);
            }
            // Switch
            if (version.Product == 70) {
	            added = true;
	            Console.WriteLine("Adding Switch Device.");
                _devicesSwitch.Add(e.Device);
            }

            if (!added) {
	            Console.WriteLine("Adding Bulb.");
	            _devicesBulb.Add(e.Device);
            }
        }
	}
}