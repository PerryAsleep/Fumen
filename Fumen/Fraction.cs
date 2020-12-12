using System;

namespace Fumen
{
	public class Fraction : IComparable, IEquatable<Fraction>
	{
		public Fraction()
		{
		}

		public Fraction(Fraction other)
		{
			Numerator = other.Numerator;
			Denominator = other.Denominator;
		}

		public int Numerator { get; }
		public int Denominator { get; }

		public Fraction(int numerator, int denominator)
		{
			Numerator = numerator;
			Denominator = denominator;
		}

		public double ToDouble()
		{
			return (double)Numerator / Denominator;
		}

		public int CompareTo(object obj)
		{
			var other = (Fraction)obj;
			if (Denominator == 0 && other.Denominator == 0)
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
			if (obj == null)
				return false;
			if (!(obj is Fraction fraction))
				return false;
			return Equals(fraction);
		}

		public override int GetHashCode()
		{
			return ShiftAndWrap(Numerator.GetHashCode(), 2) ^ Denominator.GetHashCode();
		}

		public Fraction Reduce()
		{
			var gcd = GreatestCommonDenominator();
			if (gcd < 2)
				return new Fraction(this);
			return new Fraction(Numerator / gcd, Denominator / gcd);
		}

		public int GreatestCommonDenominator()
		{
			var a = Numerator;
			var b = Denominator;
			int r;
			while (b > 0)
			{
				r = a % b;
				a = b;
				b = r;
			}
			return a;
		}

		/// <summary>
		/// Algorithm from https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=netframework-4.8#System_Object_GetHashCode
		/// </summary>
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
	}
}
