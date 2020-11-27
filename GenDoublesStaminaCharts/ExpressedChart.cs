using System.Collections.Generic;
using Fumen;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	/// <summary>
	/// An ExpressedChart is a series of events which describe the intent of a chart.
	/// Instead of specifying the specific arrows or mines in a chart is specifies
	/// the types of steps and mines that make it up.
	/// For example, instead of events like tap on P1L, tap on P1D, tap on P1R an
	/// ExpressedChart would represent that as a step with the left foot on the same arrow,
	/// a step with the right foot on a different arrow, and a crossover in front with
	/// the left foot to a new arrow.
	/// An ExpressedChart's representation comes from GraphLinks, which specify FootActions
	/// and SingleStepTypes for each foot.
	/// Creating an ExpressedChart allows for converting the chart from one set of arrows
	/// like 4-panel to a different set like 8-panel. An equivalent 4-panel and 8-panel
	/// chart would share the same ExpressedChart, though their specific PerformedCharts
	/// would be different.
	/// Given a graph of StepNodes for set of arrows and an ExpressedChart, a PerformedChart
	/// can be generated.
	/// </summary>
	public class ExpressedChart
	{
		/// <summary>
		/// All the StepEvents which make up this chart.
		/// The first StepEvent is the GraphLink from the natural starting position to the
		/// first step in the chart. For example in singles the player will have a natural
		/// starting position of P1L, P1R. If the first arrow in the chart is P1D, then the
		/// first StepEvent will a be GraphLink with a Link for one foot with a NewArrow
		/// SingleStepType and a Tap FootAction.
		/// </summary>
		public List<StepEvent> StepEvents = new List<StepEvent>();
		public List<MineEvent> MineEvents = new List<MineEvent>();
	}

	/// <summary>
	/// Common data to both StepEvents and MineEvents.
	/// </summary>
	public class ExpressedChartEvent
	{
		public MetricPosition Position;
	}

	/// <summary>
	/// Event representing all the steps occurring at a single Metric position in the chart.
	/// </summary>
	public class StepEvent : ExpressedChartEvent
	{
		/// <summary>
		/// GraphLink representing the all steps occurring at a single Metric position.
		/// This GraphLink is the Link to this Event as opposed to the link from this Event.
		/// </summary>
		public GraphLink Link;
	}

	/// <summary>
	/// Enumeration of was to express a MineEvent.
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

	/// <summary>
	/// Event representing a single mine.
	/// </summary>
	public class MineEvent : ExpressedChartEvent
	{
		/// <summary>
		/// The MineType to use for expressing this mine.
		/// </summary>
		public MineType Type = MineType.NoArrow;

		/// <summary>
		/// When expressing this mine as relative to a specific arrow, we want to know
		/// how close the arrow was to the mine relative to other arrows. For example it
		/// is meaningful that a mine follows the most recent arrow because that typically
		/// indicates a double step or a foot swap, while it means something else if it
		/// follows the least recently used arrow.
		/// </summary>
		public int ArrowIsNthClosest = InvalidArrowIndex;

		/// <summary>
		/// The foot associated with the arrow that is paired with this mine. This is
		/// useful when a mine follows one arrow of a jump to indicate footing.
		/// </summary>
		public int FootAssociatedWithPairedNote = InvalidFoot;
	}
}
