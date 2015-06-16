using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SampleApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		ObservableCollection<LifxNet.LightBulb> bulbs = new ObservableCollection<LifxNet.LightBulb>();
		LifxNet.LifxClient client = null;
		public MainPage()
		{
			this.InitializeComponent();
			bulbList.ItemsSource = bulbs;
		}
		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			client = await LifxNet.LifxClient.CreateAsync();
			client.DeviceDiscovered += Client_DeviceDiscovered;
			client.DeviceLost += Client_DeviceLost;
			client.StartDeviceDiscovery();
		}
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			client.DeviceDiscovered -= Client_DeviceDiscovered;
			client.DeviceLost -= Client_DeviceLost;
			client.StopDeviceDiscovery();
			client = null;
			base.OnNavigatingFrom(e);
		}
		private void Client_DeviceLost(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
		{
			var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				var bulb = e.Device as LifxNet.LightBulb;
				if (bulbs.Contains(bulb))
					bulbs.Remove(bulb);
			});
		}

		private void Client_DeviceDiscovered(object sender, LifxNet.LifxClient.DeviceDiscoveryEventArgs e)
		{
			var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				var bulb = e.Device as LifxNet.LightBulb;
				if (!bulbs.Contains(bulb))
					bulbs.Add(bulb);
			});
		}

		private async void bulbList_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LifxNet.LightBulb;
			if (bulb != null)
			{
				Name.Text = await client.GetDeviceLabelAsync(bulb);
				PowerState.IsOn = await client.GetLightPowerAsync(bulb);
				var state = await client.GetLightStateAsync(bulb);
				hue = state.Hue;
				saturation = state.Saturation;
				translate.X = ColorGrid.ActualWidth / 65535 * hue;
				translate.Y = ColorGrid.ActualHeight / 65535 * saturation;
				brightnessSlider.Value = state.Brightness;
			}
		}
		UInt16 hue;
		UInt16 saturation;

		private async void PowerState_Toggled(object sender, RoutedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LifxNet.LightBulb;
			if (bulb != null)
			{
				await client.SetDevicePowerStateAsync(bulb, PowerState.IsOn);
			}
		}

		private void brightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LifxNet.LightBulb;
			if (bulb != null)
				SetColor(bulb, null, null, (UInt16)e.NewValue);
		}

		private Action pendingUpdateColorAction;
		private Task pendingUpdateColor;

		private async void SetColor(LifxNet.LightBulb bulb, ushort? hue, ushort? saturation, ushort? brightness)
		{
			if (client == null || bulb == null) return;
			//Is a task already running? This avoids updating too often.
			//Come back and execute last call when currently running operation is complete
			if (pendingUpdateColor != null) 
			{
				pendingUpdateColorAction = () => SetColor(bulb, hue, saturation, brightness);
				return;
			}

			this.hue = hue.HasValue ? hue.Value : this.hue;
			this.saturation = saturation.HasValue ? saturation.Value : this.saturation;
			var b = brightness.HasValue ? brightness.Value : (UInt16)brightnessSlider.Value;
			var setColorTask = client.SetColorAsync(bulb, this.hue, this.saturation, b, 2700, TimeSpan.Zero);
			var throttleTask = Task.Delay(50); //Ensure task takes minimum 50 ms (no more than 20 messages per second)
			pendingUpdateColor = Task.WhenAll(new Task[] { setColorTask, throttleTask });
 			try
			{
				Task timeoutTask = Task.Delay(2000);
				await Task.WhenAny(new Task[] { timeoutTask, pendingUpdateColor });
				if(!pendingUpdateColor.IsCompleted)
				{
					//timeout
				}
			}
			catch { } //ignore errors (usually timeout)
			pendingUpdateColor = null;
			if (pendingUpdateColorAction != null) //if a pending action is waiting, run it now;
			{
				var a = pendingUpdateColorAction;
				pendingUpdateColorAction = null;
				a();
			}
		}

		private void ColorGrid_Tapped(object sender, TappedRoutedEventArgs e)
		{
			FrameworkElement elm = (FrameworkElement)sender;
			var p = e.GetPosition(elm);
			var Hue = p.X / elm.ActualWidth  * 65535;
			var Sat = p.Y / elm.ActualHeight * 65535;
			var bulb = bulbList.SelectedItem as LifxNet.LightBulb;
			if (bulb != null)
			{
				SetColor(bulb, (ushort)Hue, (ushort)Sat, null);
			}
			translate.X = p.X;
			translate.Y = p.Y;
		}
	}
}
