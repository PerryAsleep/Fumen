﻿using System;

namespace Fumen
{
	public class Utils
	{
		public static double ToSeconds(long micros)
		{
			return micros * 0.000001;
		}

		public static long ToMicros(double seconds)
		{
			return (long)(seconds * 1000000);
		}

		public static long ToMicrosRounded(double seconds)
		{
			return Convert.ToInt64(seconds * 1000000.0);
		}
	}

	public static class FumenExtensions
	{
		const float MinFloatDelta = 0.0000001f;

		public static bool FloatEquals(this float f, float other)
		{
			// TODO: This should be using similar logic to DoubleEquals but in the version of
			// dot net currently in use SingleToInt32Bits is not available.
			return f - MinFloatDelta <= other && f + MinFloatDelta >= other;
		}

		public static bool DoubleEquals(this double d, double other)
		{
			return HasMinimalDifference(d, other, 1);
		}

		/// <summary>
		/// Returns whether two given doubles are within less than the given number units apart
		/// where each representable double value is separated by one unit.
		/// </summary>
		/// <remarks>
		/// Adapted from https://learn.microsoft.com/en-us/dotnet/api/system.double.equals?view=net-7.0
		/// </remarks>
		private static bool HasMinimalDifference(double value1, double value2, int units)
		{
			long lValue1 = BitConverter.DoubleToInt64Bits(value1);
			long lValue2 = BitConverter.DoubleToInt64Bits(value2);

			// If the signs are different, return false except for +0 and -0.
			if ((lValue1 >> 63) != (lValue2 >> 63))
				return value1 == value2;

			return Math.Abs(lValue1 - lValue2) <= units;
		}
	}
}
