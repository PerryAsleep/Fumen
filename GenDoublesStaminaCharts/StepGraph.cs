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
		Free,
		RResting,
		RHeld,
		RRolling,
		LResting,
		LHeld,
		LRolling
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
		public readonly GraphArrowState[] State;
		public Dictionary<GraphLink, List<GraphNode>> Links = new Dictionary<GraphLink, List<GraphNode>>();

		public GraphNode(GraphArrowState[] state)
		{
			State = state;
		}

		public bool Equals(GraphNode other)
		{
			if (other == null)
				return false;
			if (State.Length != other.State.Length)
				return false;
			for (var arrow = 0; arrow < State.Length; arrow++)
			{
				if (State[arrow] != other.State[arrow])
					return false;
			}
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
				hash = unchecked(hash * 31 + (int)state);
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

			const int l = (int) Foot.Left;
			const int r = (int) Foot.Right;

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
						[l] = FromDP(new[] {P1D, P1U}),
						// Right foot on P1L is a crossover and not bracketable
						[r] = noneDP
					},

					OtherFootPairings =
					{
						// Left foot on P1L supports right foot on P1D, P1U, and P1R without crossovers
						[l] = FromDP(new[] {P1D, P1U, P1R}),
						// Right foot on P1L is a crossover with no normal left foot pairing
						[r] = noneDP,
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1L is never a crossover position
						[l] = noneDP,
						// Right foot on P1L is a crossover with right in front when left is on P1D
						[r] = FromDP(new[] {P1D}),
					},

					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1L is never a crossover position
						[l] = noneDP,
						// Right foot on P1L is a crossover with right in back when left is on P1U
						[r] = FromDP(new[] {P1U}),
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1L is never a crossover position
						[l] = noneDP,
						// Right foot on P1L is a crossover with left in front when left is on P1U
						[r] = FromDP(new[] {P1U}),
					},

					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1L is never a crossover position
						[l] = noneDP,
						// Right foot on P1L is a crossover with left in back when left is on P1D
						[r] = FromDP(new[] {P1D}),
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
						[l] = FromDP(new[] {P1L, P1R}),
						// Right foot on P1D is bracketable with P1R
						[r] = FromDP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1D supports right foot on P1U, P1R, and P2L without crossovers
						[l] = FromDP(new[] {P1U, P1R, P2L}),
						// Right foot on P1D supports Left foot on P1L and P1U without crossovers
						[r] = FromDP(new[] {P1L, P1U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[l] = noneDP,
						// Right foot on P1D is not a crossover with right in front
						[r] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1D is a crossover with left in back with right is on P1L
						[l] = FromDP(new[] {P1L}),
						// Right foot on P1D is a crossover with right in back when left is on P1R
						[r] = FromDP(new[] {P1R})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1D is a crossover with right in front with right is on P1L
						[l] = FromDP(new[] {P1L}),
						// Right foot on P1D is a crossover with left in front when left is on P1R
						[r] = FromDP(new[] {P1R})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// Right foot on P1D is not a crossover with left in back
						[r] = noneDP
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
						[l] = FromDP(new[] {P1L, P1R}),
						// Right foot on P1U is bracketable with P1R
						[r] = FromDP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1U supports right foot on P1D, P1R, and P2L without crossovers
						[l] = FromDP(new[] {P1D, P1R, P2L}),
						// Right foot on P1U supports left foot on P1L and P1D without crossovers
						[r] = FromDP(new[] {P1L, P1D})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1U is a crossover with left in front when right is on P1L
						[l] = FromDP(new[] {P1L}),
						// Right foot on P1U is a crossover with right in front when left is on P1R
						[r] = FromDP(new[] {P1R})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1U is a crossover with right in back when right is on P1L
						[l] = FromDP(new[] {P1L}),
						// Right foot on P1U is a crossover with left in back when left is on P1R
						[r] = FromDP(new[] {P1R})
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
						[l] = FromDP(new[] {P1D, P1U, P2R}),
						// Right foot on P1R is bracketable with P1D, P1U, and P2R
						[r] = FromDP(new[] {P1D, P1U, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P1R supports right foot on P2L, P2D, and P2U without crossovers
						[l] = FromDP(new[] {P2L, P2D, P2U}),
						// Right foot on P1R supports left foot on P1L, P1D, and P1U without crossovers
						[r] = FromDP(new[] {P1L, P1D, P1U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P1R is a crossover with left in front when right is on P1D
						[l] = FromDP(new[] {P1D}),
						// Right foot on P1R is not a crossover position. Not considering left on P2L, slightly too twisty
						[r] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P1R is a crossover with left in back when right is on P1U
						[l] = FromDP(new[] {P1U}),
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1R is a crossover with right in front when right is on P1U
						[l] = FromDP(new[] {P1U}),
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1R is a crossover with right in back when right is on P1D
						[l] = FromDP(new[] {P1D}),
						// none
						[r] = noneDP
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
						[l] = FromDP(new[] {P1R, P2D, P2U}),
						// Right foot on P2L is bracketable with P1R, P2D, and P2U
						[r] = FromDP(new[] {P1R, P2D, P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2L supports right foot on P2D, P2U, and P2R without crossovers
						[l] = FromDP(new[] {P2D, P2U, P2R}),
						// Right foot on P2L supports left foot on P1D, P1U, and P1L without crossovers
						[r] = FromDP(new[] {P1D, P1U, P1L})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[l] = noneDP,
						// Right foot on P2L is a crossover with right in front when left is on P2D
						[r] = FromDP(new[] {P2D})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// Right foot on P2L is a crossover with right in back when left is on P2U
						[r] = FromDP(new[] {P2U})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2L is not a crossover position. Not considering right on P1R, slightly too twisty
						[l] = noneDP,
						// Right foot on P2L is a crossover with left in front when left is on P2U
						[r] = FromDP(new[] {P2U})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// Right foot on P2L is a crossover with left in back when left is on P2D
						[r] = FromDP(new[] {P2D})
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
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2D is bracketable with P2L and P2R
						[r] = FromDP(new[] {P2L, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P2D supports right foot on P2U and P2R without crossovers
						[l] = FromDP(new[] {P2U, P2R}),
						// Right foot on P2D supports left foot on P1R, P2L, and P2U without crossovers
						[r] = FromDP(new[] {P1R, P2L, P2U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P2D is a crossover with left in back when right is on P2L
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2D is a crossover with right in back when left is on P2R
						[r] = FromDP(new[] {P2R})
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2D is a crossover with right in front when right is on P2L
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2D is a crossover with left in front when left is on P2R
						[r] = FromDP(new[] {P2R})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
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
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2U is bracketable with P2L and P2R
						[r] = FromDP(new[] {P2L, P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P2U supports right foot on P2D and P2R without crossovers
						[l] = FromDP(new[] {P2D, P2R}),
						// Right foot on P2U supports left foot on P1R, P2L, and P2D without crossovers
						[r] = FromDP(new[] {P1R, P2L, P2D})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P2U is a crossover with left in front when right is on P2L
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2U is a crossover with right in front when left is on P2R
						[r] = FromDP(new[] {P2R})
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// none
						[l] = noneDP,
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P2U is a crossover with right in back when right is on P2L
						[l] = FromDP(new[] {P2L}),
						// Right foot on P2U is a crossover with left in back when left is on P2R
						[r] = FromDP(new[] {P2R})
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
						[l] = noneDP,
						// Right foot on P2R is bracketable with P2D and P2U
						[r] = FromDP(new[] {P2D, P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2R is a crossover with no normal right foot pairing
						[l] = noneDP,
						// Right foot on P2R supports left foot on P2L, P2D, and P2U without crossovers
						[r] = FromDP(new[] {P2L, P2D, P2U})
					},

					OtherFootPairingsSameFootCrossoverFront =
					{
						// Left foot on P2R is a crossover with left in front when right is on P2D
						[l] = FromDP(new[] {P2D}),
						// Right foot on P2R is never a crossover position
						[r] = noneDP
					},
					OtherFootPairingsSameFootCrossoverBehind =
					{
						// Left foot on P2R is a crossover with left in back when right is on P2U
						[l] = FromDP(new[] {P2U}),
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P2R is a crossover with right in front when right is on P2U
						[l] = FromDP(new[] {P2U}),
						// none
						[r] = noneDP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P2R is a crossover with right in back when right is on P2D
						[l] = FromDP(new[] {P2D}),
						// none
						[r] = noneDP
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
			var root = new GraphNode(new GraphArrowState[NumSPArrows]);
			root.State[P1L] = GraphArrowState.LResting;
			root.State[P1R] = GraphArrowState.RResting;

			FillStepGraph(root, new HashSet<GraphNode>(), SPArrowData);

			return root;
		}

		public GraphNode CreateDPStepGraph()
		{
			var root = new GraphNode(new GraphArrowState[NumDPArrows]);
			root.State[P1R] = GraphArrowState.LResting;
			root.State[P2L] = GraphArrowState.RResting;

			FillStepGraph(root, new HashSet<GraphNode>(), DPArrowData);

			return root;
		}

		private static Func<GraphArrowState[], ArrowData[], int, int, Foot, FootAction[], List<GraphArrowState[]>> GetFillFunc(
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

		private static void FillStepGraph(GraphNode currentNode, HashSet<GraphNode> visitedNodes, ArrowData[] arrowData)
		{
			if (!visitedNodes.Add(currentNode))
				return;

			// Single Steps
			foreach (var stepType in Enum.GetValues(typeof(SingleStepType)).Cast<SingleStepType>())
				FillSingleFootStep(currentNode, visitedNodes, arrowData, stepType);

			// Jumps
			foreach (var jump in JumpCombinations)
				FillJump(currentNode, visitedNodes, arrowData, jump);
		}

		private static GraphNode GetOrCreateNodeByState(GraphArrowState[] state, HashSet<GraphNode> visitedNodes)
		{
			var node = new GraphNode(state);
			if (visitedNodes.TryGetValue(node, out var currentNode))
				node = currentNode;
			return node;
		}

		private static void AddNodeAndRecurse(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			GraphArrowState[] state,
			GraphLink link,
			ArrowData[] arrowData)
		{
			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();

			var newNode = GetOrCreateNodeByState(state, visitedNodes);
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);

			FillStepGraph(newNode, visitedNodes, arrowData);
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

								AddNodeAndRecurse(currentNode, visitedNodes, newState, link, arrowData);
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
			var l = (int)Foot.Left;
			var r = (int)Foot.Right;

			var actionsToStatesL = FillJumpStep(currentNode.State, arrowData, stepTypes[l], Foot.Left);
			foreach (var actionStateL in actionsToStatesL)
			{
				foreach (var newStateL in actionStateL.Value)
				{
					var actionsToStatesR = FillJumpStep(newStateL, arrowData, stepTypes[r], Foot.Right);
					foreach (var actionStateR in actionsToStatesR)
					{
						foreach (var newStateR in actionStateR.Value)
						{
							var link = new GraphLink();
							for (var f = 0; f < GetNumArrowForStep(stepTypes[l]); f++)
								link.Links[l, f] = new Tuple<SingleStepType, FootAction>(stepTypes[l], actionStateL.Key[f]);
							for (var f = 0; f < GetNumArrowForStep(stepTypes[r]); f++)
								link.Links[r, f] = new Tuple<SingleStepType, FootAction>(stepTypes[r], actionStateR.Key[f]);
							AddNodeAndRecurse(currentNode, visitedNodes, newStateR, link, arrowData);
						}
					}
				}
			}
		}

		private static Dictionary<FootAction[], List<GraphArrowState[]>> FillJumpStep(
			GraphArrowState[] currentState,
			ArrowData[] arrowData,
			SingleStepType stepType,
			Foot foot)
		{
			var result = new Dictionary<FootAction[], List<GraphArrowState[]>>();

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

		private static List<GraphArrowState[]> FillNewArrow(
			GraphArrowState[] currentState,
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
			if (!IsOn(currentState[currentIndex], foot))
				return null;
			// Cannot step on a new arrow if already holding on MaxArrowsPerFoot
			var numHeld = currentState.Count(s => IsHeldOrRolling(s, foot));
			if (numHeld >= MaxArrowsPerFoot)
				return null;
			// If bracketing, skip if this is not a valid bracketable pairing.
			if (numHeld == 1 && !arrowData[currentIndex].BracketablePairings[(int)foot][newIndex])
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (currentState[newIndex] != GraphArrowState.Free)
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
			var newState = new GraphArrowState[currentState.Length];
			for (var i = 0; i < currentState.Length; i++)
			{
				newState[i] = currentState[i];
				if (IsResting(newState[i], foot))
					newState[i] = GraphArrowState.Free;
			}
			newState[newIndex] = StateAfterAction(footAction, foot);
			return new List<GraphArrowState[]> { newState };
		}

		private static List<GraphArrowState[]> FillSameArrow(
			GraphArrowState[] currentState,
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
			if (footAction == FootAction.Release && !IsHeldOrRolling(currentState[currentIndex], foot))
				return null;
			// Normal logic. Placement action on a resting arrow.
			if (footAction != FootAction.Release && !IsResting(currentState[currentIndex], foot))
				return null;

			// Set up the state for a new node.
			// Copy the previous state and if placing a new foot, lift from any resting arrows.
			// It is necessary to lift for, e.g. the step after an SP quad.
			var newState = new GraphArrowState[currentState.Length];
			for (var i = 0; i < currentState.Length; i++)
			{
				newState[i] = currentState[i];
				if (footAction != FootAction.Release && IsResting(newState[i], foot))
					newState[i] = GraphArrowState.Free;
			}
			newState[newIndex] = StateAfterAction(footAction, foot);
			return new List<GraphArrowState[]> { newState };
		}

		private static List<GraphArrowState[]> FillFootSwap(
			GraphArrowState[] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			// TODO: If you are already crossed over, should you be able to swap?
			// TODO: Double check lifting in new state setup.
			// TODO: If you swap and the other foot is now not resting anywhere, is that a problem?
			// Yes, it will be.
			// With current data structures, can't have two feet resting on the same arrow
			// Solution 1) For the next step, if there are no resting arrows, can you assume that the foot is resting on
			// the other foot?
			// What about alternating swaps on the same arrow?
			// Solution 2) Find an arrow in this method to put the other foot on as resting?
			// probably a bracketable pairing?
			return null;

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
			if (!IsResting(currentState[currentIndex], foot))
				return null;
			// The new index must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState[newIndex], otherFoot))
				return null;
			// Disallow foot swap if this foot is holding or rolling
			if (currentState.Count(s => IsHeldOrRolling(s, foot)) > 0)
				return null;
			// Disallow foot swap if the other foot is holding or rolling
			if (currentState.Count(s => IsHeldOrRolling(s, otherFoot)) > 0)
				return null;

			// Set up the state for a new node.
			// Copy the previous state and update the state for the arrow under consideration.
			// Lift all resting arrows in case swapping from a bracket
			// The new index will now have the new foot on it and old index will be free.
			var newState = new GraphArrowState[currentState.Length];
			for (var i = 0; i < currentState.Length; i++)
			{
				newState[i] = currentState[i];
				if (IsResting(newState[i], foot))
					newState[i] = GraphArrowState.Free;
			}
			newState[newIndex] = StateAfterAction(footAction, foot);
			return new List<GraphArrowState[]> { newState };
		}

		private static List<GraphArrowState[]> FillCrossoverFront(
			GraphArrowState[] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, true);
		}

		private static List<GraphArrowState[]> FillCrossoverBack(
			GraphArrowState[] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, false);
		}

		private static List<GraphArrowState[]> FillCrossoverInternal(
			GraphArrowState[] currentState,
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
			if (!IsResting(currentState[currentIndex], foot))
				return null;
			// Cannot crossover if any arrows are held by this foot.
			var numHeld = currentState.Count(s => IsHeldOrRolling(s, foot));
			if (numHeld > 0)
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (currentState[newIndex] != GraphArrowState.Free)
				return null;
			// Skip if this next arrow is not a crossover
			if (front && !FootCrossesOverInFrontWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;
			if (!front && !FootCrossesOverInBackWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Set up the state for a new node.
			// Copy the previous state, but lift from any resting arrows for the given foot.
			// We know at this point the given foot is not holding or rolling due to a check above.
			var newState = new GraphArrowState[currentState.Length];
			for (var i = 0; i < currentState.Length; i++)
			{
				newState[i] = currentState[i];
				if (IsResting(newState[i], foot))
					newState[i] = GraphArrowState.Free;
			}
			newState[newIndex] = StateAfterAction(footAction, foot);
			return new List<GraphArrowState[]> { newState };
		}

		private static List<GraphArrowState[]> FillBracketBothNew(
			GraphArrowState[] currentState,
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
			if (!IsResting(currentState[currentIndex], foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			var numHeld = currentState.Count(s => IsHeldOrRolling(s, foot));
			if (numHeld > 0)
				return null;
			// Skip if this next arrow is occupied.
			if (currentState[newIndex] != GraphArrowState.Free)
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var newStates = new List<GraphArrowState[]>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this next arrow is occupied.
				if (currentState[secondIndex] != GraphArrowState.Free)
					continue;
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[(int)foot][secondIndex])
					continue;
				// Skip if this second arrow is occupied.
				if (currentState[secondIndex] != GraphArrowState.Free)
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
				// Copy the previous state, but lift from any arrows for the given foot.
				// We know at this point the given foot is not holding or rolling due to a check above.
				var newState = new GraphArrowState[currentState.Length];
				for (var i = 0; i < currentState.Length; i++)
				{
					newState[i] = currentState[i];
					if (IsResting(newState[i], foot))
						newState[i] = GraphArrowState.Free;
				}
				newState[newIndex] = StateAfterAction(footActions[0], foot);
				newState[secondIndex] = StateAfterAction(footActions[1], foot);
				newStates.Add(newState);
			}

			return newStates;
		}

		private static List<GraphArrowState[]> FillBracketOneNew(
			GraphArrowState[] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			Foot foot,
			FootAction[] footActions)
		{
			return null;
		}

		private static List<GraphArrowState[]> FillBracketBothSame(
			GraphArrowState[] currentState,
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
			if (!IsResting(currentState[currentIndex], foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			var numHeld = currentState.Count(s => IsHeldOrRolling(s, foot));
			if (numHeld > 0)
				return null;
			// Skip if this next arrow is occupied.
			if (currentState[newIndex] != GraphArrowState.Free)
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var newStates = new List<GraphArrowState[]>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this next arrow is occupied.
				if (currentState[secondIndex] != GraphArrowState.Free)
					continue;
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[(int)foot][secondIndex])
					continue;
				// Skip if this second arrow is occupied.
				if (currentState[secondIndex] != GraphArrowState.Free)
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
				// Copy the previous state, but lift from any arrows for the given foot.
				// We know at this point the given foot is not holding or rolling due to a check above.
				var newState = new GraphArrowState[currentState.Length];
				for (var i = 0; i < currentState.Length; i++)
				{
					newState[i] = currentState[i];
					if (IsResting(newState[i], foot))
						newState[i] = GraphArrowState.Free;
				}
				newState[newIndex] = StateAfterAction(footActions[0], foot);
				newState[secondIndex] = StateAfterAction(footActions[1], foot);
				newStates.Add(newState);
			}

			return newStates;
		}

		private static bool IsValidPairingWithAnyOtherFoot(GraphArrowState[] state, Foot foot, ArrowData[] arrowData, int index)
		{
			return GetValidPairingsWithOtherFoot(state, foot, arrowData, index).Count > 0;
		}

		private static List<int> GetValidPairingsWithOtherFoot(GraphArrowState[] state, Foot foot, ArrowData[] arrowData, int index)
		{
			var result = new List<int>();
			var otherFoot = Other(foot);
			for (var i = 0; i < state.Length; i++)
			{
				if (!IsOn(state[i], otherFoot))
					continue;
				if (arrowData[i].OtherFootPairings[(int)otherFoot][index])
					result.Add(i);
			}

			return result;
		}

		private static bool FootCrossesOverWithAnyOtherFoot(GraphArrowState[] state, Foot foot, ArrowData[] arrowData, int index)
		{
			return FootCrossesOverInFrontWithAnyOtherFoot(state, foot, arrowData, index)
			       || FootCrossesOverInBackWithAnyOtherFoot(state, foot, arrowData, index);
		}

		private static bool FootCrossesOverInFrontWithAnyOtherFoot(GraphArrowState[] state, Foot foot, ArrowData[] arrowData, int index)
		{
			var otherFoot = Other(foot);
			for (var i = 0; i < state.Length; i++)
			{
				if (!IsOn(state[i], otherFoot))
					continue;

				if (arrowData[i].OtherFootPairingsOtherFootCrossoverFront[(int)otherFoot][index])
					return true;
			}
			return false;
		}

		private static bool FootCrossesOverInBackWithAnyOtherFoot(GraphArrowState[] state, Foot foot, ArrowData[] arrowData, int index)
		{
			var otherFoot = Other(foot);
			for (var i = 0; i < state.Length; i++)
			{
				if (!IsOn(state[i], otherFoot))
					continue;

				if (arrowData[i].OtherFootPairingsOtherFootCrossoverBehind[(int)otherFoot][index])
					return true;
			}
			return false;
		}

		private static bool IsHeldOrRolling(GraphArrowState state, Foot foot)
		{
			if (foot == Foot.Left)
				return state == GraphArrowState.LHeld || state == GraphArrowState.LRolling;
			return state == GraphArrowState.RHeld || state == GraphArrowState.RRolling;
		}

		private static bool IsResting(GraphArrowState state, Foot foot)
		{
			if (foot == Foot.Left)
				return state == GraphArrowState.LResting;
			return state == GraphArrowState.RResting;
		}

		public static bool IsOn(GraphArrowState state, Foot foot)
		{
			if (foot == Foot.Left)
				return state == GraphArrowState.LHeld || state == GraphArrowState.LRolling || state == GraphArrowState.LResting;
			return state == GraphArrowState.RHeld || state == GraphArrowState.RRolling || state == GraphArrowState.RResting;
		}

		private static Foot Other(Foot foot)
		{
			return foot == Foot.Left ? Foot.Right : Foot.Left;
		}

		private static GraphArrowState StateAfterAction(FootAction footAction, Foot foot)
		{
			switch (footAction)
			{
				case FootAction.Tap:
				case FootAction.Release:
					return foot == Foot.Left ? GraphArrowState.LResting : GraphArrowState.RResting;
				case FootAction.Hold:
					return foot == Foot.Left ? GraphArrowState.LHeld : GraphArrowState.RHeld;
				case FootAction.Roll:
					return foot == Foot.Left ? GraphArrowState.LRolling : GraphArrowState.RRolling;
			}
			return GraphArrowState.Free;
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
