using System;

namespace Fumen.ChartDefinition
{
	/// <summary>
	/// Event within a Chart Layer.
	/// </summary>
	public abstract class Event
	{
		/// <summary>
		/// Position of this Event represented as time in microseconds.
		/// </summary>
		public long TimeMicros { get; set; }
		/// <summary>
		/// Position of this Event represented as a MetricPosition.
		/// </summary>
		public MetricPosition MetricPosition { get; set; }
		/// <summary>
		/// Position of this Event represented as an integer value.
		/// </summary>
		public int IntegerPosition { get; set; }

		/// <summary>
		/// Arbitrary string for storing the type of this Event from the source file.
		/// </summary>
		public string SourceType { get; set; }
		/// <summary>
		/// Arbitrary string for storing the type of this Event for the destination file.
		/// </summary>
		public string DestType { get; set; }

		/// <summary>
		/// Miscellaneous extra information associated with this Event.
		/// </summary>
		public Extras Extras { get; set; } = new Extras();

		protected Event()
		{
		}

		protected Event(Event other)
		{
			TimeMicros = other.TimeMicros;
			if (other.MetricPosition != null)
				MetricPosition = new MetricPosition(other.MetricPosition);
			IntegerPosition = other.IntegerPosition;
			SourceType = other.SourceType;
			DestType = other.DestType;
			Extras = new Extras(other.Extras);
		}
	}

	/// <summary>
	/// Event representing a stop.
	/// Stops are for absolute measures of time and do not affect tempo, position, or time signature.
	/// </summary>
	public class Stop : Event
	{
		/// <summary>
		/// Length of the stop at this Stop Event in microseconds.
		/// </summary>
		public readonly long LengthMicros;

		public Stop(long lengthMicros)
		{
			LengthMicros = lengthMicros;
		}

		public Stop(Stop other)
			: base(other)
		{
			LengthMicros = other.LengthMicros;
		}
	}

	/// <summary>
	/// Event representing a change in tempo.
	/// </summary>
	public class TempoChange : Event
	{
		/// <summary>
		/// Tempo at this TempoChange Event in beats per minute.
		/// </summary>
		public readonly double TempoBPM;

		public TempoChange(double tempoBPM)
		{
			TempoBPM = tempoBPM;
		}

		public TempoChange(TempoChange other)
			: base(other)
		{
			TempoBPM = other.TempoBPM;
		}
	}

	/// <summary>
	/// Event representing a change in time signature.
	/// </summary>
	public class TimeSignature : Event
	{
		/// <summary>
		/// Time signature at this TimeSignature Event.
		/// </summary>
		public readonly Fraction Signature;

		public TimeSignature(Fraction signature)
		{
			Signature = signature;
		}

		public TimeSignature(TimeSignature other)
			: base(other)
		{
			Signature = new Fraction(other.Signature);
		}
	}
}
