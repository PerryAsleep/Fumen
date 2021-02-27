using System;

namespace Fumen.ChartDefinition
{
	/// <summary>
	/// Event within a Chart Layer.
	/// </summary>
	public abstract class Event : IComparable<Event>
	{
		/// <summary>
		/// Time in microseconds of this Event.
		/// </summary>
		public long TimeMicros { get; set; }
		/// <summary>
		/// MetricPosition for this Event.
		/// </summary>
		public MetricPosition Position { get; set; }
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
			if (other.Position != null)
				Position = new MetricPosition(other.Position);
			SourceType = other.SourceType;
			DestType = other.DestType;
			Extras = new Extras(other.Extras);
		}

		#region IComparable Implementation
		public int CompareTo(Event other)
		{
			if (null != Position && null != other.Position)
				return Position.CompareTo(other.Position);
			return TimeMicros.CompareTo(other.TimeMicros);
		}
		#endregion IComparable Implementation
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
