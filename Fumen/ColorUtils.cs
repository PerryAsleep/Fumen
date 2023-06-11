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
	}
}
