using System;
using System.Collections.Generic;
using System.Text;

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
	}
}
