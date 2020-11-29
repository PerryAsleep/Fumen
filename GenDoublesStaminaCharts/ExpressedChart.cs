using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.Converters;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
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
		private class ChartSearchNode : IEquatable<ChartSearchNode>
		{
			private static long IdCounter;

			private readonly long Id;
			public readonly GraphNode GraphNode;
			public readonly MetricPosition Position;
			public readonly int Depth;
			public readonly int Cost;
			public readonly ChartSearchNode PreviousNode;
			public readonly GraphLink PreviousLink;
			public Dictionary<GraphLink, HashSet<ChartSearchNode>> NextNodes = new Dictionary<GraphLink, HashSet<ChartSearchNode>>();

			public ChartSearchNode(
				GraphNode graphNode,
				MetricPosition position,
				int depth,
				int cost,
				ChartSearchNode previousNode,
				GraphLink previousLink)
			{
				Id = IdCounter++;
				GraphNode = graphNode;
				Position = position;
				Depth = depth;
				Cost = cost;
				PreviousNode = previousNode;
				PreviousLink = previousLink;
			}

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
		}

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

		public static ExpressedChart CreateFromSMEvents(List<Event> events, StepGraph stepGraph)
		{
			var expressedChart = new ExpressedChart();
			var root = new ChartSearchNode(stepGraph.Root, null, 0, 0, null, null);

			var numArrows = stepGraph.NumArrows;
			var currentState = new SearchState[numArrows];
			var lastMines = new MetricPosition[numArrows];
			var lastReleases = new MetricPosition[numArrows];
			var minesByArrow = new List<MineEvent>[numArrows];
			for (var a = 0; a < numArrows; a++)
			{
				currentState[a] = SearchState.Empty;
				lastMines[a] = new MetricPosition();
				lastReleases[a] = new MetricPosition();
				minesByArrow[a] = new List<MineEvent>();
			}

			var eventIndex = 0;
			var numEvents = events.Count;
			var currentSearchNodes = new HashSet<ChartSearchNode> { root };
			var depth = 0;
			while (true)
			{
				// Reached the end
				if (eventIndex >= numEvents)
				{
					// Choose path with lowest cost.
					ChartSearchNode bestNode = null;
					foreach (var node in currentSearchNodes)
						if (bestNode == null || node.Cost < bestNode.Cost)
							bestNode = node;

					// Remove any nodes that are not chosen so there is only one path through the chart.
					foreach (var node in currentSearchNodes)
					{
						if (node.Equals(bestNode))
							continue;

						var currentNode = node;
						// Prune up until parent that shares an unpruned path
						while (currentNode.PreviousNode != null)
						{
							currentNode.PreviousNode.NextNodes[currentNode.PreviousLink].Remove(currentNode);
							if (currentNode.PreviousNode.NextNodes[currentNode.PreviousLink].Count == 0)
								currentNode.PreviousNode.NextNodes.Remove(currentNode.PreviousLink);
							if (currentNode.PreviousNode.NextNodes.Count != 0)
								break;
							currentNode = currentNode.PreviousNode;
						}
					}

					// Stop looping.
					break;
				}

				var childSearchNodes = new HashSet<ChartSearchNode>();

				ParseNextEvents(events, ref eventIndex, out var releases, out var mines, out var steps);

				// Process Releases
				if (releases.Count > 0)
				{
					// Update state
					foreach (var releaseEvent in releases)
					{
						currentState[releaseEvent.Lane] = SearchState.Empty;
						lastReleases[releaseEvent.Lane] = releaseEvent.Position;
					}

					// Add
					// TODO: method
					foreach (var searchNode in currentSearchNodes)
					{
						foreach (var l in searchNode.GraphNode.Links)
						{
							foreach (var childNode in l.Value)
							{
								if (!DoesStateMatch(currentState, childNode))
									continue;

								var cost = GetCost(l.Key, searchNode, currentState, lastMines, lastReleases, stepGraph.ArrowData);

								var childSearchNode = new ChartSearchNode(
									childNode,
									releases[0].Position,
									depth,
									searchNode.Cost + cost,
									searchNode,
									l.Key);

								AddChildSearchNode(searchNode, l.Key, childSearchNode, childSearchNodes);
							}
						}
					}

					// Prune
					currentSearchNodes = Prune(childSearchNodes);
					depth++;
				}

				// Get mines and record them
				if (mines.Count > 0)
				{
					foreach (var mineNote in mines)
					{
						var mineEvent = new MineEvent { Position = mineNote.Position };

						lastMines[mineNote.Lane] = mineEvent.Position;
						minesByArrow[mineNote.Lane].Add(mineEvent);
						expressedChart.MineEvents.Add(mineEvent);
					}
				}

				// Get taps/holds/rolls
				if (steps.Count > 0)
				{
					// Update state
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

					// Add
					foreach (var searchNode in currentSearchNodes)
					{
						foreach (var l in searchNode.GraphNode.Links)
						{
							foreach (var childNode in l.Value)
							{
								if (!DoesStateMatch(currentState, childNode))
									continue;

								var cost = GetCost(l.Key, searchNode, currentState, lastMines, lastReleases, stepGraph.ArrowData);

								var childSearchNode = new ChartSearchNode(
									childNode,
									releases[0].Position,
									depth,
									searchNode.Cost + cost,
									searchNode,
									l.Key);

								AddChildSearchNode(searchNode, l.Key, childSearchNode, childSearchNodes);
							}
						}
					}

					// Prune
					currentSearchNodes = Prune(childSearchNodes);
					depth++;
				}

				// Taps only last for a moment, clear them out before continuing.
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

			SetExpressedChartEvents(expressedChart, numArrows, root, minesByArrow);

			return expressedChart;
		}

		private static void SetExpressedChartEvents(
			ExpressedChart expressedChart,
			int numArrows,
			ChartSearchNode root,
			List<MineEvent>[] minesByArrow)
		{
			var lastReleases = new MetricPosition[numArrows];
			var lastFeet = new int[numArrows];
			var lastMineIndices = new int[numArrows];
			var lastReleaseDepth = new int[numArrows];
			// The arrow at the given index is the nth most recently released (closest in the past) arrow relative to other arrows.
			var nthMostRecentRelease = new int[numArrows];
			// The arrow at the given index is the nth least recently released (furthest in the past) arrow relative to other arrows.
			var nthLeastRecentRelease = new int[numArrows];
			for (var a = 0; a < numArrows; a++)
			{
				lastFeet[a] = InvalidFoot;
				lastReleaseDepth[a] = -1;
			}

			// Current node for iterating through the chart.
			var stepNode = root;

			// The first node is the resting position and not an event in tdhe chart.
			var previousLink = stepNode.NextNodes.First().Key;
			stepNode = stepNode.NextNodes.First().Value.First();

			while (stepNode != null)
			{
				// Create a new StepEvent for this step ChartSearchNode for adding to the ExpressedChart.
				var stepEvent = new StepEvent { Position = stepNode.Position };

				// Process AfterArrow mines.
				// This is the most preferable way to describe a mine.
				for (var a = 0; a < numArrows; a++)
				{
					// Skip if this arrow has not had a release yet.
					if (lastReleases[a] != null)
						continue;
					while (lastMineIndices[a] < minesByArrow[a].Count)
					{
						if (minesByArrow[a][lastMineIndices[a]].Position <= stepNode.Position)
						{
							minesByArrow[a][lastMineIndices[a]].Type = MineType.AfterArrow;
							minesByArrow[a][lastMineIndices[a]].ArrowIsNthClosest = nthMostRecentRelease[a];
							minesByArrow[a][lastMineIndices[a]].FootAssociatedWithPairedNote = lastFeet[a];
							lastMineIndices[a]++;
						}
					}
				}

				// Find if any feet were released on this stepNode.
				for (var f = 0; f < NumFeet; f++)
				{
					for (var a = 0; a < MaxArrowsPerFoot; a++)
					{
						// This is a release.
						if (stepNode.GraphNode.State[f, a].Arrow != InvalidArrowIndex
							&& stepNode.GraphNode.State[f, a].State == GraphArrowState.Resting
							&& previousLink.Links[f, a].Valid
							&& (previousLink.Links[f, a].Action == FootAction.Release ||
								previousLink.Links[f, a].Action == FootAction.Tap))
						{
							lastReleaseDepth[stepNode.GraphNode.State[f, a].Arrow] = stepNode.Depth;
							lastFeet[stepNode.GraphNode.State[f, a].Arrow] = f;
						}
					}
				}
				// Update the trackers for how recent each arrow is.
				for (var a = 0; a < numArrows; a++)
				{
					nthMostRecentRelease[a] = 0;
					nthLeastRecentRelease[a] = 0;
					for (var other = 0; other < numArrows; other++)
					{
						if (other == a || lastReleaseDepth[other] == -1 || lastReleaseDepth[a] == -1)
							continue;
						if (lastReleaseDepth[a] < lastReleaseDepth[other])
							nthMostRecentRelease[a]++;
						else if (lastReleaseDepth[a] > lastReleaseDepth[other])
							nthLeastRecentRelease[a]++;
					}
				}

				// TODO: Is it correct to record least recent releases for BeforeArrow types?
				// Shouldn't we be recording stepping onto an arrow, and not releasing for it?

				// Update the lastReleases for released arrows, and process any mines occurring before the 
				// first release.
				for (var arrow = 0; arrow < numArrows; arrow++)
				{
					if (lastReleaseDepth[arrow] != stepNode.Depth)
						continue;

					// If this is the first stepNode for this arrow, process mines occurring before this stepNode as BeforeArrow.
					if (lastReleases[arrow] == null)
					{
						while (lastMineIndices[arrow] < minesByArrow[arrow].Count)
						{
							if (minesByArrow[arrow][lastMineIndices[arrow]].Position >= stepNode.Position)
								break;

							minesByArrow[arrow][lastMineIndices[arrow]].Type = MineType.BeforeArrow;
							// When using BeforeArrow types, we want to know how least recent this arrow is.
							// This arrow is always the 0th most recent here, since it was just released.
							minesByArrow[arrow][lastMineIndices[arrow]].ArrowIsNthClosest = nthLeastRecentRelease[arrow];
							// lastFeet[arrow] has been updated above for this stepNode's arrow.
							minesByArrow[arrow][lastMineIndices[arrow]].FootAssociatedWithPairedNote = lastFeet[arrow];
							lastMineIndices[arrow]++;
						}
					}

					// Update last release for this arrow.
					lastReleases[arrow] = stepNode.Position;
				}

				// Set up the Link for the StepEvent and advance to the next ChartSearchNode.
				if (stepNode.NextNodes.Count > 0)
				{
					var linkEntry = stepNode.NextNodes.First();
					stepEvent.Link = linkEntry.Key;
					previousLink = linkEntry.Key;
					stepNode = linkEntry.Value.First();
				}
				else
				{
					stepNode = null;
				}

				// Record the StepEvent.
				expressedChart.StepEvents.Add(stepEvent);
			}

			// Process any remaining mines which may have occurred after the last step.
			for (var a = 0; a < numArrows; a++)
			{
				// It is possible for there to be no steps for an arrow. In that case the MineType
				// will remain the default NoArrow type.
				if (lastReleases[a] != null)
					continue;

				while (lastMineIndices[a] < minesByArrow[a].Count)
				{
					minesByArrow[a][lastMineIndices[a]].Type = MineType.AfterArrow;
					minesByArrow[a][lastMineIndices[a]].ArrowIsNthClosest = nthMostRecentRelease[a];
					minesByArrow[a][lastMineIndices[a]].FootAssociatedWithPairedNote = lastFeet[a];
					lastMineIndices[a]++;
				}
			}
		}

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
				else if (events[eventIndex] is LaneNote ln && ln.SourceType == SMCommon.NoteType.Mine.ToString())
					mines.Add(ln);
				else if (events[eventIndex] is LaneHoldStartNote lhsn)
					steps.Add(lhsn);
				else if (events[eventIndex] is LaneTapNote ltn)
					steps.Add(ltn);
				eventIndex++;
			}
		}

		private enum SearchState
		{
			Empty,
			Tap,
			Hold,
			Holding,
			Roll,
			Rolling
		}

		private static bool DoesArrowStateMatch(SearchState searchState, GraphArrowState graphArrowState)
		{
			if ((searchState == SearchState.Empty || searchState == SearchState.Tap)
				&& graphArrowState == GraphArrowState.Resting)
				return true;
			if ((searchState == SearchState.Hold || searchState == SearchState.Holding)
				&& graphArrowState == GraphArrowState.Held)
				return true;
			if ((searchState == SearchState.Roll || searchState == SearchState.Rolling)
				&& graphArrowState == GraphArrowState.Rolling)
				return true;
			return false;
		}

		private static bool DoesStateMatch(SearchState[] searchState, GraphNode node)
		{
			var checkedArrows = new bool[searchState.Length];
			for (var f = 0; f < NumFeet; f++)
			{
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (node.State[f, a].Arrow == InvalidArrowIndex)
						continue;
					if (!DoesArrowStateMatch(searchState[node.State[f, a].Arrow], node.State[f, a].State))
						return false;
					checkedArrows[node.State[f, a].Arrow] = true;
				}
			}

			for (var a = 0; a < searchState.Length; a++)
			{
				if (checkedArrows[a])
					continue;
				if (searchState[a] != SearchState.Empty)
					return false;
			}

			return true;
		}

		private static void AddChildSearchNode(ChartSearchNode node, GraphLink link, ChartSearchNode child, HashSet<ChartSearchNode> childSearchNodes)
		{
			if (!node.NextNodes.TryGetValue(link, out var childNodes))
			{
				childNodes = new HashSet<ChartSearchNode>();
				node.NextNodes[link] = childNodes;
			}
			childNodes.Add(child);
			childSearchNodes.Add(child);
		}

		private static HashSet<ChartSearchNode> Prune(HashSet<ChartSearchNode> nodes)
		{
			var bestNodes = new Dictionary<GraphNode, ChartSearchNode>();
			foreach (var node in nodes)
			{
				if (bestNodes.TryGetValue(node.GraphNode, out var currentNode))
				{
					if (node.Cost < currentNode.Cost)
					{
						// Prune up until parent that shares an unpruned path
						while (currentNode.PreviousNode != null)
						{
							currentNode.PreviousNode.NextNodes[currentNode.PreviousLink].Remove(currentNode);
							if (currentNode.PreviousNode.NextNodes[currentNode.PreviousLink].Count == 0)
								currentNode.PreviousNode.NextNodes.Remove(currentNode.PreviousLink);
							if (currentNode.PreviousNode.NextNodes.Count != 0)
								break;
							currentNode = currentNode.PreviousNode;
						}

						currentNode = node;
					}
				}
				else
				{
					currentNode = node;
				}
				bestNodes[currentNode.GraphNode] = currentNode;
			}

			return bestNodes.Values.ToHashSet();
		}

		private static GraphLink GetPreviousStepLink(ChartSearchNode node)
		{
			if (node == null)
				return null;
			while (node.PreviousNode != null)
			{
				node = node.PreviousNode;
				foreach (var linkEntry in node.NextNodes)
				{
					var link = linkEntry.Key;
					for (var f = 0; f < NumFeet; f++)
					{
						for (var a = 0; a < MaxArrowsPerFoot; a++)
						{
							if (link.Links[f, a].Valid && link.Links[f, a].Action != FootAction.Release)
								return link;
						}
					}
				}
			}
			return null;
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
			out bool canBracketToNewArrow,
			out MetricPosition minePositionFollowingPreviousStep,
			out MetricPosition releasePositionOfPreviousStep)
		{
			anyHeld = false;
			allHeld = true;
			canStepToNewArrow = false;
			canBracketToNewArrow = false;
			minePositionFollowingPreviousStep = null;
			releasePositionOfPreviousStep = new MetricPosition();

			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (previousState[foot, a].Arrow != InvalidArrowIndex)
				{
					if (previousState[foot, a].State == GraphArrowState.Held ||
						previousState[foot, a].State == GraphArrowState.Rolling)
					{
						anyHeld = true;
						canStepToNewArrow = arrowData[previousState[foot, a].Arrow].ValidNextArrows[arrow];
						canBracketToNewArrow = arrowData[previousState[foot, a].Arrow].BracketablePairings[foot][arrow];
					}
					else
					{
						allHeld = false;
						if (previousState[foot, a].State == GraphArrowState.Resting)
						{
							if (!anyHeld)
							{
								canStepToNewArrow = arrowData[previousState[foot, a].Arrow].ValidNextArrows[arrow];
								canBracketToNewArrow = arrowData[previousState[foot, a].Arrow].BracketablePairings[foot][arrow];
							}

							if (lastMines[previousState[foot, a].Arrow] != null
								&& lastMines[previousState[foot, a].Arrow] > lastReleases[previousState[foot, a].Arrow])
								minePositionFollowingPreviousStep = lastMines[previousState[foot, a].Arrow];
							releasePositionOfPreviousStep = lastReleases[previousState[foot, a].Arrow];
						}
					}
				}
				else
				{
					allHeld = false;
				}
			}

			if (allHeld)
			{
				canStepToNewArrow = false;
				canBracketToNewArrow = false;
			}
		}

		private static bool GetSingleStepStepAndFoot(GraphLink link, out SingleStepType step, out int foot)
		{
			step = SingleStepType.SameArrow;
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

		private static bool GetBracketStepAndFoot(GraphLink link, out SingleStepType step, out int foot)
		{
			step = SingleStepType.BracketBothNew;
			foot = 0;
			var numValid = 0;
			for (var f = 0; f < NumFeet; f++)
			{
				if (link.Links[f, 0].Valid && (link.Links[f, 0].Step == SingleStepType.BracketBothNew
					|| link.Links[f, 0].Step == SingleStepType.BracketOneNew
					|| link.Links[f, 0].Step == SingleStepType.BracketBothSame))
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
							&& state[a] == SearchState.Empty)
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
			bool otherCanBracketToNewArrow,
			bool thisCanBracketToNewArrow,
			MetricPosition otherMinePositionFollowingPreviousStep,
			MetricPosition thisMinePositionFollowingPreviousStep,
			MetricPosition otherReleasePositionOfPreviousStep,
			MetricPosition thisReleasePositionOfPreviousStep)
		{
			if (!otherCanStepToNewArrow)
				return 0;

			// Mine indication for only other foot to make this step.
			if (otherMinePositionFollowingPreviousStep != null && thisMinePositionFollowingPreviousStep == null)
				return 0;

			// Mine indication for both but other foot is sooner
			if (otherMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep != null
				&& otherMinePositionFollowingPreviousStep > thisMinePositionFollowingPreviousStep)
				return 0;

			// Mine indication for both but this foot is sooner
			if (otherMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep != null
				&& thisMinePositionFollowingPreviousStep > otherMinePositionFollowingPreviousStep)
				return 0;

			// Mine indication for only this foot to make this step.
			if (thisMinePositionFollowingPreviousStep != null && otherMinePositionFollowingPreviousStep == null)
				return 0;

			// Release indication for other foot
			if (otherReleasePositionOfPreviousStep > thisReleasePositionOfPreviousStep)
				return 0;

			// Release indication for this foot
			if (thisReleasePositionOfPreviousStep > otherReleasePositionOfPreviousStep)
				return 0;

			// The other foot is bracketable to this arrow and this foot is not
			if (otherCanBracketToNewArrow && !thisCanBracketToNewArrow)
				return 0;

			// Equal choice
			return 0;
		}

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
			MetricPosition[] lastMines,
			MetricPosition[] lastReleases,
			ArrowData[] arrowData)
		{
			// TODO: Real costs.

			// Releases
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (link.Links[f, a].Valid && link.Links[f, a].Action == FootAction.Release)
						return 0;

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

			GraphLink previousStepLink = GetPreviousStepLink(parentSearchNode);

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
							out var thisCanBracketToNewArrow,
							out var thisMinePositionFollowingPreviousStep,
							out var thisReleasePositionOfPreviousStep);
						GetOneArrowStepInfo(otherFoot, thisArrow, lastMines, lastReleases, arrowData, previousState,
							out var otherAnyHeld,
							out var otherAllHeld,
							out var otherCanStepToNewArrow,
							out var otherCanBracketToNewArrow,
							out var otherMinePositionFollowingPreviousStep,
							out var otherReleasePositionOfPreviousStep);
						var doubleStep = previousStepLink != null && previousStepLink.IsStepWithFoot(thisFoot) && !otherAnyHeld;

						switch (step)
						{
							case SingleStepType.SameArrow:
								return 0;
							case SingleStepType.NewArrow:
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
												return 0;
											else
												return 0;
										}

										// Both feet are holding one arrow.
										// One foot needs to bracket.
										if (thisAnyHeld)
										{
											// Ambiguous bracket
											if (otherCanBracketToNewArrow)
											{
												// Is this better?
												if (previousStepLink.IsStepWithFoot(otherFoot))
												{
													return 0;
												}

												if (previousStepLink.IsJump())
												{
													return GetCostNewArrowStepFromJump(
														otherCanStepToNewArrow,
														otherCanBracketToNewArrow,
														thisCanBracketToNewArrow,
														otherMinePositionFollowingPreviousStep,
														thisMinePositionFollowingPreviousStep,
														otherReleasePositionOfPreviousStep,
														thisReleasePositionOfPreviousStep);
												}


												return 0;
											}

											// Only this foot can bracket
											return 0;
										}

										// The other foot is holding but this foot is not holding
										else
										{
											// If the other foot can bracket this arrow and this foot can't should
											// we prefer the bracket? I don't think so.
											if (otherCanBracketToNewArrow && !thisCanBracketToNewArrow)
												return 0;

											return 0;
										}
									}

									// Bracket step
									if (thisAnyHeld)
									{
										return 7;
									}
									// No bracketing or holds

									if (doubleStep)
									{
										// Mine indicated
										if (thisMinePositionFollowingPreviousStep != null)
											return 50;

										// No indication
										return 100;
									}

									// No previous step
									if (previousStepLink == null)
									{
										// The other foot is bracketable to this arrow and this foot is not
										if (otherCanBracketToNewArrow && !thisCanBracketToNewArrow)
											return 1;
										// Both feet are steppable but not bracketable
										if (otherCanStepToNewArrow && !thisCanBracketToNewArrow)
											return 1;
										// Only this foot can make the step.
										if (!otherCanStepToNewArrow)
											return 0;
										return 1;
									}

									// Previous step was with the other foot.
									if (previousStepLink.IsStepWithFoot(otherFoot))
									{
										return 0;
									}

									// Jump into a new arrow. This could be done with either foot.
									// Rank by which is more natural
									if (previousStepLink.IsJump())
									{
										return GetCostNewArrowStepFromJump(
											otherCanStepToNewArrow,
											otherCanBracketToNewArrow,
											thisCanBracketToNewArrow,
											otherMinePositionFollowingPreviousStep,
											thisMinePositionFollowingPreviousStep,
											otherReleasePositionOfPreviousStep,
											thisReleasePositionOfPreviousStep);
									}

									// Unreachable? step with same foot that is not a double step or a same arrow step
									return 0;
								}
							case SingleStepType.CrossoverFront:
							case SingleStepType.CrossoverBehind:
								{
									if (otherAnyHeld)
										return 5;

									if (doubleStep)
									{
										// Mine indicated
										if (thisMinePositionFollowingPreviousStep != null)
											return 100;

										// No indication
										return 200;
									}

									return 25;
								}
							case SingleStepType.FootSwap:
								{
									if (doubleStep && thisMinePositionFollowingPreviousStep == null)
										return 100;

									// Mine indicated
									if (thisMinePositionFollowingPreviousStep != null)
										return 15;

									// If previous was swap
									if (previousStepLink.IsFootSwap())
										return 20;

									// No indication
									return 30;
								}
							default:
								return 0;
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
						// of the new arrows, and the new set of arrows is bracketable by the other foot, we should
						// prefer the bracket.
						var preferBracketDueToAmountOfMovement = false;
						if (previousStepLink != null)
						{
							for (var f = 0; f < NumFeet; f++)
							{
								if (previousStepLink.IsStepWithFoot(f)
									&& couldBeBracketed[OtherFoot(f)]
									&& newArrowIfThisFootSteps[f])
								{
									preferBracketDueToAmountOfMovement = true;
								}
							}
						}

						// Bracket
						if (GetBracketStepAndFoot(link, out var step, out var foot))
						{
							var otherFoot = OtherFoot(foot);

							// If the other foot is holding all possible arrows, there is no choice.
							if (holdingAll[otherFoot])
								return 0;

							// If this is a double step we should prefer the jump
							// A double step is fairly normal into a jump, but less normal into a bracket
							var doubleStep = previousStepLink != null
											 && previousStepLink.IsStepWithFoot(foot)
											 && !holdingAny[otherFoot]
											 && step != SingleStepType.BracketBothSame;
							if (doubleStep)
								return 100;

							if (preferBracketDueToAmountOfMovement)
								return 5;

							return 10;
						}

						// Jump
						else if (link.IsJump())
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
								return 0;
							// If only one foot is holding one and the two new arrows are not bracketable
							// by the other foot, we should prefer the jump.
							if (onlyFootHoldingOne != -1 && !couldBeBracketed[OtherFoot(onlyFootHoldingOne)])
								return 0;

							// No hold.

							if (preferBracketDueToAmountOfMovement)
								return 0;

							return 0;
						}

						return 0;
					}

				case 3:
					// Bracket jump. The various ways to do this don't make an appreciable difference for cost.
					// All three arrow steps must be a jump, must be 2 arrows with one foot and 1 arrow with the other.
					// TODO: Same as quad, there is a choice
					return 0;

				case 4:
					// Quads don't have any choice.
					// TODO: That's not true. There are two quads, LLRR and LRLR
					return 0;
				default:
					return 0;
			}
		}
	}
}
