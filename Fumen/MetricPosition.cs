using System;
using System.Collections.Generic;
using System.Text;

namespace Fumen
{
	/// <summary>
	/// Metric Position.
	/// </summary>
	public class MetricPosition : IComparable
	{
		public MetricPosition()
		{
		}

		public MetricPosition(MetricPosition other)
		{
			Measure = other.Measure;
			Beat = other.Beat;
			SubDivision = new Fraction(other.SubDivision);
		}

		public int Measure { get; set; }
		public int Beat { get; set; }
		public Fraction SubDivision { get; set; } = new Fraction(0, 0);

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
			return a.CompareTo(b) == 0;
		}
		public static bool operator !=(MetricPosition a, MetricPosition b)
		{
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

		public int CompareTo(object obj)
		{
			var other = (MetricPosition)obj;
			if (Measure != other.Measure)
				return Measure.CompareTo(other.Measure);
			if (Beat != other.Beat)
				return Beat.CompareTo(other.Beat);
			return SubDivision.CompareTo(other.SubDivision);
		}
	}
}
