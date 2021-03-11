using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace LifxNet {
	/// <summary>
	/// Extend the normal System.Drawing.Color class and make it work with HSBK
	/// </summary>
	public class LifxColor {
		private static double Tolerance
			=> 0.000000000000001;

		private Color _color;

		
		/// <summary>
		/// Red
		/// </summary>
		public byte R {
			get => _color.R;
			set => _color = Color.FromArgb(_color.A, value, _color.G, _color.B);
		}

		/// <summary>
		/// Green
		/// </summary>
		public byte G {
			get => _color.G;
			set => _color = Color.FromArgb(_color.A, _color.R, value, _color.B);
		}

		/// <summary>
		/// Blue
		/// </summary>
		public byte B {
			get => _color.B;
			set => _color = Color.FromArgb(_color.A, _color.R, _color.G, value);
		}

		/// <summary>
		/// The hue, in degrees, of this Color. The hue is measured in degrees, ranging from 0.0 through 360.0, in HSL color space.
		/// </summary>
		public float Hue {
			get => _color.GetHue();
			set => _color = HsbToRgb(value, _color.GetSaturation(), _color.GetBrightness());
		}

		/// <summary>
		/// The hue, in the standard Lifx format, of this Color. The lifx hue is measured from 0 - 65565. 
		/// </summary>
		public int LifxHue => (int) _color.GetHue() / 360 * 65536;

		/// <summary>
		/// The saturation of this Color. The saturation ranges from 0.0 through 1.0, where 0.0 is grayscale and 1.0 is the most saturated.
		/// </summary>
		public float Saturation {
			get => _color.GetSaturation();
			set => _color = HsbToRgb(_color.GetHue(), value, _color.GetBrightness());
		}

		/// <summary>
		/// The saturation of this color in Lifx format. Range is 0 - 65535;
		/// </summary>
		public int LifxSaturation => (int) _color.GetSaturation() * 65535;

		/// <summary>
		/// The lightness of this Color. The lightness ranges from 0.0 through 1.0, where 0.0 represents black and 1.0 represents white.
		/// </summary>
		public float Brightness {
			get => _color.GetBrightness();
			set => _color = HsbToRgb(_color.GetHue(), _color.GetSaturation(), value);
		}

		/// <summary>
		/// The brightness of this color. The brightness range is 0 - 65535
		/// </summary>
		public int LifxBrightness => (int) _color.GetBrightness() * 65535;

		/// <summary>
		/// The temperature of this Color. The temperature ranges from 2700-9000
		/// </summary>
		public float K {
			get;
			set;
		}

		/// <summary>
		/// Retrieve the base System.Drawing.Color of this Color
		/// </summary>
		public Color Color => _color;

		/// <summary>
		/// Create a new LifxColor
		/// </summary>
		public LifxColor() {
			_color = Color.FromArgb(255, 0, 0, 0);
			K = 2700;
		}
		
		/// <summary>
		/// Create a color from HSBK values
		/// </summary>
		/// <param name="h">Hue: range 0 to 65535.</param>
		/// <param name="s">Saturation: range 0 to 65535.</param>
		/// <param name="b">Brightness: range 0 to 65535.</param>
		/// <param name="k">Kelvin: range 2500° (warm) to 9000° (cool). Default is 2700.</param>
		public LifxColor(ushort h, ushort s, ushort b, ushort k = 2700) {
			var hue = h / 65535 * 360;
			var sat = s / 65535f;
			var bri = b / 65535f;
			_color = HsbToRgb(hue, sat, bri);
			K = k;
		}
		
		/// <summary>
		/// Create a color from RGB Value, with default alpha of 255
		/// </summary>
		/// <param name="r">Red: Range 0 to 255</param>
		/// <param name="g">Green: Range 0 to 255</param>
		/// <param name="b">Blue: Range 0 to 255</param>
		public LifxColor(int r, int g, int b) {
			_color = Color.FromArgb(255, r, g, b);
			K = 2700;
		}

		/// <summary>
		/// Create a LifxColor from a System.Drawing.Color
		/// </summary>
		/// <param name="color">Base System.Drawing.Color</param>
		public LifxColor(Color color) {
			_color = color;
			K = 2700;
		}

		/// <summary>
		/// Serialize our color to a byte array
		/// </summary>
		/// <returns>HSBK formatted array of bytes.</returns>
		public byte[] ToBytes() {
			var output = new List<byte>();
			var hue = Hue / 360 * 65535;
			var sat = Saturation * 65535;
			var bri = Brightness * 65535;
			foreach (var u in new[] {(ushort) hue, (ushort) sat, (ushort) bri, (ushort) K}) {
				output.AddRange(BitConverter.GetBytes(u));
			}
				
			return output.ToArray();
		}


		/// <summary>
		/// Return System.Drawing.Color RGB string representation of the color
		/// </summary>
		/// <returns></returns>
		public string ToRgbString() {
			return R + ", " + G + ", " + B;
		}
		
		/// <summary>
		/// Return Lifx HSBK string representation of the color
		/// </summary>
		/// <returns></returns>
		public string ToHsbkString() {
			var hue = Hue / 360 * 65535;
			var sat = 65535 * Saturation;
			var bri = 65536 * Brightness;
			return hue + ", " + sat + ", " + bri + ", " + K;
		}

		
		/// <summary>
        /// Converts HSB to RGB, with a specified output Alpha.
        /// Arguments are limited to the defined range:
        /// does not raise exceptions.
        /// </summary>
        /// <param name="h">Hue, must be in [0, 360].</param>
        /// <param name="s">Saturation, must be in [0, 1].</param>
        /// <param name="b">Brightness, must be in [0, 1].</param>
        /// <param name="a">Output Alpha, must be in [0, 255].</param>
		private static Color HsbToRgb(double h, double s, double b, int a = 255) {
            h = Math.Max(0D, Math.Min(360D, h));
            s = Math.Max(0D, Math.Min(1D, s));
            b = Math.Max(0D, Math.Min(1D, b));
            a = Math.Max(0, Math.Min(255, a));

            var r = 0D;
            var g = 0D;
            var bl = 0D;

            if (Math.Abs(s) < 0.000000000000001) {
	            r = g = bl = b;
            } else {
                // the argb wheel consists of 6 sectors. Figure out which sector
                // you're in.
                var sectorPos = h / 60D;
                var sectorNumber = (int)Math.Floor(sectorPos);
                // get the fractional part of the sector
                var fractionalSector = sectorPos - sectorNumber;

                // calculate values for the three axes of the argb.
                var p = b * (1D - s);
                var q = b * (1D - s * fractionalSector);
                var t = b * (1D - s * (1D - fractionalSector));

                // assign the fractional colors to r, g, and b based on the sector
                // the angle is in.
                switch (sectorNumber) {
                    case 0 :
                        r = b;
                        g = t;
                        bl = p;
                        break;
                    case 1 :
                        r = q;
                        g = b;
                        bl = p;
                        break;
                    case 2 :
                        r = p;
                        g = b;
                        bl = t;
                        break;
                    case 3 :
                        r = p;
                        g = q;
                        bl = b;
                        break;
                    case 4 :
                        r = t;
                        g = p;
                        bl = b;
                        break;
                    case 5 :
                        r = b;
                        g = p;
                        bl = q;
                        break;
                }
            }

            return Color.FromArgb(
                    a,
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{r * 255D:0.00}")))),
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{g * 255D:0.00}")))),
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{bl * 250D:0.00}")))));
        }

	}
}