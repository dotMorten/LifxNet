using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using LifxNet;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp.Universal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

		ObservableCollection<LightBulb> bulbs = new ObservableCollection<LightBulb>();
		LifxClient _client;
        public MainPage()
        {
            InitializeComponent();
			bulbList.ItemsSource = bulbs;
		}
		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_client = await LifxClient.CreateAsync();
			_client.DeviceDiscovered += ClientDeviceDeviceDiscovered;
			_client.DeviceLost += ClientDeviceDeviceLost;
			_client.StartDeviceDiscovery();
			await Task.FromResult(true);
		}
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			_client.DeviceDiscovered -= ClientDeviceDeviceDiscovered;
			_client.DeviceLost -= ClientDeviceDeviceLost;
			_client.StopDeviceDiscovery();
			_client = null;
			base.OnNavigatingFrom(e);
		}
		private void ClientDeviceDeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
		{
			var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var bulb = e.Device as LightBulb;
				if (bulbs.Contains(bulb))
					bulbs.Remove(bulb);
			});
		}

		private void ClientDeviceDeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
		{
			var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var bulb = e.Device as LightBulb;
				if (!bulbs.Contains(bulb))
					bulbs.Add(bulb);
			});
		}

        private async void bulbList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var bulb = bulbList.SelectedItem as LightBulb;
            if (bulb != null)
            {
                var state = await _client.GetLightStateAsync(bulb);
                Name.Text = state.Label;
                PowerState.IsOn = state.IsOn;
                hue = state.Hue;
                saturation = state.Saturation;
                translate.X = ColorGrid.ActualWidth / 65535 * hue;
                translate.Y = ColorGrid.ActualHeight / 65535 * saturation;
                brightnessSlider.Value = state.Brightness;
                statePanel.Visibility = Visibility.Visible;
            }
            else
                statePanel.Visibility = Visibility.Collapsed;
        }
        UInt16 hue;
		UInt16 saturation;

		private async void PowerState_Toggled(object sender, RoutedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LightBulb;
			if (bulb != null)
			{
				await _client.SetDevicePowerStateAsync(bulb, PowerState.IsOn);
			}
		}

		private void brightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LightBulb;
			if (bulb != null)
				SetColor(bulb, null, null, (UInt16)e.NewValue);
		}

		private Action pendingUpdateColorAction;
		private Task pendingUpdateColor;

		private async void SetColor(LightBulb bulb, ushort? hue, ushort? saturation, ushort? brightness)
		{
			if (_client == null || bulb == null) return;
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
			var setColorTask = _client.SetColorAsync(bulb, this.hue, this.saturation, b, 2700, TimeSpan.Zero);
			var throttleTask = Task.Delay(50); //Ensure task takes minimum 50 ms (no more than 20 messages per second)
			pendingUpdateColor = Task.WhenAll(setColorTask, throttleTask);
			try
			{
				Task timeoutTask = Task.Delay(2000);
				await Task.WhenAny(timeoutTask, pendingUpdateColor);
				if (!pendingUpdateColor.IsCompleted)
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
			var Hue = p.X / elm.ActualWidth * 65535;
			var Sat = p.Y / elm.ActualHeight * 65535;
			var bulb = bulbList.SelectedItem as LightBulb;
			if (bulb != null)
			{
				SetColor(bulb, (ushort)Hue, (ushort)Sat, null);
			}
			translate.X = p.X;
			translate.Y = p.Y;
        }
    }
}
