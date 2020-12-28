using System.Collections.Generic;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	/// <summary>
	/// Information about an arrow in an array of arrows representing the layout
	/// of one or more pads. This data informs how the other arrows are associated with
	/// this arrow. For example, per arrow, it is useful to know which other arrows
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
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// toes are on this arrow and the heel is on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] BracketablePairingsOtherHeel = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// heel is on this arrow and the toes are on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] BracketablePairingsOtherToe = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are valid pairings for the other foot.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot without crossing over.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairings = new bool[NumFeet][];

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
		/// Which arrows form an inverted position.
		/// An inverted position is one where if the player stood normally without
		/// twisting their body to face the screen they would be facing completely backwards.
		/// For example, left foot on right and right foot on left.
		/// For this data structure, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that the player is inverted.
		/// While there are two BodyOrientations for being inverted, every inverted position
		/// can be performed with right over left and left over right, so we only need one
		/// data structure.
		/// First index is foot, second is arrow.
		/// </summary>
		public bool[][] OtherFootPairingsInverted = new bool[NumFeet][];

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
			bool[] noneSP = { false, false, false, false };
			bool[] FromSP(IEnumerable<int> arrows)
			{
				var ret = new bool[NumSPArrows];
				foreach (var arrow in arrows)
					ret[arrow] = true;
				return ret;
			}

			SPArrowData = new[]
			{
				// P1L
				new ArrowData
				{
					Position = P1L,

					// A foot on P1L can move next to P1D, P1U, or P1R
					ValidNextArrows = FromSP(new[] {P1D, P1U, P1R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1L is bracketable with the heel on other arrows on P1D
						[L] = FromSP(new[] {P1D}),
						// Right foot on P1L is a crossover and not bracketable
						[R] = noneSP
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1L is bracketable with the toes on other arrows on P1U
						[L] = FromSP(new[] {P1U}),
						// Right foot on P1L is a crossover and not bracketable
						[R] = noneSP
					},

					OtherFootPairings =
					{
						// Left foot on P1L supports right foot on P1D, P1U, and P1R without crossovers
						[L] = FromSP(new[] {P1D, P1U, P1R}),
						// Right foot on P1L is a crossover with no normal left foot pairing
						[R] = noneSP,
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneSP,
						// Right foot on P1L is a crossover with left in front when left is on P1U
						[R] = FromSP(new[] {P1U}),
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1L is never a crossover position
						[L] = noneSP,
						// Right foot on P1L is a crossover with left in back when left is on P1D
						[R] = FromSP(new[] {P1D}),
					},

					OtherFootPairingsInverted =
					{
						// Left foot on P1L is never inverted.
						[L] = noneSP,
						// Right foot on P1L is inverted with left when left is on P1R
						[R] = FromSP(new[] {P1R}),
					},
				},

				// P1D
				new ArrowData
				{
					Position = P1D,

					// A foot on P1D can move next to P1L, P1U, or P1R
					ValidNextArrows = FromSP(new[] {P1L, P1U, P1R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1D uses the heel.
						[L] = noneSP,
						// Right foot on P1D uses the heel.
						[R] = noneSP
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1D is bracketable with the toes on other arrows on P1L
						[L] = FromSP(new[] {P1L}),
						// Right foot on P1D is bracketable with the toes on other arrows on P1R
						[R] = FromSP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1D supports right foot on P1U and P1R without crossovers
						[L] = FromSP(new[] {P1U, P1R}),
						// Right foot on P1D supports Left foot on P1L and P1U without crossovers
						[R] = FromSP(new[] {P1L, P1U})
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1D is a crossover with right in front with right is on P1L
						[L] = FromSP(new[] {P1L}),
						// Right foot on P1D is a crossover with left in front when left is on P1R
						[R] = FromSP(new[] {P1R})
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// none
						[L] = noneSP,
						// Right foot on P1D is not a crossover with left in back
						[R] = noneSP
					},

					OtherFootPairingsInverted =
					{
						// Left foot on P1D is never inverted.
						[L] = noneSP,
						// Right foot on P1D is never inverted.
						[R] = noneSP,
					},
				},

				// P1U
				new ArrowData
				{
					Position = P1U,

					// A foot on P1U can move next to P1L, P1D, or P1R
					ValidNextArrows = FromSP(new[] {P1L, P1D, P1R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1U is bracketable with the heel on other arrows on P1L
						[L] = FromSP(new[] {P1L}),
						// Right foot on P1U is bracketable with the heel on other arrows on P1R
						[R] = FromSP(new[] {P1R})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1U uses the toes.
						[L] = noneSP,
						// Right foot on P1U uses the toes.
						[R] = noneSP
					},

					OtherFootPairings =
					{
						// Left foot on P1U supports right foot on P1D and P1R without crossovers
						[L] = FromSP(new[] {P1D, P1R}),
						// Right foot on P1U supports left foot on P1L and P1D without crossovers
						[R] = FromSP(new[] {P1L, P1D})
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// none
						[L] = noneSP,
						// none
						[R] = noneSP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1U is a crossover with right in back when right is on P1L
						[L] = FromSP(new[] {P1L}),
						// Right foot on P1U is a crossover with left in back when left is on P1R
						[R] = FromSP(new[] {P1R})
					},

					OtherFootPairingsInverted =
					{
						// Left foot on P1U is never inverted.
						[L] = noneSP,
						// Right foot on P1U is never inverted.
						[R] = noneSP,
					},
				},

				// P1R
				new ArrowData
				{
					Position = P1R,

					// A foot on P1R can move next to P1L, P1D, or P1U
					ValidNextArrows = FromSP(new[] {P1L, P1D, P1U}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1R is a crossover and not bracketable.
						[L] = noneSP,
						// Right foot on P1R is bracketable with the heel on other arrows on P1D
						[R] = FromSP(new[] {P1D})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1R is a crossover and not bracketable.
						[L] = noneSP,
						// Right foot on P1R is bracketable with the toes on other arrows on P1U
						[R] = FromSP(new[] {P1U})
					},

					OtherFootPairings =
					{
						// Left foot on P1R is a crossover with no normal right foot pairing
						[L] = noneSP,
						// Right foot on P1R supports left foot on P1L, P1D, and P1U without crossovers
						[R] = FromSP(new[] {P1L, P1D, P1U})
					},

					OtherFootPairingsOtherFootCrossoverFront =
					{
						// Left foot on P1R is a crossover with right in front when right is on P1U
						[L] = FromSP(new[] {P1U}),
						// Right foot on P1R is not a crossover position
						[R] = noneSP
					},
					OtherFootPairingsOtherFootCrossoverBehind =
					{
						// Left foot on P1R is a crossover with right in back when right is on P1D
						[L] = FromSP(new[] {P1D}),
						// Right foot on P1R is not a crossover position
						[R] = noneSP
					},

					OtherFootPairingsInverted =
					{
						// Left foot on P1R is inverted with right when right is on P1L
						[L] = FromSP(new[] {P1L}),
						// Right foot on P1R is never inverted.
						[R] = noneSP,
					},
				}
			};

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

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1L is bracketable with the heel on other arrows on P1D
						[L] = FromDP(new[] {P1D}),
						// Right foot on P1L is a crossover and not bracketable
						[R] = noneDP
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1L is bracketable with the toes on other arrows on P1U
						[L] = FromDP(new[] {P1U}),
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
					},

					OtherFootPairingsInverted =
					{
						// Left foot on P1L is never inverted.
						[L] = noneDP,
						// Right foot on P1L is inverted with left when left is on P1R
						[R] = FromDP(new[] {P1R}),
					},
				},

				// P1D
				new ArrowData
				{
					Position = P1D,

					// A foot on P1D can move next to P1L, P1U, P1R, or P2L
					ValidNextArrows = FromDP(new[] {P1L, P1U, P1R, P2L}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1D uses the heel.
						[L] = noneDP,
						// Right foot on P1D uses the heel.
						[R] = noneDP
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1D is bracketable with the toes on other arrows on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1D is bracketable with the toes on other arrows on P1R
						[R] = FromDP(new[] {P1R})
					},

					OtherFootPairings =
					{
						// Left foot on P1D supports right foot on P1U, P1R, and P2L without crossovers
						[L] = FromDP(new[] {P1U, P1R, P2L}),
						// Right foot on P1D supports Left foot on P1L and P1U without crossovers
						[R] = FromDP(new[] {P1L, P1U})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P1D is never inverted.
						[L] = noneDP,
						// Right foot on P1D is never inverted.
						[R] = noneDP,
					},
				},

				// P1U
				new ArrowData
				{
					Position = P1U,

					// A foot on P1U can move next to P1L, P1D, P1R, or P2L
					ValidNextArrows = FromDP(new[] {P1L, P1D, P1R, P2L}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1U is bracketable with the heel on other arrows on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1U is bracketable with the heel on other arrows on P1R
						[R] = FromDP(new[] {P1R})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1U uses the toes.
						[L] = noneDP,
						// Right foot on P1U uses the toes.
						[R] = noneDP
					},

					OtherFootPairings =
					{
						// Left foot on P1U supports right foot on P1D, P1R, and P2L without crossovers
						[L] = FromDP(new[] {P1D, P1R, P2L}),
						// Right foot on P1U supports left foot on P1L and P1D without crossovers
						[R] = FromDP(new[] {P1L, P1D})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P1U is never inverted.
						[L] = noneDP,
						// Right foot on P1U is never inverted.
						[R] = noneDP,
					},
				},

				// P1R
				new ArrowData
				{
					Position = P1R,

					// A foot on P1R can move next to P1L, P1D, P1U, P2L, P2D, or P2U
					ValidNextArrows = FromDP(new[] {P1L, P1D, P1U, P2L, P2D, P2U}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P1R is bracketable with the heel on other arrows on P1D and P2L.
						[L] = FromDP(new[] {P1D, P2L}),
						// Right foot on P1R is bracketable with the heel on other arrows on P1D and P2L.
						[R] = FromDP(new[] {P1D, P2L})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P1R is bracketable with the toes on other arrows on P1U and P2L.
						[L] = FromDP(new[] {P1U, P2L}),
						// Right foot on P1R is bracketable with the toes on other arrows on P1U and P2L.
						[R] = FromDP(new[] {P1U, P2L})
					},

					OtherFootPairings =
					{
						// Left foot on P1R supports right foot on P2L, P2D, and P2U without crossovers
						[L] = FromDP(new[] {P2L, P2D, P2U}),
						// Right foot on P1R supports left foot on P1L, P1D, and P1U without crossovers
						[R] = FromDP(new[] {P1L, P1D, P1U})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P1R is inverted with right when right is on P1L
						[L] = FromDP(new[] {P1L}),
						// Right foot on P1R is inverted with left when left is on P2L
						[R] = FromDP(new[] {P2L}),
					},
				},

				// P2L
				new ArrowData
				{
					Position = P2L,

					// A foot on P2L can move next to P1D, P1U, P1R, P2D, P2U, or P2R
					ValidNextArrows = FromDP(new[] {P1D, P1U, P1R, P2D, P2U, P2R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P2L is bracketable with the heel on other arrows on P1R and P2D.
						[L] = FromDP(new[] {P1R, P2D}),
						// Right foot on P2L is bracketable with the heel on other arrows on P1R and P2D.
						[R] = FromDP(new[] {P1R, P2D})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P2L is bracketable with the toes on other arrows on P1R and P2U.
						[L] = FromDP(new[] {P1R, P2U}),
						// Right foot on P2L is bracketable with the toes on other arrows on P1R and P2U.
						[R] = FromDP(new[] {P1R, P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2L supports right foot on P2D, P2U, and P2R without crossovers
						[L] = FromDP(new[] {P2D, P2U, P2R}),
						// Right foot on P2L supports left foot on P1D, P1U, and P1R without crossovers
						[R] = FromDP(new[] {P1D, P1U, P1R})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P2L is inverted with right when right is on P1R
						[L] = FromDP(new[] {P1R}),
						// Right foot on P2L is inverted with left when left is on P2R
						[R] = FromDP(new[] {P2R}),
					},
				},

				// P2D
				new ArrowData
				{
					Position = P2D,

					// A foot on P2D can move next to P1R, P2L, P2U, and P2R
					ValidNextArrows = FromDP(new[] {P1R, P2L, P2U, P2R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P2D uses the heel.
						[L] = noneDP,
						// Right foot on P2D uses the heel.
						[R] = noneDP
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P2D is bracketable with the toes on other arrows on P2L.
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2D is bracketable with the toes on other arrows on P2R.
						[R] = FromDP(new[] {P2R})
					},

					OtherFootPairings =
					{
						// Left foot on P2D supports right foot on P2U and P2R without crossovers
						[L] = FromDP(new[] {P2U, P2R}),
						// Right foot on P2D supports left foot on P1R, P2L, and P2U without crossovers
						[R] = FromDP(new[] {P1R, P2L, P2U})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P2D is never inverted.
						[L] = noneDP,
						// Right foot on P2D is never inverted.
						[R] = noneDP,
					},
				},

				// P2U
				new ArrowData
				{
					Position = P2U,

					// A foot on P2U can move next to P1R, P2L, P2D, and P2R
					ValidNextArrows = FromDP(new[] {P1R, P2L, P2D, P2R}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P2U is bracketable with the heel on other arrows on P2L.
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2U is bracketable with the heel on other arrows on P2R.
						[R] = FromDP(new[] {P2R})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P2U uses the toes.
						[L] = noneDP,
						// Right foot on P2U uses the toes.
						[R] = noneDP
					},

					OtherFootPairings =
					{
						// Left foot on P2U supports right foot on P2D and P2R without crossovers
						[L] = FromDP(new[] {P2D, P2R}),
						// Right foot on P2U supports left foot on P1R, P2L, and P2D without crossovers
						[R] = FromDP(new[] {P1R, P2L, P2D})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P2U is never inverted.
						[L] = noneDP,
						// Right foot on P2U is never inverted.
						[R] = noneDP,
					},
				},

				// P2R
				new ArrowData
				{
					Position = P2R,

					// A foot on P2R can move next to P2L, P2D, and P2U
					ValidNextArrows = FromDP(new[] {P2L, P2D, P2U}),

					BracketablePairingsOtherHeel =
					{
						// Left foot on P2R is a crossover and not bracketable.
						[L] = noneDP,
						// Right foot on P2R is bracketable with the heel on other arrows on P2D.
						[R] = FromDP(new[] {P2D})
					},
					BracketablePairingsOtherToe =
					{
						// Left foot on P2R is a crossover and not bracketable.
						[L] = noneDP,
						// Right foot on P2R is bracketable with the toes on other arrows on P2U.
						[R] = FromDP(new[] {P2U})
					},

					OtherFootPairings =
					{
						// Left foot on P2R is a crossover with no normal right foot pairing
						[L] = noneDP,
						// Right foot on P2R supports left foot on P2L, P2D, and P2U without crossovers
						[R] = FromDP(new[] {P2L, P2D, P2U})
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

					OtherFootPairingsInverted =
					{
						// Left foot on P2R is inverted with right when right is on P2L
						[L] = FromDP(new[] {P2L}),
						// Right foot on P2R is never inverted.
						[R] = noneDP,
					},
				}
			};
		}
	}
}
