using System;
using System.Drawing;
using System.Globalization;

namespace LifxNet
{
    /// <summary>
    /// Extend the normal System.Drawing.Color class and make it work with HSBK
    /// </summary>
    public class LifxColor {
        private static double tolerance
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

        public float Hue {
            get => _color.GetHue();
            set => _color = HsbToColor(value, _color.GetSaturation(), _color.GetBrightness());
        }
        
        public float Saturation {
            get => _color.GetSaturation();
            set => _color = HsbToColor(_color.GetHue(), value, _color.GetBrightness());
        }
        
        public float Brightness {
            get => _color.GetBrightness();
            set => _color = HsbToColor(_color.GetHue(), _color.GetSaturation(), value);
        }

        public LifxColor(short h, short s, short b, short k) {
            _color = HsbToColor(h, s, b);
        }

        public LifxColor(int a, int r, int g, int b) {
            _color = Color.FromArgb(a, r, g, b);
        }
        
        public LifxColor(int r, int g, int b) {
            _color = Color.FromArgb(255, r, g, b);
        }

        public LifxColor(Color color) {
            _color = color;
        }


        private static Color HsbToColor(double h, double s, double b, int a = 255) {
            h = Math.Max(0D, Math.Min(360D, h));
            s = Math.Max(0D, Math.Min(1D, s));
            b = Math.Max(0D, Math.Min(1D, b));
            a = Math.Max(0, Math.Min(255, a));

            double r = 0D;
            double g = 0D;
            double bl = 0D;

            if (Math.Abs(s) < tolerance)
                r = g = bl = b;
            else {
                // the argb wheel consists of 6 sectors. Figure out which sector
                // you're in.
                double sectorPos = h / 60D;
                int sectorNumber = (int) Math.Floor(sectorPos);
                // get the fractional part of the sector
                double fractionalSector = sectorPos - sectorNumber;

                // calculate values for the three axes of the argb.
                double p = b * (1D - s);
                double q = b * (1D - s * fractionalSector);
                double t = b * (1D - s * (1D - fractionalSector));

                // assign the fractional colors to r, g, and b based on the sector
                // the angle is in.
                switch (sectorNumber) {
                    case 0:
                        r = b;
                        g = t;
                        bl = p;
                        break;
                    case 1:
                        r = q;
                        g = b;
                        bl = p;
                        break;
                    case 2:
                        r = p;
                        g = b;
                        bl = t;
                        break;
                    case 3:
                        r = p;
                        g = q;
                        bl = b;
                        break;
                    case 4:
                        r = t;
                        g = p;
                        bl = b;
                        break;
                    case 5:
                        r = b;
                        g = p;
                        bl = q;
                        break;
                }
            }

            return Color.FromArgb(
                a,
                Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{r * 255D:0.00}",CultureInfo.InvariantCulture)))),
                Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{g * 255D:0.00}",CultureInfo.InvariantCulture)))),
                Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{bl * 250D:0.00}", CultureInfo.InvariantCulture)))));
        }
    }
}