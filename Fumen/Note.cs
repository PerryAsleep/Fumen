using System;
using System.Collections.Generic;
using System.Text;

namespace Fumen
{
	public abstract class Note : Event
	{
		public Note()
		{
		}

		public Note(Note other)
			: base(other)
		{
			other.Player = Player;
			other.Appendage = Appendage;
		}

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
	}

	public class LaneNote : Note
	{
		public LaneNote()
		{
		}

		public LaneNote(LaneNote other)
			: base(other)
		{
			Lane = other.Lane;
		}

		public int Lane { get; set; }
	}

	public class LaneTapNote : LaneNote
	{
		public LaneTapNote()
		{
		}

		public LaneTapNote(LaneTapNote other)
			: base(other)
		{
		}
	}

	public class LaneHoldStartNote : LaneNote
	{
		public LaneHoldStartNote()
		{
		}

		public LaneHoldStartNote(LaneHoldStartNote other)
			: base(other)
		{
		}
	}

	public class LaneHoldEndNote : LaneNote
	{
		public LaneHoldEndNote()
		{
		}

		public LaneHoldEndNote(LaneHoldEndNote other)
			: base(other)
		{
		}
	}

	public class PositionalNote : Note
	{
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

		public double PositionX { get; set; }
		public double PositionY { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
	}

	public class PositionalTapNote : LaneNote
	{
		public PositionalTapNote()
		{
		}

		public PositionalTapNote(PositionalTapNote other)
			: base(other)
		{
		}
	}

	public class PositionalHoldStartNote : PositionalNote
	{
		public PositionalHoldStartNote()
		{
		}

		public PositionalHoldStartNote(PositionalHoldStartNote other)
			: base(other)
		{
		}
	}

	public class PositionalHoldEndNote : PositionalNote
	{
		public PositionalHoldEndNote()
		{
		}

		public PositionalHoldEndNote(PositionalHoldEndNote other)
			: base(other)
		{
		}
	}
}
