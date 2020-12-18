using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.Converters;
using static ChartGenerator.Constants;

namespace ChartGenerator
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
	/// </summary>
	public class ExpressedChart
	{
		/// <summary>
		/// Common data to the events which make up an ExpressedChart.
		/// </summary>
		public class ChartEvent
		{
			public MetricPosition Position;
		}

		/// <summary>
		/// Event representing all the steps occurring at a single Metric position in the chart.
		/// </summary>
		public class StepEvent : ChartEvent
		{
			/// <summary>
			/// GraphLink representing the all steps occurring at a single Metric position.
			/// This GraphLink is the Link to this Event as opposed to the link from this Event.
			/// This represents how the player got to this position.
			/// </summary>
			public GraphLink Link;
		}

		/// <summary>
		/// Event representing a single mine.
		/// </summary>
		public class MineEvent : ChartEvent
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
			/// Position in the SM Chart of this ChartSearchNode.
			/// </summary>
			public readonly MetricPosition Position;
			/// <summary>
			/// Cumulative Cost to reach this ChartSearchNode.
			/// </summary>
			public readonly int TotalCost;
			/// <summary>
			/// Cost to reach this ChartSearchNode from the previous node.
			/// </summary>
			public readonly int Cost;
			/// <summary>
			/// Previous ChartSearchNode.
			/// </summary>
			public readonly ChartSearchNode PreviousNode;
			/// <summary>
			/// The GraphLink from the previous ChartSearchNode that results in this ChartSearchNode.
			/// </summary>
			public readonly GraphLink PreviousLink;
			/// <summary>
			/// All possible next ChartSearchNodes.
			/// Key is the GraphLink leading out of the GraphNode.
			/// Value is set of all ChartSearchNodes possible from that GraphLink.
			/// </summary>
			public readonly Dictionary<GraphLink, HashSet<ChartSearchNode>> NextNodes = new Dictionary<GraphLink, HashSet<ChartSearchNode>>();

			public ChartSearchNode(
				GraphNode graphNode,
				MetricPosition position,
				int cost,
				int totalCost,
				ChartSearchNode previousNode,
				GraphLink previousLink)
			{
				Id = IdCounter++;
				GraphNode = graphNode;
				Position = position;
				Cost = cost;
				TotalCost = totalCost;
				PreviousNode = previousNode;
				PreviousLink = previousLink;
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
			public GraphLink GetPreviousStepLink(int nthPrevious = 1)
			{
				var node = this;
				while (node.PreviousNode != null)
				{
					var link = node.PreviousLink;
					for (var f = 0; f < NumFeet; f++)
					{
						for (var a = 0; a < MaxArrowsPerFoot; a++)
						{
							if (link.Links[f, a].Valid && link.Links[f, a].Action != FootAction.Release)
							{
								nthPrevious--;
								if (nthPrevious <= 0)
								{
									return link;
								}
							}
						}
					}
					node = node.PreviousNode;
				}
				return null;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
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
			public GraphNode GetGraphNode() { return GraphNode; }
			public GraphLink GetGraphLinkToNode() { return PreviousLink; }
			public MetricPosition GetPosition() { return Position; }
			#endregion
		}

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
			Hold,
			Holding,
			Roll,
			Rolling
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
		public List<MineEvent> MineEvents = new List<MineEvent>();

		/// <summary>
		/// Custom Comparer for MineEvents so the EffectiveChart uses a consistent order.
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
		/// <param name="events">List of Events from an SM Chart.</param>
		/// <param name="stepGraph">StepGraph to use for searching the Events.</param>
		/// <returns></returns>
		public static (ExpressedChart, ChartSearchNode) CreateFromSMEvents(List<Event> events, StepGraph stepGraph)
		{
			var root = new ChartSearchNode(stepGraph.Root, new MetricPosition(), 0, 0, null, null);

			var numArrows = stepGraph.NumArrows;
			var currentState = new SearchState[numArrows];
			var lastMines = new MetricPosition[numArrows];
			var lastReleases = new MetricPosition[numArrows];
			for (var a = 0; a < numArrows; a++)
			{
				currentState[a] = SearchState.Empty;
				lastMines[a] = new MetricPosition();
				lastReleases[a] = new MetricPosition();
			}

			var smMines = new List<LaneNote>();
			var eventIndex = 0;
			var numEvents = events.Count;
			var currentSearchNodes = new HashSet<ChartSearchNode> { root };
			
			while (true)
			{
				// Reached the end.
				if (eventIndex >= numEvents)
				{
					// Choose path with lowest cost.
					ChartSearchNode bestNode = null;
					foreach (var node in currentSearchNodes)
						if (bestNode == null || node.TotalCost < bestNode.TotalCost)
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
				ParseNextEvents(events, ref eventIndex, out var releases, out var mines, out var steps);

				// Process Releases.
				if (releases.Count > 0)
				{
					// Update state.
					foreach (var releaseEvent in releases)
					{
						currentState[releaseEvent.Lane] = SearchState.Empty;
						lastReleases[releaseEvent.Lane] = releaseEvent.Position;
					}

					// Add children and prune.
					currentSearchNodes = AddChildrenAndPrune(currentSearchNodes, currentState,
						releases[0].Position, stepGraph, lastMines, lastReleases);
				}

				// Get mines and record them for processing after the search is complete.
				if (mines.Count > 0)
				{
					smMines.AddRange(mines);
					foreach (var mineNote in mines)
						lastMines[mineNote.Lane] = mineNote.Position;
				}

				// Get taps, holds, and rolls.
				if (steps.Count > 0)
				{
					// Update state.
					foreach (var stepEvent in steps)
					{
						if (stepEvent is LaneTapNote)
							currentState[stepEvent.Lane] = SearchState.Tap;
						else if (stepEvent is LaneHoldStartNote lhsn)
						{
							if (lhsn.SourceType == SMCommon.SNoteChars[(int)SMCommon.NoteType.RollStart].ToString())
								currentState[stepEvent.Lane] = SearchState.Roll;
							else
								currentState[stepEvent.Lane] = SearchState.Hold;
						}
					}

					// Add children and prune.
					currentSearchNodes = AddChildrenAndPrune(currentSearchNodes, currentState,
						steps[0].Position, stepGraph, lastMines, lastReleases);
				}

				// Update the current state now that the events at this position have been processed.
				for (var a = 0; a < numArrows; a++)
				{
					if (currentState[a] == SearchState.Tap)
					{
						currentState[a] = SearchState.Empty;
						lastReleases[a] = steps[0].Position;
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

			// Now that the search is complete, add all the events to a new ExpressedChart.
			var expressedChart = new ExpressedChart();
			AddStepsToExpressedChart(expressedChart, root);
			AddMinesToExpressedChart(expressedChart, root, smMines, numArrows);
			return (expressedChart, root);
		}

		/// <summary>
		/// Parses the next events from the given List of Events that occur at or after the given
		/// eventIndex and occur at the same position into releases, steps, and mines. Updates
		/// eventIndex based on which events were parsed.
		/// </summary>
		/// <param name="events">List of Events to parse.</param>
		/// <param name="eventIndex">Current index into Events. Will be updated.</param>
		/// <param name="releases">List of LaneHoldEndNotes to hold all releases.</param>
		/// <param name="mines">List of LaneNotes to hold all mines.</param>
		/// <param name="steps">List of LaneNotes to hold all taps, holds, and rolls.</param>
		private static void ParseNextEvents(
			List<Event> events,
			ref int eventIndex,
			out List<LaneHoldEndNote> releases,
			out List<LaneNote> mines,
			out List<LaneNote> steps)
		{
			releases = new List<LaneHoldEndNote>();
			mines = new List<LaneNote>();
			steps = new List<LaneNote>();

			if (eventIndex >= events.Count)
				return;

			var pos = events[eventIndex].Position;
			while (eventIndex < events.Count && events[eventIndex].Position == pos)
			{
				if (events[eventIndex] is LaneHoldEndNote lhen)
					releases.Add(lhen);
				else if (events[eventIndex] is LaneNote ln
				         && ln.SourceType == SMCommon.SNoteChars[(int)SMCommon.NoteType.Mine].ToString())
					mines.Add(ln);
				else if (events[eventIndex] is LaneHoldStartNote lhsn)
					steps.Add(lhsn);
				else if (events[eventIndex] is LaneTapNote ltn)
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
		/// <param name="position">Position of the state.</param>
		/// <param name="stepGraph">StepGraph. Needed for cost determination.</param>
		/// <param name="lastMines">Position of last mines per arrow. Needed for cost determination.</param>
		/// <param name="lastReleases">Position of last releases per arrow. Needed for cost determination.</param>
		/// <returns>HashSet of all lowest cost ChartSearchNodes satisfying this state.</returns>
		private static HashSet<ChartSearchNode> AddChildrenAndPrune(
			HashSet<ChartSearchNode> currentSearchNodes,
			SearchState[] currentState,
			MetricPosition position,
			StepGraph stepGraph,
			MetricPosition[] lastMines,
			MetricPosition[] lastReleases)
		{
			var childSearchNodes = new HashSet<ChartSearchNode>();

			// Check every current ChartSearchNode.
			foreach (var searchNode in currentSearchNodes)
			{
				var deadEnd = true;

				// Check every GraphLink out of the ChartSearchNode's GraphNode.
				foreach (var l in searchNode.GraphNode.Links)
				{
					// Check every resulting child GraphNode.
					foreach (var childNode in l.Value)
					{
						// Most children will not match the new state. Ignore them.
						if (!DoesStateMatch(currentState, childNode, l.Key))
							continue;

						if (position == new MetricPosition
						    {
							    Measure = 68,
							    Beat = 3,
							    SubDivision = new Fraction(0, 1)
						    }
						    && !l.Key.Links[R, 0].Valid
						    && l.Key.Links[L, 0].Valid
						    && l.Key.Links[L, 1].Valid
						    && l.Key.Links[L, 0].Step == StepType.BracketBothSame
						    && StateMatches(searchNode.GraphNode, 0, GraphArrowState.Resting, 2, GraphArrowState.Resting,
							    1, GraphArrowState.Resting, 3, GraphArrowState.Resting)
						    && StateMatches(childNode, 0, GraphArrowState.Held, 2, GraphArrowState.Resting, 1,
							    GraphArrowState.Resting, 3, GraphArrowState.Resting))
						{
							int a = 1;
						}

						if (position == new MetricPosition
						    {
							    Measure = 69,
							    Beat = 0,
							    SubDivision = new Fraction(0, 1)
						    }
						    && !l.Key.Links[L, 0].Valid
						    && l.Key.Links[R, 0].Valid
						    && l.Key.Links[R, 0].Step == StepType.NewArrow
						    && StateMatches(searchNode.GraphNode, 0, GraphArrowState.Resting, 2, GraphArrowState.Resting,
							    -1, GraphArrowState.Resting, 1, GraphArrowState.Resting)
						    && StateMatches(childNode, 0, GraphArrowState.Resting, 2, GraphArrowState.Resting,
							    -1, GraphArrowState.Resting, 3, GraphArrowState.Resting))
						{
							int a = 1;
						}

						// This GraphLink and child GraphNode result in a matching state.
						// Determine the cost to go from this GraphLink to this GraphNode.
						var cost = GetCost(l.Key, searchNode, currentState, position, lastMines, lastReleases, stepGraph.ArrowData);

						// Record the result as a new ChartSearchNode to be checked for pruning once
						// all children have been determined.
						var childSearchNode = new ChartSearchNode(
							childNode,
							position,
							cost,
							searchNode.TotalCost + cost,
							searchNode,
							l.Key);
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

			// 0, 4, 8, 10
			// Child node is 8460
			// 0th has it, 4 nodes up
			// 0 cost 1363
			// which node has the path that we know ultimately gets accepted?
			if (position == new MetricPosition
			    {
				    Measure = 69,
				    Beat = 0,
				    SubDivision = new Fraction(1, 4)
			    })
			{
				int a = 1;
			}

			// Prune the children and return the results.
			return Prune(childSearchNodes);
		}

		/// <summary>
		/// Checks if the given array of SearchStates and the given GraphNode represent the same
		/// state.
		/// </summary>
		/// <param name="searchState">Array of SearchStates. One SearchState per arrow.</param>
		/// <param name="node">GraphNode with state per foot.</param>
		/// <param name="link">GraphLink that linked to the given node.</param>
		/// <returns>True if the two representations match and false if they do not.</returns>
		private static bool DoesStateMatch(SearchState[] searchState, GraphNode node, GraphLink link)
		{
			SearchState[] generatedState = new SearchState[searchState.Length];
			for (var s = 0; s < generatedState.Length; s++)
				generatedState[s] = SearchState.Empty;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (node.State[f, a].Arrow == InvalidArrowIndex)
						continue;

					// What was the step into
					bool acted = false;
					for (var la = 0; la < MaxArrowsPerFoot; la++)
					{
						if (!link.Links[f, la].Valid)
							continue;

						if (node.State[f, a].State == StateAfterAction(link.Links[f, la].Action))
						{
							switch (node.State[f, a].State)
							{
								case GraphArrowState.Held:
									generatedState[node.State[f, a].Arrow] = SearchState.Hold;
									break;
								case GraphArrowState.Rolling:
									generatedState[node.State[f, a].Arrow] = SearchState.Roll;
									break;
								case GraphArrowState.Resting:
									if (link.Links[f, la].Action == FootAction.Release)
										generatedState[node.State[f, a].Arrow] = SearchState.Empty;
									else
										generatedState[node.State[f, a].Arrow] = SearchState.Tap;
									break;
							}

							acted = true;
						}
					}

					if (!acted)
					{
						switch (node.State[f, a].State)
						{
							case GraphArrowState.Held:
								generatedState[node.State[f, a].Arrow] = SearchState.Holding;
								break;
							case GraphArrowState.Rolling:
								generatedState[node.State[f, a].Arrow] = SearchState.Rolling;
								break;
						}
					}
				}
			}

			for (var s = 0; s < generatedState.Length; s++)
				if (generatedState[s] != searchState[s])
					return false;
			return true;
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
		private static void AddChildSearchNode(ChartSearchNode parent, GraphLink link, ChartSearchNode child, HashSet<ChartSearchNode> childSearchNodes)
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
		/// only one ChartSearchNode per GraphNode representing the lowest cost ChartSearchNode.
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
					if (node.TotalCost < currentNode.TotalCost)
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
				if (node.Position == new MetricPosition
				    {
					    Measure = 68,
					    Beat = 3,
					    SubDivision = new Fraction(0, 1)
				    }
				    && !node.PreviousLink.Links[R, 0].Valid
				    && node.PreviousLink.Links[L, 0].Valid
				    && node.PreviousLink.Links[L, 1].Valid
				    && node.PreviousLink.Links[L, 0].Step == StepType.BracketBothSame
				    && StateMatches(node.GraphNode, 0, GraphArrowState.Held, 2, GraphArrowState.Resting, 1, GraphArrowState.Resting, 3, GraphArrowState.Resting))
				{
					int a = 1;
				}

				node.PreviousNode.NextNodes[node.PreviousLink].Remove(node);
				if (node.PreviousNode.NextNodes[node.PreviousLink].Count == 0)
					node.PreviousNode.NextNodes.Remove(node.PreviousLink);
				if (node.PreviousNode.NextNodes.Count != 0)
					break;
				node = node.PreviousNode;
			}
		}

		#region Cost Evaluation
		/// <summary>
		/// Determine the cost of a step represented by the given GraphLink to the
		/// given GraphNode from the given parent ChartSearchNode.
		/// The cost represents how unlikely it is that this step is the best step
		/// to take to perform the chart being searched. Higher values are worse.
		/// For example, a double-step has high cost.
		/// </summary>
		/// <param name="link"></param>
		/// <param name="parentSearchNode"></param>
		/// <param name="state"></param>
		/// <param name="lastMines"></param>
		/// <param name="lastReleases"></param>
		/// <param name="arrowData"></param>
		/// <returns></returns>
		private static int GetCost(
			GraphLink link,
			ChartSearchNode parentSearchNode,
			SearchState[] state,
			MetricPosition position,
			MetricPosition[] lastMines,
			MetricPosition[] lastReleases,
			ArrowData[] arrowData)
		{
			// Releases have a 0 cost.
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (link.Links[f, a].Valid && link.Links[f, a].Action == FootAction.Release)
						return CostRelease;

			// Determine how many steps are in this state.
			var numSteps = 0;
			var lastArrowStep = 0;
			for (var a = 0; a < state.Length; a++)
			{
				if (state[a] == SearchState.Tap || state[a] == SearchState.Hold || state[a] == SearchState.Roll)
				{
					numSteps++;
					lastArrowStep = a;
				}
			}

			GraphLink previousStepLink = parentSearchNode.GetPreviousStepLink();
			GraphLink previousPreviousStepLink = parentSearchNode.GetPreviousStepLink(2);

			switch (numSteps)
			{
				case 1:
					{
						var thisArrow = lastArrowStep;
						GetSingleStepStepAndFoot(link, out var step, out var thisFoot);
						var otherFoot = OtherFoot(thisFoot);
						var previousState = parentSearchNode.GraphNode.State;
						GetOneArrowStepInfo(thisFoot, thisArrow, lastMines, lastReleases, arrowData, previousState,
							out var thisAnyHeld,
							out var thisAllHeld,
							out var thisCanStepToNewArrow,
							out var thisCanStepToNewArrowWithoutCrossover,
							out var thisCanBracketToNewArrow,
							out var thisCanCrossoverToNewArrow,
							out var thisMinePositionFollowingPreviousStep,
							out var thisReleasePositionOfPreviousStep);
						GetOneArrowStepInfo(otherFoot, thisArrow, lastMines, lastReleases, arrowData, previousState,
							out var otherAnyHeld,
							out var otherAllHeld,
							out var otherCanStepToNewArrow,
							out var otherCanStepToNewArrowWithoutCrossover,
							out var otherCanBracketToNewArrow,
							out var otherCanCrossoverToNewArrow,
							out var otherMinePositionFollowingPreviousStep,
							out var otherReleasePositionOfPreviousStep);

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

						var tripleStep = doubleStep && previousPreviousStepLink != null && previousPreviousStepLink.IsStepWithFoot(thisFoot);

						// I think in all cases we should consider an arrow held if it released at this time.
						thisAnyHeld |= thisReleasePositionOfPreviousStep == position;
						otherAnyHeld |= otherReleasePositionOfPreviousStep == position;

						switch (step)
						{
							case StepType.SameArrow:
							{
								if (thisAnyHeld && !otherAnyHeld && otherCanStepToNewArrow)
									return CostSameArrow_OtherHoldingNone_ThisHeld_OtherCanStep;
								return CostSameArrow;
							}
							case StepType.NewArrow:
								{
									// TODO: give preference to alternating in long patters
									// For example,		LR, U, LR, D, LR, U, LR D
									// Should not be all L or all R on the single steps

									// TODO: very slight preference to right foot on downbeat if all else is equal?

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
											// TODO: There could be patterns where you roll two feet
											// while one foot holds a bracket. This isn't considering that.
											return CostNewArrow_OtherHoldingOne;
										}
									}

									// Bracket step with the other foot not holding
									if (thisAnyHeld)
									{
										// The other foot could make this step.
										if (otherCanStepToNewArrow)
											return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep;

										// The other foot cannot hit this arrow.
										if (doubleStep)
											return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep_DoubleStep;
										return CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep;
									}

									// No bracketing or holds

									if (doubleStep)
									{
										// When there are lots of double steps we want to promote alternating.
										// It is better to hit two double steps with two feet rather than two with
										// one foot.
										if (tripleStep)
											return CostNewArrow_TripleStep;

										// If the other foot released later than this one then we are double-stepping
										// out of a pattern where the author likely intended it.
										if (doubleStepOtherFootReleasedAtSameTime)
											return CostNewArrow_OtherHoldingOne;
										if (doubleStepOtherFootReleasedAfterThisFoot)
											return CostNewArrow_DoubleStepOtherFootReleasedLater;

										// Mine indicated
										if (thisMinePositionFollowingPreviousStep != null)
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
								{
									if (otherAnyHeld)
										return CostNewArrow_Crossover_OtherHeld;

									if (doubleStep)
									{
										// Mine indicated
										if (thisMinePositionFollowingPreviousStep != null)
											return CostNewArrow_Crossover_OtherFree_DoubleStep_MineIndicated;

										// No indication
										return CostNewArrow_Crossover_OtherFree_DoubleStep_NoIndication;
									}

									return CostNewArrow_Crossover;
								}
							case StepType.FootSwap:
								{
									if (doubleStep)
									{
										if (thisMinePositionFollowingPreviousStep == null)
											return CostNewArrow_FootSwap_DoubleStep_NoMineIndication;
										return CostNewArrow_FootSwap_DoubleStep_MineIndication;
									}

									// Mine indicated
									if (thisMinePositionFollowingPreviousStep != null)
										return CostNewArrow_FootSwap_MineIndication;

									// If previous was swap
									if (previousStepLink?.IsFootSwap(out _) ?? false)
										return CostNewArrow_FootSwap_SubsequentSwap;

									// No indication and bracketable.
									if (thisCanBracketToNewArrow)
										return CostNewArrow_FootSwap_NoIndication_Bracketable;

									// No indication and not bracketable
									return CostNewArrow_FootSwap_NoIndication_NotBracketable;
								}
							default:
								return CostUnknown;
						}
					}

				case 2:
					{

						var couldBeBracketed = new bool[NumFeet];
						var holdingAny = new bool[NumFeet];
						var holdingAll = new bool[NumFeet];
						var newArrowIfThisFootSteps = new bool[NumFeet];
						for (var f = 0; f < NumFeet; f++)
						{
							GetTwoArrowStepInfo(
								parentSearchNode,
								state,
								f,
								arrowData,
								out couldBeBracketed[f],
								out holdingAny[f],
								out holdingAll[f],
								out newArrowIfThisFootSteps[f]);
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
									&& newArrowIfThisFootSteps[OtherFoot(f)])
								{
									preferBracketDueToAmountOfMovement[f] = true;
									atLeastOneFootPrefersBracket = true;
								}
							}
						}

						// Evaluate Bracket
						if (GetBracketStepAndFoot(link, out var step, out var foot))
						{
							var otherFoot = OtherFoot(foot);

							// If the other foot is holding all possible arrows, there is no choice.
							if (holdingAll[otherFoot])
								return CostTwoArrows_Bracket_OtherFootHoldingBoth;

							// If this is a double step we should prefer the jump
							// A double step is fairly normal into a jump, but less normal into a bracket
							var doubleStep = previousStepLink != null
											 && previousStepLink.IsStepWithFoot(foot)
											 && !holdingAny[otherFoot]
											 && step != StepType.BracketBothSame;
							if (doubleStep)
								return CostTwoArrows_Bracket_DoubleStep;

							if (preferBracketDueToAmountOfMovement[foot])
								return CostTwoArrows_Bracket_PreferredDueToMovement;

							return CostTwoArrows_Bracket;
						}

						// Evaluate Jump
						if (link.IsJump())
						{
							var onlyFootHoldingOne = -1;
							for (var f = 0; f < NumFeet; f++)
							{
								if (holdingAny[f] && !holdingAll[f])
								{
									if (onlyFootHoldingOne == -1)
										onlyFootHoldingOne = f;
									else
									{
										onlyFootHoldingOne = -1;
										break;
									}
								}
							}

							// If only one foot is holding one, we should prefer a bracket if the two arrows
							// are bracketable by the other foot.
							if (onlyFootHoldingOne != -1 && couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
								return CostTwoArrows_Jump_OtherFootHoldingOne_ThisFootCouldBracket;
							// If only one foot is holding one and the two new arrows are not bracketable
							// by the other foot, we should prefer the jump.
							if (onlyFootHoldingOne != -1 && !couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
								return CostTwoArrows_Jump_OtherFootHoldingOne_NotBracketable;

							// No hold or both feet holding

							if (atLeastOneFootPrefersBracket)
								return CostTwoArrows_Jump_OneFootPrefersBracketToDueMovement;

							return CostTwoArrows_Jump;
						}

						return CostUnknown;
					}

				case 3:
					// Bracket jump. The various ways to do this don't make an appreciable difference for cost.
					// All three arrow steps must be a jump, must be 2 arrows with one foot and 1 arrow with the other.
					// TODO: Same as quad, there is a choice
					return CostThreeArrows;

				case 4:
					// Quads don't have any choice.
					// TODO: That's not true. There are two quads, LLRR and LRLR
					return CostFourArrows;
				default:
					return CostUnknown;
			}
		}

		

		private static void GetOneArrowStepInfo(
			int foot,
			int arrow,
			MetricPosition[] lastMines,
			MetricPosition[] lastReleases,
			ArrowData[] arrowData,
			GraphNode.FootArrowState[,] previousState,
			out bool anyHeld,
			out bool allHeld,
			out bool canStepToNewArrow,
			out bool canStepToNewArrowWithoutCrossover,
			out bool canBracketToNewArrow,
			out bool canCrossoverToNewArrow,
			out MetricPosition minePositionFollowingPreviousStep,
			out MetricPosition releasePositionOfPreviousStep)
		{
			anyHeld = false;
			allHeld = true;
			canStepToNewArrow = false;
			canStepToNewArrowWithoutCrossover = false;
			canBracketToNewArrow = false;
			minePositionFollowingPreviousStep = null;
			canCrossoverToNewArrow = false;
			releasePositionOfPreviousStep = new MetricPosition();

			var otherFoot = OtherFoot(foot);

			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (previousState[otherFoot, a].Arrow != InvalidArrowIndex)
				{
					canCrossoverToNewArrow |=
						arrowData[previousState[otherFoot, a].Arrow].OtherFootPairingsOtherFootCrossoverBehind[otherFoot][arrow]
						|| arrowData[previousState[otherFoot, a].Arrow].OtherFootPairingsOtherFootCrossoverFront[otherFoot][arrow];
					canStepToNewArrowWithoutCrossover |=
						arrowData[previousState[otherFoot, a].Arrow].OtherFootPairings[otherFoot][arrow];
				}

				if (previousState[foot, a].Arrow != InvalidArrowIndex)
				{
					if (previousState[foot, a].State == GraphArrowState.Held ||
						previousState[foot, a].State == GraphArrowState.Rolling)
					{
						anyHeld = true;
						canBracketToNewArrow = arrowData[previousState[foot, a].Arrow].BracketablePairings[foot][arrow];
					}
					else
					{
						allHeld = false;
						if (previousState[foot, a].State == GraphArrowState.Resting)
						{
							if (!anyHeld)
							{
								canBracketToNewArrow = arrowData[previousState[foot, a].Arrow].BracketablePairings[foot][arrow];
							}

							if (lastMines[previousState[foot, a].Arrow] != null
								&& lastMines[previousState[foot, a].Arrow] > lastReleases[previousState[foot, a].Arrow])
								minePositionFollowingPreviousStep = lastMines[previousState[foot, a].Arrow];

							// A foot could be coming from a bracket with multiple releases. In this case we want to
							// choose the latest.
							if (releasePositionOfPreviousStep < lastReleases[previousState[foot, a].Arrow])
								releasePositionOfPreviousStep = lastReleases[previousState[foot, a].Arrow];
						}
					}
				}
				else
				{
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

		private static bool GetSingleStepStepAndFoot(GraphLink link, out StepType step, out int foot)
		{
			step = StepType.SameArrow;
			foot = 0;
			var numValid = 0;
			for (var f = 0; f < NumFeet; f++)
			{
				if (link.Links[f, 0].Valid)
				{
					step = link.Links[f, 0].Step;
					foot = f;
					numValid++;
				}
			}
			return numValid == 1;
		}

		private static bool GetBracketStepAndFoot(GraphLink link, out StepType step, out int foot)
		{
			step = StepType.BracketBothNew;
			foot = 0;
			var numValid = 0;
			for (var f = 0; f < NumFeet; f++)
			{
				if (link.Links[f, 0].Valid && (link.Links[f, 0].Step == StepType.BracketBothNew
					|| link.Links[f, 0].Step == StepType.BracketOneNew
					|| link.Links[f, 0].Step == StepType.BracketBothSame))
				{
					step = link.Links[f, 0].Step;
					foot = f;
					numValid++;
				}
			}
			return numValid == 1;
		}

		private static void GetTwoArrowStepInfo(
			ChartSearchNode parentSearchNode,
			SearchState[] state,
			int foot,
			ArrowData[] arrowData,
			out bool couldBeBracketed,
			out bool holdingAny,
			out bool holdingAll,
			out bool newArrowIfThisFootSteps)
		{
			couldBeBracketed = false;
			holdingAny = false;
			holdingAll = false;
			newArrowIfThisFootSteps = false;

			// Determine if any are held by this foot
			if (parentSearchNode != null)
			{
				holdingAll = true;
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (parentSearchNode.GraphNode.State[foot, a].Arrow != InvalidArrowIndex)
					{
						if (parentSearchNode.GraphNode.State[foot, a].State == GraphArrowState.Held ||
							parentSearchNode.GraphNode.State[foot, a].State == GraphArrowState.Rolling)
						{
							holdingAny = true;
						}
						else
						{
							holdingAll = false;
						}
						if (parentSearchNode.GraphNode.State[foot, a].State == GraphArrowState.Resting
							&& state[parentSearchNode.GraphNode.State[foot, a].Arrow] == SearchState.Empty)
						{
							newArrowIfThisFootSteps = true;
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
				int steppedArrow = InvalidArrowIndex;
				for (var a = 0; a < state.Length; a++)
				{
					if (state[a] == SearchState.Tap || state[a] == SearchState.Hold || state[a] == SearchState.Roll)
					{
						numSteps++;
						if (steppedArrow != InvalidArrowIndex)
						{
							if (!arrowData[a].BracketablePairings[foot][steppedArrow]
								|| !arrowData[steppedArrow].BracketablePairings[foot][a])
								couldBeBracketed = false;
						}

						steppedArrow = a;
					}
				}
				if (numSteps != MaxArrowsPerFoot)
					couldBeBracketed = false;
			}

		}

		private static int GetCostNewArrowStepFromJump(
			bool otherCanStepToNewArrow,
			bool otherCanCrossoverToNewArrow,
			bool otherCanBracketToNewArrow,
			bool thisCanCrossoverToNewArrow,
			bool thisCanBracketToNewArrow,
			MetricPosition otherMinePositionFollowingPreviousStep,
			MetricPosition thisMinePositionFollowingPreviousStep,
			MetricPosition otherReleasePositionOfPreviousStep,
			MetricPosition thisReleasePositionOfPreviousStep)
		{
			if (!otherCanStepToNewArrow)
				return CostNewArrow_StepFromJump_OtherCannotStep;

			if (otherCanCrossoverToNewArrow)
				return CostNewArrow_StepFromJump_OtherCrossover;

			if (thisCanCrossoverToNewArrow)
				return CostNewArrow_StepFromJump_ThisCrossover;

			// Mine indication for only other foot to make this step.
			if (otherMinePositionFollowingPreviousStep != null && thisMinePositionFollowingPreviousStep == null)
				return CostNewArrow_StepFromJump_OtherMineIndicated_ThisNotMineIndicated;

			// Mine indication for both but other foot is sooner
			if (otherMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep != null
				&& otherMinePositionFollowingPreviousStep > thisMinePositionFollowingPreviousStep)
				return CostNewArrow_StepFromJump_BothMineIndicated_ThisSooner;

			// Mine indication for both but this foot is sooner
			if (otherMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep > otherMinePositionFollowingPreviousStep)
				return CostNewArrow_StepFromJump_BothMineIndicated_OtherSooner;

			// Mine indication for only this foot to make this step.
			if (thisMinePositionFollowingPreviousStep != null && otherMinePositionFollowingPreviousStep == null)
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
		/// <param name="expressedChart">ExpressedChart to add step events to.</param>
		/// <param name="rootSearchNode">Root ChartSearchNode.</param>
		private static void AddStepsToExpressedChart(ExpressedChart expressedChart, ChartSearchNode rootSearchNode)
		{
			// Current node for iterating through the chart.
			var searchNode = rootSearchNode;

			// The first node is the resting position and not an event in the chart.
			searchNode = searchNode.GetNextNode();

			while (searchNode != null)
			{
				// Create a new StepEvent for this step ChartSearchNode for adding to the ExpressedChart.
				var stepEvent = new StepEvent
				{
					Position = searchNode.Position,
					Link = searchNode.PreviousLink
				};

				// Set up the Link for the StepEvent and advance to the next ChartSearchNode.
				if (searchNode.NextNodes.Count > 0)
					searchNode = searchNode.NextNodes.First().Value.First();
				else
					searchNode = null;

				// Record the StepEvent.
				expressedChart.StepEvents.Add(stepEvent);
			}
		}

		/// <summary>
		/// Adds MineEvents for all given mines to the given ExpressedChart.
		/// This is run after the SM Chart has been searched and a single path of
		/// ChartSearchNodes has been generated.
		/// </summary>
		/// <param name="expressedChart">ExpressedChart to add mines to.</param>
		/// <param name="rootSearchNode">Root ChartSearchNode.</param>
		/// <param name="smMines">List of LaneNotes representing mines from the SM Chart.</param>
		/// <param name="numArrows">Number of arrows in the SM Chart.</param>
		private static void AddMinesToExpressedChart(
			ExpressedChart expressedChart,
			ChartSearchNode rootSearchNode,
			List<LaneNote> smMines,
			int numArrows)
		{
			// Create sorted lists of releases and steps.
			var stepEvents = new List<ChartSearchNode>();
			var chartSearchNode = rootSearchNode;
			while (chartSearchNode != null)
			{
				stepEvents.Add(chartSearchNode);
				chartSearchNode = chartSearchNode.GetNextNode();
			}
			var (releases, steps) = MineUtils.GetReleasesAndSteps(stepEvents, numArrows);

			var stepIndex = 0;
			var releaseIndex = -1;
			foreach (var smMineEvent in smMines)
			{
				// Advance the step and release indices to follow and precede the event respectively.
				while (stepIndex < steps.Count && steps[stepIndex].Position <= smMineEvent.Position)
					stepIndex++;
				while (releaseIndex + 1 < releases.Count && releases[releaseIndex + 1].Position < smMineEvent.Position)
					releaseIndex++;

				// Create and add a new MineEvent.
				var expressedMineEvent = MineUtils.CreateExpressedMineEvent(
					numArrows, releases, releaseIndex, steps, stepIndex, smMineEvent);
				expressedChart.MineEvents.Add(expressedMineEvent);
			}

			expressedChart.MineEvents.Sort(new MineEventComparer());
		}
	}
}
