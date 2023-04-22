using System;
using System.Linq;
using static StepManiaLibrary.Constants;

namespace StepManiaLibrary
{
	/// <summary>
	/// Types of steps which can be performed with one foot.
	/// Jumps are combinations of StepTypes for each foot.
	/// </summary>
	public enum StepType
	{
		// Simple step types.
		SameArrow,
		NewArrow,
		CrossoverFront,
		CrossoverBehind,
		InvertFront,
		InvertBehind,
		FootSwap,
		Swing,

		// Simple stretch types.
		NewArrowStretch,
		CrossoverFrontStretch,
		CrossoverBehindStretch,
		InvertFrontStretch,
		InvertBehindStretch,

		// Swapping into crossover or inverted orientations due to brackets.
		FootSwapCrossoverFront,
		FootSwapCrossoverBehind,
		FootSwapInvertFront,
		FootSwapInvertBehind,

		// Brackets.
		BracketHeelNewToeNew,
		BracketHeelNewToeSame,
		BracketHeelSameToeNew,
		BracketHeelSameToeSame,
		BracketHeelSameToeSwap,
		BracketHeelNewToeSwap,
		BracketHeelSwapToeSame,
		BracketHeelSwapToeNew,
		BracketHeelSwapToeSwap,
		BracketSwing,

		// Crossover brackets.
		BracketCrossoverFrontHeelNewToeNew,
		BracketCrossoverFrontHeelNewToeSame,
		BracketCrossoverFrontHeelSameToeNew,
		BracketCrossoverBehindHeelNewToeNew,
		BracketCrossoverBehindHeelNewToeSame,
		BracketCrossoverBehindHeelSameToeNew,

		// Invert brackets.
		BracketInvertFrontHeelNewToeNew,
		BracketInvertFrontHeelNewToeSame,
		BracketInvertFrontHeelSameToeNew,
		BracketInvertBehindHeelNewToeNew,
		BracketInvertBehindHeelNewToeSame,
		BracketInvertBehindHeelSameToeNew,

		// Stretch brackets.
		BracketStretchHeelNewToeNew,
		BracketStretchHeelNewToeSame,
		BracketStretchHeelSameToeNew,

		// Single arrow brackets.
		BracketOneArrowHeelSame,
		BracketOneArrowHeelNew,
		BracketOneArrowHeelSwap,
		BracketOneArrowToeSame,
		BracketOneArrowToeNew,
		BracketOneArrowToeSwap,

		// Single arrow crossover brackets.
		BracketCrossoverFrontOneArrowHeelNew,
		BracketCrossoverFrontOneArrowToeNew,
		BracketCrossoverBehindOneArrowHeelNew,
		BracketCrossoverBehindOneArrowToeNew,

		// Single arrow invert brackets.
		BracketInvertFrontOneArrowHeelNew,
		BracketInvertFrontOneArrowToeNew,
		BracketInvertBehindOneArrowHeelNew,
		BracketInvertBehindOneArrowToeNew,

		// Single arrow stretch brackets.
		BracketStretchOneArrowHeelNew,
		BracketStretchOneArrowToeNew,

		// This enum is serialized as an integer in StepGraph.
		// When changing this enum update serialization logic accordingly.
	}

	/// <summary>
	/// Actions that can be performed when stepping with a foot.
	/// Releases can only occur on StepTypes that keep the foot on the same arrow.
	/// Not considering rolls as a unique action.
	/// </summary>
	public enum FootAction
	{
		Tap,
		Hold,
		Release
		// This enum is serialized as an integer in StepGraph.
		// When changing this enum update serialization logic accordingly.
	}

	/// <summary>
	/// The orientation the body can be in based on certain steps.
	/// InvertFront and InvertBack will result in inverted orientations.
	/// </summary>
	public enum BodyOrientation
	{
		Normal,
		InvertedRightOverLeft,
		InvertedLeftOverRight
		// This enum is serialized as an integer in StepGraph.
		// When changing this enum update serialization logic accordingly.
	}

	/// <summary>
	/// The direction the body is facing relative to the center of the pads.
	/// </summary>
	public enum Facing
	{
		Normal,
		Inward,
		Outward,
	}

	/// <summary>
	/// The state a foot on an arrow in StepGraph can be in.
	/// Each foot is on one or more arrows each in one of these states.
	/// Lifted only occurs when a foot swaps and forces the other foot to release.
	/// Rolls are considered no different than holds in the StepGraph.
	/// </summary>
	public enum GraphArrowState
	{
		Resting,
		Held,
		Lifted,
		// This enum is serialized as an integer in StepGraph.
		// When changing this enum update serialization logic accordingly.
	}

	/// <summary>
	/// Data for each StepType.
	/// </summary>
	public class StepData
	{
		/// <summary>
		/// Whether or not this StepType can be used in a jump.
		/// </summary>
		public readonly bool CanBeUsedInJump;

		/// <summary>
		/// Whether or not this StepType can be used with a Release FootAction.
		/// </summary>
		public readonly bool CanBeUsedInRelease;

		/// <summary>
		/// Whether or not this StepType involves a foot swap.
		/// Index is the FootPortion.
		/// </summary>
		public readonly bool[] IsFootSwap;

		/// <summary>
		/// Whether any portion of this StepType the foot involves a foot swap.
		/// </summary>
		public readonly bool IsFootSwapWithAnyPortion;

		/// <summary>
		/// Whether or not this StepType is a bracket on more than one arrow with one foot.
		/// </summary>
		public readonly bool IsBracket;

		/// <summary>
		/// Whether or not this StepType is a bracket on a single panel.
		/// </summary>
		public readonly bool IsOneArrowBracket;

		/// <summary>
		/// The foot portion used for this StepType if it is a single step.
		/// </summary>
		public readonly int SingleStepFootPortion;

		/// <summary>
		/// Whether or not this StepType involves Swing.
		/// </summary>
		public readonly bool IsSwing;

		/// <summary>
		/// Whether or not this StepType is a crossover. Swing steps are not considered crossovers.
		/// </summary>
		public readonly bool IsCrossover;

		/// <summary>
		/// Whether or not this StepType is a invert. Swing steps are not considered inverts.
		/// </summary>
		public readonly bool IsInvert;

		/// <summary>
		/// The number of possible new arrows for for this StepType. For some types like FootSwap the
		/// number could be 0 or non-zero.
		/// </summary>
		public readonly int NumPossibleNewArrows;

		/// <summary>
		/// Private Constructor
		/// </summary>
		private StepData(
			bool canBeUsedInJump,
			bool canBeUsedInRelease,
			bool[] isFootSwap,
			bool isBracket,
			bool isOneArrowBracket,
			int singleStepFootPortion,
			bool isCrossover,
			bool isInvert,
			bool isSwing,
			int numPossibleNewArrows)
		{
			CanBeUsedInJump = canBeUsedInJump;
			CanBeUsedInRelease = canBeUsedInRelease;
			IsFootSwap = isFootSwap;
			IsBracket = isBracket;
			foreach (var swap in isFootSwap)
				IsFootSwapWithAnyPortion |= swap;
			IsOneArrowBracket = isOneArrowBracket;
			SingleStepFootPortion = singleStepFootPortion;
			IsCrossover = isCrossover;
			IsInvert = isInvert;
			IsSwing = isSwing;
			NumPossibleNewArrows = numPossibleNewArrows;
		}

		/// <summary>
		/// Static cached StepData for each StepType.
		/// </summary>
		public static readonly StepData[] Steps;

		/// <summary>
		/// Static cached mapping of FootAction to the GraphArrowState that results from
		/// performing that action;
		/// </summary>
		public static readonly GraphArrowState[] StateAfterAction;

		/// <summary>
		/// Static initializer.
		/// </summary>
		static StepData()
		{
			// Initialize StateAfterAction.
			var footActions = Enum.GetValues(typeof(FootAction)).Cast<FootAction>().ToList();
			StateAfterAction = new GraphArrowState[footActions.Count];
			StateAfterAction[(int) FootAction.Tap] = GraphArrowState.Resting;
			StateAfterAction[(int) FootAction.Hold] = GraphArrowState.Held;
			StateAfterAction[(int) FootAction.Release] = GraphArrowState.Resting;

			var steps = Enum.GetValues(typeof(StepType)).Cast<StepType>().ToList();

			// Set up swap data arrays for legibility below.
			var noFootSwap = new bool[NumFootPortions];
			var defaultFootSwap = new bool[NumFootPortions];
			var heelFootSwap = new bool[NumFootPortions];
			var toeFootSwap = new bool[NumFootPortions];
			var allFootSwap = new bool[NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				noFootSwap[p] = false;
				defaultFootSwap[p] = p == DefaultFootPortion;
				heelFootSwap[p] = p == Heel;
				toeFootSwap[p] = p == Toe;
				allFootSwap[p] = true;
			}

			// Configure the Steps.
			Steps = new StepData[steps.Count];

			// Simple step types.
			Steps[(int) StepType.SameArrow] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 0
			);
			Steps[(int) StepType.NewArrow] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.CrossoverFront] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.CrossoverBehind] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.InvertFront] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.InvertBehind] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.FootSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.Swing] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: false,
				isSwing: true,
				numPossibleNewArrows: 1
			);

			// Simple stretch types.
			Steps[(int)StepType.NewArrowStretch] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.CrossoverFrontStretch] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.CrossoverBehindStretch] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.InvertFrontStretch] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.InvertBehindStretch] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Swapping into crossover or inverted orientations due to brackets.
			Steps[(int)StepType.FootSwapCrossoverFront] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.FootSwapCrossoverBehind] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.FootSwapInvertFront] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.FootSwapInvertBehind] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				isOneArrowBracket: false,
				singleStepFootPortion: DefaultFootPortion,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Brackets.
			Steps[(int) StepType.BracketHeelNewToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int) StepType.BracketHeelNewToeSame] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.BracketHeelSameToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.BracketHeelSameToeSame] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: true,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 0
			);
			Steps[(int) StepType.BracketHeelSameToeSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: toeFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.BracketHeelNewToeSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: toeFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int) StepType.BracketHeelSwapToeSame] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: heelFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.BracketHeelSwapToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: heelFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketHeelSwapToeSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: allFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketSwing] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: true,
				numPossibleNewArrows: 2
			);

			// Crossover brackets.
			Steps[(int)StepType.BracketCrossoverFrontHeelNewToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketCrossoverFrontHeelNewToeSame] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverFrontHeelSameToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverBehindHeelNewToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketCrossoverBehindHeelNewToeSame] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverBehindHeelSameToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Invert brackets.
			Steps[(int)StepType.BracketInvertFrontHeelNewToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketInvertFrontHeelNewToeSame] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertFrontHeelSameToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertBehindHeelNewToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketInvertBehindHeelNewToeSame] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertBehindHeelSameToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Stretch brackets.
			Steps[(int)StepType.BracketStretchHeelNewToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 2
			);
			Steps[(int)StepType.BracketStretchHeelNewToeSame] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketStretchHeelSameToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: true,
				isOneArrowBracket: false,
				singleStepFootPortion: InvalidArrowIndex,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Single arrow brackets.
			Steps[(int) StepType.BracketOneArrowHeelSame] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 0
			);
			Steps[(int) StepType.BracketOneArrowHeelNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketOneArrowHeelSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: heelFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int) StepType.BracketOneArrowToeSame] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 0
			);
			Steps[(int) StepType.BracketOneArrowToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketOneArrowToeSwap] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: toeFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Single arrow crossover brackets.
			Steps[(int)StepType.BracketCrossoverFrontOneArrowHeelNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverFrontOneArrowToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverBehindOneArrowHeelNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketCrossoverBehindOneArrowToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: true,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Single arrow invert brackets.
			Steps[(int)StepType.BracketInvertFrontOneArrowHeelNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertFrontOneArrowToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertBehindOneArrowHeelNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketInvertBehindOneArrowToeNew] = new StepData(
				canBeUsedInJump: false,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: true,
				isSwing: false,
				numPossibleNewArrows: 1
			);

			// Single arrow stretch brackets.
			Steps[(int)StepType.BracketStretchOneArrowHeelNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Heel,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
			Steps[(int)StepType.BracketStretchOneArrowToeNew] = new StepData(
				canBeUsedInJump: true,
				canBeUsedInRelease: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				isOneArrowBracket: true,
				singleStepFootPortion: Toe,
				isCrossover: false,
				isInvert: false,
				isSwing: false,
				numPossibleNewArrows: 1
			);
		}
	}
}
