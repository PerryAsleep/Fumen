namespace Fumen.ChartDefinition;

/// <summary>
/// Event within a Chart Layer.
/// </summary>
public abstract class Event
{
	/// <summary>
	/// Position of this Event represented as time in seconds.
	/// </summary>
	public double TimeSeconds { get; set; }

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
	public Extras Extras { get; set; } = new();

	protected Event()
	{
	}

	protected Event(Event other)
	{
		TimeSeconds = other.TimeSeconds;
		if (other.MetricPosition != null)
			MetricPosition = new MetricPosition(other.MetricPosition);
		IntegerPosition = other.IntegerPosition;
		SourceType = other.SourceType;
		DestType = other.DestType;
		Extras = new Extras(other.Extras);
	}

	/// <summary>
	/// Clones the event.
	/// This is a deep clone except for Extra values.
	/// </summary>
	public abstract Event Clone();

	/// <summary>
	/// Returns whether this Event matches another Events.
	/// Events match if they would be considered the same between different Charts.
	/// They should occur at the same time and position and be of the same type.
	/// Derived classes should implement more checks as appropriate for their types.
	/// </summary>
	/// <param name="other">Other Event to compare to this Event.</param>
	/// <returns>True if this Event matches the given other Event and false otherwise.</returns>
	public virtual bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;

		// Omitting Extras.
		// When testing for matches we care about if two events result in the
		// same note. Extras are used for storing information from deserialization
		// or for serialization.
		return TimeSeconds.DoubleEquals(other.TimeSeconds)
		       && Equals(MetricPosition, other.MetricPosition)
		       && IntegerPosition == other.IntegerPosition
		       && SourceType == other.SourceType
		       && DestType == other.DestType;
	}
}

/// <summary>
/// Event representing a stop.
/// Stops are for absolute measures of time and do not affect tempo, position, or time signature.
/// </summary>
public class Stop : Event
{
	/// <summary>
	/// Length of the stop at this Stop Event as time in seconds.
	/// </summary>
	public double LengthSeconds;

	/// <summary>
	/// Delays are Stops which occur before other Events at the same time.
	/// </summary>
	public bool IsDelay;

	public Stop(double lengthSeconds, bool isDelay = false)
	{
		LengthSeconds = lengthSeconds;
		IsDelay = isDelay;
	}

	public Stop(Stop other)
		: base(other)
	{
		LengthSeconds = other.LengthSeconds;
		IsDelay = other.IsDelay;
	}

	public override Event Clone()
	{
		return new Stop(this);
	}

	public bool Matches(Stop other)
	{
		return base.Matches(other)
		       && LengthSeconds.DoubleEquals(other.LengthSeconds)
		       && IsDelay == other.IsDelay;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((Stop)other);
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

	public override Event Clone()
	{
		return new Warp(this);
	}

	public bool Matches(Warp other)
	{
		return base.Matches(other)
		       && LengthIntegerPosition == other.LengthIntegerPosition;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((Warp)other);
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

	public override Event Clone()
	{
		return new ScrollRate(this);
	}

	public bool Matches(ScrollRate other)
	{
		return base.Matches(other)
		       && Rate.DoubleEquals(other.Rate);
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((ScrollRate)other);
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
	public double PeriodTimeSeconds;
	public bool PreferPeriodAsTime;

	public ScrollRateInterpolation(
		double rate,
		int periodLengthIntegerPosition,
		double periodTimeSeconds,
		bool preferPeriodAsTime)
	{
		Rate = rate;
		PeriodLengthIntegerPosition = periodLengthIntegerPosition;
		PeriodTimeSeconds = periodTimeSeconds;
		PreferPeriodAsTime = preferPeriodAsTime;
	}

	public ScrollRateInterpolation(ScrollRateInterpolation other)
		: base(other)
	{
		Rate = other.Rate;
		PeriodLengthIntegerPosition = other.PeriodLengthIntegerPosition;
		PeriodTimeSeconds = other.PeriodTimeSeconds;
		PreferPeriodAsTime = other.PreferPeriodAsTime;
	}

	public override Event Clone()
	{
		return new ScrollRateInterpolation(this);
	}

	public bool Matches(ScrollRateInterpolation other)
	{
		return base.Matches(other)
		       && Rate.DoubleEquals(other.Rate)
		       && PeriodLengthIntegerPosition == other.PeriodLengthIntegerPosition
		       && PeriodTimeSeconds.DoubleEquals(other.PeriodTimeSeconds)
		       && PreferPeriodAsTime == other.PreferPeriodAsTime;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((ScrollRateInterpolation)other);
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

	public override Event Clone()
	{
		return new Tempo(this);
	}

	public double GetRowsPerSecond(int rowsPerBeat)
	{
		return 1.0 / GetSecondsPerRow(rowsPerBeat);
	}

	public double GetSecondsPerRow(int rowsPerBeat)
	{
		return 60.0 / TempoBPM / rowsPerBeat;
	}

	public bool Matches(Tempo other)
	{
		return base.Matches(other)
		       && TempoBPM.DoubleEquals(other.TempoBPM);
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((Tempo)other);
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

	public override Event Clone()
	{
		return new TimeSignature(this);
	}

	public bool Matches(TimeSignature other)
	{
		return base.Matches(other)
		       && Equals(Signature, other.Signature);
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((TimeSignature)other);
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

	public override Event Clone()
	{
		return new TickCount(this);
	}

	public bool Matches(TickCount other)
	{
		return base.Matches(other)
		       && Ticks == other.Ticks;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((TickCount)other);
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

	public override Event Clone()
	{
		return new Label(this);
	}

	public bool Matches(Label other)
	{
		return base.Matches(other)
		       && Text == other.Text;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((Label)other);
	}
}

/// <summary>
/// Event representing a fake segment.
/// </summary>
/// <remarks>
/// This is extremely StepMania-specific.
/// </remarks>
public class FakeSegment : Event
{
	/// <summary>
	/// Length of the fake segment in integer position units.
	/// </summary>
	public int LengthIntegerPosition;

	public FakeSegment(int lengthIntegerPosition)
	{
		LengthIntegerPosition = lengthIntegerPosition;
	}

	public FakeSegment(FakeSegment other)
		: base(other)
	{
		LengthIntegerPosition = other.LengthIntegerPosition;
	}

	public override Event Clone()
	{
		return new FakeSegment(this);
	}

	public bool Matches(FakeSegment other)
	{
		return base.Matches(other)
		       && LengthIntegerPosition == other.LengthIntegerPosition;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((FakeSegment)other);
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
	/// Multiplier for hits. This is an int to match StepMania.
	/// </summary>
	public int HitMultiplier;

	/// <summary>
	/// Multiplier for misses. This is an int to match StepMania.
	/// </summary>
	public int MissMultiplier;

	public Multipliers(int hitMultiplier, int missMultiplier)
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

	public override Event Clone()
	{
		return new Multipliers(this);
	}

	public bool Matches(Multipliers other)
	{
		return base.Matches(other)
		       && HitMultiplier == other.HitMultiplier
		       && MissMultiplier == other.MissMultiplier;
	}

	public override bool Matches(Event other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (other.GetType() != GetType())
			return false;
		return Matches((Multipliers)other);
	}
}
