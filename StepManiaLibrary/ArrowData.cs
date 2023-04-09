using static StepManiaLibrary.Constants;
using System.Text.Json.Serialization;

namespace StepManiaLibrary
{
	/// <summary>
	/// Information about an arrow in an array of arrows representing the layout
	/// of one or more pads. This data informs how the other arrows are associated with
	/// this arrow. For example, per arrow, it is useful to know which other arrows
	/// are bracketable with it, are steppable to from it, form crossovers with it, etc.
	/// Deserialized from json.
	/// </summary>
	public class ArrowData
	{
		/// <summary>
		/// The lane / index of this arrow.
		/// Set after deserialization based on index of this ArrowData in the containing array.
		/// </summary>
		[JsonIgnore] public int Lane = InvalidArrowIndex;
		/// <summary>
		/// The lane / index of this arrow when all arrows are mirrored.
		/// Set after deserialization.
		/// </summary>
		[JsonIgnore] public int MirroredLane = InvalidArrowIndex;
		/// <summary>
		/// The lane / index of this arrow when all arrows are flipped.
		/// Set after deserialization.
		/// </summary>
		[JsonIgnore] public int FlippedLane = InvalidArrowIndex;

		/// <summary>
		/// X position of the center of this arrow on the pads.
		/// </summary>
		[JsonInclude] public int X;

		/// <summary>
		/// Y position of the center of this arrow on the pads.
		/// </summary>
		[JsonInclude] public int Y;

		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// toes are on this arrow and the heel is on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] BracketablePairingsOtherHeel = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// heel is on this arrow and the toes are on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] BracketablePairingsOtherToe = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are valid pairings for the other foot.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot without crossing over or stretching.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairings = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are valid pairings for the other foot with stretching.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot without crossing over, but with stretching.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsStretch = new bool[NumFeet][];

		/// <summary>
		/// Which arrows form a front crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in front.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsOtherFootCrossoverFront = new bool[NumFeet][];

		/// <summary>
		/// Which arrows form a back crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in back.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsOtherFootCrossoverBehind = new bool[NumFeet][];

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
		[JsonInclude] public bool[][] OtherFootPairingsInverted = new bool[NumFeet][];
	}
}
