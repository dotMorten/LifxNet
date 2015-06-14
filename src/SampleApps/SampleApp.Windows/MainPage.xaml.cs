using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
			}
		}

		private async void PowerState_Toggled(object sender, RoutedEventArgs e)
		{
			var bulb = bulbList.SelectedItem as LifxNet.LightBulb;
			if (bulb != null)
			{
				await client.SetDevicePowerStateAsync(bulb, PowerState.IsOn);
			}
		}
	}
}
