
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

		public const int CostUnknown = 1000;

		public const int CostRelease = 0;
		public const int CostSameArrow = 0;

		public const int CostNewArrow_AllOtherHeld_ThisFootCanBracketToNewArrow = 1;
		public const int CostNewArrow_AllOtherHeld_ThisFootCannotBracketToNewArrow = 2;

		public const int CostNewArrow_BothFeetHolding_OtherCanBracket_DoubleStep = 6;
		public const int CostNewArrow_BothFeetHolding_OtherCanBracket_AlternatingStep = 5;
		public const int CostNewArrow_BothFeetHolding_OtherCannotBracket = 4;

		public const int CostNewArrow_OtherHoldingOne = 3;

		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep = 7;
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep_DoubleStep = 9;
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep = 8;

		public const int CostNewArrow_DoubleStep = 100;
		public const int CostNewArrow_DoubleStepMineIndicated = 50;

		public const int CostNewArrow_FirstStep_OtherIsBracketable_ThisIsNotBracketable = 3;
		public const int CostNewArrow_FirstStep_OtherIsNotBracketable_ThisIsBracketable = 1;
		public const int CostNewArrow_FirstStep_OtherCannotStep = 0;
		public const int CostNewArrow_FirstStep_Ambiguous = 2;

		public const int CostNewArrow_Alternating = 0;

		public const int CostNewArrow_StepFromJump_OtherCannotStep = 0;
		public const int CostNewArrow_StepFromJump_OtherMineIndicated_ThisNotMineIndicated = 6;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_OtherSooner = 5;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_ThisSooner = 3;
		public const int CostNewArrow_StepFromJump_OtherNotMineIndicated_ThisMineIndicated = 0;
		public const int CostNewArrow_StepFromJump_OtherFootReleasedLater = 1;
		public const int CostNewArrow_StepFromJump_ThisFootReleasedLater = 4;
		public const int CostNewArrow_StepFromJump_OtherFootBracketable_ThisFootNotBracketable = 2;
		public const int CostNewArrow_StepFromJump_Ambiguous = 2;

		public const int CostNewArrow_Crossover_OtherHeld = 4;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_MineIndicated = 100;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_NoIndication = 200;
		public const int CostNewArrow_Crossover = 5;

		public const int CostNewArrow_FootSwap_DoubleStep_NoMineIndication = 150;
		public const int CostNewArrow_FootSwap_DoubleStep_MineIndication = 75;
		public const int CostNewArrow_FootSwap_MineIndication = 7;  // Worse than a crossover, but better than 2
		public const int CostNewArrow_FootSwap_SubsequentSwap = 6;
		public const int CostNewArrow_FootSwap_NoIndication = 11;

		public const int CostTwoArrows_Bracket_OtherFootHoldingBoth = 1;
		public const int CostTwoArrows_Bracket_DoubleStep = 100;
		public const int CostTwoArrows_Bracket_PreferredDueToMovement = 5;
		public const int CostTwoArrows_Bracket = 10;

		public const int CostTwoArrows_Jump_OtherFootHoldingOne_ThisFootCouldBracket = 20;
		public const int CostTwoArrows_Jump_OtherFootHoldingOne_NotBracketable = 15;
		public const int CostTwoArrows_Jump_OneFootPrefersBracketToDueMovement = 7;
		public const int CostTwoArrows_Jump = 6;

		public const int CostThreeArrows = 0;
		public const int CostFourArrows = 0;

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
