using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	/// <summary>
	/// Information about an arrow in an array of arrows representing the layout
	/// of one or more pads. This data informs how the other arrows are associated with
	/// this arrow. For example, per Arrow, it is useful to know which other arrows
	/// are bracketable with it, are steppable to from it, form crossovers with it, etc.
	/// </summary>
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

		/// <summary>
		/// Static accessor for 4-panel SP ArrowData.
		/// </summary>
		public static ArrowData[] SPArrowData { get; }
		/// <summary>
		/// Static accessor for 8-panel DP ArrowData.
		/// </summary>
		public static ArrowData[] DPArrowData { get; }

		/// <summary>
		/// Static initializer. Creates SPArrowData and DPArrowData.
		/// </summary>
		static ArrowData()
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
						// Left foot on P1R is bracketable with P1D, P1U, and P2L
						[L] = FromDP(new[] {P1D, P1U, P2L}),
						// Right foot on P1R is bracketable with P1D, P1U, and P2L
						[R] = FromDP(new[] {P1D, P1U, P2L})
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
						// Right foot on P2L supports left foot on P1D, P1U, and P1R without crossovers
						[R] = FromDP(new[] {P1D, P1U, P1R})
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
					BracketablePairings = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
					OtherFootPairings = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
					OtherFootPairingsSameFootCrossoverFront = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
					OtherFootPairingsSameFootCrossoverBehind = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
					OtherFootPairingsOtherFootCrossoverFront = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
					OtherFootPairingsOtherFootCrossoverBehind = {[0] = new bool[NumSPArrows], [1] = new bool[NumSPArrows]},
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

			TestSymmetry();
		}

		private static void TestSymmetry()
		{
			for (var a = 0; a < NumDPArrows; a++)
			{
				for (var a2 = 0; a2 < NumDPArrows; a2++)
				{
					var oppositeA = NumDPArrows - a - 1;
					var oppositeA2 = NumDPArrows - a2 - 1;

					Debug.Assert(DPArrowData[a].ValidNextArrows[a2]
					             == DPArrowData[oppositeA].ValidNextArrows[oppositeA2]);

					for (var f = 0; f < NumFeet; f++)
					{
						var oppositeF = NumFeet - f - 1;
						Debug.Assert(DPArrowData[a].BracketablePairings[f][a2]
						             != DPArrowData[oppositeA].BracketablePairings[oppositeF][oppositeA2]);
						Debug.Assert(DPArrowData[a].OtherFootPairings[f][a2]
						             == DPArrowData[oppositeA].OtherFootPairings[oppositeF][oppositeA2]);
						Debug.Assert(DPArrowData[a].OtherFootPairingsOtherFootCrossoverBehind[f][a2]
						             == DPArrowData[oppositeA].OtherFootPairingsOtherFootCrossoverFront[oppositeF][oppositeA2]);
						Debug.Assert(DPArrowData[a].OtherFootPairingsSameFootCrossoverBehind[f][a2]
						             == DPArrowData[oppositeA].OtherFootPairingsSameFootCrossoverFront[oppositeF][oppositeA2]);
						Debug.Assert(DPArrowData[a].OtherFootPairingsOtherFootCrossoverFront[f][a2]
						             == DPArrowData[oppositeA].OtherFootPairingsOtherFootCrossoverBehind[oppositeF][oppositeA2]);
						Debug.Assert(DPArrowData[a].OtherFootPairingsSameFootCrossoverFront[f][a2]
						             == DPArrowData[oppositeA].OtherFootPairingsSameFootCrossoverBehind[oppositeF][oppositeA2]);
					}
				}
			}
		}
	}
}
