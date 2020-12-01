
namespace GenDoublesStaminaCharts
{
	public static class Constants
	{
		public const int NumSPArrows = 4;
		public const int NumDPArrows = 8;
		public const int MaxArrowsPerFoot = 2;
		public const int NumFeet = 2;
		public const int InvalidArrowIndex = -1;
		public const int InvalidFoot = -1;

		public const int L = 0;
		public const int R = 1;

		public const int P1L = 0;
		public const int P1D = 1;
		public const int P1U = 2;
		public const int P1R = 3;
		public const int P2L = 4;
		public const int P2D = 5;
		public const int P2U = 6;
		public const int P2R = 7;

		// TODO: Find a better spot
		public static int OtherFoot(int foot)
		{
			return foot == L ? R : L;
		}

		// TODO: Rename
		public enum SingleStepType
		{
			SameArrow,
			NewArrow,
			CrossoverFront,
			CrossoverBehind,
			FootSwap,
			BracketBothNew,
			BracketOneNew,
			BracketBothSame
		}

		public enum FootAction
		{
			Tap,
			Hold,
			Roll,
			Release
		}

		/// <summary>
		/// Enumeration of ways to express a MineEvent.
		/// </summary>
		public enum MineType
		{
			/// <summary>
			/// Expressing a mine as occurring after a specific arrow is most preferable as
			/// this is typically done to inform future footing like a double step or a foot
			/// swap.
			/// </summary>
			AfterArrow,

			/// <summary>
			/// If a mine can't be expressed as occurring after an arrow because no arrow
			/// precedes it, then the next best way to express it is as occurring before a
			/// specific arrow.
			/// </summary>
			BeforeArrow,

			/// <summary>
			/// In the rare case that a mine is in a lane with no arrows then it is expressed
			/// as occurring with no arrow.
			/// </summary>
			NoArrow
		}
	}
}
