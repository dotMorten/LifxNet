using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	internal static class Utilities
	{
		public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static void RunOnDispatcher(Action action)
		{
			var dispatcher = Windows.UI.Xaml.Window.Current.CoreWindow.Dispatcher;
			if (dispatcher == null)
				return;
			if (dispatcher == null || dispatcher.HasThreadAccess)
			{
				action();
			}
			else
			{
				var _ = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
					() => { action(); });
			}
		}

		public static UInt16[] RgbToHsl(Windows.UI.Color rgb)
		{
			// normalize red, green and blue values
			double r = (rgb.R / 255.0);
			double g = (rgb.G / 255.0);
			double b = (rgb.B / 255.0);

			double max = Math.Max(r, Math.Max(g, b));
			double min = Math.Min(r, Math.Min(g, b));

			double h = 0.0;
			if (max == r && g >= b)
			{
				h = 60 * (g - b) / (max - min);
			}
			else if (max == r && g < b)
			{
				h = 60 * (g - b) / (max - min) + 360;
			}
			else if (max == g)
			{
				h = 60 * (b - r) / (max - min) + 120;
			}
			else if (max == b)
			{
				h = 60 * (r - g) / (max - min) + 240;
			}

			double s = (max == 0) ? 0.0 : (1.0 - (min / max));
			return new UInt16[] {
				(UInt16)(h / 360 * 65535),
				(UInt16)(s * 65535),
				(UInt16)(max * 65535)
			};
		}


	}
}
