namespace Fumen;

public class MathUtils
{
	public static double Clamp(double value, double min, double max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static float Clamp(float value, float min, float max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static int Clamp(int value, int min, int max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static int FloorDouble(double value)
	{
		if (value <= int.MinValue)
			return int.MinValue;
		if (value >= int.MaxValue)
			return int.MaxValue;
		return (int)value;
	}
}
