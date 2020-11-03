using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenDoublesStaminaCharts
{
	public static class Constants
	{
		public const int NumSPArrows = 4;
		public const int NumDPArrows = 8;
		public const int MaxArrowsPerFoot = 2;
		public const int NumFeet = 2;
		public const int InvalidArrowIndex = -1;

		// Indices in Foot
		public const int L = (int)Foot.Left;
		public const int R = (int)Foot.Right;

		public const int P1L = 0;
		public const int P1D = 1;
		public const int P1U = 2;
		public const int P1R = 3;
		public const int P2L = 4;
		public const int P2D = 5;
		public const int P2U = 6;
		public const int P2R = 7;

		public enum MineType
		{
			SingleAfterLast,
			SingleAfterSecondLast,
			SingleAfterThirdLast,
			SingleAfterFourthLast,
			// hm what about after a jump? just pick one I guess.



			Quad,
		}

		public enum SingleStepType
		{
			SameArrow,
			NewArrow,
			DoubleStep,
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

		public enum Foot
		{
			Left,
			Right
		}
	}
}
