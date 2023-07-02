namespace Fumen.ChartDefinition;

/// <summary>
/// Note Event.
/// Represents an Event in the Chart that the player needs to perform an action against.
/// </summary>
public abstract class Note : Event
{
	/// <summary>
	/// The index of the player which is supposed to use this note.
	/// </summary>
	public int Player { get; set; }

	/// <summary>
	/// The index of the appendage which is supposed to be used for this note.
	/// </summary>
	/// <remarks>
	/// For example, Dance Rush indicates which of the player's feet each note is for.
	/// </remarks>
	public int Appendage { get; set; }

	protected Note()
	{
	}

	protected Note(Note other)
		: base(other)
	{
		other.Player = Player;
		other.Appendage = Appendage;
	}
}

/// <summary>
/// A Note with a position represented by a lane index.
/// </summary>
public class LaneNote : Note
{
	/// <summary>
	/// The lane index of this LaneNote.
	/// </summary>
	public int Lane { get; set; }

	public LaneNote()
	{
	}

	public LaneNote(LaneNote other)
		: base(other)
	{
		Lane = other.Lane;
	}

	public override Event Clone()
	{
		return new LaneNote(this);
	}
}

/// <summary>
/// A LaneNote that is performed by tapping.
/// </summary>
public class LaneTapNote : LaneNote
{
	public LaneTapNote()
	{
	}

	public LaneTapNote(LaneTapNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new LaneTapNote(this);
	}
}

/// <summary>
/// A LaneNote representing the start of a hold.
/// </summary>
public class LaneHoldStartNote : LaneNote
{
	public LaneHoldStartNote()
	{
	}

	public LaneHoldStartNote(LaneHoldStartNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new LaneHoldStartNote(this);
	}
}

/// <summary>
/// A LaneNote representing the end of a hold.
/// </summary>
public class LaneHoldEndNote : LaneNote
{
	public LaneHoldEndNote()
	{
	}

	public LaneHoldEndNote(LaneHoldEndNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new LaneHoldEndNote(this);
	}
}

/// <summary>
/// A LaneNote with a position represented by an analog position.
/// PositionalNotes may have variable width and height.
/// </summary>
public class PositionalNote : Note
{
	public double PositionX { get; set; }
	public double PositionY { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }

	public PositionalNote()
	{
	}

	public PositionalNote(PositionalNote other)
		: base(other)
	{
		PositionX = other.PositionX;
		PositionY = other.PositionY;
		Width = other.Width;
		Height = other.Height;
	}

	public override Event Clone()
	{
		return new PositionalNote(this);
	}
}

/// <summary>
/// A PositionalNote that is performed by tapping.
/// </summary>
public class PositionalTapNote : LaneNote
{
	public PositionalTapNote()
	{
	}

	public PositionalTapNote(PositionalTapNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new PositionalTapNote(this);
	}
}

/// <summary>
/// A PositionalNote representing the start of a hold.
/// </summary>
public class PositionalHoldStartNote : PositionalNote
{
	public PositionalHoldStartNote()
	{
	}

	public PositionalHoldStartNote(PositionalHoldStartNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new PositionalHoldStartNote(this);
	}
}

/// <summary>
/// A PositionalNote representing the end of a hold.
/// </summary>
public class PositionalHoldEndNote : PositionalNote
{
	public PositionalHoldEndNote()
	{
	}

	public PositionalHoldEndNote(PositionalHoldEndNote other)
		: base(other)
	{
	}

	public override Event Clone()
	{
		return new PositionalHoldEndNote(this);
	}
}
