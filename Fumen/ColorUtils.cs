using System;

namespace Fumen
{
	public class ColorUtils
	{
		public static uint ColorRGBAInterpolate(uint startColor, uint endColor, float endPercent)
		{
			var startPercent = 1.0f - endPercent;
			return (uint)((startColor & 0xFF) * startPercent + (endColor & 0xFF) * endPercent)
				   | ((uint)(((startColor >> 8) & 0xFF) * startPercent + ((endColor >> 8) & 0xFF) * endPercent) << 8)
				   | ((uint)(((startColor >> 16) & 0xFF) * startPercent + ((endColor >> 16) & 0xFF) * endPercent) << 16)
				   | ((uint)(((startColor >> 24) & 0xFF) * startPercent + ((endColor >> 24) & 0xFF) * endPercent) << 24);
		}

		public static uint ColorRGBAInterpolateBGR(uint startColor, uint endColor, float endPercent)
		{
			var startPercent = 1.0f - endPercent;
			return (uint)((startColor & 0xFF) * startPercent + (endColor & 0xFF) * endPercent)
				   | ((uint)(((startColor >> 8) & 0xFF) * startPercent + ((endColor >> 8) & 0xFF) * endPercent) << 8)
				   | ((uint)(((startColor >> 16) & 0xFF) * startPercent + ((endColor >> 16) & 0xFF) * endPercent) << 16)
				   | (endColor & 0xFF000000);
		}

		public static uint ColorRGBAMultiply(uint color, float multiplier)
		{
			return (uint)Math.Min((color & 0xFF) * multiplier, byte.MaxValue)
				   | ((uint)Math.Min(((color >> 8) & 0xFF) * multiplier, byte.MaxValue) << 8)
				   | ((uint)Math.Min(((color >> 16) & 0xFF) * multiplier, byte.MaxValue) << 16)
				   | (color & 0xFF000000);
		}

		public static ushort ToBGR565(float r, float g, float b)
		{
			return (ushort)(((ushort)(r * 31) << 11) + ((ushort)(g * 63) << 5) + (ushort)(b * 31));
		}

		public static (byte, byte, byte, byte) ToChannels(uint rgba)
		{
			return ((byte)(rgba & 0x000000FF),
				(byte)((rgba & 0x0000FF00) >> 8),
				(byte)((rgba & 0x00FF0000) >> 16),
				(byte)((rgba & 0xFF000000) >> 24));
		}

		public static ushort ToBGR565(uint rgba)
		{
			return ToBGR565(
				(byte)((rgba & 0x00FF0000) >> 16) / (float)byte.MaxValue,
				(byte)((rgba & 0x0000FF00) >> 8) / (float)byte.MaxValue,
				(byte)(rgba & 0x000000FF) / (float)byte.MaxValue);
		}

		public static uint ToRGBA(float r, float g, float b, float a)
		{
			return ((uint)(byte)(a * byte.MaxValue) << 24)
				   + ((uint)(byte)(b * byte.MaxValue) << 16)
				   + ((uint)(byte)(g * byte.MaxValue) << 8)
				   + (byte)(r * byte.MaxValue);
		}

		public static (float, float, float, float) ToFloats(uint rgba)
		{
			return ((byte)(rgba & 0x000000FF) / (float)byte.MaxValue,
				(byte)((rgba & 0x0000FF00) >> 8) / (float)byte.MaxValue,
				(byte)((rgba & 0x00FF0000) >> 16) / (float)byte.MaxValue,
				(byte)((rgba & 0xFF000000) >> 24) / (float)byte.MaxValue);
		}

		/// <summary>
		/// Given a color represented by red, green, and blue floating point values ranging from 0.0f to 1.0f,
		/// return the hue, saturation, and value of the color.
		/// Hue is represented as a degree in radians between 0.0 and 2*pi.
		/// For pure grey colors the returned hue will be 0.0.
		/// </summary>
		public static (float, float, float) RgbToHsv(float r, float g, float b)
		{
			var h = 0.0f;
			var min = Math.Min(Math.Min(r, g), b);
			var max = Math.Max(Math.Max(r, g), b);

			var v = max;
			var s = max.FloatEquals(0.0f) ? 0.0f : (max - min) / max;
			if (!s.FloatEquals(0.0f))
			{
				var d = max - min;
				if (r.FloatEquals(max))
				{
					h = (g - b) / d;
				}
				else if (g.FloatEquals(max))
				{
					h = 2 + (b - r) / d;
				}
				else
				{
					h = 4 + (r - g) / d;
				}

				h *= (float)(Math.PI / 3.0f);
				if (h < 0.0f)
				{
					h += (float)(2.0f * Math.PI);
				}
			}

			return (h, s, v);
		}

		/// <summary>
		/// Given a color represented by hue, saturation, and value return the red, blue, and green
		/// values of the color. The saturation and value parameters are expected to be in the range
		/// of 0.0 to 1.0. The hue value is expected to be between 0.0 and 2*pi. The returned color
		/// values will be between 0.0 and 1.0.
		/// </summary>
		public static (float, float, float) HsvToRgb(float h, float s, float v)
		{
			float r, g, b;

			if (s.FloatEquals(0.0f))
			{
				r = v;
				g = v;
				b = v;
			}
			else
			{
				if (h.FloatEquals((float)(Math.PI * 2.0f)))
					h = 0.0f;
				else
					h = (float)(h * 3.0f / Math.PI);
				var sextant = (float)Math.Floor(h);
				var f = h - sextant;
				var p = v * (1.0f - s);
				var q = v * (1.0f - s * f);
				var t = v * (1.0f - s * (1.0f - f));
				switch (sextant)
				{
					default:
						r = v;
						g = t;
						b = p;
						break;
					case 1:
						r = q;
						g = v;
						b = p;
						break;
					case 2:
						r = p;
						g = v;
						b = t;
						break;
					case 3:
						r = p;
						g = q;
						b = v;
						break;
					case 4:
						r = t;
						g = p;
						b = v;
						break;
					case 5:
						r = v;
						g = p;
						b = q;
						break;
				}
			}

			return (r, g, b);
		}
	}
}
