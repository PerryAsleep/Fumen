using System.Collections.Generic;

namespace ChartGenerator
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
		public const int CostNewArrow_DoubleStepOtherFootReleasedLater = 5;

		public const int CostNewArrow_FirstStep_OtherIsBracketable_ThisIsNotBracketable = 3;
		public const int CostNewArrow_FirstStep_OtherIsNotBracketable_ThisIsBracketable = 1;
		public const int CostNewArrow_FirstStep_OtherCannotStep = 0;
		public const int CostNewArrow_FirstStep_Ambiguous = 2;

		public const int CostNewArrow_Alternating = 0;

		public const int CostNewArrow_StepFromJump_OtherCannotStep = 1;
		public const int CostNewArrow_StepFromJump_OtherCrossover = 1;
		public const int CostNewArrow_StepFromJump_ThisCrossover = 10;
		public const int CostNewArrow_StepFromJump_OtherMineIndicated_ThisNotMineIndicated = 9;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_OtherSooner = 8;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_ThisSooner = 4;
		public const int CostNewArrow_StepFromJump_OtherNotMineIndicated_ThisMineIndicated = 2;
		public const int CostNewArrow_StepFromJump_OtherFootReleasedLater = 2;
		public const int CostNewArrow_StepFromJump_ThisFootReleasedLater = 8;
		public const int CostNewArrow_StepFromJump_OtherFootBracketable_ThisFootNotBracketable = 5;
		public const int CostNewArrow_StepFromJump_OtherFootNotBracketable_ThisFootBracketable = 3;
		public const int CostNewArrow_StepFromJump_Ambiguous = 2;

		public const int CostNewArrow_Crossover_OtherHeld = 3;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_MineIndicated = 100;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_NoIndication = 200;
		public const int CostNewArrow_Crossover = 4;

		public const int CostNewArrow_FootSwap_DoubleStep_NoMineIndication = 150;
		public const int CostNewArrow_FootSwap_DoubleStep_MineIndication = 75;
		public const int CostNewArrow_FootSwap_MineIndication = 5;  // Worse than a crossover, but better than 2
		public const int CostNewArrow_FootSwap_SubsequentSwap = 6;
		public const int CostNewArrow_FootSwap_NoIndication = 7;

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

		public static GraphArrowState StateAfterAction(FootAction footAction)
		{
			switch (footAction)
			{
				case FootAction.Hold:
					return GraphArrowState.Held;
				case FootAction.Roll:
					return GraphArrowState.Rolling;
				default:
					return GraphArrowState.Resting;
			}
		}

		#region Debug
		public static void FindStateMatching(GraphNode root,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var trackedNodes = new HashSet<GraphNode>();
			var nodes = new HashSet<GraphNode>();
			nodes.Add(root);
			trackedNodes.Add(root);
			while (true)
			{
				var newNodes = new HashSet<GraphNode>();

				foreach (var node in nodes)
				{
					if (StateMatches(node, leftArrow, leftState, rightArrow, rightState))
					{
						int a = 1;
					}

					foreach (var l in node.Links)
					{
						foreach (var g in l.Value)
						{
							if (!trackedNodes.Contains(g))
							{
								trackedNodes.Add(g);
								newNodes.Add(g);
							}
						}
					}
				}

				nodes = newNodes;
				if (nodes.Count == 0)
					break;
			}
		}

		public static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var state = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					state[f, a] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			state[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			GraphNode newNode = new GraphNode(state);
			return node.Equals(newNode);
		}

		public static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int leftArrow2, GraphArrowState leftState2,
			int rightArrow, GraphArrowState rightState,
			int rightArrow2, GraphArrowState rightState2)
		{
			var state = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			state[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[0, 1] = new GraphNode.FootArrowState(leftArrow2, leftState2);
			state[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			state[1, 1] = new GraphNode.FootArrowState(rightArrow2, rightState2);
			GraphNode newNode = new GraphNode(state);
			return node.Equals(newNode);
		}

		public static bool StateMatches(GraphNode.FootArrowState[,] state,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			GraphNode node = new GraphNode(state);
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					newState[f, a] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			newState[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			newState[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			GraphNode newNode = new GraphNode(newState);
			return node.Equals(newNode);
		}
		#endregion

		public enum StepType
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
