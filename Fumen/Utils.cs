using System;

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
		public static bool FloatEquals(this float f, float other)
		{
			return f - float.Epsilon <= other && f + float.Epsilon >= other;
		}

		public static bool DoubleEquals(this double d, double other)
		{
			return d - double.Epsilon <= other && d + double.Epsilon >= other;
		}
	}
}
