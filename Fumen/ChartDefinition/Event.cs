﻿using System;

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
		/// Length of the stop at this Stop Event as time in microseconds.
		/// </summary>
		public long LengthMicros;
		/// <summary>
		/// Delays are Stops which occur before other Events at the same time.
		/// </summary>
		public bool IsDelay;

		public Stop(long lengthMicros, bool isDelay = false)
		{
			LengthMicros = lengthMicros;
			IsDelay = isDelay;
		}

		public Stop(Stop other)
			: base(other)
		{
			LengthMicros = other.LengthMicros;
			IsDelay = other.IsDelay;
		}
	}

	/// <summary>
	/// Event representing a warp.
	/// Warps are instantaneous jumps ahead to a different time in the song.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class Warp : Event
	{
		/// <summary>
		/// Length of the stop at this Warp Event in integer position units.
		/// </summary>
		public int LengthIntegerPosition;

		public Warp(int lengthIntegerPosition)
		{
			LengthIntegerPosition = lengthIntegerPosition;
		}

		public Warp(Warp other)
			: base(other)
		{
			LengthIntegerPosition = other.LengthIntegerPosition;
		}
	}

	/// <summary>
	/// Change in ScrollRate for all Events on this Layer.
	/// </summary>
	public class ScrollRate : Event
	{
		/// <summary>
		/// New Scroll Rate.
		/// </summary>
		public double Rate;

		public ScrollRate(double rate)
		{
			Rate = rate;
		}

		public ScrollRate(ScrollRate other)
			: base(other)
		{
			Rate = other.Rate;
		}
	}

	/// <summary>
	/// Change in ScrollRate for all Events on this Layer.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class ScrollRateInterpolation : Event
	{
		/// <summary>
		/// New Scroll Rate.
		/// </summary>
		public double Rate;

		public int PeriodLengthIntegerPosition;
		public long PeriodTimeMicros;
		public bool PreferPeriodAsTimeMicros;

		public ScrollRateInterpolation(
			double rate,
			int periodLengthIntegerPosition,
			long periodTimeMicros,
			bool preferPeriodAsTimeMicros)
		{
			Rate = rate;
			PeriodLengthIntegerPosition = periodLengthIntegerPosition;
			PeriodTimeMicros = periodTimeMicros;
			PreferPeriodAsTimeMicros = preferPeriodAsTimeMicros;
		}

		public ScrollRateInterpolation(ScrollRateInterpolation other)
			: base(other)
		{
			Rate = other.Rate;
		}
	}

	/// <summary>
	/// Event representing a tempo.
	/// </summary>
	public class Tempo : Event
	{
		/// <summary>
		/// Tempo at this Tempo Event in beats per minute.
		/// </summary>
		public double TempoBPM;

		public Tempo(double tempoBPM)
		{
			TempoBPM = tempoBPM;
		}

		public Tempo(Tempo other)
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
		public Fraction Signature;

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

	/// <summary>
	/// Event representing a tick count segment.
	/// Tick count segments define how frequently held notes should increment combo.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class TickCount : Event
	{
		/// <summary>
		/// Number of ticks per beat to set for all following notes.
		/// </summary>
		public int Ticks;

		public TickCount(int ticks)
		{
			Ticks = ticks;
		}

		public TickCount(TickCount other)
			: base(other)
		{
			Ticks = other.Ticks;
		}
	}

	/// <summary>
	/// Event representing a text label.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class Label : Event
	{
		/// <summary>
		/// The text of the label.
		/// </summary>
		public string Text;

		public Label(string text)
		{
			Text = text;
		}

		public Label(Label other)
			: base(other)
		{
			Text = other.Text;
		}
	}

	/// <summary>
	/// Event representing a text label.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class FakeSegment : Event
	{
		/// <summary>
		/// Length of the fake segment as time in microseconds.
		/// </summary>
		public long LengthMicros;

		public FakeSegment(long lengthMicros)
		{
			LengthMicros = lengthMicros;
		}

		public FakeSegment(FakeSegment other)
			: base(other)
		{
			LengthMicros = other.LengthMicros;
		}
	}

	/// <summary>
	/// Event representing multipliers to apply to hits and misses for all following notes.
	/// </summary>
	/// <remarks>
	/// This is extremely StepMania-specific.
	/// </remarks>
	public class Multipliers : Event
	{
		/// <summary>
		/// Number of ticks per beat to set for all following notes.
		/// </summary>
		public double HitMultiplier;
		public double MissMultiplier;

		public Multipliers(double hitMultiplier, double missMultiplier)
		{
			HitMultiplier = hitMultiplier;
			MissMultiplier = missMultiplier;
		}

		public Multipliers(Multipliers other)
			: base(other)
		{
			HitMultiplier = other.HitMultiplier;
			MissMultiplier = other.MissMultiplier;
		}
	}
}
