using System;

namespace Fumen;

/// <summary>
/// Fraction.
/// </summary>
public class Fraction : IComparable, IEquatable<Fraction>
{
	public readonly int Numerator;
	public readonly int Denominator;

	public static Fraction FromString(string s)
	{
		if (string.IsNullOrEmpty(s))
			return null;
		var parts = s.Split('/');
		if (parts.Length != 2)
			return null;
		if (!int.TryParse(parts[0], out var numerator))
			return null;
		if (!int.TryParse(parts[1], out var denominator))
			return null;
		return new Fraction(numerator, denominator);
	}

	public Fraction(Fraction other)
	{
		Numerator = other.Numerator;
		Denominator = other.Denominator;
	}

	public Fraction(int numerator, int denominator)
	{
		Numerator = numerator;
		Denominator = denominator;
	}

	public double ToDouble()
	{
		return (double)Numerator / Denominator;
	}

	public static Fraction operator +(Fraction a, Fraction b)
	{
		return new Fraction(
			a.Numerator * b.Denominator + b.Numerator * a.Denominator,
			a.Denominator * b.Denominator).Reduce();
	}

	public static Fraction operator -(Fraction a, Fraction b)
	{
		return new Fraction(
			a.Numerator * b.Denominator - b.Numerator * a.Denominator,
			a.Denominator * b.Denominator).Reduce();
	}

	public int CompareTo(object obj)
	{
		var other = (Fraction)obj;
		if (other == null)
			return 1;
		if (Denominator == 0 && other.Denominator == 0)
			return 0;
		if (Numerator == 0 && other.Numerator == 0)
			return 0;
		if (Denominator == 0)
			return -1;
		if (other.Denominator == 0)
			return 1;
		return (int)((long)Numerator * other.Denominator - (long)other.Numerator * Denominator);
	}

	public bool Equals(Fraction other)
	{
		if (other == null)
			return false;
		return CompareTo(other) == 0;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Fraction fraction))
			return false;
		return Equals(fraction);
	}

	public override int GetHashCode()
	{
		return ShiftAndWrap(Numerator.GetHashCode(), 2) ^ Denominator.GetHashCode();
	}

	public override string ToString()
	{
		return $"{Numerator}/{Denominator}";
	}

	public Fraction Reduce()
	{
		var gcd = GreatestCommonDenominator(Numerator, Denominator);
		if (gcd < 2)
			return new Fraction(this);
		return new Fraction(Numerator / gcd, Denominator / gcd);
	}

	/// <summary>
	/// Shift and Wrap method for HashCode determination.
	/// </summary>
	/// <remarks>
	/// Algorithm from https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=netframework-4.8#System_Object_GetHashCode
	/// </remarks>
	/// <param name="value"></param>
	/// <param name="positions"></param>
	/// <returns></returns>
	private static int ShiftAndWrap(int value, int positions)
	{
		positions &= 0x1F;

		// Save the existing bit pattern, but interpret it as an unsigned integer.
		var number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
		// Preserve the bits to be discarded.
		var wrapped = number >> (32 - positions);
		// Shift and wrap the discarded bits.
		return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
	}

	public static int GreatestCommonDenominator(int a, int b)
	{
		while (b > 0)
		{
			var r = a % b;
			a = b;
			b = r;
		}

		return a;
	}

	public static int LeastCommonMultiple(int a, int b)
	{
		return a / GreatestCommonDenominator(a, b) * b;
	}
}
