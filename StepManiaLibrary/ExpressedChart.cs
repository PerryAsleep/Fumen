using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using static StepManiaLibrary.Constants;
using static StepManiaLibrary.ExpressedChartCosts;

namespace StepManiaLibrary
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
	/// and StepTypes for each foot.
	/// Creating an ExpressedChart allows for converting the chart from one set of arrows
	/// like 4-panel to a different set like 8-panel. An equivalent 4-panel and 8-panel
	/// chart would share the same ExpressedChart, though their specific PerformedCharts
	/// would be different.
	/// Given a graph of StepNodes for set of arrows and an ExpressedChart, a PerformedChart
	/// can be generated.
	/// TODO: Consider consolidating search logic with PerformedChart?
	/// </summary>
	public class ExpressedChart
	{
		private const string LogTag = "Expressed Chart";

		/// <summary>
		/// Enumeration of states that each arrow can be in at each position.
		/// Differentiates states that last only for the duration of one position
		/// (Tap, Hold, and Roll) and states which last for a duration of time
		/// (Empty, Holding, Rolling). Useful to track the current state when searching
		/// and making decisions about weights.
		/// </summary>
		private enum SearchState
		{
			Empty,
			Tap,
			Fake,
			Lift,
			Hold,
			Holding,
			Roll,
			Rolling,
		}

		/// <summary>
		/// Common data to the events which make up an ExpressedChart.
		/// Abstract base class.
		/// </summary>
		public abstract class ChartEvent
		{
			/// <summary>
			/// IntegerPosition of this event.
			/// </summary>
			public int Position;

			/// <summary>
			/// Time in seconds of this event.
			/// </summary>
			public double Time;

			protected ChartEvent(int position, double time)
			{
				Position = position;
				Time = time;
			}
		}

		/// <summary>
		/// Event representing all the steps occurring at a single position in the chart.
		/// </summary>
		public class StepEvent : ChartEvent
		{
			public StepEvent(int position, double time, GraphLinkInstance linkInstance)
				: base(position, time)
			{
				LinkInstance = linkInstance;
			}

			/// <summary>
			/// GraphLinkInstance representing the all steps occurring at a single position.
			/// This GraphLink is the Link to this Event as opposed to the link from this Event.
			/// This represents how the player got to this position.
			/// </summary>
			public GraphLinkInstance LinkInstance;
		}

		/// <summary>
		/// Event representing a single mine.
		/// </summary>
		public class MineEvent : ChartEvent
		{
			public MineEvent(int position, double time, int arrow)
				: base(position, time)
			{
				OriginalArrow = arrow;
			}

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

			/// <summary>
			/// The arrow that this mine originally occupied in the source chart. This is
			/// only needed for illustrating the ExpressedChart.
			/// </summary>
			public int OriginalArrow;
		}

		/// <summary>
		/// Node for searching through an SM Chart and finding the best path for an
		/// ExpressedChart. When searching each ChartSearchNode has at most one previous
		/// ChartSearchNode and potentially many next ChartSearchNodes, one for each valid
		/// GraphNode reachable from each valid GraphLink out of this node that match
		/// the SM Chart at the corresponding position.
		/// When the search is complete each ChartSearchNode will have at most one previous
		/// ChartSearchNode and at most one next ChartSearchNode.
		/// Each ChartSearchNode has a unique Id even if it represents the same GraphNode
		/// and the position, so that all nodes can be stored and compared without
		/// conflicting.
		/// </summary>
		[DebuggerDisplay("{ToString()}")]
		public class ChartSearchNode : IEquatable<ChartSearchNode>, MineUtils.IChartNode
		{
			private static long IdCounter;

			/// <summary>
			/// Unique identifier for preventing conflicts when storing ChartSearchNodes in
			/// HashSets or other data structures that rely on the IEquatable interface.
			/// </summary>
			private readonly long Id;

			/// <summary>
			/// Corresponding GraphNode.
			/// </summary>
			public readonly GraphNode GraphNode;

			/// <summary>
			/// InstanceStepTypes for each arrow at this node.
			/// </summary>
			public readonly InstanceStepType[] InstanceTypes;

			/// <summary>
			/// IntegerPosition in the Chart of this ChartSearchNode.
			/// </summary>
			public readonly int Position;

			/// <summary>
			/// Time in microseconds in the Chart of this ChartSearchNode.
			/// </summary>
			public readonly double TimeSeconds;

			/// <summary>
			/// Cumulative Cost to reach this ChartSearchNode.
			/// </summary>
			public int TotalCost;

			/// <summary>
			/// Cost to reach this ChartSearchNode from the previous node.
			/// </summary>
			public int Cost;

			/// <summary>
			/// For tie breaking, a cost associated with orientations. If two interpretations are equally
			/// plausible, we should prefer interpretations that minimize twisting.
			/// </summary>
			public int TotalOrientationCost;

			/// <summary>
			/// Previous ChartSearchNode.
			/// </summary>
			public readonly ChartSearchNode PreviousNode;

			/// <summary>
			/// The GraphLink from the previous ChartSearchNode that results in this ChartSearchNode.
			/// </summary>
			public readonly GraphLinkInstance PreviousLink;

			/// <summary>
			/// All possible next ChartSearchNodes.
			/// Key is the GraphLink leading out of the GraphNode.
			/// Value is set of all ChartSearchNodes possible from that GraphLink.
			/// </summary>
			public readonly Dictionary<GraphLink, HashSet<ChartSearchNode>> NextNodes =
				new Dictionary<GraphLink, HashSet<ChartSearchNode>>();

			/// <summary>
			/// For each arrow/lane, the last foot which stepped on it. Updated with this
			/// ChartSearchNode's state during construction.
			/// </summary>
			public int[] LastFootUsedToStepOnArrow;

			public ChartSearchNode(
				GraphNode graphNode,
				int position,
				double timeSeconds,
				ChartSearchNode previousNode,
				GraphLinkInstance previousLink,
				InstanceStepType[] instanceTypes)
			{
				Id = Interlocked.Increment(ref IdCounter);
				GraphNode = graphNode;
				Position = position;
				TimeSeconds = timeSeconds;
				PreviousNode = previousNode;
				PreviousLink = previousLink;
				InstanceTypes = instanceTypes;

				// Copy the LastFootUsedToStepOnArrow data from the previous node and update
				// lanes based on this state.
				LastFootUsedToStepOnArrow = new int[InstanceTypes.Length];
				for (var a = 0; a < LastFootUsedToStepOnArrow.Length; a++)
					LastFootUsedToStepOnArrow[a] = previousNode?.LastFootUsedToStepOnArrow[a] ?? InvalidFoot;
				if (PreviousLink != null)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (PreviousLink.GraphLink.Links[f, p].Valid
							    && (PreviousLink.GraphLink.Links[f, p].Action == FootAction.Tap
							        || PreviousLink.GraphLink.Links[f, p].Action == FootAction.Hold))
							{
								LastFootUsedToStepOnArrow[GraphNode.State[f, p].Arrow] = f;
							}
						}
					}
				}
			}

			/// <summary>
			/// Sets the cost of this ChartSearchNode.
			/// Will update the Cost and, TotalCost, and TotalOrientationCost.
			/// </summary>
			/// <param name="cost">Cost of this ChartSearchNode.</param>
			/// <param name="orientationCost">Orientation cost of this ChartSearchNode.</param>
			public void SetCost(int cost, int orientationCost)
			{
				Cost = cost;
				TotalCost = (PreviousNode?.TotalCost ?? 0) + Cost;
				TotalOrientationCost = (PreviousNode?.TotalOrientationCost ?? 0) + orientationCost;
			}

			/// <summary>
			/// Gets the next ChartSearchNode.
			/// Assumes that the search is complete and there is at most one next ChartSearchNode.
			/// </summary>
			/// <returns>The next ChartSearchNode or null if none exists.</returns>
			public ChartSearchNode GetNextNode()
			{
				if (NextNodes.Count == 0 || NextNodes.First().Value.Count == 0)
					return null;
				return NextNodes.First().Value.First();
			}

			/// <summary>
			/// Gets the preceding GraphLink that was a step.
			/// Will recurse until one is found or the root ChartSearchNode has been reached.
			/// Skips releases.
			/// </summary>
			/// <returns>The preceding step GraphLink or null if none exists.</returns>
			public GraphLinkInstance GetPreviousStepLink(int nthPrevious = 1)
			{
				var node = this;
				while (node.PreviousNode != null)
				{
					var linkToNode = node.PreviousLink;
					if (!linkToNode.GraphLink.IsRelease())
					{
						nthPrevious--;
						if (nthPrevious <= 0)
						{
							return linkToNode;
						}
					}

					node = node.PreviousNode;
				}

				return null;
			}

			/// <summary>
			/// Gets the preceding ChartSearchNode that was a step.
			/// Will recurse until one is found or the root ChartSearchNode has been reached.
			/// Skips releases.
			/// </summary>
			/// <returns>The preceding step ChartSearchNode or null if none exists.</returns>
			public ChartSearchNode GetPreviousStepSearchNode(int nthPrevious = 1)
			{
				var node = PreviousNode;
				while (node != null && node.PreviousNode != null)
				{
					var linkToNode = node.PreviousLink;
					if (!linkToNode.GraphLink.IsRelease())
					{
						nthPrevious--;
						if (nthPrevious <= 0)
						{
							return node;
						}
					}

					node = node.PreviousNode;
				}

				return null;
			}

			public override string ToString()
			{
				const int positionLen = 5;
				const int totalCostLen = 5;
				const int linkLen = 25;
				const int nodeLen = 25;
				const int costLen = 5;
				const int dressingLen = 15;

				var sb = new StringBuilder(
					positionLen
					+ totalCostLen
					+ costLen
					+ linkLen
					+ nodeLen
					+ dressingLen);

				// Position.
				sb.Append(Position.ToString("D5"));
				sb.Append(" ");

				// Total cost.
				sb.Append("(");
				sb.Append(TotalCost.ToString("D5"));
				sb.Append(") ");

				// Link to this node.
				sb.Append("[");
				var linkStringLen = 0;
				if (PreviousLink != null)
				{
					var linkString = PreviousLink.ToString();
					sb.Append(linkString);
					linkStringLen = linkString.Length;
				}

				for (var i = linkStringLen; i < linkLen; i++)
				{
					sb.Append(" ");
				}

				sb.Append("] -> [");

				// Node.
				var graphNodeString = GraphNode.ToString();
				sb.Append(graphNodeString);
				for (var i = graphNodeString.Length; i < nodeLen; i++)
				{
					sb.Append(" ");
				}

				sb.Append("]");

				// Link cost.
				sb.Append(" (");
				sb.Append(Cost.ToString("D5"));
				sb.Append(")");

				return sb.ToString();
			}

			public virtual int CompareTo(ChartSearchNode other, bool test = false)
			{
				if (Equals(other))
					return 0;

				// Compare the total cost. Almost all comparisons should have different total costs.
				if (TotalCost != other.TotalCost)
					return TotalCost.CompareTo(other.TotalCost);

				// At this point the interpretations are considered equally valid by cost.
				// We need to tie break for a deterministic result.

				// Tie breaking edge case.
				// Consider the total orientation cost.
				if (TotalOrientationCost != other.TotalOrientationCost)
					return TotalOrientationCost.CompareTo(other.TotalOrientationCost);

				// Tie breaking edge case.
				// If two interpretations are equal by cost, prefer patterns which have a lower cost sooner.
				// As an example, if an interpretation is only equal to another 10 measures from now, but right
				// now there is a slight preference to make one move over another, it is likely you are supposed
				// to interpret it that way and do the more natural pattern first, rather than something more
				// awkward now in hopes it will even out later.
				var thisNode = this;
				var otherNode = other;
				var lastBestNonZeroCostComparison = 0;
				// Iterate backwards until we find the common ancestor.
				while (thisNode != null && otherNode != null && !thisNode.Equals(otherNode))
				{
					if (thisNode.Cost != otherNode.Cost)
						lastBestNonZeroCostComparison = thisNode.Cost.CompareTo(otherNode.Cost);
					thisNode = thisNode.PreviousNode;
					otherNode = otherNode.PreviousNode;
				}

				if (lastBestNonZeroCostComparison != 0)
					return lastBestNonZeroCostComparison;

				// Tie breaking edge case
				// Compare links. This is only for extreme edge cases when all costs are equal.
				thisNode = this;
				otherNode = other;
				while (thisNode?.PreviousLink != null && otherNode?.PreviousLink != null)
				{
					var thisLink = thisNode.PreviousLink.GraphLink.Links;
					var otherLink = otherNode.PreviousLink.GraphLink.Links;
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (thisLink[f, p].Valid != otherLink[f, p].Valid)
								return thisLink[f, p].Valid ? -1 : 1;
							if (!thisLink[f, p].Valid)
								continue;
							if (thisLink[f, p].Step != otherLink[f, p].Step)
								return thisLink[f, p].Step.CompareTo(otherLink[f, p].Step);
						}
					}

					thisNode = thisNode.PreviousNode;
					otherNode = otherNode.PreviousNode;
				}

				// This is impossible.
				return 0;
			}

			#region IEquatable Implementation

			public override bool Equals(object obj)
			{
				if (obj is ChartSearchNode n)
					return Equals(n);
				return false;
			}

			public bool Equals(ChartSearchNode other)
			{
				if (other == null)
					return false;
				return Id == other.Id;
			}

			public override int GetHashCode()
			{
				return (int)Id;
			}

			#endregion

			#region MineUtils.IChartNode Implementation

			public GraphNode GetGraphNode()
			{
				return GraphNode;
			}

			public GraphLink GetGraphLinkToNode()
			{
				return PreviousLink?.GraphLink;
			}

			public int GetPosition()
			{
				return Position;
			}

			#endregion
		}

		/// <summary>
		/// All the StepEvents which make up this chart.
		/// The first StepEvent is the GraphLink from the natural starting position to the
		/// first step in the chart. For example in singles the player will have a natural
		/// starting position of P1L, P1R. If the first arrow in the chart is P1D, then the
		/// first StepEvent will a be GraphLink with a Link for one foot with a NewArrow
		/// StepType and a Tap FootAction.
		/// </summary>
		public List<StepEvent> StepEvents = new List<StepEvent>();

		/// <summary>
		/// All the MineEvents which make up this chart.
		/// </summary>
		public List<MineEvent> MineEvents = new List<MineEvent>();

		/// <summary>
		/// How to parse brackets when encountering steps which could be brackets or jumps.
		/// </summary>
		private BracketParsingMethod BracketParsingMethod;

		/// <summary>
		/// Identifier to use in log messages.
		/// </summary>
		private readonly string LogIdentifier;

		/// <summary>
		/// StepGraph to use for parsing the Chart's Events.
		/// </summary>
		private readonly StepGraph StepGraph;

		/// <summary>
		/// Root ChartSearchNode of the ExpressedChart.
		/// </summary>
		private ChartSearchNode Root;

		/// <summary>
		/// Cached Events from the Chart to express.
		/// Unneeded and set to null after search is complete.
		/// </summary>
		private List<Event> Events;

		/// <summary>
		/// Cached LaneNotes representing Mines from the Chart's Events.
		/// Unneeded and set to null after search is complete.
		/// </summary>
		private List<LaneNote> MineNotes;

		public ChartSearchNode GetRootSearchNode()
		{
			return Root;
		}

		public BracketParsingMethod GetBracketParsingMethod()
		{
			return BracketParsingMethod;
		}

		/// <summary>
		/// Custom Comparer for MineEvents so the ExpressedChart uses a consistent order.
		/// </summary>
		public class MineEventComparer : IComparer<MineEvent>
		{
			int IComparer<MineEvent>.Compare(MineEvent e1, MineEvent e2)
			{
				if (null == e1 && null == e2)
					return 0;
				if (null == e1)
					return -1;
				if (null == e2)
					return 1;

				// Order by position
				var comparison = e1.Position.CompareTo(e2.Position);
				if (comparison != 0)
					return comparison;

				// Order by type
				comparison = e1.Type - e2.Type;
				if (comparison != 0)
					return comparison;

				// Order by n
				comparison = e1.ArrowIsNthClosest - e2.ArrowIsNthClosest;
				if (comparison != 0)
					return comparison;

				// Order by foot
				comparison = e1.FootAssociatedWithPairedNote - e2.FootAssociatedWithPairedNote;
				return comparison;
			}
		}

		/// <summary>
		/// Private Constructor.
		/// </summary>
		/// <param name="events">List of Events from the Chart to express.</param>
		/// <param name="logIdentifier">Log identifier string to use when logging.</param>
		/// <param name="stepGraph">StepGraph for the Events</param>
		private ExpressedChart(List<Event> events, string logIdentifier, StepGraph stepGraph)
		{
			Events = events;
			LogIdentifier = logIdentifier;
			StepGraph = stepGraph;
		}

		/// <summary>
		/// Creates an ExpressedChart by iteratively searching through the List of given Events
		/// that correspond to an SM Chart. Multiple ExpressedCharts may represent the same SM
		/// Chart. This method tries to generate the ExpressedChart that best matches the intent
		/// of the original chart by exploring all possible paths through the given StepGraph that
		/// result in the arrows of the original chart and pruning paths that result in a shared
		/// state and have a high "cost". Cost is a measure of how unlikely it is to perform a
		/// certain type of action compared to others. For example, if a series of arrows can
		/// be performed by alternating or by double stepping, this method will choose the
		/// alternating expression as that has a lower cost and is more natural. See GetCost for
		/// more details on how cost is determined.
		/// </summary>
		/// <param name="events">List of Events from the Chart to express.</param>
		/// <param name="stepGraph">StepGraph to use for searching the Events.</param>
		/// <param name="config">ExpressedChartConfig to use for generating the ExpressedChart.</param>
		/// <param name="difficultyRating">
		/// Difficulty rating of the Chart for the events. Used in some cases when the
		/// ExpressedChartConfig specifies to use difficulty for bracket parsing.
		/// </param>
		/// <param name="logIdentifier">
		/// Identifier to use when logging messages about this ExpressedChart.
		/// </param>
		/// <returns>
		/// The generated ExpressedChart or null if no ExpressedChart could be found.
		/// </returns>
		public static ExpressedChart CreateFromSMEvents(
			List<Event> events,
			StepGraph stepGraph,
			ExpressedChartConfig config,
			double difficultyRating,
			string logIdentifier = null)
		{
			// Set up a new ExpressedChart.
			var expressedChart = new ExpressedChart(events, logIdentifier, stepGraph);

			// Log Lift and Fake information.
			expressedChart.LogLiftAndFakeInformation();

			// Determine the BracketParsingMethod to use.
			var bracketParsingMethod = BracketParsingMethod.Balanced;
			var needToSearch = true;
			switch (config.BracketParsingDetermination)
			{
				// When configured to use the default method, simply use that.
				case BracketParsingDetermination.UseDefaultMethod:
				{
					bracketParsingMethod = config.DefaultBracketParsingMethod;
					break;
				}

				// When configured to determine the method dynamically, perform a series of checks.
				case BracketParsingDetermination.ChooseMethodDynamically:
				{
					// If this Chart's DifficultyRating is under the minimum level for brackets, then
					// use BracketParsingMethod NoBrackets.
					if (difficultyRating < config.MinLevelForBrackets)
					{
						bracketParsingMethod = BracketParsingMethod.NoBrackets;
					}
					// If configured to use Aggressive parsing when the Chart has more simultaneous
					// arrows than one foot can cover, scan the chart for simultaneous notes and use
					// BracketParsingMethod Aggressive if appropriate.
					else if (config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets
					         && expressedChart.HasMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets())
					{
						bracketParsingMethod = BracketParsingMethod.Aggressive;
					}
					// Otherwise, check for balanced brackets per minute.
					else if (config.BalancedBracketsPerMinuteForAggressiveBrackets > 0.0)
					{
						needToSearch = false;

						// Parse the chart with balanced bracket parsing so we can assess
						// what kind of chart it is and potentially adjust the bracket parsing
						// method.
						expressedChart.BracketParsingMethod = BracketParsingMethod.Balanced;
						if (!expressedChart.Search())
						{
							return null;
						}

						// Determine brackets per minute
						var brackets = expressedChart.GetBracketCount();
						if (brackets > 0)
						{
							var startTime = expressedChart.Events.First().TimeSeconds;
							var endTime = expressedChart.Events.Last().TimeSeconds;
							var bracketsPerMinute = brackets / ((endTime - startTime) / 60.0);
							if (bracketsPerMinute >= config.BalancedBracketsPerMinuteForAggressiveBrackets)
							{
								bracketParsingMethod = BracketParsingMethod.Aggressive;
								needToSearch = true;
							}
							else if (bracketsPerMinute <= config.BalancedBracketsPerMinuteForNoBrackets)
							{
								bracketParsingMethod = BracketParsingMethod.NoBrackets;
								needToSearch = true;
							}
						}
					}

					break;
				}
			}

			// If we still need to search, search now.
			if (needToSearch)
			{
				expressedChart.BracketParsingMethod = bracketParsingMethod;
				if (!expressedChart.Search())
				{
					return null;
				}
			}

			// Now that the search is complete, add all the events to a new ExpressedChart.
			expressedChart.AddSteps();
			expressedChart.AddMines();

			// We no longer need to maintain references to the Chart data.
			expressedChart.Events = null;
			expressedChart.MineNotes = null;

			return expressedChart;
		}

		/// <summary>
		/// Searches through this ExpressedChart's Events for the best representation.
		/// If successful, Root will represent the root ChartSearchNode of the ExpressedChart.
		/// </summary>
		/// <returns>True if the search was successful and false otherwise.</returns>
		private bool Search()
		{
			Root = new ChartSearchNode(
				StepGraph.GetRoot(),
				0,
				0L,
				null,
				null,
				new InstanceStepType[StepGraph.NumArrows]);

			var numArrows = StepGraph.NumArrows;
			var currentState = new SearchState[numArrows];
			var lastMines = new int[numArrows];
			var lastReleases = new int[numArrows];
			for (var a = 0; a < numArrows; a++)
			{
				currentState[a] = SearchState.Empty;
				lastMines[a] = 0;
				lastReleases[a] = 0;
			}

			MineNotes = new List<LaneNote>();
			var eventIndex = 0;
			var numEvents = Events.Count;
			var currentSearchNodes = new HashSet<ChartSearchNode> { Root };

			// Performance optimization.
			// Keep one array for generating SearchStates to use for GetLinkInstanceIfStateMatches.
			// If this method gets refactored to construct the ExpressedChart earlier this should be
			// a member variable so we don't have to pass it around.
			var generatedStateBuffer = new SearchState[numArrows];

			while (true)
			{
				// Failed to find a path through the chart
				if (currentSearchNodes.Count == 0)
				{
					Root = null;
					return false;
				}

				// Reached the end.
				if (eventIndex >= numEvents)
				{
					// Choose path with lowest cost.
					ChartSearchNode bestNode = null;
					foreach (var node in currentSearchNodes)
						if (bestNode == null || node.CompareTo(bestNode) < 0)
							bestNode = node;

					// Remove any nodes that are not chosen so there is only one path through the chart.
					foreach (var node in currentSearchNodes)
					{
						if (node.Equals(bestNode))
							continue;
						Prune(node);
					}

					// Stop looping.
					break;
				}

				// Parse all the events at the next position.
				ParseNextEvents(ref eventIndex, out var releases, out var mines, out var steps, out var timeSeconds);

				// Process Releases.
				if (releases.Count > 0)
				{
					// Update state.
					for (var r = 0; r < releases.Count; r++)
					{
						var releaseEvent = releases[r];
						currentState[releaseEvent.Lane] = SearchState.Empty;
						lastReleases[releaseEvent.Lane] = releaseEvent.IntegerPosition;
					}

					// Add children and prune.
					currentSearchNodes = AddChildrenAndPrune(currentSearchNodes, currentState, generatedStateBuffer,
						releases[0].IntegerPosition, timeSeconds, lastMines, lastReleases, true);
				}

				// Get mines and record them for processing after the search is complete.
				if (mines.Count > 0)
				{
					MineNotes.AddRange(mines);
					for (var m = 0; m < mines.Count; m++)
						lastMines[mines[m].Lane] = mines[m].IntegerPosition;
				}

				// Get taps, holds, and rolls.
				if (steps.Count > 0)
				{
					// Update state.
					for (var s = 0; s < steps.Count; s++)
					{
						var stepEvent = steps[s];
						switch (stepEvent)
						{
							case LaneTapNote _ when stepEvent.SourceType ==
							                        SMCommon.NoteChars[(int)SMCommon.NoteType.Fake].ToString():
								currentState[stepEvent.Lane] = SearchState.Fake;
								break;
							case LaneTapNote _ when stepEvent.SourceType ==
							                        SMCommon.NoteChars[(int)SMCommon.NoteType.Lift].ToString():
								currentState[stepEvent.Lane] = SearchState.Lift;
								break;
							case LaneTapNote _:
								currentState[stepEvent.Lane] = SearchState.Tap;
								break;
							case LaneHoldStartNote lhsn
								when lhsn.SourceType == SMCommon.NoteChars[(int)SMCommon.NoteType.RollStart].ToString():
								currentState[stepEvent.Lane] = SearchState.Roll;
								break;
							case LaneHoldStartNote _:
								currentState[stepEvent.Lane] = SearchState.Hold;
								break;
						}
					}

					// Add children and prune.
					currentSearchNodes = AddChildrenAndPrune(currentSearchNodes, currentState, generatedStateBuffer,
						steps[0].IntegerPosition, timeSeconds, lastMines, lastReleases, false);
				}

				// Update the current state now that the events at this position have been processed.
				for (var a = 0; a < numArrows; a++)
				{
					if (currentState[a] == SearchState.Tap
					    || currentState[a] == SearchState.Fake
					    || currentState[a] == SearchState.Lift)
					{
						currentState[a] = SearchState.Empty;
						lastReleases[a] = steps[0].IntegerPosition;
					}
					else if (currentState[a] == SearchState.Hold)
					{
						currentState[a] = SearchState.Holding;
					}
					else if (currentState[a] == SearchState.Roll)
					{
						currentState[a] = SearchState.Rolling;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Logs information about if the Events for this ExpressedChart use Fakes or Lifts.
		/// </summary>
		private void LogLiftAndFakeInformation()
		{
			var hasLifts = false;
			var hasFakes = false;
			for (var i = 0; i < Events.Count; i++)
			{
				var smEvent = Events[i];
				if (smEvent is LaneTapNote ltn)
				{
					if (ltn.SourceType == SMCommon.NoteChars[(int)SMCommon.NoteType.Lift].ToString())
						hasLifts = true;
					else if (ltn.SourceType == SMCommon.NoteChars[(int)SMCommon.NoteType.Fake].ToString())
						hasFakes = true;
				}
			}

			if (hasLifts)
				LogWarn("Chart has lifts. These will be treated as taps for ExpressedChart generation.", LogIdentifier);
			if (hasFakes)
				LogInfo("Chart has fakes. These will be treated as taps for ExpressedChart generation.", LogIdentifier);
		}

		/// <summary>
		/// Determines whether the Events for this ExpressedChart include any portions
		/// where there are more arrows being stepped on than can be covered without using brackets.
		/// In other words, are there completely unambiguous brackets.
		/// O(n) time complexity.
		/// </summary>
		/// <returns>
		/// True if the Events for this ExpressedChart include portions with more
		/// simultaneous notes than can be covered without brackets and false otherwise.
		/// </returns>
		private bool HasMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets()
		{
			var heldLanes = new bool[StepGraph.NumArrows];
			for (var i = 0; i < Events.Count;)
			{
				var numTaps = 0;
				// Process each note at the same position.
				do
				{
					var smEvent = Events[i];
					switch (smEvent)
					{
						case LaneHoldStartNote lhsn:
							heldLanes[lhsn.Lane] = true;
							break;
						case LaneHoldEndNote lhen:
							heldLanes[lhen.Lane] = false;
							break;
						case LaneTapNote _:
							numTaps++;
							break;
					}

					i++;
				}
				// Continue looping if the next event is at the same position.
				while (i < Events.Count && Events[i].IntegerPosition == Events[i - 1].IntegerPosition);

				var numHeld = 0;
				for (var l = 0; l < StepGraph.NumArrows; l++)
				{
					if (heldLanes[l])
						numHeld++;
				}

				if (numTaps + numHeld > NumFeet)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the number of brackets in the ExpressedChart.
		/// Includes both single arrow and multiple arrow steps.
		/// O(n) time complexity.
		/// </summary>
		/// <returns>Number of brackets in the ExpressedChart.</returns>
		private int GetBracketCount()
		{
			var bracketCount = 0;
			var node = Root;
			while (node != null)
			{
				if (node.PreviousLink?.GraphLink?.InvolvesBracketOrSingleArrowBracket() ?? false)
					bracketCount++;
				node = node.GetNextNode();
			}

			return bracketCount;
		}

		/// <summary>
		/// Parses the next events from the given List of Events that occur at or after the given
		/// eventIndex and occur at the same position into releases, steps, and mines. Updates
		/// eventIndex based on which events were parsed.
		/// </summary>
		/// <param name="eventIndex">Current index into Events. Will be updated.</param>
		/// <param name="releases">List of LaneHoldEndNotes to hold all releases.</param>
		/// <param name="mines">List of LaneNotes to hold all mines.</param>
		/// <param name="steps">List of LaneNotes to hold all taps, holds, and rolls.</param>
		/// <param name="time">Time in seconds of the events.</param>
		private void ParseNextEvents(
			ref int eventIndex,
			out List<LaneHoldEndNote> releases,
			out List<LaneNote> mines,
			out List<LaneNote> steps,
			out double time)
		{
			releases = new List<LaneHoldEndNote>();
			mines = new List<LaneNote>();
			steps = new List<LaneNote>();
			time = 0.0;

			if (eventIndex >= Events.Count)
				return;

			var pos = Events[eventIndex].IntegerPosition;
			time = Events[eventIndex].TimeSeconds;
			while (eventIndex < Events.Count && Events[eventIndex].IntegerPosition == pos)
			{
				if (Events[eventIndex] is LaneHoldEndNote lhen)
					releases.Add(lhen);
				else if (Events[eventIndex] is LaneNote ln
				         && ln.SourceType == SMCommon.NoteChars[(int)SMCommon.NoteType.Mine].ToString())
					mines.Add(ln);
				else if (Events[eventIndex] is LaneHoldStartNote lhsn)
					steps.Add(lhsn);
				else if (Events[eventIndex] is LaneTapNote ltn)
					steps.Add(ltn);
				eventIndex++;
			}
		}

		/// <summary>
		/// Adds children to all current ChartSearchNode that satisfy the new state and are the lowest
		/// cost paths to their respective GraphNodes.
		/// </summary>
		/// <param name="currentSearchNodes">All lowest-cost current ChartSearchNodes from previous state.</param>
		/// <param name="currentState">Current state to search for paths into.</param>
		/// <param name="generatedStateBuffer">Buffer to hold State when comparing in GetLinkInstanceIfStateMatches.</param>
		/// <param name="position">IntegerPosition of the state.</param>
		/// <param name="timeSeconds">Time in seconds of the state.</param>
		/// <param name="lastMines">IntegerPosition of last mines per arrow. Needed for cost determination.</param>
		/// <param name="lastReleases">IntegerPosition of last releases per arrow. Needed for cost determination.</param>
		/// <param name="isRelease">Whether or not the current step is a release.</param>
		/// <returns>HashSet of all lowest cost ChartSearchNodes satisfying this state.</returns>
		private HashSet<ChartSearchNode> AddChildrenAndPrune(
			HashSet<ChartSearchNode> currentSearchNodes,
			SearchState[] currentState,
			SearchState[] generatedStateBuffer,
			int position,
			double timeSeconds,
			int[] lastMines,
			int[] lastReleases,
			bool isRelease)
		{
			var childSearchNodes = new HashSet<ChartSearchNode>();

			var instanceTypes = new InstanceStepType[StepGraph.NumArrows];
			var numNewActions = 0;
			for (var a = 0; a < StepGraph.NumArrows; a++)
			{
				var c = currentState[a];
				if (c == SearchState.Tap || c == SearchState.Hold || c == SearchState.Roll || c == SearchState.Fake ||
				    c == SearchState.Lift)
					numNewActions++;
				switch (c)
				{
					case SearchState.Roll:
					case SearchState.Rolling:
						instanceTypes[a] = InstanceStepType.Roll;
						break;
					case SearchState.Fake:
						instanceTypes[a] = InstanceStepType.Fake;
						break;
					case SearchState.Lift:
						instanceTypes[a] = InstanceStepType.Lift;
						break;
				}
			}

			var couldPossiblyBracket = numNewActions > 1;
			var couldPossiblyJump = numNewActions > 1;

			// Check every current ChartSearchNode.
			foreach (var searchNode in currentSearchNodes)
			{
				var deadEnd = true;

				// Check every GraphLink out of the ChartSearchNode's GraphNode.
				foreach (var l in searchNode.GraphNode.Links)
				{
					// Early out. Most steps are simple. Most StepTypes are complicated. Earlying out when we know
					// we won't jump or bracket saves us from looping over a large number of unnecessary bracket and
					// jump StepType combinations below. This is a huge performance optimization.
					if (!couldPossiblyJump && l.Key.IsJump())
						continue;
					if (!couldPossiblyBracket && l.Key.InvolvesBracket())
						continue;
					if (isRelease && !l.Key.IsRelease())
						continue;

					// Check every resulting child GraphNode.
					for (var childNodeIndex = 0; childNodeIndex < l.Value.Count; childNodeIndex++)
					{
						var childNode = l.Value[childNodeIndex];

						// Most children will not match the new state. Ignore them.
						var linkInstance = GetLinkInstanceIfStateMatches(currentState, generatedStateBuffer, childNode, l.Key);
						if (linkInstance == null)
							continue;

						var childSearchNode = new ChartSearchNode(
							childNode,
							position,
							timeSeconds,
							searchNode,
							linkInstance,
							instanceTypes);

						// This GraphLink and child GraphNode result in a matching state.
						// Determine the cost to go from this GraphLink to this GraphNode.
						var cost = GetCost(childSearchNode, currentState, lastMines, lastReleases, StepGraph.PadData);
						var orientationCost = GetOrientationCost(childSearchNode);
						childSearchNode.SetCost(cost, orientationCost);

						// Record the result as a new ChartSearchNode to be checked for pruning once
						// all children have been determined.

						AddChildSearchNode(searchNode, l.Key, childSearchNode, childSearchNodes);

						deadEnd = false;
					}
				}

				// If this node has no valid link out to the new state it should be pruned
				if (deadEnd)
				{
					Prune(searchNode);
				}
			}

			if (childSearchNodes.Count == 0)
			{
				LogError($"Failed to find node at {position}.", LogIdentifier);
			}

			// Prune the children and return the results.
			return Prune(childSearchNodes);
		}

		/// <summary>
		/// Checks if the given array of SearchStates and the given GraphNode represent the same
		/// state.
		/// </summary>
		/// <param name="searchState">Array of SearchStates. One SearchState per arrow.</param>
		/// <param name="generatedStateBuffer">Buffer to hold State when comparing.</param>
		/// <param name="node">GraphNode with state per foot.</param>
		/// <param name="link">GraphLink that linked to the given node.</param>
		/// <returns>True if the two representations match and false if they do not.</returns>
		private static GraphLinkInstance GetLinkInstanceIfStateMatches(
			SearchState[] searchState,
			SearchState[] generatedStateBuffer,
			GraphNode node,
			GraphLink link)
		{
			for (var s = 0; s < generatedStateBuffer.Length; s++)
				generatedStateBuffer[s] = SearchState.Empty;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (node.State[f, p].Arrow == InvalidArrowIndex)
						continue;

					// What was the step into
					if (link.Links[f, p].Valid &&
					    node.State[f, p].State == StepData.StateAfterAction[(int)link.Links[f, p].Action])
					{
						switch (node.State[f, p].State)
						{
							case GraphArrowState.Held:
							{
								generatedStateBuffer[node.State[f, p].Arrow] = SearchState.Hold;
								break;
							}
							case GraphArrowState.Resting:
							{
								if (link.Links[f, p].Action == FootAction.Release)
									generatedStateBuffer[node.State[f, p].Arrow] = SearchState.Empty;
								else
									generatedStateBuffer[node.State[f, p].Arrow] = SearchState.Tap;
								break;
							}
							case GraphArrowState.Lifted:
								generatedStateBuffer[node.State[f, p].Arrow] = SearchState.Empty;
								break;
						}
					}
					else
					{
						switch (node.State[f, p].State)
						{
							case GraphArrowState.Held:
								generatedStateBuffer[node.State[f, p].Arrow] = SearchState.Holding;
								break;
						}
					}
				}
			}

			for (var s = 0; s < generatedStateBuffer.Length; s++)
			{
				if (generatedStateBuffer[s] == searchState[s])
					continue;
				if (generatedStateBuffer[s] == SearchState.Hold && searchState[s] == SearchState.Roll)
					continue;
				if (generatedStateBuffer[s] == SearchState.Tap &&
				    (searchState[s] == SearchState.Fake || searchState[s] == SearchState.Lift))
					continue;
				if (generatedStateBuffer[s] == SearchState.Holding && searchState[s] == SearchState.Rolling)
					continue;
				return null;
			}

			// Apply InstanceStepTypes.
			var instanceTypes = new InstanceStepType[NumFeet, NumFootPortions];
			for (var s = 0; s < searchState.Length; s++)
			{
				if (searchState[s] == SearchState.Fake)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (node.State[f, p].Arrow == s
							    && link.Links[f, p].Valid
							    && link.Links[f, p].Action == FootAction.Tap)
							{
								instanceTypes[f, p] = InstanceStepType.Fake;
							}
						}
					}
				}
				else if (searchState[s] == SearchState.Lift)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (node.State[f, p].Arrow == s
							    && link.Links[f, p].Valid
							    && link.Links[f, p].Action == FootAction.Tap)
							{
								instanceTypes[f, p] = InstanceStepType.Lift;
							}
						}
					}
				}
				else if (searchState[s] == SearchState.Roll)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (node.State[f, p].Arrow == s
							    && link.Links[f, p].Valid
							    && link.Links[f, p].Action == FootAction.Hold)
							{
								instanceTypes[f, p] = InstanceStepType.Roll;
							}
						}
					}
				}
			}

			return new GraphLinkInstance(link, instanceTypes);
		}

		/// <summary>
		/// Adds the given child ChartSearchNode to the given parent ChartSearchNode linked
		/// to by the given GraphLink. Also adds the child to the given HashSet of running
		/// child ChartSearchNode.
		/// </summary>
		/// <param name="parent">Parent ChartSearchNode.</param>
		/// <param name="link">GraphLink linking to the child ChartSearchNode.</param>
		/// <param name="child">Child ChartSearchNode.</param>
		/// <param name="childSearchNodes">HashSet of ChartSearchNode to update with the new child.</param>
		private static void AddChildSearchNode(
			ChartSearchNode parent,
			GraphLink link,
			ChartSearchNode child,
			HashSet<ChartSearchNode> childSearchNodes)
		{
			if (!parent.NextNodes.TryGetValue(link, out var childNodes))
			{
				childNodes = new HashSet<ChartSearchNode>();
				parent.NextNodes[link] = childNodes;
			}

			childNodes.Add(child);
			childSearchNodes.Add(child);
		}

		/// <summary>
		/// Prunes the given HashSet of ChartSearchNodes to a HashSet that contains
		/// only the lowest cost ChartSearchNode per unique GraphNode.
		/// </summary>
		/// <param name="nodes">HashSet of ChartSearchNodes to prune.</param>
		/// <returns>Pruned ChartSearchNodes.</returns>
		private static HashSet<ChartSearchNode> Prune(HashSet<ChartSearchNode> nodes)
		{
			// Set up a Dictionary to track the best ChartSearchNode per GraphNode.
			var bestNodes = new Dictionary<GraphNode, ChartSearchNode>();
			foreach (var node in nodes)
			{
				// There is already a best node for this GraphNode, compare them.
				if (bestNodes.TryGetValue(node.GraphNode, out var currentNode))
				{
					// This node is better.
					if (node.CompareTo(currentNode, true) < 0)
					{
						Prune(currentNode);

						// Set the currentNode to this new best node so we record it below.
						currentNode = node;
					}
					else
					{
						Prune(node);
					}
				}
				// There is not yet a best node recorded for this GraphNode. Record this node
				// as the current best.
				else
				{
					currentNode = node;
				}

				bestNodes[currentNode.GraphNode] = currentNode;
			}

			return bestNodes.Values.ToHashSet();
		}

		/// <summary>
		/// Removes the given ChartSearchNode from the tree.
		/// Removes all parents up until the first parent with other children.
		/// </summary>
		/// <param name="node">ChartSearchNode to prune.</param>
		private static void Prune(ChartSearchNode node)
		{
			// Prune the node up until parent that has other children.
			while (node.PreviousNode != null)
			{
				node.PreviousNode.NextNodes[node.PreviousLink.GraphLink].Remove(node);
				if (node.PreviousNode.NextNodes[node.PreviousLink.GraphLink].Count == 0)
					node.PreviousNode.NextNodes.Remove(node.PreviousLink.GraphLink);
				if (node.PreviousNode.NextNodes.Count != 0)
					break;
				node = node.PreviousNode;
			}
		}

		private static bool IsStep(SearchState state)
		{
			return state == SearchState.Tap
			       || state == SearchState.Fake
			       || state == SearchState.Lift
			       || state == SearchState.Hold
			       || state == SearchState.Roll;
		}

		#region Cost Evaluation

		private int GetOrientationCost(ChartSearchNode searchNode)
		{
			switch (searchNode.GraphNode.Orientation)
			{
				case BodyOrientation.InvertedLeftOverRight:
				case BodyOrientation.InvertedRightOverLeft:
					return CostTieBreak_Orientation_Invert;
				case BodyOrientation.Normal:
				default:
					if (StepGraph.IsCrossoverWithOrWithoutStretch(searchNode.GraphNode))
						return CostTieBreak_Orientation_Crossover;
					return CostTieBreak_Orientation_Normal;
			}
		}

		/// <summary>
		/// Determine the cost of arriving to the ChartSearchNode from its parent.
		/// The cost represents how unlikely it is that this step is the best step
		/// to take to perform the chart being searched. Higher values are worse.
		/// For example, a double-step has high cost.
		/// </summary>
		/// <param name="searchNode">ChartSearchNode to get the cost of.</param>
		/// <param name="state"></param>
		/// <param name="lastMines">
		/// IntegerPosition of the last Mines encountered up to this point, per lane.
		/// </param>
		/// <param name="lastReleases">
		/// IntegerPosition of the last time there was a release on each lane.
		/// </param>
		/// <param name="padData">PadData for the Chart.</param>
		/// <returns>Cost to the given ChartSearchNode from its parent.</returns>
		private int GetCost(
			ChartSearchNode searchNode,
			SearchState[] state,
			int[] lastMines,
			int[] lastReleases,
			PadData padData)
		{
			var position = searchNode.Position;
			var link = searchNode.PreviousLink.GraphLink;

			// Releases have a 0 cost.
			if (link.IsRelease())
				return CostRelease;

			// Determine how many steps are in this state.
			var numSteps = 0;
			var lastArrowStep = 0;
			for (var a = 0; a < state.Length; a++)
			{
				if (IsStep(state[a]))
				{
					numSteps++;
					lastArrowStep = a;
				}
			}

			var previousStepLink = searchNode.PreviousNode.GetPreviousStepLink()?.GraphLink ?? null;
			var previousPreviousStepLink = searchNode.PreviousNode.GetPreviousStepLink(2)?.GraphLink ?? null;

			switch (numSteps)
			{
				case 1:
				{
					var thisArrow = lastArrowStep;
					GetSingleStepStepAndFoot(link, out var step, out var thisFoot);
					var otherFoot = OtherFoot(thisFoot);
					var previousState = searchNode.PreviousNode.GraphNode.State;
					// ReSharper disable UnusedVariable
					GetOneArrowStepInfo(thisFoot, thisArrow, lastMines, lastReleases, padData, previousState,
						out var thisAnyHeld,
						out var thisAllHeld,
						out var thisCanStepToNewArrow,
						out var thisCanStepToNewArrowWithoutCrossover,
						out var thisCanBracketToNewArrow,
						out var thisCanCrossoverToNewArrow,
						out var thisMinePositionFollowingPreviousStep,
						out var thisReleasePositionOfPreviousStep,
						out var thisFootPreviousArrows,
						out var thisFootInBracketPosture,
						out var thisLifted);
					GetOneArrowStepInfo(otherFoot, thisArrow, lastMines, lastReleases, padData, previousState,
						out var otherAnyHeld,
						out var otherAllHeld,
						out var otherCanStepToNewArrow,
						out var otherCanStepToNewArrowWithoutCrossover,
						out var otherCanBracketToNewArrow,
						out var otherCanCrossoverToNewArrow,
						out var otherMinePositionFollowingPreviousStep,
						out var otherReleasePositionOfPreviousStep,
						out var otherFootPreviousArrows,
						out var otherFootInBracketPosture,
						out var otherLifted);
					// ReSharper restore UnusedVariable

					var doubleStep = previousStepLink != null && previousStepLink.IsStepWithFoot(thisFoot) && !otherAnyHeld;
					var doubleStepOtherFootReleasedAtSameTime = false;
					var doubleStepOtherFootReleasedAfterThisFoot = false;
					if (otherReleasePositionOfPreviousStep == position)
					{
						doubleStepOtherFootReleasedAtSameTime = true;
						doubleStepOtherFootReleasedAfterThisFoot = true;
					}
					else if (otherReleasePositionOfPreviousStep < position &&
					         otherReleasePositionOfPreviousStep > thisReleasePositionOfPreviousStep)
					{
						doubleStepOtherFootReleasedAfterThisFoot = true;
					}

					var tripleStep = doubleStep && previousPreviousStepLink != null &&
					                 previousPreviousStepLink.IsStepWithFoot(thisFoot);
					var mostRecentRelease = thisReleasePositionOfPreviousStep;
					if ((mostRecentRelease < 0 && otherReleasePositionOfPreviousStep >= 0)
					    || (mostRecentRelease >= 0
					        && otherReleasePositionOfPreviousStep >= 0
					        && otherReleasePositionOfPreviousStep > thisReleasePositionOfPreviousStep))
						mostRecentRelease = otherReleasePositionOfPreviousStep;

					// I think in all cases we should consider an arrow held if it released at this time.
					thisAnyHeld |= position > 0 && thisReleasePositionOfPreviousStep == position;
					otherAnyHeld |= position > 0 && otherReleasePositionOfPreviousStep == position;

					var bracket = StepData.Steps[(int)step].IsOneArrowBracket;

					if (BracketParsingMethod == BracketParsingMethod.NoBrackets && bracket)
					{
						return NoBrackets_CostBracket;
					}

					switch (step)
					{
						case StepType.Swing:
							return CostSwing;

						case StepType.SameArrow:
						case StepType.BracketOneArrowHeelSame:
						case StepType.BracketOneArrowToeSame:
						{
							// If this is a same arrow step after a swap with this foot it is likely that we should continue
							// to alternate swaps rather than jack
							if (previousStepLink != null
							    && previousStepLink.IsFootSwap(out var swappingFoot, out _)
							    && swappingFoot == thisFoot)
								return CostSameArrow_AfterSwap;

							if (thisAnyHeld && !otherAnyHeld && otherCanStepToNewArrow)
								return CostSameArrow_OtherHoldingNone_ThisHeld_OtherCanStep;
							return CostSameArrow;
						}
						case StepType.NewArrow:
						case StepType.NewArrowStretch:
						case StepType.BracketOneArrowHeelNew:
						case StepType.BracketOneArrowToeNew:
						case StepType.BracketStretchOneArrowHeelNew:
						case StepType.BracketStretchOneArrowToeNew:
						{
							// TODO: give preference to alternating in long patters
							// For example,		LR, U, LR, D, LR, U, LR D
							// Should not be all L or all R on the single steps

							if (otherAnyHeld)
							{
								// This foot must make this step
								if (otherAllHeld)
								{
									// Slightly better to step on a closer arrow.
									if (thisCanBracketToNewArrow)
										return CostNewArrow_AllOtherHeld_ThisFootCanBracketToNewArrow;
									else
										return CostNewArrow_AllOtherHeld_ThisFootCannotBracketToNewArrow;
								}

								// Both feet are holding one arrow.
								// One foot needs to bracket.
								if (thisAnyHeld)
								{
									// Ambiguous bracket
									if (otherCanBracketToNewArrow)
									{
										// Alternating step bracket.
										if (previousStepLink.IsStepWithFoot(otherFoot))
										{
											return CostNewArrow_BothFeetHolding_OtherCanBracket_AlternatingStep;
										}

										if (previousStepLink.IsJump())
										{
											return GetCostNewArrowStepFromJump(
												otherCanStepToNewArrow,
												otherCanCrossoverToNewArrow,
												otherCanBracketToNewArrow,
												thisCanCrossoverToNewArrow,
												thisCanBracketToNewArrow,
												otherMinePositionFollowingPreviousStep,
												thisMinePositionFollowingPreviousStep,
												otherReleasePositionOfPreviousStep,
												thisReleasePositionOfPreviousStep);
										}

										// Double step bracket.
										return CostNewArrow_BothFeetHolding_OtherCanBracket_DoubleStep;
									}

									// Only this foot can bracket.
									return CostNewArrow_BothFeetHolding_OtherCannotBracket;
								}

								// The other foot is holding but this foot is not holding
								else
								{
									if (previousStepLink.IsStepWithFoot(otherFoot))
										return CostNewArrow_Alternating;

									// TODO: There could be patterns where you roll two feet
									// while one foot holds a bracket. This isn't considering that.
									return CostNewArrow_OtherHoldingOne;
								}
							}

							// Bracket step with the other foot not holding
							if (thisAnyHeld)
							{
								if (!thisCanBracketToNewArrow)
									return CostNewArrow_OtherHoldingNone_ThisHeld_ThisCannotBracket;

								// The other foot could make this step.
								if (otherCanStepToNewArrow)
								{
									if (doubleStep)
										return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep_DoubleStep;
									return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep;
								}

								// The other foot cannot hit this arrow.
								if (doubleStep)
									return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep_DoubleStep;
								return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep;
							}

							// No bracketing or holds

							if (doubleStep)
							{
								// If the other foot released later than this one then we are double-stepping
								// out of a pattern where the author likely intended it.
								if (doubleStepOtherFootReleasedAtSameTime)
									return CostNewArrow_OtherHoldingOne;
								if (doubleStepOtherFootReleasedAfterThisFoot)
									return CostNewArrow_DoubleStepOtherFootReleasedLater;

								// When there are lots of double steps we want to promote alternating.
								// It is better to hit two double steps with two feet rather than two with
								// one foot.
								if (tripleStep)
									return CostNewArrow_TripleStep;

								// Mine indicated
								if (thisMinePositionFollowingPreviousStep >= 0)
									return CostNewArrow_DoubleStepMineIndicated;

								// No indication
								return CostNewArrow_DoubleStep;
							}

							// No previous step.
							if (previousStepLink == null)
							{
								// The other foot is bracketable to this arrow and this foot is not.
								if (otherCanBracketToNewArrow && !thisCanBracketToNewArrow)
									return CostNewArrow_FirstStep_OtherIsBracketable_ThisIsNotBracketable;
								// This foot is bracketable to this arrow and the other foot is not.
								if (!otherCanBracketToNewArrow && thisCanBracketToNewArrow)
									return CostNewArrow_FirstStep_OtherIsNotBracketable_ThisIsBracketable;
								// Only this foot can make the step.
								if (!otherCanStepToNewArrow)
									return CostNewArrow_FirstStep_OtherCannotStep;
								// Ambiguous.
								return CostNewArrow_FirstStep_Ambiguous;
							}

							// Previous step was with the other foot.
							if (previousStepLink.IsStepWithFoot(otherFoot))
							{
								return CostNewArrow_Alternating;
							}

							// Jump into a new arrow. This could be done with either foot.
							// Rank by which is more natural
							if (previousStepLink.IsJump())
							{
								return GetCostNewArrowStepFromJump(
									otherCanStepToNewArrow,
									otherCanCrossoverToNewArrow,
									otherCanBracketToNewArrow,
									thisCanCrossoverToNewArrow,
									thisCanBracketToNewArrow,
									otherMinePositionFollowingPreviousStep,
									thisMinePositionFollowingPreviousStep,
									otherReleasePositionOfPreviousStep,
									thisReleasePositionOfPreviousStep);
							}

							// Unreachable? step with same foot that is not a double step or a same arrow step
							return CostUnknown;
						}
						case StepType.CrossoverFront:
						case StepType.CrossoverBehind:
						case StepType.CrossoverFrontStretch:
						case StepType.CrossoverBehindStretch:
						case StepType.BracketCrossoverFrontOneArrowHeelNew:
						case StepType.BracketCrossoverFrontOneArrowToeNew:
						case StepType.BracketCrossoverBehindOneArrowHeelNew:
						case StepType.BracketCrossoverBehindOneArrowToeNew:
						{
							var costAfterJump = CostNewArrow_Crossover_AfterJump;
							var costOtherHeld = CostNewArrow_Crossover_OtherHeld;
							var costDoubleStepMineIndicated = CostNewArrow_Crossover_OtherFree_DoubleStep_MineIndicated;
							var costDoubleStepNoIndication = CostNewArrow_Crossover_OtherFree_DoubleStep_NoIndication;
							var costCrossover = CostNewArrow_Crossover;
							var costOtherFootInBracketPosture = CostNewArrow_Crossover_OtherInBracketPosture;
							if (bracket)
							{
								costAfterJump = CostNewArrow_Bracket_Crossover_AfterJump;
								costOtherHeld = CostNewArrow_Bracket_Crossover_OtherHeld;
								costDoubleStepMineIndicated = CostNewArrow_Bracket_Crossover_OtherFree_DoubleStep_MineIndicated;
								costDoubleStepNoIndication = CostNewArrow_Bracket_Crossover_OtherFree_DoubleStep_NoIndication;
								costCrossover = CostNewArrow_Bracket_Crossover;
								costOtherFootInBracketPosture = CostNewArrow_Bracket_Crossover_OtherInBracketPosture;
							}
							else if (step == StepType.CrossoverFrontStretch || step == StepType.CrossoverBehindStretch)
							{
								costAfterJump = CostNewArrow_Crossover_Stretch_AfterJump;
								costOtherHeld = CostNewArrow_Crossover_Stretch_OtherHeld;
								costDoubleStepMineIndicated = CostNewArrow_Crossover_Stretch_OtherFree_DoubleStep_MineIndicated;
								costDoubleStepNoIndication = CostNewArrow_Crossover_Stretch_OtherFree_DoubleStep_NoIndication;
								costCrossover = CostNewArrow_Crossover_Stretch;
								costOtherFootInBracketPosture = CostNewArrow_Crossover_Stretch_OtherInBracketPosture;
							}

							if ((previousStepLink?.IsJump() ?? false) && !otherAnyHeld)
								return costAfterJump;

							if (otherAnyHeld)
								return costOtherHeld;

							if (doubleStep)
							{
								// Mine indicated
								if (thisMinePositionFollowingPreviousStep >= 0)
									return costDoubleStepMineIndicated;

								// No indication
								return costDoubleStepNoIndication;
							}

							if (otherFootInBracketPosture)
								return costOtherFootInBracketPosture;

							return costCrossover;
						}
						case StepType.FootSwap:
						case StepType.BracketOneArrowHeelSwap:
						case StepType.BracketOneArrowToeSwap:
						case StepType.FootSwapCrossoverFront:
						case StepType.FootSwapCrossoverBehind:
						case StepType.FootSwapInvertFront:
						case StepType.FootSwapInvertBehind:
						{
							var mineIndicatedOnThisFootsArrow = thisMinePositionFollowingPreviousStep >= 0;

							if (step == StepType.FootSwapInvertFront || step == StepType.FootSwapInvertBehind)
								return Cost_FootSwap_Invert;
							if (step == StepType.FootSwapCrossoverFront || step == StepType.FootSwapCrossoverBehind)
								return Cost_FootSwap_Crossover;

							if (doubleStep)
							{
								if (mineIndicatedOnThisFootsArrow)
									return Cost_FootSwap_DoubleStep_MineIndication;
								return Cost_FootSwap_DoubleStep_NoMineIndication;
							}

							// If the other foot is holding this swap becomes more unlikely as it may
							// mean a previous bracket should have been a jump.
							if (otherAnyHeld && !thisAnyHeld)
								return CostNewArrow_FootSwap_OtherHolding;

							// Determine if there was a mine on another free lane.
							// Some chart authors use this to signal a footswap.
							var mineIndicatedOnFreeLaneArrow = false;
							for (var arrow = 0; arrow < padData.NumArrows; arrow++)
							{
								// Skip this arrow if it was hit by the other foot.
								var thisArrowIsForOtherFoot = false;
								for (var p = 0; p < NumFootPortions; p++)
								{
									if (otherFootPreviousArrows[p] == arrow)
									{
										thisArrowIsForOtherFoot = true;
										break;
									}
								}

								if (thisArrowIsForOtherFoot)
									continue;

								// Check if the last mine for this arrow was at or after the last
								// release.
								if (lastMines[arrow] < 0)
									continue;
								if (lastMines[arrow] >= mostRecentRelease)
								{
									mineIndicatedOnFreeLaneArrow = true;
									break;
								}
							}

							// Swapping after a bracket. Usually this means the previous bracket should have been a jump,
							// but if it is mine indicated then lower the cost to prefer the bracket. We want to avoid
							// this cost when using aggressive bracket parsing though because it can result in choosing
							// awkward paths in order to set up the jump/bracket to be only jump eligible (e.g. crossover
							// into a jump).
							if (otherFootInBracketPosture
							    && !thisAnyHeld
							    && !mineIndicatedOnThisFootsArrow
							    && !mineIndicatedOnFreeLaneArrow
							    && BracketParsingMethod != BracketParsingMethod.Aggressive)
								return CostNewArrow_FootSwap_OtherInBracketPosture;

							var previousStepNode = searchNode.GetPreviousStepSearchNode();
							var thisSameArrowStep = previousStepNode != null
							                        && (previousStepNode.GraphNode.State[thisFoot, Heel].Arrow == thisArrow
							                            || previousStepNode.GraphNode.State[thisFoot, Toe].Arrow == thisArrow);

							// Mine indicated
							if (mineIndicatedOnThisFootsArrow)
								return thisSameArrowStep
									? CostSameArrow_FootSwap_MineIndicationOnThisFootsArrow
									: CostNewArrow_FootSwap_MineIndicationOnThisFootsArrow;
							if (mineIndicatedOnFreeLaneArrow)
								return thisSameArrowStep
									? CostSameArrow_FootSwap_MineIndicationOnFreeLaneArrow
									: CostNewArrow_FootSwap_MineIndicationOnFreeLaneArrow;

							// Swapping after a same arrow step with the other foot should be penalized to promote
							// alternating swaps or no swaps at all
							var previousPreviousStepNode = searchNode.GetPreviousStepSearchNode(2);
							if (previousStepNode != null && previousPreviousStepNode != null
							                             && previousStepLink != null && previousPreviousStepLink != null)
							{
								var previousStateHadOtherFootOnArrow = false;
								var previousPreviousStateHadOtherFootOnArrow = false;

								for (var p = 0; p < NumFootPortions; p++)
								{
									if (previousStepNode.GraphNode.State[otherFoot, p].Arrow == thisArrow
									    && previousStepNode.GraphNode.State[otherFoot, p].State != GraphArrowState.Lifted)
									{
										previousStateHadOtherFootOnArrow = true;
									}

									if (previousPreviousStepNode.GraphNode.State[otherFoot, p].Arrow == thisArrow
									    && previousPreviousStepNode.GraphNode.State[otherFoot, p].State != GraphArrowState.Lifted)
									{
										previousPreviousStateHadOtherFootOnArrow = true;
									}
								}

								if (previousStateHadOtherFootOnArrow
								    && previousPreviousStateHadOtherFootOnArrow
								    && previousStepLink.IsSingleStep(out _, out var previousFoot) && previousFoot == otherFoot
								    && previousPreviousStepLink.IsSingleStep(out _, out var previousPreviousFoot) &&
								    previousPreviousFoot == otherFoot)
								{
									return Cost_FootSwap_AfterOtherFootJack;
								}
							}

							// If the other foot is already lifted then we are swapping twice with the same foot.
							if (otherLifted)
							{
								if (thisSameArrowStep)
									return CostSameArrow_FootSwap_DoubleSwap;
								return CostNewArrow_FootSwap_SubsequentSwap;
							}

							// If this foot is lifted, meaning it was swapped off of its current arrow, and it can
							// swap back onto the same arrow
							if (thisSameArrowStep && thisLifted)
								return CostSameArrow_FootSwap_AlternatingSwap;

							// No indication and bracketable.
							if (thisCanBracketToNewArrow)
								return CostNewArrow_FootSwap_NoIndication_Bracketable;

							// No indication and not bracketable
							return CostNewArrow_FootSwap_NoIndication_NotBracketable;
						}
						case StepType.InvertFront:
						case StepType.InvertBehind:
						case StepType.InvertFrontStretch:
						case StepType.InvertBehindStretch:
						case StepType.BracketInvertFrontOneArrowHeelNew:
						case StepType.BracketInvertFrontOneArrowToeNew:
						case StepType.BracketInvertBehindOneArrowHeelNew:
						case StepType.BracketInvertBehindOneArrowToeNew:
						{
							var costFromSwap = CostNewArrow_Invert_FromSwap;
							var costOtherHeld = CostNewArrow_Invert_OtherHeld;
							var costDoubleStepMineIndicated = CostNewArrow_Invert_OtherFree_DoubleStep_MineIndicated;
							var costDoubleStepNoIndication = CostNewArrow_Invert_OtherFree_DoubleStep_NoIndication;
							var costInvert = CostNewArrow_Invert;
							var costOtherFootInBracketPosture = CostNewArrow_Invert_OtherInBracketPosture;
							if (bracket)
							{
								costFromSwap = CostNewArrow_Bracket_Invert_FromSwap;
								costOtherHeld = CostNewArrow_Bracket_Invert_OtherHeld;
								costDoubleStepMineIndicated = CostNewArrow_Bracket_Invert_OtherFree_DoubleStep_MineIndicated;
								costDoubleStepNoIndication = CostNewArrow_Bracket_Invert_OtherFree_DoubleStep_NoIndication;
								costInvert = CostNewArrow_Bracket_Invert;
								costOtherFootInBracketPosture = CostNewArrow_Bracket_Invert_OtherInBracketPosture;
							}
							else if (step == StepType.InvertFrontStretch || step == StepType.InvertBehindStretch)
							{
								costFromSwap = CostNewArrow_Stretch_Invert_FromSwap;
								costOtherHeld = CostNewArrow_Stretch_Invert_OtherHeld;
								costDoubleStepMineIndicated = CostNewArrow_Stretch_Invert_OtherFree_DoubleStep_MineIndicated;
								costDoubleStepNoIndication = CostNewArrow_Stretch_Invert_OtherFree_DoubleStep_NoIndication;
								costInvert = CostNewArrow_Stretch_Invert;
								costOtherFootInBracketPosture = CostNewArrow_Stretch_Invert_OtherInBracketPosture;
							}

							// Inversion from a foot swap
							if (previousStepLink?.IsFootSwap(out _, out _) ?? false)
								return costFromSwap;

							if (otherAnyHeld)
								return costOtherHeld;

							if (doubleStep)
							{
								// Mine indicated
								if (thisMinePositionFollowingPreviousStep >= 0)
									return costDoubleStepMineIndicated;

								// No indication
								return costDoubleStepNoIndication;
							}

							if (otherFootInBracketPosture)
								return costOtherFootInBracketPosture;

							return costInvert;
						}
						default:
						{
							LogError($"[Cost Determination] Unexpected StepType {step:G} for {numSteps} step at {position}.",
								LogIdentifier);
							return CostUnknown;
						}
					}
				}

				case 2:
				{
					var couldBeBracketed = new bool[NumFeet];
					var holdingAny = new bool[NumFeet];
					var holdingAll = new bool[NumFeet];
					var movesOffArrowIfThisFootSteps = new bool[NumFeet];
					var numNewArrowsIfThisFootSteps = new int[NumFeet];
					var bracketableDistanceIfThisFootSteps = new bool[NumFeet];
					var involvesSwapIfBracketed = new bool[NumFeet];
					var involvesCrossoverIfBracketed = new bool[NumFeet];
					var involvesInvertIfBracketed = new bool[NumFeet];
					for (var f = 0; f < NumFeet; f++)
					{
						GetTwoArrowStepInfo(
							searchNode,
							state,
							f,
							padData,
							lastReleases,
							out couldBeBracketed[f],
							out holdingAny[f],
							out holdingAll[f],
							out movesOffArrowIfThisFootSteps[f],
							out numNewArrowsIfThisFootSteps[f],
							out bracketableDistanceIfThisFootSteps[f],
							out involvesSwapIfBracketed[f],
							out involvesCrossoverIfBracketed[f],
							out involvesInvertIfBracketed[f]);
					}

					// If previous was step with other foot and that other foot would need to move to reach one
					// of the new arrows, and the new set of arrows is bracketable by this foot, we should
					// prefer the bracket.
					var preferBracketDueToAmountOfMovement = new bool[NumFeet];
					var atLeastOneFootPrefersBracket = false;
					if (previousStepLink != null)
					{
						for (var f = 0; f < NumFeet; f++)
						{
							if (previousStepLink.IsStepWithFoot(OtherFoot(f))
							    && couldBeBracketed[f]
							    && movesOffArrowIfThisFootSteps[OtherFoot(f)])
							{
								preferBracketDueToAmountOfMovement[f] = true;
								atLeastOneFootPrefersBracket = true;
							}
						}
					}

					// Evaluate Bracket
					if (GetBracketStepAndFoot(link, out var step, out var foot))
					{
						if (BracketParsingMethod == BracketParsingMethod.NoBrackets)
						{
							return NoBrackets_CostBracket;
						}

						if (step == StepType.BracketSwing)
							return CostBracketSwing;

						if (StepData.Steps[(int)step].IsInvert)
							return CostTwoArrows_Bracket_Invert;
						if (StepData.Steps[(int)step].IsCrossover)
							return CostTwoArrows_Bracket_Crossover;

						var otherFoot = OtherFoot(foot);

						// If the other foot is holding all possible arrows, there is no choice.
						if (holdingAll[otherFoot])
							return CostTwoArrows_Bracket_OtherFootHoldingBoth;

						// If this is a double step we should prefer the jump
						// A double step is fairly normal into a jump, but less normal into a bracket
						var doubleStep = previousStepLink != null
						                 && previousStepLink.IsStepWithFoot(foot)
						                 && !holdingAny[otherFoot]
						                 && step != StepType.BracketHeelSameToeSame;
						if (doubleStep)
						{
							// We may want to differentiate here between a double step that involves the same
							// arrow and one that does not involve any of the same arrows or is a swap.
							return CostTwoArrows_Bracket_DoubleStep;
						}

						var swap = StepData.Steps[(int)step].IsFootSwapWithAnyPortion;

						if (step == StepType.BracketHeelSameToeSame)
							return CostTwoArrows_Bracket_BothSame;
						if (preferBracketDueToAmountOfMovement[foot])
						{
							if (swap)
								return CostTwoArrows_Bracket_PreferredDueToMovement_Swap;
							return CostTwoArrows_Bracket_PreferredDueToMovement;
						}

						if (swap)
						{
							if (numNewArrowsIfThisFootSteps[foot] == NumFootPortions)
								return CostTwoArrows_Bracket_Swap_BothNew;
							if (numNewArrowsIfThisFootSteps[foot] > 0)
								return CostTwoArrows_Bracket_Swap_OneNew;
							return CostTwoArrows_Bracket_Swap;
						}

						return CostTwoArrows_Bracket;
					}

					// Evaluate Jump
					if (link.IsJump())
					{
						if (BracketParsingMethod == BracketParsingMethod.Aggressive)
						{
							for (var f = 0; f < NumFeet; f++)
							{
								if (preferBracketDueToAmountOfMovement[f]
								    && !involvesSwapIfBracketed[f]
								    && !involvesCrossoverIfBracketed[f]
								    && !involvesInvertIfBracketed[f])
								{
									return AggressiveBrackets_CostJump_BracketPreferredDueToMovement;
								}
							}
						}

						var onlyFootHoldingOne = InvalidFoot;
						for (var f = 0; f < NumFeet; f++)
						{
							if (holdingAny[f] && !holdingAll[f])
							{
								if (onlyFootHoldingOne == InvalidFoot)
								{
									onlyFootHoldingOne = f;
								}
								else
								{
									onlyFootHoldingOne = InvalidFoot;
									break;
								}
							}
						}

						if (BracketParsingMethod == BracketParsingMethod.Aggressive)
						{
							if (onlyFootHoldingOne != InvalidFoot && couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
							{
								return AggressiveBrackets_CostJump_OtherFootHoldingOne_ThisFootCouldBracket;
							}
						}

						// If only one foot is holding one, we should prefer a bracket if the two arrows
						// are bracketable by the other foot.
						if (onlyFootHoldingOne != InvalidFoot && couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
							return CostTwoArrows_Jump_OtherFootHoldingOne_ThisFootCouldBracket;
						// If only one foot is holding one and the two new arrows are not bracketable
						// by the other foot, we should prefer the jump.
						if (onlyFootHoldingOne != InvalidFoot && !couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
							return CostTwoArrows_Jump_OtherFootHoldingOne_NotBracketable;

						// No hold or both feet holding

						if (atLeastOneFootPrefersBracket)
							return CostTwoArrows_Jump_OneFootPrefersBracketToDueMovement;

						var inverted = searchNode.GraphNode.Orientation == BodyOrientation.InvertedLeftOverRight
						               || searchNode.GraphNode.Orientation == BodyOrientation.InvertedRightOverLeft;
						if (inverted)
							return CostTwoArrows_Jump_Inverted;

						var bothNew = true;
						var bothSame = true;
						for (var f = 0; f < NumFeet; f++)
						{
							for (var p = 0; p < NumFootPortions; p++)
							{
								if (!link.Links[f, p].Valid)
									continue;
								if (link.Links[f, p].Step == StepType.NewArrow)
									bothSame = false;
								if (link.Links[f, p].Step == StepType.SameArrow)
									bothNew = false;
							}
						}

						var crossedOver = false;
						if (!holdingAny[L] && !holdingAny[R])
						{
							crossedOver = StepGraph.IsCrossoverWithOrWithoutStretch(searchNode.GraphNode);
						}

						if (crossedOver)
							return CostTwoArrows_Jump_CrossedOver;

						if (bothSame)
							return CostTwoArrows_Jump_BothSame;
						if (bothNew)
						{
							if (!bracketableDistanceIfThisFootSteps[L] && !bracketableDistanceIfThisFootSteps[R])
								return CostTwoArrows_Jump_BothNewAndNeitherBracketable;
							if (!bracketableDistanceIfThisFootSteps[L] || !bracketableDistanceIfThisFootSteps[R])
								return CostTwoArrows_Jump_BothNewAndOneBracketable;

							return CostTwoArrows_Jump_BothNew;
						}

						return CostTwoArrows_Jump_OneNew;
					}

					LogError($"[Cost Determination] Unexpected behavior for {numSteps} steps at {position}.", LogIdentifier);
					return CostUnknown;
				}

				case 3:
				case 4:
				{
					// Prefer combinations with the simplest patterns
					var numCrossovers = 0;
					var numInverts = 0;
					var numSwaps = 0;
					var numNewArrows = 0;
					if (link != null)
					{
						for (var f = 0; f < NumFeet; f++)
						{
							var portionAccountedFor = false;
							for (var p = 0; p < NumFootPortions; p++)
							{
								if (link.Links[f, p].Valid)
								{
									if (!portionAccountedFor)
									{
										var sd = StepData.Steps[(int)link.Links[f, p].Step];
										numNewArrows += sd.NumPossibleNewArrows;
										if (sd.IsFootSwapWithAnyPortion)
											numSwaps++;
										if (sd.IsInvert)
											numInverts++;
										if (sd.IsCrossover)
											numCrossovers++;
									}

									portionAccountedFor = true;
								}
							}
						}
					}

					return CostBracketJump
					       + CostBracketJump_Invert_Penalty * numInverts
					       + CostBracketJump_Crossover_Penalty * numCrossovers
					       + CostBracketJump_Swap_Penalty * numSwaps
					       + CostBracketJump_NewArrow_Penalty * numNewArrows;
				}
				default:
				{
					LogError($"[Cost Determination] Unexpected number of steps ({numSteps}) at {position}.", LogIdentifier);
					return CostUnknown;
				}
			}
		}

		private static void GetOneArrowStepInfo(
			int foot,
			int arrow,
			int[] lastMines,
			int[] lastReleases,
			PadData padData,
			GraphNode.FootArrowState[,] previousState,
			out bool anyHeld,
			out bool allHeld,
			out bool canStepToNewArrow,
			out bool canStepToNewArrowWithoutCrossover,
			out bool canBracketToNewArrow,
			out bool canCrossoverToNewArrow,
			out int minePositionFollowingPreviousStep,
			out int releasePositionOfPreviousStep,
			out int[] previousArrows,
			out bool inBracketPosture,
			out bool lifted)
		{
			anyHeld = false;
			allHeld = true;
			canStepToNewArrow = false;
			canStepToNewArrowWithoutCrossover = false;
			canBracketToNewArrow = false;
			minePositionFollowingPreviousStep = -1;
			canCrossoverToNewArrow = false;
			releasePositionOfPreviousStep = 0;
			previousArrows = new int[NumFootPortions];
			inBracketPosture = true;
			lifted = false;
			var arrowData = padData.ArrowData;

			// TODO: Should this logic include inverted steps too?

			var otherFoot = OtherFoot(foot);

			for (var p = 0; p < NumFootPortions; p++)
			{
				previousArrows[p] = previousState[foot, p].Arrow;

				if (previousState[otherFoot, p].Arrow == arrow)
				{
					lifted = previousState[foot, p].State == GraphArrowState.Lifted;
				}

				if (previousState[otherFoot, p].Arrow != InvalidArrowIndex)
				{
					canCrossoverToNewArrow |=
						arrowData[previousState[otherFoot, p].Arrow].OtherFootPairingsCrossoverBehind[otherFoot][arrow]
						|| arrowData[previousState[otherFoot, p].Arrow].OtherFootPairingsCrossoverFront[otherFoot][arrow];
					canStepToNewArrowWithoutCrossover |=
						arrowData[previousState[otherFoot, p].Arrow].OtherFootPairings[otherFoot][arrow];
				}

				if (previousState[foot, p].Arrow != InvalidArrowIndex)
				{
					if (previousState[foot, p].State == GraphArrowState.Held)
					{
						anyHeld = true;
						canBracketToNewArrow =
							arrowData[previousState[foot, p].Arrow].BracketablePairingsHeel[foot][arrow]
							|| arrowData[previousState[foot, p].Arrow].BracketablePairingsToe[foot][arrow];
					}
					else
					{
						allHeld = false;
						if (previousState[foot, p].State == GraphArrowState.Resting ||
						    previousState[foot, p].State == GraphArrowState.Lifted)
						{
							if (!anyHeld)
							{
								canBracketToNewArrow =
									arrowData[previousState[foot, p].Arrow].BracketablePairingsHeel[foot][arrow]
									|| arrowData[previousState[foot, p].Arrow].BracketablePairingsToe[foot][arrow];
							}

							if (lastMines[previousState[foot, p].Arrow] >= 0
							    && lastMines[previousState[foot, p].Arrow] > lastReleases[previousState[foot, p].Arrow])
								minePositionFollowingPreviousStep = lastMines[previousState[foot, p].Arrow];

							// A foot could be coming from a bracket with multiple releases. In this case we want to
							// choose the latest.
							if (releasePositionOfPreviousStep < lastReleases[previousState[foot, p].Arrow])
								releasePositionOfPreviousStep = lastReleases[previousState[foot, p].Arrow];
						}
					}
				}
				else
				{
					inBracketPosture = false;
					allHeld = false;
				}
			}

			if (anyHeld)
			{
				canCrossoverToNewArrow = false;
			}

			if (allHeld)
			{
				canStepToNewArrowWithoutCrossover = false;
				canBracketToNewArrow = false;
			}

			canStepToNewArrow = canCrossoverToNewArrow || canStepToNewArrowWithoutCrossover;
		}

		private static void GetSingleStepStepAndFoot(GraphLink link, out StepType step, out int foot)
		{
			step = StepType.SameArrow;
			foot = 0;
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (link.Links[f, p].Valid)
					{
						step = link.Links[f, p].Step;
						foot = f;
					}
				}
			}
		}

		private static bool GetBracketStepAndFoot(GraphLink link, out StepType step, out int foot)
		{
			step = StepType.BracketHeelNewToeNew;
			foot = 0;
			var numValid = 0;
			for (var f = 0; f < NumFeet; f++)
			{
				if (link.Links[f, 0].Valid && StepData.Steps[(int)link.Links[f, 0].Step].IsBracket)
				{
					step = link.Links[f, 0].Step;
					foot = f;
					numValid++;
				}
			}

			return numValid == 1;
		}

		private static void GetTwoArrowStepInfo(
			ChartSearchNode searchNode,
			SearchState[] state,
			int foot,
			PadData padData,
			int[] lastReleases,
			out bool couldBeBracketed,
			out bool holdingAny,
			out bool holdingAll,
			out bool movesOffArrowIfThisFootSteps,
			out int numNewArrowsIfThisFootSteps,
			out bool bracketableDistanceIfThisFootSteps,
			out bool involvesSwapIfBracketed,
			out bool involvesCrossoverIfBracketed,
			out bool involvesInvertIfBracketed)
		{
			var parentSearchNode = searchNode.PreviousNode;

			couldBeBracketed = false;
			holdingAny = false;
			holdingAll = false;
			movesOffArrowIfThisFootSteps = false;
			numNewArrowsIfThisFootSteps = 0;
			bracketableDistanceIfThisFootSteps = false;
			involvesSwapIfBracketed = false;
			involvesCrossoverIfBracketed = false;
			involvesInvertIfBracketed = false;
			var arrowData = padData.ArrowData;

			// Determine if any are held by this foot
			if (parentSearchNode != null)
			{
				// Get the last release position so that we can consider an arrow still held if it released at the same
				// position.
				var releasePositionOfPreviousStep = 0;
				var previousState = parentSearchNode.GraphNode.State;
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (previousState[foot, p].Arrow != InvalidArrowIndex
					    && (previousState[foot, p].State == GraphArrowState.Resting ||
					        previousState[foot, p].State == GraphArrowState.Lifted))
					{
						// A foot could be coming from a bracket with multiple releases. In this case we want to
						// choose the latest.
						if (releasePositionOfPreviousStep < lastReleases[previousState[foot, p].Arrow])
							releasePositionOfPreviousStep = lastReleases[previousState[foot, p].Arrow];
					}
				}

				// Set the number of arrows which would be steps onto a new arrow if this foot steps.
				// This includes swaps.
				for (var a = 0; a < state.Length; a++)
				{
					if (IsStep(state[a]))
					{
						var previousStateHadThisFootOnSameArrow = false;
						for (var previousPortion = 0; previousPortion < NumFootPortions; previousPortion++)
						{
							if (parentSearchNode.GraphNode.State[foot, previousPortion].Arrow == a)
							{
								previousStateHadThisFootOnSameArrow = true;
								break;
							}
						}

						if (!previousStateHadThisFootOnSameArrow)
						{
							numNewArrowsIfThisFootSteps++;
						}
					}
				}

				holdingAll = true;
				for (var p = 0; p < NumFootPortions; p++)
				{
					var previousArrow = parentSearchNode.GraphNode.State[foot, p].Arrow;
					if (previousArrow != InvalidArrowIndex)
					{
						var held = parentSearchNode.GraphNode.State[foot, p].State == GraphArrowState.Held;
						if (!held)
						{
							held = searchNode.Position == releasePositionOfPreviousStep
							       && parentSearchNode.LastFootUsedToStepOnArrow[previousArrow] == foot;
						}

						if (held)
						{
							holdingAny = true;
						}
						else
						{
							holdingAll = false;
						}

						if ((parentSearchNode.GraphNode.State[foot, p].State == GraphArrowState.Resting
						     || parentSearchNode.GraphNode.State[foot, p].State == GraphArrowState.Lifted)
						    && state[previousArrow] == SearchState.Empty)
						{
							movesOffArrowIfThisFootSteps = true;
						}

						// Determine, if this foot steps as part of a jump, would that step be a
						// bracketable distance.
						var newArrowBeingSteppedOnByThisFoot = InvalidArrowIndex;
						for (var newP = 0; newP < NumFootPortions; newP++)
						{
							newArrowBeingSteppedOnByThisFoot = searchNode.GraphNode.State[foot, newP].Arrow;
							if (newArrowBeingSteppedOnByThisFoot != InvalidArrowIndex)
								break;
						}

						if (newArrowBeingSteppedOnByThisFoot != InvalidArrowIndex
						    && (arrowData[previousArrow].BracketablePairingsHeel[foot][newArrowBeingSteppedOnByThisFoot]
						        || arrowData[previousArrow].BracketablePairingsToe[foot][newArrowBeingSteppedOnByThisFoot]))
						{
							bracketableDistanceIfThisFootSteps = true;
						}
					}
					else
					{
						holdingAll = false;
					}
				}
			}

			// Determine if the two arrows could be bracketed by this foot
			if (!holdingAny)
			{
				couldBeBracketed = true;
				var numSteps = 0;
				var steppedArrow = InvalidArrowIndex;
				var otherFoot = OtherFoot(foot);
				for (var a = 0; a < state.Length; a++)
				{
					if (IsStep(state[a]))
					{
						numSteps++;
						if (steppedArrow != InvalidArrowIndex)
						{
							var bracketable =
								(arrowData[a].BracketablePairingsHeel[foot][steppedArrow]
								 && arrowData[steppedArrow].BracketablePairingsToe[foot][a])
								|| (arrowData[a].BracketablePairingsToe[foot][steppedArrow]
								    && arrowData[steppedArrow].BracketablePairingsHeel[foot][a]);
							if (!bracketable)
								couldBeBracketed = false;
						}

						if (parentSearchNode != null)
						{
							for (var p = 0; p < NumFootPortions; p++)
							{
								var otherA = parentSearchNode.GraphNode.State[otherFoot, p].Arrow;
								if (otherA == InvalidArrowIndex)
									continue;

								if (otherA == a)
								{
									involvesSwapIfBracketed = true;
								}

								var ad = arrowData[otherA];
								if (ad.OtherFootPairingsCrossoverBehind[otherFoot][a]
								    || ad.OtherFootPairingsCrossoverBehind[otherFoot][a])
								{
									involvesCrossoverIfBracketed = true;
								}

								if (ad.OtherFootPairingsInverted[otherFoot][a])
								{
									involvesInvertIfBracketed = true;
								}
							}
						}

						steppedArrow = a;
					}
				}

				if (numSteps != NumFootPortions)
					couldBeBracketed = false;
			}
		}

		private static int GetCostNewArrowStepFromJump(
			bool otherCanStepToNewArrow,
			bool otherCanCrossoverToNewArrow,
			bool otherCanBracketToNewArrow,
			bool thisCanCrossoverToNewArrow,
			bool thisCanBracketToNewArrow,
			int otherMinePositionFollowingPreviousStep,
			int thisMinePositionFollowingPreviousStep,
			int otherReleasePositionOfPreviousStep,
			int thisReleasePositionOfPreviousStep)
		{
			if (!otherCanStepToNewArrow)
				return CostNewArrow_StepFromJump_OtherCannotStep;

			if (otherCanCrossoverToNewArrow)
				return CostNewArrow_StepFromJump_OtherCrossover;

			if (thisCanCrossoverToNewArrow)
				return CostNewArrow_StepFromJump_ThisCrossover;

			// Mine indication for only other foot to make this step.
			if (otherMinePositionFollowingPreviousStep >= 0 && thisMinePositionFollowingPreviousStep < 0)
				return CostNewArrow_StepFromJump_OtherMineIndicated_ThisNotMineIndicated;

			// Mine indication for both but other foot is sooner
			if (otherMinePositionFollowingPreviousStep >= 0
			    && thisMinePositionFollowingPreviousStep >= 0
			    && otherMinePositionFollowingPreviousStep > thisMinePositionFollowingPreviousStep)
				return CostNewArrow_StepFromJump_BothMineIndicated_ThisSooner;

			// Mine indication for both but this foot is sooner
			if (otherMinePositionFollowingPreviousStep >= 0
			    && thisMinePositionFollowingPreviousStep >= 0
			    && thisMinePositionFollowingPreviousStep > otherMinePositionFollowingPreviousStep)
				return CostNewArrow_StepFromJump_BothMineIndicated_OtherSooner;

			// Mine indication for only this foot to make this step.
			if (thisMinePositionFollowingPreviousStep >= 0 && otherMinePositionFollowingPreviousStep < 0)
				return CostNewArrow_StepFromJump_OtherNotMineIndicated_ThisMineIndicated;

			// Release indication for this foot (other released later)
			if (otherReleasePositionOfPreviousStep > thisReleasePositionOfPreviousStep)
				return CostNewArrow_StepFromJump_OtherFootReleasedLater;

			// Release indication for other foot (this released later)
			if (thisReleasePositionOfPreviousStep > otherReleasePositionOfPreviousStep)
				return CostNewArrow_StepFromJump_ThisFootReleasedLater;

			// The other foot is bracketable to this arrow and this foot is not
			if (otherCanBracketToNewArrow && !thisCanBracketToNewArrow)
				return CostNewArrow_StepFromJump_OtherFootBracketable_ThisFootNotBracketable;

			// The other foot is not bracketable to this arrow and this foot is
			if (!otherCanBracketToNewArrow && thisCanBracketToNewArrow)
				return CostNewArrow_StepFromJump_OtherFootNotBracketable_ThisFootBracketable;

			// Equal choice
			return CostNewArrow_StepFromJump_Ambiguous;
		}

		#endregion

		/// <summary>
		/// Adds StepEvents to the given ExpressedChart.
		/// This is run after the SM Chart has been searched and a single path of
		/// ChartSearchNodes has been generated.
		/// </summary>
		private void AddSteps()
		{
			// Current node for iterating through the chart.
			var searchNode = Root;

			// The first node is the resting position and not an event in the chart.
			searchNode = searchNode.GetNextNode();

			while (searchNode != null)
			{
				// Create a new StepEvent for this step ChartSearchNode for adding to the ExpressedChart.
				var stepEvent = new StepEvent(searchNode.Position, searchNode.TimeSeconds, searchNode.PreviousLink);

				// Set up the Link for the StepEvent and advance to the next ChartSearchNode.
				if (searchNode.NextNodes.Count > 0)
					searchNode = searchNode.NextNodes.First().Value.First();
				else
					searchNode = null;

				// Record the StepEvent.
				StepEvents.Add(stepEvent);
			}
		}

		/// <summary>
		/// Adds MineEvents for all given mines to the given ExpressedChart.
		/// This is run after the SM Chart has been searched and a single path of
		/// ChartSearchNodes has been generated.
		/// </summary>
		private void AddMines()
		{
			// Create sorted lists of releases and steps.
			var stepEvents = new List<ChartSearchNode>();
			var chartSearchNode = Root;
			while (chartSearchNode != null)
			{
				stepEvents.Add(chartSearchNode);
				chartSearchNode = chartSearchNode.GetNextNode();
			}

			var (releases, steps) = MineUtils.GetReleasesAndSteps(stepEvents, StepGraph.NumArrows);

			var stepIndex = 0;
			var releaseIndex = InvalidArrowIndex;
			for (var i = 0; i < MineNotes.Count; i++)
			{
				var smMineEvent = MineNotes[i];
				// Advance the step and release indices to follow and precede the event respectively.
				while (stepIndex < steps.Count && steps[stepIndex].Position <= smMineEvent.IntegerPosition)
					stepIndex++;
				while (releaseIndex + 1 < releases.Count && releases[releaseIndex + 1].Position < smMineEvent.IntegerPosition)
					releaseIndex++;

				// Create and add a new MineEvent.
				var expressedMineEvent = MineUtils.CreateExpressedMineEvent(
					StepGraph.NumArrows, releases, releaseIndex, steps, stepIndex, smMineEvent);
				MineEvents.Add(expressedMineEvent);
			}

			MineEvents.Sort(new MineEventComparer());
		}

		#region Logging

		private static void LogError(string message, string logIdentifier)
		{
			if (!string.IsNullOrEmpty(logIdentifier))
				Logger.Error($"[{LogTag}] {logIdentifier} {message}");
			else
				Logger.Error($"[{LogTag}] {message}");
		}

		private static void LogWarn(string message, string logIdentifier)
		{
			if (!string.IsNullOrEmpty(logIdentifier))
				Logger.Warn($"[{LogTag}] {logIdentifier} {message}");
			else
				Logger.Warn($"[{LogTag}] {message}");
		}

		private static void LogInfo(string message, string logIdentifier)
		{
			if (!string.IsNullOrEmpty(logIdentifier))
				Logger.Info($"[{LogTag}] {logIdentifier} {message}");
			else
				Logger.Info($"[{LogTag}] {message}");
		}

		#endregion Logging
	}
}
