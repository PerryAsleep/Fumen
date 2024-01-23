using System;

namespace Fumen;

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
			return MathUtils.Clamp(ret, startValue, endValue);
		return MathUtils.Clamp(ret, endValue, startValue);
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
			return MathUtils.Clamp(ret, startValue, endValue);
		return MathUtils.Clamp(ret, endValue, startValue);
	}

	/// <summary>
	/// Return a linearly interpolated value between the given start and end values based on the given
	/// current time with respect to the given start and end times.
	/// The returned value will be clamped to be within the range defined by the given start and end values.
	/// </summary>
	public static float Lerp(float startValue, float endValue, int startTime, int endTime, int currentTime)
	{
		if (endTime == startTime)
			return currentTime >= endTime ? endValue : startValue;
		var ret = startValue + (float)(currentTime - startTime) / (endTime - startTime) * (endValue - startValue);
		if (startValue < endValue)
			return MathUtils.Clamp(ret, startValue, endValue);
		return MathUtils.Clamp(ret, endValue, startValue);
	}

	/// <summary>
	/// Return a linearly interpolated value between the given start and end values based on the given
	/// current time with respect to the given start and end times.
	/// The returned value will be clamped to be within the range defined by the given start and end values.
	/// </summary>
	public static float Lerp(float startValue, float endValue, long startTime, long endTime, long currentTime)
	{
		if (endTime == startTime)
			return currentTime >= endTime ? endValue : startValue;
		var ret = (float)(startValue + (double)(currentTime - startTime) / (endTime - startTime) * (endValue - startValue));
		if (startValue < endValue)
			return MathUtils.Clamp(ret, startValue, endValue);
		return MathUtils.Clamp(ret, endValue, startValue);
	}

	/// <summary>
	/// Return a logarithmicly interpolated value between the given start and end values based on the given
	/// current time with respect to the given start and end times.
	/// The returned value will be clamped to be within the range defined by the given start and end values.
	/// Note that logarithmic values approach infinity as their inputs approach 0.
	/// </summary>
	public static float LogarithmicInterpolate(float startValue, float endValue, float startTime, float endTime,
		float currentTime)
	{
		return (float)Lerp(startValue, endValue, Math.Log(startTime), Math.Log(endTime), Math.Log(currentTime));
	}

	/// <summary>
	/// Return a logarithmicly interpolated value between the given start and end values based on the given
	/// current time with respect to the given start and end times.
	/// The returned value will be clamped to be within the range defined by the given start and end values.
	/// Note that logarithmic values approach infinity as their inputs approach 0.
	/// </summary>
	public static double LogarithmicInterpolate(double startValue, double endValue, double startTime, double endTime,
		double currentTime)
	{
		return Lerp(startValue, endValue, Math.Log(startTime), Math.Log(endTime), Math.Log(currentTime));
	}

	/// <summary>
	/// Performs Hermite spline interpolation of values for four sequential, equally-spaced points.
	/// Adapted from https://stackoverflow.com/a/72122178
	/// </summary>
	/// <param name="x0">Point 0.</param>
	/// <param name="x1">Point 1.</param>
	/// <param name="x2">Point 2.</param>
	/// <param name="x3">Point 3.</param>
	/// <param name="t">Time value, normalized such that t at x1 is 0.0f and t at x2 is 1.0f.</param>
	/// <returns>Interpolated result at time t.</returns>
	public static float HermiteInterpolate(float x0, float x1, float x2, float x3, float t)
	{
		var d = x1 - x2;
		var c1 = x2 - x0;
		var c3 = x3 - x0 + 3.0f * d;
		var c2 = -2.0f * d - c1 - c3;
		return 0.5f * ((c3 * t + c2) * t + c1) * t + x1;
	}
}
