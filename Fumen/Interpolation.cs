using System;

namespace Fumen
{
	public class Interpolation
	{
		/// <summary>
		/// Return a linearly interpolated value between the given start and end values based on the given
		/// current time with respect to the given start and end times.
		/// The returned value will be clamped to be within the range defined by the given start and end values.
		/// </summary>
		public static double Lerp(double startValue, double endValue, double startTime, double endTime, double currentTime)
		{
			if (endTime.DoubleEquals(startTime))
				return currentTime >= endTime ? endValue : startValue;
			var ret = startValue + (currentTime - startTime) / (endTime - startTime) * (endValue - startValue);
			if (startValue < endValue)
				return Clamp(ret, startValue, endValue);
			return Clamp(ret, endValue, startValue);
		}

		// TODO: Move
		public static double Clamp(double value, double min, double max)
		{
			return value < min ? min : value > max ? max : value;
		}

		/// <summary>
		/// Return a linearly interpolated value between the given start and end values based on the given
		/// current time with respect to the given start and end times.
		/// The returned value will be clamped to be within the range defined by the given start and end values.
		/// </summary>
		public static float Lerp(float startValue, float endValue, float startTime, float endTime, float currentTime)
		{
			if (endTime.FloatEquals(startTime))
				return currentTime >= endTime ? endValue : startValue;
			var ret = startValue + (currentTime - startTime) / (endTime - startTime) * (endValue - startValue);
			if (startValue < endValue)
				return Clamp(ret, startValue, endValue);
			return Clamp(ret, endValue, startValue);
		}

		// TODO: Move
		public static float Clamp(float value, float min, float max)
		{
			return value < min ? min : value > max ? max : value;
		}

		/// <summary>
		/// Return a logarithmicly interpolated value between the given start and end values based on the given
		/// current time with respect to the given start and end times.
		/// The returned value will be clamped to be within the range defined by the given start and end values.
		/// Note that logarithmic values approach infinity as their inputs approach 0.
		/// </summary>
		public static float LogarithmicInterpolate(float startValue, float endValue, float startTime, float endTime, float currentTime)
		{
			return (float)Lerp(startValue, endValue, Math.Log(startTime), Math.Log(endTime), Math.Log(currentTime));
		}

		/// <summary>
		/// Return a logarithmicly interpolated value between the given start and end values based on the given
		/// current time with respect to the given start and end times.
		/// The returned value will be clamped to be within the range defined by the given start and end values.
		/// Note that logarithmic values approach infinity as their inputs approach 0.
		/// </summary>
		public static double LogarithmicInterpolate(double startValue, double endValue, double startTime, double endTime, double currentTime)
		{
			return Lerp(startValue, endValue, Math.Log(startTime), Math.Log(endTime), Math.Log(currentTime));
		}
	}
}
