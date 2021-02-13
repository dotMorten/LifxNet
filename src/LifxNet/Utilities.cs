using System;

namespace LifxNet
{
	internal static class Utilities
	{
		public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
		public static UInt16[] RgbToHsl(LifxColor rgb)
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
			return new[] {
				(UInt16)(h / 360 * 65535),
				(UInt16)(s * 65535),
				(UInt16)(max * 65535)
			};
		}


	}
}
