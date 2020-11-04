using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	public enum GraphArrowState
	{
		Resting,
		Held,
		Rolling,
	}

	public class GraphLink : IEquatable<GraphLink>
	{
		private static readonly int NullLinkHash;
		static GraphLink()
		{
			NullLinkHash = new Tuple<int, int>(
				Enum.GetValues(typeof(SingleStepType)).Cast<SingleStepType>().Count(),
				Enum.GetValues(typeof(FootAction)).Cast<FootAction>().Count()).GetHashCode();
		}

		public readonly Tuple<SingleStepType, FootAction>[,] Links = new Tuple<SingleStepType, FootAction>[NumFeet, MaxArrowsPerFoot];

		// Jumps - any problems?
		// We may want to know IsJump(), IsJumpBothNew() etc
		// I think the reason we want to do know that is in order to use a more sensible weight 
		// because jump-step-jump patterns are 'natural' despite one foot looking like double steps/jacks
		//	though, is there any other path through that pattern that would looks worse?

		public bool Equals(GraphLink other)
		{
			if (other == null)
				return false;
			for (var foot = 0; foot < NumFeet; foot++)
			{
				for (var arrow = 0; arrow < MaxArrowsPerFoot; arrow++)
				{
					if (Links[foot, arrow] == null && other.Links[foot, arrow] == null)
						continue;
					if (Links[foot, arrow] == null || other.Links[foot, arrow] == null)
						return false;
					if (!Links[foot, arrow].Equals(other.Links[foot, arrow]))
						return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphLink g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var foot = 0; foot < NumFeet; foot++)
				for (var arrow = 0; arrow < MaxArrowsPerFoot; arrow++)
					hash = unchecked(hash * 31 + Links[foot, arrow]?.GetHashCode() ?? NullLinkHash);
			return hash;
		}
	}

	public class GraphNode : IEquatable<GraphNode>
	{
		public struct FootArrowState
		{
			public FootArrowState(int arrow, GraphArrowState state)
			{
				Arrow = arrow;
				State = state;
			}

			public int Arrow { get; }
			public GraphArrowState State { get; }

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Arrow == f.Arrow && State == f.State;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + Arrow);
				hash = unchecked(hash * 31 + (int)State);
				return hash;
			}
		}

		public static readonly FootArrowState InvalidFootArrowState;
		static GraphNode()
		{
			InvalidFootArrowState = new FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);
		}

		public readonly FootArrowState[,] State;
		public Dictionary<GraphLink, List<GraphNode>> Links = new Dictionary<GraphLink, List<GraphNode>>();

		public GraphNode(FootArrowState[,] state)
		{
			State = state;
			// Sort so that comparisons are easier
			for (var foot = 0; foot < NumFeet; foot++)
			{
				// TODO: Remove assumption that MaxArrowsPerFoot == 2.
				if (State[foot, 0].Arrow <= State[foot, 1].Arrow)
					continue;
				var swap = State[foot, 0];
				State[foot, 0] = State[foot, 1];
				State[foot, 1] = swap;
			}
		}

		public bool Equals(GraphNode other)
		{
			if (other == null)
				return false;
			if (State.Length != other.State.Length)
				return false;
			for(var foot = 0; foot < NumFeet; foot++)
				for(var arrow = 0; arrow < MaxArrowsPerFoot; arrow++)
					if (!State[foot,arrow].Equals(other.State[foot, arrow]))
						return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphNode g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			foreach (var state in State)
				hash = unchecked(hash * 31 + state.GetHashCode());
			return hash;
		}
	}

	public class ArrowData
	{
		/// <summary>
		/// The position / index of this arrow.
		/// </summary>
		public int Position;
		/// <summary>
		/// Which arrows are valid as a next step from this arrow for either foot.
		/// Index is arrow.
		/// </summary>
		public bool[] ValidNextArrows;
		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] BracketablePairings = new bool[NumFeet][];
		/// <summary>
		/// Which arrows are valid pairings for the other foot.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot without crossing over.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairings = new bool[NumFeet][];
		/// <summary>
		/// Which arrows form a front crossover with the original arrow in front.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Left is crossing over in front.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairingsSameFootCrossoverFront = new bool[NumFeet][];
		/// <summary>
		/// Which arrows form a front crossover with the original arrow in back.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Left is crossing over in back.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairingsSameFootCrossoverBehind = new bool[NumFeet][];
		/// <summary>
		/// Which arrows form a front crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in front.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairingsOtherFootCrossoverFront = new bool[NumFeet][];
		/// <summary>
		/// Which arrows form a back crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in back.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairingsOtherFootCrossoverBehind = new bool[NumFeet][];
	}

	class StepGraph
	{
		private static ArrowData[] SPArrowData;
		private static ArrowData[] DPArrowData;
		private static readonly List<SingleStepType[]> JumpCombinations;
		private static readonly List<FootAction[]>[] ActionCombinations;

		static StepGraph()
		{
			InitArrowData();

			// Initialize JumpCombinations
			var jumpSingleSteps = new[]
			{
				SingleStepType.SameArrow,
				SingleStepType.NewArrow,
				SingleStepType.BracketBothNew,
				SingleStepType.BracketOneNew,
				SingleStepType.BracketBothSame,
			};
			JumpCombinations = Combinations(jumpSingleSteps, NumFeet);

			// Initialize ActionCombination
			ActionCombinations = new List<FootAction[]>[MaxArrowsPerFoot];
			for(var i = 0; i < MaxArrowsPerFoot; i++)
				ActionCombinations[i] = Combinations<FootAction>(i + 1);
		}

		private static void InitArrowData()
		{
			bool[] noneDP = {false, false, false, false, false, false, false, false};

			bool[] FromDP(IEnumerable<int> arrows)
			{
				var ret = new bool[NumDPArrows];
				foreach (var arrow in arrows)
					ret[arrow] = true;
				return ret;
			}

			DPArrowData = new[]
			{
				// P1L
				new ArrowData
				{
					Position = P1L,

					// A foot on P1L can move next to P1D, P1U, or P1R
					ValidNextArrows = FromDP(new[] {P1D, P1U, P1R}),

					BracketablePairings =
					{
						// Left foot on P1L is bracketable with P1U and P1D
						[L] = FromDP(new[] {P1D, P1U}),
						// Right foot on P1L is a crossover and not bracketable
						[R] = noneDP
					},

					OtherFootPairings =
					{
						// Left foot on P1L supports right foot on P1D, P1U, and P1R without crossovers
						[L] = FromDP(new[] {P1D, P1U, P1R}),
						// Right foot on P1L is a crossover with no normal left foot pairing
						[R] = noneDP,
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneDP,
						// Right foot on P1L is a crossover with right in front when left is on P1D
						[R] = FromDP(new[] {P1D}),
					},

					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneDP,
						// Right foot on P1L is a crossover with right in back when left is on P1U
						[R] = FromDP(new[] {P1U}),
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneDP,
						// Right foot on P1L is a crossover with left in front when left is on P1U
						[R] = FromDP(new[] {P1U}),
					},

					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneDP,
						// Right foot on P1L is a crossover with left in back when left is on P1D
						[R] = FromDP(new[] {P1D}),
					}
				},

				// P1D
				new ArrowData
				{
					Position = P1D,

					// A foot on P1D can move next to P1L, P1U, P1R, or P2L
					ValidNextArrows = FromDP(new[] {P1L, P1U, P1R, P2L}),

					BracketablePairings =
					{
						// Left foot on P1D is bracketable with P1L and P1R
						[L] = FromDP(new[] {P1L, P1R}),
						// Right foot on P1D is bracketable with P1R
						[R] = FromDP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1D supports right foot on P1U, P1R, and P2L without crossovers
						[L] = FromDP(new[] {P1U, P1R, P2L}),
						// Right foot on P1D supports Left foot on P1L and P1U without crossovers
						[R] = FromDP(new[] {P1L, P1U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[L] = noneDP,
						// Right foot on P1D is not a crossover with right in front
						[R] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1D is a crossover with left in back with right is on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1D is a crossover with right in back when left is on P1R
						[R] = FromDP(new[] {P1R})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1D is a crossover with right in front with right is on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1D is a crossover with left in front when left is on P1R
						[R] = FromDP(new[] {P1R})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// Right foot on P1D is not a crossover with left in back
						[R] = noneDP
					},
				},

				// P1U
				new ArrowData
				{
					Position = P1U,

					// A foot on P1U can move next to P1L, P1D, P1R, or P2L
					ValidNextArrows = FromDP(new[] {P1L, P1D, P1R, P2L}),

					BracketablePairings =
					{
						// Left foot on P1U is bracketable with P1L and P1R
						[L] = FromDP(new[] {P1L, P1R}),
						// Right foot on P1U is bracketable with P1R
						[R] = FromDP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1U supports right foot on P1D, P1R, and P2L without crossovers
						[L] = FromDP(new[] {P1D, P1R, P2L}),
						// Right foot on P1U supports left foot on P1L and P1D without crossovers
						[R] = FromDP(new[] {P1L, P1D})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1U is a crossover with left in front when right is on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1U is a crossover with right in front when left is on P1R
						[R] = FromDP(new[] {P1R})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1U is a crossover with right in back when right is on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1U is a crossover with left in back when left is on P1R
						[R] = FromDP(new[] {P1R})
					},
				},

				// P1R
				new ArrowData
				{
					Position = P1R,

					// A foot on P1R can move next to P1L, P1D, P1U, P2L, P2D, or P2U
					ValidNextArrows = FromDP(new[] {P1L, P1D, P1U, P2L, P2D, P2U}),

					BracketablePairings =
					{
						// Left foot on P1R is bracketable with P1D, P1U, and P2R
						[L] = FromDP(new[] {P1D, P1U, P2R}),
						// Right foot on P1R is bracketable with P1D, P1U, and P2R
						[R] = FromDP(new[] {P1D, P1U, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P1R supports right foot on P2L, P2D, and P2U without crossovers
						[L] = FromDP(new[] {P2L, P2D, P2U}),
						// Right foot on P1R supports left foot on P1L, P1D, and P1U without crossovers
						[R] = FromDP(new[] {P1L, P1D, P1U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1R is a crossover with left in front when right is on P1D
						[L] = FromDP(new[] {P1D}),
						// Right foot on P1R is not a crossover position. Not considering left on P2L, slightly too twisty
						[R] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1R is a crossover with left in back when right is on P1U
						[L] = FromDP(new[] {P1U}),
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1R is a crossover with right in front when right is on P1U
						[L] = FromDP(new[] {P1U}),
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1R is a crossover with right in back when right is on P1D
						[L] = FromDP(new[] {P1D}),
						// none
						[R] = noneDP
					},
				},

				// P2L
				new ArrowData
				{
					Position = P2L,

					// A foot on P2L can move next to P1D, P1U, P1R, P2D, P2U, or P2R
					ValidNextArrows = FromDP(new[] {P1D, P1U, P1R, P2D, P2U, P2R}),

					BracketablePairings =
					{
						// Left foot on P2L is bracketable with P1R, P2D, and P2U
						[L] = FromDP(new[] {P1R, P2D, P2U}),
						// Right foot on P2L is bracketable with P1R, P2D, and P2U
						[R] = FromDP(new[] {P1R, P2D, P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2L supports right foot on P2D, P2U, and P2R without crossovers
						[L] = FromDP(new[] {P2D, P2U, P2R}),
						// Right foot on P2L supports left foot on P1D, P1U, and P1L without crossovers
						[R] = FromDP(new[] {P1D, P1U, P1L})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[L] = noneDP,
						// Right foot on P2L is a crossover with right in front when left is on P2D
						[R] = FromDP(new[] {P2D})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// Right foot on P2L is a crossover with right in back when left is on P2U
						[R] = FromDP(new[] {P2U})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2L is not a crossover position. Not considering right on P1R, slightly too twisty
						[L] = noneDP,
						// Right foot on P2L is a crossover with left in front when left is on P2U
						[R] = FromDP(new[] {P2U})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// Right foot on P2L is a crossover with left in back when left is on P2D
						[R] = FromDP(new[] {P2D})
					},
				},

				// P2D
				new ArrowData
				{
					Position = P2D,

					// A foot on P2D can move next to P1R, P2L, P2U, and P2R
					ValidNextArrows = FromDP(new[] {P1R, P2L, P2U, P2R}),

					BracketablePairings =
					{
						// Left foot on P2D is bracketable with P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2D is bracketable with P2L and P2R
						[R] = FromDP(new[] {P2L, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P2D supports right foot on P2U and P2R without crossovers
						[L] = FromDP(new[] {P2U, P2R}),
						// Right foot on P2D supports left foot on P1R, P2L, and P2U without crossovers
						[R] = FromDP(new[] {P1R, P2L, P2U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P2D is a crossover with left in back when right is on P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2D is a crossover with right in back when left is on P2R
						[R] = FromDP(new[] {P2R})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2D is a crossover with right in front when right is on P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2D is a crossover with left in front when left is on P2R
						[R] = FromDP(new[] {P2R})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
				},

				// P2U
				new ArrowData
				{
					Position = P2U,

					// A foot on P2U can move next to P1R, P2L, P2D, and P2R
					ValidNextArrows = FromDP(new[] {P1R, P2L, P2D, P2R}),

					BracketablePairings =
					{
						// Left foot on P2U is bracketable with P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2U is bracketable with P2L and P2R
						[R] = FromDP(new[] {P2L, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P2U supports right foot on P2D and P2R without crossovers
						[L] = FromDP(new[] {P2D, P2R}),
						// Right foot on P2U supports left foot on P1R, P2L, and P2D without crossovers
						[R] = FromDP(new[] {P1R, P2L, P2D})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P2U is a crossover with left in front when right is on P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2U is a crossover with right in front when left is on P2R
						[R] = FromDP(new[] {P2R})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// none
						[L] = noneDP,
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P2U is a crossover with right in back when right is on P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2U is a crossover with left in back when left is on P2R
						[R] = FromDP(new[] {P2R})
					},
				},

				// P2R
				new ArrowData
				{
					Position = P2R,

					// A foot on P2R can move next to P2L, P2D, and P2U
					ValidNextArrows = FromDP(new[] {P2L, P2D, P2U}),

					BracketablePairings =
					{
						// Left foot on P2R is a crossover and not bracketable
						[L] = noneDP,
						// Right foot on P2R is bracketable with P2D and P2U
						[R] = FromDP(new[] {P2D, P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2R is a crossover with no normal right foot pairing
						[L] = noneDP,
						// Right foot on P2R supports left foot on P2L, P2D, and P2U without crossovers
						[R] = FromDP(new[] {P2L, P2D, P2U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P2R is a crossover with left in front when right is on P2D
						[L] = FromDP(new[] {P2D}),
						// Right foot on P2R is never a crossover position
						[R] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P2R is a crossover with left in back when right is on P2U
						[L] = FromDP(new[] {P2U}),
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2R is a crossover with right in front when right is on P2U
						[L] = FromDP(new[] {P2U}),
						// none
						[R] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P2R is a crossover with right in back when right is on P2D
						[L] = FromDP(new[] {P2D}),
						// none
						[R] = noneDP
					},
				}
			};

			// Copy DP data to SP
			SPArrowData = new ArrowData[NumSPArrows];
			for (var arrow = 0; arrow < NumSPArrows; arrow++)
			{
				SPArrowData[arrow] = new ArrowData
				{
					Position = DPArrowData[arrow].Position,
					ValidNextArrows = new bool[NumSPArrows],
					BracketablePairings = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
					OtherFootPairings = { [0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
					OtherFootPairingsSameFootCrossoverFront = { [0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
					OtherFootPairingsSameFootCrossoverBehind = { [0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
					OtherFootPairingsOtherFootCrossoverFront = { [0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
					OtherFootPairingsOtherFootCrossoverBehind = { [0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows] },
				};

				Array.Copy(DPArrowData[arrow].ValidNextArrows, SPArrowData[arrow].ValidNextArrows, NumSPArrows);
				for (var foot = 0; foot < NumFeet; foot++)
				{
					Array.Copy(DPArrowData[arrow].BracketablePairings[foot], SPArrowData[arrow].BracketablePairings[foot],
						NumSPArrows);
					Array.Copy(DPArrowData[arrow].OtherFootPairings[foot], SPArrowData[arrow].OtherFootPairings[foot],
						NumSPArrows);
					Array.Copy(DPArrowData[arrow].OtherFootPairingsSameFootCrossoverFront[foot],
						SPArrowData[arrow].OtherFootPairingsSameFootCrossoverFront[foot], NumSPArrows);
					Array.Copy(DPArrowData[arrow].OtherFootPairingsSameFootCrossoverBehind[foot],
						SPArrowData[arrow].OtherFootPairingsSameFootCrossoverBehind[foot], NumSPArrows);
					Array.Copy(DPArrowData[arrow].OtherFootPairingsOtherFootCrossoverFront[foot],
						SPArrowData[arrow].OtherFootPairingsOtherFootCrossoverFront[foot], NumSPArrows);
					Array.Copy(DPArrowData[arrow].OtherFootPairingsOtherFootCrossoverBehind[foot],
						SPArrowData[arrow].OtherFootPairingsOtherFootCrossoverBehind[foot], NumSPArrows);
				}
			}
		}

		public GraphNode CreateSPStepGraph()
		{
			var state = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					state[L, a] = new GraphNode.FootArrowState(P1L, GraphArrowState.Resting);
					state[R, a] = new GraphNode.FootArrowState(P1R, GraphArrowState.Resting);
				}
				else
				{
					state[L, a] = GraphNode.InvalidFootArrowState;
					state[R, a] = GraphNode.InvalidFootArrowState;
				}
			}
			var root = new GraphNode(state);
			FillStepGraph(root, SPArrowData);
			return root;
		}

		public GraphNode CreateDPStepGraph()
		{
			var state = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					state[L, a] = new GraphNode.FootArrowState(P1R, GraphArrowState.Resting);
					state[R, a] = new GraphNode.FootArrowState(P2L, GraphArrowState.Resting);
				}
				else
				{
					state[L, a] = GraphNode.InvalidFootArrowState;
					state[R, a] = GraphNode.InvalidFootArrowState;
				}
			}
			var root = new GraphNode(state);
			FillStepGraph(root, DPArrowData);
			return root;
		}

		private static Func<GraphNode.FootArrowState[,], ArrowData[], int, int, Foot, FootAction[], List<GraphNode.FootArrowState[,]>> GetFillFunc(
			SingleStepType stepType)
		{
			switch (stepType)
			{
				case SingleStepType.SameArrow:
					return FillSameArrow;
				case SingleStepType.NewArrow:
				case SingleStepType.DoubleStep:
					return FillNewArrow;
				case SingleStepType.CrossoverFront:
					return FillCrossoverFront;
				case SingleStepType.CrossoverBehind:
					return FillCrossoverBack;
				case SingleStepType.FootSwap:
					return FillFootSwap;
				case SingleStepType.BracketBothNew:
					return FillBracketBothNew;
				case SingleStepType.BracketOneNew:
					return FillBracketOneNew;
				case SingleStepType.BracketBothSame:
					return FillBracketBothSame;
			}

			return null;
		}

		private static int GetNumArrowForStep(SingleStepType stepType)
		{
			switch (stepType)
			{
				case SingleStepType.BracketBothNew:
				case SingleStepType.BracketOneNew:
				case SingleStepType.BracketBothSame:
					return 2;
				default:
					return 1;
			}
		}

		private static void FillStepGraph(GraphNode root, ArrowData[] arrowData)
		{
			var completeNodes = new HashSet<GraphNode>();
			var currentNodes = new List<GraphNode> { root };
			while (currentNodes.Count > 0)
			{
				var allChildren = new HashSet<GraphNode>();
				foreach (var currentNode in currentNodes)
				{
					// Mark node complete
					// Doing this before filling so it is retrievable as a visited node when linking.
					completeNodes.Add(currentNode);

					// Fill node
					foreach (var stepType in Enum.GetValues(typeof(SingleStepType)).Cast<SingleStepType>())
						FillSingleFootStep(currentNode, completeNodes, arrowData, stepType);
					foreach (var jump in JumpCombinations)
						FillJump(currentNode, completeNodes, arrowData, jump);

					// Collect children
					foreach (var linkEntry in currentNode.Links)
						foreach (var childNode in linkEntry.Value)
							allChildren.Add(childNode);
				}

				// Remove all complete nodes
				allChildren.RemoveWhere(n => completeNodes.Contains(n));

				// Search one level deeper
				currentNodes = allChildren.ToList();
			}
		}

		private static GraphNode GetOrCreateNodeByState(GraphNode.FootArrowState[,] state, HashSet<GraphNode> visitedNodes)
		{
			var node = new GraphNode(state);
			if (visitedNodes.TryGetValue(node, out var currentNode))
				node = currentNode;
			return node;
		}

		private static void AddNode(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			GraphNode.FootArrowState[,] state,
			GraphLink link)
		{
			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();

			var newNode = GetOrCreateNodeByState(state, visitedNodes);
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);
		}

		private static void FillSingleFootStep(GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			ArrowData[] arrowData,
			SingleStepType stepType)
		{
			var numStepArrows = GetNumArrowForStep(stepType);
			var fillFunc = GetFillFunc(stepType);
			var actionSets = ActionCombinations[numStepArrows - 1];
			var feet = Enum.GetValues(typeof(Foot)).Cast<Foot>().ToList();
			var numArrows = arrowData.Length;
			for (var currentIndex = 0; currentIndex < numArrows; currentIndex++)
			{
				for (var newIndex = 0; newIndex < numArrows; newIndex++)
				{
					foreach (var foot in feet)
					{
						foreach (var actionSet in actionSets)
						{
							var newStates = fillFunc(currentNode.State, arrowData, currentIndex, newIndex, foot, actionSet);
							if (newStates == null || newStates.Count == 0)
								continue;

							foreach (var newState in newStates)
							{
								var link = new GraphLink();
								for (var f = 0; f < numStepArrows; f++)
								{
									link.Links[(int) foot, f] = new Tuple<SingleStepType, FootAction>(stepType, actionSet[f]);
								}

								AddNode(currentNode, visitedNodes, newState, link);
							}
						}
					}
				}
			}
		}

		private static void FillJump(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			ArrowData[] arrowData,
			SingleStepType[] stepTypes)
		{
			var actionsToStatesL = FillJumpStep(currentNode.State, arrowData, stepTypes[L], Foot.Left);
			foreach (var actionStateL in actionsToStatesL)
			{
				foreach (var newStateL in actionStateL.Value)
				{
					var actionsToStatesR = FillJumpStep(newStateL, arrowData, stepTypes[R], Foot.Right);
					foreach (var actionStateR in actionsToStatesR)
					{
						foreach (var newStateR in actionStateR.Value)
						{
							var link = new GraphLink();
							for (var f = 0; f < GetNumArrowForStep(stepTypes[L]); f++)
								link.Links[L, f] = new Tuple<SingleStepType, FootAction>(stepTypes[L], actionStateL.Key[f]);
							for (var f = 0; f < GetNumArrowForStep(stepTypes[R]); f++)
								link.Links[R, f] = new Tuple<SingleStepType, FootAction>(stepTypes[R], actionStateR.Key[f]);
							AddNode(currentNode, visitedNodes, newStateR, link);
						}
					}
				}
			}
		}

		private static Dictionary<FootAction[], List<GraphNode.FootArrowState[,]>> FillJumpStep(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			SingleStepType stepType,
			Foot foot)
		{
			var result = new Dictionary<FootAction[], List<GraphNode.FootArrowState[,]>>();

			var numArrows = arrowData.Length;
			var numStepArrows = GetNumArrowForStep(stepType);
			var fillFunc = GetFillFunc(stepType);
			var actionSets = ActionCombinations[numStepArrows - 1];
			for (var currentIndex = 0; currentIndex < numArrows; currentIndex++)
			{
				for (var newIndex = 0; newIndex < numArrows; newIndex++)
				{
					foreach (var actionSet in actionSets)
					{
						var newStates = fillFunc(currentState, arrowData, currentIndex, newIndex, foot, actionSet);
						if (newStates == null || newStates.Count == 0)
							continue;
						result[actionSet] = newStates;
					}
				}
			}

			return result;
		}

		private static List<GraphNode.FootArrowState[,]> FillNewArrow(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			if (footActions.Length != 1)
				return null;
			var footAction = footActions[0];

			// TODO: Double-Step is maybe slightly different since it isn't a bracket?

			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;
			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Only consider moving to a new arrow when the currentIndex is on an arrow
			if (!IsOn(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new arrow if already holding on MaxArrowsPerFoot
			var numHeld = NumHeldOrRolling(currentState, foot);
			if (numHeld >= MaxArrowsPerFoot)
				return null;
			// If bracketing, skip if this is not a valid bracketable pairing.
			if (numHeld == 1 && !arrowData[currentIndex].BracketablePairings[(int)foot][newIndex])
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;
			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			if (!IsValidPairingWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Set up the state for a new node.
			// Copy the previous state, but lift from any resting arrows for the given foot.
			// Leave holds/rolls.
			var otherFoot = (int)Other(foot);
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				newState[otherFoot, a] = currentState[otherFoot, a];

				if (IsResting(currentState, newIndex, foot))
					newState[(int)foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[(int)foot, a] = currentState[(int)foot, a];
			}
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[(int) foot, a].Arrow == InvalidArrowIndex)
				{
					newState[(int)foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			return new List<GraphNode.FootArrowState[,]> { newState };
		}

		private static List<GraphNode.FootArrowState[,]> FillSameArrow(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			if (footActions.Length != 1)
				return null;
			var footAction = footActions[0];

			// Must be same arrow.
			if (currentIndex != newIndex)
				return null;
			// Release logic. Lift from a hold or roll.
			if (footAction == FootAction.Release && !IsHeldOrRolling(currentState, currentIndex, foot))
				return null;
			// Normal logic. Placement action on a resting arrow.
			if (footAction != FootAction.Release && !IsResting(currentState, currentIndex, foot))
				return null;

			// Set up the state for a new node.
			// Copy the previous state and if placing a new foot, lift from any resting arrows.
			// It is necessary to lift for, e.g. the step after an SP quad.
			var otherFoot = (int) Other(foot);
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				newState[otherFoot, a] = currentState[otherFoot, a];

				if(footAction != FootAction.Release && IsResting(currentState, newIndex, foot))
					newState[(int)foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[(int)foot, a] = currentState[(int)foot, a];
			}
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[(int)foot, a].Arrow == InvalidArrowIndex)
				{
					newState[(int)foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}
			return new List<GraphNode.FootArrowState[,]> { newState };
		}

		private static List<GraphNode.FootArrowState[,]> FillFootSwap(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			if (footActions.Length != 1)
				return null;
			var footAction = footActions[0];

			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;

			var otherFoot = Other(foot);

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// The new index must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState, newIndex, otherFoot))
				return null;
			// Disallow foot swap if this foot is holding or rolling
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Disallow foot swap if the other foot is holding or rolling
			if (NumHeldOrRolling(currentState, otherFoot) > 0)
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// The other foot should remain Resting on the newIndex, even though it is slightly lifted
				if(currentState[(int)otherFoot, a].Arrow == newIndex)
					newState[(int)otherFoot, a] = new GraphNode.FootArrowState(newIndex, GraphArrowState.Resting);
				// All other arrows under the other foot should be lifted.
				else
					newState[(int)otherFoot, a] = GraphNode.InvalidFootArrowState;
				// The first arrow under the foot in the new state should be at the newIndex, with the appropriate state.
				if (a == 0)
					newState[(int)foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
				// All other arrows under the foot should be lifted. 
				else
					newState[(int)foot, a] = GraphNode.InvalidFootArrowState;
			}
			return new List<GraphNode.FootArrowState[,]> { newState };
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverFront(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, true);
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverBack(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, false);
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverInternal(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions,
			bool front)
		{
			if (footActions.Length != 1)
				return null;
			var footAction = footActions[0];

			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;
			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot crossover if any arrows are held by this foot.
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is not a crossover
			if (front && !FootCrossesOverInFrontWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;
			if (!front && !FootCrossesOverInBackWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Set up the state for a new node.
			var otherFoot = (int)Other(foot);
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// Copy previous state for other foot
				newState[otherFoot, a] = currentState[otherFoot, a];

				// Lift any resting arrows for the given foot.
				if (IsResting(currentState, newIndex, foot))
					newState[(int)foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[(int)foot, a] = currentState[(int)foot, a];
			}
			// Set up the FootArrowState for the new arrow
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[(int)foot, a].Arrow == InvalidArrowIndex)
				{
					newState[(int)foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			return new List<GraphNode.FootArrowState[,]> { newState };
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketBothNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			if (footActions.Length != MaxArrowsPerFoot)
				return null;

			// Cannot release on a new arrow
			foreach (var footAction in footActions)
			{
				if (footAction == FootAction.Release)
					return null;
			}
			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var newStates = new List<GraphNode.FootArrowState[,]>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[(int)foot][secondIndex])
					continue;
				// Skip if this next arrow is occupied.
				if (!IsFree(currentState, secondIndex))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, secondIndex))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, secondIndex);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[secondIndex];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
				var otherFoot = (int)Other(foot);
				// The other foot doesn't change
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					newState[otherFoot, a] = currentState[otherFoot, a];
				// The given foot brackets the two new arrows
				newState[(int)foot, 0] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footActions[0]));
				newState[(int)foot, 1] = new GraphNode.FootArrowState(secondIndex, StateAfterAction(footActions[1]));
				return new List<GraphNode.FootArrowState[,]> { newState };
			}

			return newStates;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketOneNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return null;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketBothSame(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			//TODO: Implement. This is currently a copy of FillBracketBothNew
			return null;

			if (footActions.Length != MaxArrowsPerFoot)
				return null;

			// Cannot release on a new arrow
			foreach (var footAction in footActions)
			{
				if (footAction == FootAction.Release)
					return null;
			}
			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var newStates = new List<GraphNode.FootArrowState[,]>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[(int)foot][secondIndex])
					continue;
				// Skip if this second arrow is occupied.
				if (!IsFree(currentState, secondIndex))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, secondIndex))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, secondIndex);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[secondIndex];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
				var otherFoot = (int)Other(foot);
				// The other foot doesn't change
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					newState[otherFoot, a] = currentState[otherFoot, a];
				// The given foot brackets the two new arrows
				newState[(int)foot, 0] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footActions[0]));
				newState[(int)foot, 1] = new GraphNode.FootArrowState(secondIndex, StateAfterAction(footActions[1]));
				return new List<GraphNode.FootArrowState[,]> { newState };
			}

			return newStates;
		}

		private static bool IsValidPairingWithAnyOtherFoot(GraphNode.FootArrowState[,] state, Foot foot, ArrowData[] arrowData, int arrow)
		{
			return GetValidPairingsWithOtherFoot(state, foot, arrowData, arrow).Count > 0;
		}

		private static List<int> GetValidPairingsWithOtherFoot(GraphNode.FootArrowState[,] state, Foot foot, ArrowData[] arrowData, int arrow)
		{
			var result = new List<int>();
			var otherFoot = Other(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[(int)otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairings[(int) otherFoot][arrow])
					result.Add(otherFootArrowIndex);
			}
			return result;
		}

		private static bool FootCrossesOverWithAnyOtherFoot(GraphNode.FootArrowState[,] state, Foot foot, ArrowData[] arrowData, int arrow)
		{
			return FootCrossesOverInFrontWithAnyOtherFoot(state, foot, arrowData, arrow)
			       || FootCrossesOverInBackWithAnyOtherFoot(state, foot, arrowData, arrow);
		}

		private static bool FootCrossesOverInFrontWithAnyOtherFoot(GraphNode.FootArrowState[,] state, Foot foot, ArrowData[] arrowData, int arrow)
		{
			var otherFoot = Other(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[(int) otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverFront[(int)otherFoot][arrow])
					return true;
			}
			return false;
		}

		private static bool FootCrossesOverInBackWithAnyOtherFoot(GraphNode.FootArrowState[,] state, Foot foot, ArrowData[] arrowData, int arrow)
		{
			var otherFoot = Other(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[(int)otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverBehind[(int)otherFoot][arrow])
					return true;
			}
			return false;
		}

		private static bool IsFree(GraphNode.FootArrowState[,] state, int arrow)
		{
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (state[f, a].Arrow == arrow)
						return false;
			return true;
		}

		private static int NumHeldOrRolling(GraphNode.FootArrowState[,] state, Foot foot)
		{
			var num = 0;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[(int)foot, a].Arrow != InvalidArrowIndex
					&& (state[(int)foot, a].State == GraphArrowState.Held
				        || state[(int)foot, a].State == GraphArrowState.Rolling))
					num++;
			return num;
		}

		private static bool IsHeldOrRolling(GraphNode.FootArrowState[,] state, int arrow, Foot foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[(int)foot, a].Arrow == arrow
				    && (state[(int)foot, a].State == GraphArrowState.Held
				        || state[(int)foot, a].State == GraphArrowState.Rolling))
					return true;
			return false;
		}

		private static bool IsResting(GraphNode.FootArrowState[,] state, int arrow, Foot foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[(int)foot, a].Arrow == arrow && state[(int)foot, a].State == GraphArrowState.Resting)
					return true;
			return false;
		}

		public static bool IsOn(GraphNode.FootArrowState[,] state, int arrow, Foot foot)
		{
			for(var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[(int)foot, a].Arrow == arrow)
					return true;
			return false;
		}

		private static Foot Other(Foot foot)
		{
			return foot == Foot.Left ? Foot.Right : Foot.Left;
		}

		private static GraphArrowState StateAfterAction(FootAction footAction)
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

		private static List<T[]> Combinations<T>(int size) where T : Enum
		{
			return Combinations(Enum.GetValues(typeof(T)).Cast<T>().ToList(), size);
		}

		private static List<T[]> Combinations<T>(IEnumerable<T> elements, int size)
		{
			var result = new List<T[]>();
			if (size < 1)
				return result;

			var elementList = elements.ToList();
			var len = elementList.Count;
			var indices = new int[size];

			bool Inc()
			{
				var i = size - 1;
				while (i >= 0 && indices[i] == len - 1)
				{
					indices[i] = 0;
					i--;
				}
				if (i < 0)
					return false;
				indices[i]++;
				return true;
			}

			do
			{
				var r = new T[size];
				for (var i = 0; i < size; i++)
					r[i] = elementList[indices[i]];
				result.Add(r);
			} while (Inc());

			return result;
		}

	}
}
