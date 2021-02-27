using System;

namespace Fumen
{
	/// <summary>
	/// Metric Position.
	/// Implements IComparable for comparable checks with other MetricPositions from the same
	/// piece if music. Comparing MetricPositions from separate pieces of music may produce
	/// unwanted results as different pieces of music use different time signatures and tempos.
	/// </summary>
	public class MetricPosition : IComparable<MetricPosition>, IEquatable<MetricPosition>
	{
		/// <summary>
		/// Measure within a piece of music. 0 indexed.
		/// </summary>
		public readonly int Measure;
		/// <summary>
		/// Beat within Measure. 0 indexed.
		/// </summary>
		public readonly int Beat;
		/// <summary>
		/// Subdivision within Beat.
		/// </summary>
		public readonly Fraction SubDivision = new Fraction(0, 0);

		public MetricPosition()
		{
		}

		public MetricPosition(int measure, int beat)
		{
			Measure = measure;
			Beat = beat;
		}

		public MetricPosition(int measure, int beat, int numerator, int denominator)
		{
			Measure = measure;
			Beat = beat;
			SubDivision = new Fraction(numerator, denominator);
		}

		public MetricPosition(int measure, int beat, Fraction subdivision)
		{
			Measure = measure;
			Beat = beat;
			SubDivision = subdivision;
		}

		public MetricPosition(MetricPosition other)
		{
			Measure = other.Measure;
			Beat = other.Beat;
			SubDivision = new Fraction(other.SubDivision);
		}

		public static bool operator >(MetricPosition a, MetricPosition b)
		{
			return a.CompareTo(b) > 0;
		}
		public static bool operator <(MetricPosition a, MetricPosition b)
		{
			return a.CompareTo(b) < 0;
		}
		public static bool operator ==(MetricPosition a, MetricPosition b)
		{
			if (a is null)
				return b is null;
			if (b is null)
				return false;
			return a.CompareTo(b) == 0;
		}
		public static bool operator !=(MetricPosition a, MetricPosition b)
		{
			if (a is null)
				return !(b is null);
			if (b is null)
				return true;
			return a.CompareTo(b) != 0;
		}
		public static bool operator >=(MetricPosition a, MetricPosition b)
		{
			return a.CompareTo(b) >= 0;
		}
		public static bool operator <=(MetricPosition a, MetricPosition b)
		{
			return a.CompareTo(b) <= 0;
		}

		public override bool Equals(object o)
		{
			return o is MetricPosition p && this == p;
		}

		public bool Equals(MetricPosition other)
		{
			return this == other;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			hash = unchecked(hash * 31 + Measure);
			hash = unchecked(hash * 31 + Beat);
			hash = unchecked(hash * 31 + SubDivision.GetHashCode());
			return hash;
		}

		public int CompareTo(MetricPosition other)
		{
			if (Measure != other.Measure)
				return Measure.CompareTo(other.Measure);
			if (Beat != other.Beat)
				return Beat.CompareTo(other.Beat);
			return SubDivision.CompareTo(other.SubDivision);
		}

		public override string ToString()
		{
			return $"Measure {Measure} Beat {Beat} SubDivision {SubDivision}";
		}
	}
}
