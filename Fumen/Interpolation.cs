using System;
using System.Collections.Generic;
using System.Text;

namespace Fumen
{
	public class Interpolation
	{
		public static double Lerp(double startValue, double endValue, double startTime, double endTime, double currentTime)
		{
			if (endTime == startTime)
				return currentTime >= endTime ? endValue : startValue;
			var ret = startValue + ((currentTime - startTime) / (endTime - startTime)) * (endValue - startValue);
			if (startValue < endValue)
				return Clamp(ret, startValue, endValue);
			return Clamp(ret, endValue, startValue);
		}

		// TODO: Move
		public static double Clamp(double value, double min, double max)
		{
			return value < min ? min : value > max ? max : value;
		}

		public static float Lerp(float startValue, float endValue, float startTime, float endTime, float currentTime)
		{
			if (endTime == startTime)
				return currentTime >= endTime ? endValue : startValue;
			var ret = startValue + ((currentTime - startTime) / (endTime - startTime)) * (endValue - startValue);
			if (startValue < endValue)
				return Clamp(ret, startValue, endValue);
			return Clamp(ret, endValue, startValue);
		}

		// TODO: Move
		public static float Clamp(float value, float min, float max)
		{
			return value < min ? min : value > max ? max : value;
		}
	}
}
