using System;
using System.Collections.Generic;
using System.Text;

namespace Fumen
{
	public abstract class Event : IComparable
	{
		public Event()
		{
		}

		public Event(Event other)
		{
			TimeMicros = other.TimeMicros;
			Position = new MetricPosition(other.Position);
			SourceType = other.SourceType;
			DestType = other.DestType;
			SourceExtras = new Dictionary<string, object>();
			// Shallow copy on dictionary values
			foreach (var entry in other.SourceExtras)
			{
				SourceExtras[entry.Key] = entry.Value;
			}
		}

		public long TimeMicros { get; set; }
		public MetricPosition Position { get; set; } = new MetricPosition();

		/// <summary>
		/// Extra Information from the source file for this Event.
		/// </summary>
		public Dictionary<string, object> SourceExtras { get; set; } = new Dictionary<string, object>();

		public string SourceType { get; set; }
		public string DestType { get; set; }

		public int CompareTo(object obj)
		{
			var other = (Event)obj;
			if (null != Position && null != other.Position)
				return Position.CompareTo(other.Position);
			return TimeMicros.CompareTo(other.TimeMicros);
		}
	}

	public class Stop : Event
	{
		public Stop()
		{
		}

		public Stop(Stop other)
			: base(other)
		{
			LengthMicros = other.LengthMicros;
		}

		public long LengthMicros { get; set; }
	}

	public class TempoChange : Event
	{
		public TempoChange()
		{
		}

		public TempoChange(TempoChange other)
			: base(other)
		{
			TempoBPM = other.TempoBPM;
		}

		public double TempoBPM { get; set; }
	}

	public class TimeSignature : Event
	{
		public TimeSignature()
		{
		}

		public TimeSignature(TimeSignature other)
			: base(other)
		{
			Signature = new Fraction(other.Signature);
		}

		public Fraction Signature { get; set; } = new Fraction(0, 0);
	}
}
