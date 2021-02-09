using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fumen;
using Fumen.Converters;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	/// <summary>
	/// A PerformedChart is a series of events which describe how a Chart is played.
	/// This includes specifics about which feet hit which arrows in what ways.
	/// An ExpressedChart can be turned into a PerformedChart for a StepGraph.
	/// A PerformedChart can be used to generate an SM Chart.
	/// A PerformedChart's representation comes from GraphLinkInstances and
	/// GraphNodeInstances.
	/// TODO: Consider consolidating search logic with ExpressedChart?
	/// </summary>
	public class PerformedChart
	{
		private const string LogTag = "Performed Chart";

		/// <summary>
		/// Enumeration of states each arrow can be in at each position.
		/// Used to assist with translating a PerformedChart into an SM Chart.
		/// </summary>
		private enum PerformanceFootAction
		{
			None,
			Tap,
			Fake,
			Lift,
			Hold,
			Roll,
			Release
		}

		/// <summary>
		/// A PerformedChart contains a series of PerformanceNodes.
		/// Abstract base class for the various types of PerformanceNodes in a PerformedChart.
		/// </summary>
		public abstract class PerformanceNode
		{
			/// <summary>
			/// Position of this node in the Chart.
			/// </summary>
			public MetricPosition Position;
			/// <summary>
			/// Next PerformanceNode in the series.
			/// </summary>
			public PerformanceNode Next;
			/// <summary>
			/// Previous PerformanceNode in the series.
			/// </summary>
			public PerformanceNode Prev;
		}

		/// <summary>
		/// PerformanceNode representing a normal step or release.
		/// </summary>
		public class StepPerformanceNode : PerformanceNode, MineUtils.IChartNode
		{
			/// <summary>
			/// GraphNodeInstance representing the state at this PerformanceNode.
			/// </summary>
			public GraphNodeInstance GraphNodeInstance;
			/// <summary>
			/// GraphLinkInstance to the GraphNodeInstance at this PerformanceNode.
			/// </summary>
			public GraphLinkInstance GraphLinkInstance;

			#region MineUtils.IChartNode Implementation
			public GraphNode GetGraphNode() { return GraphNodeInstance?.Node; }
			public GraphLink GetGraphLinkToNode() { return GraphLinkInstance?.GraphLink; }
			public MetricPosition GetPosition() { return Position; }
			#endregion
		}

		/// <summary>
		/// PerformanceNode representing a mine.
		/// </summary>
		public class MinePerformanceNode : PerformanceNode
		{
			/// <summary>
			/// The lane or arrow this Mine occurs on.
			/// </summary>
			public int Arrow;
		}

		/// <summary>
		/// Search node for performing a search through an ExpressedChart to find the best PerformedChart.
		/// When searching each SearchNode has at most one previous SearchNode and potentially
		/// many next SearchNode, one for each valid GraphNode reachable from each valid
		/// GraphLink out of this node.
		/// When the search is complete each SearchNode will have at most one previous
		/// SearchNode and at most one next SearchNode.
		/// Each SearchNode has a unique Id even if it represents the same GraphNode
		/// and the position, so that all nodes can be stored and compared without
		/// conflicting.
		/// </summary>
		private class SearchNode : IEquatable<SearchNode>
		{
			private static long IdCounter;
			
			/// <summary>
			/// Unique identifier for preventing conflicts when storing SearchNodes in
			/// HashSets or other data structures that rely on the IEquatable interface.
			/// </summary>
			private readonly long Id;
			/// <summary>
			/// The GraphNode at this SearchNode.
			/// </summary>
			public readonly GraphNode GraphNode;
			/// <summary>
			/// The GraphLink from the Previous SearchNode that links to this SearchNode.
			/// </summary>
			public readonly GraphLink GraphLinkFromPreviousNode;
			/// <summary>
			/// The depth of this SearchNode.
			/// This depth can also index the ExpressedChart StepEvents for accessing the StepType.
			/// Depth is the index of this SearchNode in the sequence of nodes that make up the chart.
			/// Note that while there are N SearchNodes in a complete chart, there are N-1 StepEvents in
			/// the corresponding ExpressedChart since the StepEvents represent actions between nodes.
			/// The GraphLink out of SearchNode N is StepEvent N's LinkInstance.
			/// The GraphLink into SearchNode N is StepEvent N-1's LinkInstance.
			/// </summary>
			public readonly int Depth;
			/// <summary>
			/// The previous SearchNode.
			/// Used for backing up when hitting a dead end in a search.
			/// </summary>
			public readonly SearchNode PreviousNode;
			/// <summary>
			/// All the GraphLinks which are valid for linking out of this SearchNode and into the next SearchNodes.
			/// This is a List and not just one GraphLink due to configurable StepType replacements.
			/// See Config.StepTypeReplacements.
			/// </summary>
			public readonly List<GraphLink> GraphLinks;
			/// <summary>
			/// All the valid NextNodes out of this SearchNode.
			/// These are added during the search and pruned so at the end there is at most one next SearchNode.
			/// </summary>
			public readonly Dictionary<GraphLink, HashSet<SearchNode>> NextNodes = new Dictionary<GraphLink, HashSet<SearchNode>>();
			/// <summary>
			/// The Cost of this SearchNode for comparing to other SearchNodes in order to determine the best path.
			/// Higher values are worse.
			/// </summary>
			public readonly double Cost;
			/// <summary>
			/// The number of steps on each arrow up to and including this SearchNode.
			/// Used to determine Cost.
			/// </summary>
			private readonly int[] StepCounts;

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="graphNode">
			/// GraphNode representing the state of this SearchNode.
			/// </param>
			/// <param name="originalGraphLinkToNextNode">
			/// The original GraphLink to the next GraphNode.
			/// This will be used to determine which replacement GraphLinks are acceptable from
			/// this SearchNode.
			/// </param>
			/// <param name="graphLinkFromPreviousNode">
			/// The GraphLink to this SearchNode from the previous SearchNode.
			/// </param>
			/// <param name="depth">
			/// The 0-based depth of this SearchNode.
			/// </param>
			/// <param name="previousNode">
			/// The previous SearchNode.
			/// </param>
			/// <param name="steps">
			/// For each arrow, whether it was stepped on to arrive at this SearchNode from the previous
			/// SearchNode.
			/// </param>
			public SearchNode(
				GraphNode graphNode,
				GraphLink originalGraphLinkToNextNode,
				GraphLink graphLinkFromPreviousNode,
				int depth,
				SearchNode previousNode,
				bool[] steps)
			{
				Id = Interlocked.Increment(ref IdCounter);
				GraphNode = graphNode;
				GraphLinkFromPreviousNode = graphLinkFromPreviousNode;
				Depth = depth;
				PreviousNode = previousNode;

				StepCounts = new int[steps.Length];
				for (var a = 0; a < steps.Length; a++)
					StepCounts[a] = (steps[a] ? 1 : 0) + (previousNode?.StepCounts[a] ?? 0);
				
				// Get the GraphLinks to use as replacements for the original GraphLink
				GraphLinks = originalGraphLinkToNextNode == null ? new List<GraphLink>()
					: GraphLinkReplacementCache[originalGraphLinkToNextNode];

				Cost = DetermineCost(steps);
			}

			/// <summary>
			/// Gets the next ChartSearchNode.
			/// Assumes that the search is complete and there is at most one next SearchNode.
			/// </summary>
			/// <returns>The next SearchNode or null if none exists.</returns>
			public SearchNode GetNextNode()
			{
				if (NextNodes.Count == 0 || NextNodes.First().Value.Count == 0)
					return null;
				return NextNodes.First().Value.First();
			}

			/// <summary>
			/// Determines the Cost of this SearchNode.
			/// Higher values are worse.
			/// </summary>
			/// <param name="steps">
			/// Whether each arrow was stepped on in order to move from the previous SearchNode to this SearchNode.
			/// </param>
			/// <returns>Cost to use for this SearchNode.</returns>
			private double DetermineCost(bool[] steps)
			{
				// Determine how far off the chart up to this point is from the desired distribution of arrows.
				var weights = Config.Instance.GetOutputDesiredArrowWeightsNormalized();
				var totalSteps = 0;
				for (var a = 0; a < StepCounts.Length; a++)
					totalSteps += StepCounts[a];
				var totalDifferenceFromDesiredLanePercentage = 0.0;
				for (var a = 0; a < StepCounts.Length; a++)
					totalDifferenceFromDesiredLanePercentage += Math.Abs((double)StepCounts[a] / totalSteps - weights[a]);
				var deviationFromDesiredDistribution = totalDifferenceFromDesiredLanePercentage / StepCounts.Length;

				// TODO: Scale the deviation from the desired distribution so that we don't consider it too heavily
				// at the start of the chart where there aren't enough arrows for it to be meaningful.

				// TODO: Consider this step.

				return deviationFromDesiredDistribution;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is SearchNode n)
					return Equals(n);
				return false;
			}

			public bool Equals(SearchNode other)
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
		}

		/// <summary>
		/// Cache of GraphLink to all replacement GraphLink which can be used in a PerformedChart
		/// based on the StepTypeReplacements in Config and the available GraphLinks in the
		/// StepGraph for the OutputChartType.
		/// This is cached as a performance optimization so we do not need to construct the list for
		/// each node of a search.
		/// It is expected that this 
		/// </summary>
		private static readonly Dictionary<GraphLink, List<GraphLink>> GraphLinkReplacementCache = new Dictionary<GraphLink, List<GraphLink>>();

		/// <summary>
		/// Root PerformanceNode of the PerformedChart.
		/// </summary>
		public readonly PerformanceNode Root;
		/// <summary>
		/// Number of arrows in the Chart.
		/// </summary>
		public readonly int NumArrows;
		/// <summary>
		/// Identifier to use when logging messages about this PerformedChart.
		/// </summary>
		private readonly string LogIdentifier;

		/// <summary>
		/// Private constructor.
		/// PerformedCharts are created publicly though CreateFromExpressedChart.
		/// </summary>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <param name="root">Root PerformanceNode of the PerformedChart.</param>
		/// <param name="logIdentifier">
		/// Identifier to use when logging messages about this PerformedChart.
		/// </param>
		private PerformedChart(int numArrows, PerformanceNode root, string logIdentifier)
		{
			NumArrows = numArrows;
			Root = root;
			LogIdentifier = logIdentifier;
		}

		/// <summary>
		/// Creates a PerformedChart by iteratively searching for a series of GraphNodes that satisfy
		/// the given ExpressedChart's StepEvents.
		/// </summary>
		/// <param name="stepGraph">
		/// StepGraph representing all possible states that can be traversed.
		/// </param>
		/// <param name="rootNodes">
		/// Tiers of root GraphNodes to try as the root.
		/// Outer list expected to be sorted by how desirable the GraphNodes are with the
		/// first List being the most desirable GraphNodes and the last List being the least
		/// desirable GraphNodes. Inner Lists expected to contain GraphNodes of equal preference.
		/// </param>
		/// <param name="expressedChart">ExpressedChart to search.</param>
		/// <param name="logIdentifier">
		/// Identifier to use when logging messages about this PerformedChart.
		/// </param>
		/// <returns>
		/// PerformedChart satisfying the given ExpressedChart for the given StepGraph.
		/// </returns>
		public static PerformedChart CreateFromExpressedChart(
			StepGraph stepGraph,
			List<List<GraphNode>> rootNodes,
			ExpressedChart expressedChart,
			string logIdentifier)
		{
			if (GraphLinkReplacementCache.Count == 0)
			{
				LogError("Programmer Error. No cached GraphLink replacements. See CacheGraphLinks.", logIdentifier);
				return null;
			}

			if (rootNodes == null || rootNodes.Count < 1 || rootNodes[0] == null || rootNodes[0].Count < 1)
				return null;

			SearchNode rootSearchNode = null;
			GraphNode rootGraphNodeToUse = null;

			// HACK consistent seed
			var random = new Random(1);

			// Find a path of SearchNodes through the ExpressedChart.
			if (expressedChart.StepEvents.Count > 0)
			{
				// Try each tier of root nodes in order until we find a chart.
				var tier = -1;
				foreach (var currentTierOfRootNodes in rootNodes)
				{
					tier++;

					// Order the root nodes at this tier randomly since they are weighted evenly.
					var roots = currentTierOfRootNodes.OrderBy(a => random.Next()).ToList();

					// Try each root node.
					foreach (var rootGraphNode in roots)
					{
						var depth = 0;

						// Set up a root search node at the root GraphNode.
						rootSearchNode = new SearchNode(
							rootGraphNode,
							expressedChart.StepEvents[0].LinkInstance.GraphLink,
							null,
							depth,
							null,
							new bool[stepGraph.NumArrows]);
						var currentSearchNodes = new HashSet<SearchNode>();
						currentSearchNodes.Add(rootSearchNode);
						
						while (true)
						{
							// Finished
							if (depth >= expressedChart.StepEvents.Count)
							{
								// Choose path with lowest cost.
								SearchNode bestNode = null;
								foreach (var node in currentSearchNodes)
									if (bestNode == null || node.Cost < bestNode.Cost)
										bestNode = node;

								// Remove any nodes that are not chosen so there is only one path through the chart.
								foreach (var node in currentSearchNodes)
								{
									if (node.Equals(bestNode))
										continue;
									Prune(node);
								}

								rootGraphNodeToUse = rootGraphNode;
								break;
							}

							// Failed to find a path. Break out and try the next root.
							if (currentSearchNodes.Count == 0)
								break;

							// Accumulate the next level of SearchNodes by looping over each SearchNode
							// in the current set.
							var nextDepth = depth + 1;
							var nextSearchNodes = new HashSet<SearchNode>();
							foreach (var searchNode in currentSearchNodes)
							{
								// Check every GraphLink out of the SearchNode.
								var deadEnd = true;
								foreach (var graphLink in searchNode.GraphLinks)
								{
									// The GraphNode may not actually have this GraphLink due to
									// the StepTypeReplacements.
									if (!searchNode.GraphNode.Links.ContainsKey(graphLink))
										continue;
									
									// Check every GraphNode linked to by this GraphLink.
									foreach (var nextGraphNode in searchNode.GraphNode.Links[graphLink])
									{
										// Determine new step information.
										var actions = GetActionsForNode(nextGraphNode, graphLink, stepGraph.NumArrows);
										var steps = new bool[stepGraph.NumArrows];
										for (var a = 0; a < stepGraph.NumArrows; a++)
										{
											if (actions[a] == PerformanceFootAction.Tap
											    || actions[a] == PerformanceFootAction.Hold)
												steps[a] = true;
										}

										GraphLink graphLinkToNextNode = null;
										if (nextDepth < expressedChart.StepEvents.Count)
											graphLinkToNextNode = expressedChart.StepEvents[nextDepth].LinkInstance.GraphLink;

										// Set up a new SearchNode.
										var nextSearchNode = new SearchNode(
											nextGraphNode,
											graphLinkToNextNode,
											graphLink,
											nextDepth,
											searchNode,
											steps
										);

										// Do not consider this next SearchNode if it results in an invalid state.
										if (DoesNodeStepOnReleaseAtSamePosition(nextSearchNode, expressedChart, stepGraph.NumArrows))
											continue;

										// Update the previous SearchNode's NextNodes to include the new SearchNode.
										if (!searchNode.NextNodes.ContainsKey(graphLink))
											searchNode.NextNodes[graphLink] = new HashSet<SearchNode>();
										searchNode.NextNodes[graphLink].Add(nextSearchNode);

										// Add this node to the set of next SearchNodes to be pruned after they are all found.
										nextSearchNodes.Add(nextSearchNode);
										deadEnd = false;
									}
								}

								// This SearchNode has no valid children. Prune it.
								if (deadEnd)
									Prune(searchNode);
							}

							// Prune all the next SearchNodes, store them in currentSearchNodes, and advance.
							currentSearchNodes = Prune(nextSearchNodes);
							depth = nextDepth;
						}

						// If we found a path from a root GraphNode, then the search is complete.
						if (rootGraphNodeToUse != null)
							break;
					}

					// If we found a path from a root GraphNode, then the search is complete.
					if (rootGraphNodeToUse != null)
						break;
				}

				// If we exhausted all valid root GraphNodes and did not find a path, log an error
				// and return a null PerformedChart.
				if (rootGraphNodeToUse == null)
				{
					LogError("Unable to find performance.", logIdentifier);
					return null;
				}

				// Log a warning if we had to fall back to a worse tier of root GraphNodes.
				if (tier > 0)
				{
					LogWarn($"Using fallback root at tier {tier}.", logIdentifier);
				}
			}

			// Set up a new PerformedChart
			var performedChart = new PerformedChart(
				stepGraph.NumArrows,
				new StepPerformanceNode
				{
					Position = new MetricPosition(),
					GraphNodeInstance = new GraphNodeInstance { Node = rootGraphNodeToUse ?? rootNodes[0][0] },
				},
				logIdentifier);

			// Add the StepPerformanceNodes to the PerformedChart
			var currentPerformanceNode = performedChart.Root;
			var currentSearchNode = rootSearchNode;
			currentSearchNode = currentSearchNode?.GetNextNode();
			while (currentSearchNode != null)
			{
				// Create GraphNodeInstance.
				var stepEventIndex = currentSearchNode.Depth - 1;
				var graphNodeInstance = new GraphNodeInstance { Node = currentSearchNode.GraphNode };
				for (var f = 0; f < NumFeet; f++)
					for (var p = 0; p < NumFootPortions; p++)
						graphNodeInstance.InstanceTypes[f, p] = expressedChart.StepEvents[stepEventIndex].LinkInstance.InstanceTypes[f, p];

				// Create GraphLinkInstance.
				var graphLink = currentSearchNode.GraphLinkFromPreviousNode;
				GraphLinkInstance graphLinkInstance = new GraphLinkInstance { GraphLink = graphLink };
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						graphLinkInstance.InstanceTypes[f, p] =
							expressedChart.StepEvents[stepEventIndex].LinkInstance.InstanceTypes[f, p];
					}
				}

				// Add new StepPerformanceNode and advance.
				var newNode = new StepPerformanceNode
				{
					Position = expressedChart.StepEvents[stepEventIndex].Position,
					GraphLinkInstance = graphLinkInstance,
					GraphNodeInstance = graphNodeInstance,
					Prev = currentPerformanceNode
				};
				currentPerformanceNode.Next = newNode;
				currentPerformanceNode = newNode;
				currentSearchNode = currentSearchNode.GetNextNode();
			}
			var lastPerformanceNode = currentPerformanceNode;

			// Add Mines
			AddMinesToPerformedChart(performedChart, stepGraph, expressedChart, lastPerformanceNode);

			return performedChart;
		}

		private static HashSet<SearchNode> Prune(HashSet<SearchNode> nodes)
		{
			// Set up a Dictionary to track the best ChartSearchNode per GraphNode.
			var bestNodes = new Dictionary<GraphNode, SearchNode>();
			foreach (var node in nodes)
			{
				// There is already a best node for this GraphNode, compare them.
				if (bestNodes.TryGetValue(node.GraphNode, out var currentNode))
				{
					// This node is better.
					if (node.Cost < currentNode.Cost)
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

		private static void Prune(SearchNode node)
		{
			// Prune the node up until parent that has other children.
			while (node.PreviousNode != null)
			{
				node.PreviousNode.NextNodes[node.GraphLinkFromPreviousNode].Remove(node);
				if (node.PreviousNode.NextNodes[node.GraphLinkFromPreviousNode].Count == 0)
					node.PreviousNode.NextNodes.Remove(node.GraphLinkFromPreviousNode);
				if (node.PreviousNode.NextNodes.Count != 0)
					break;
				node = node.PreviousNode;
			}
		}

		/// <summary>
		/// Checks whether the given node has a step that occurs at the same time as a release on the same arrow.
		/// Some valid expressions might otherwise cause this kind of pattern to be generated in a PerformedChart
		/// but this does not represent a valid SM Chart. This can happen when there is a jump and the foot in
		/// question is supposed to jump on the same arrow but the previous step was a bracket so there are two
		/// arrows to choose from. We could apply the SameArrow step to the arrow which just released even though
		/// that is impossible in the ExpressedChart.
		/// </summary>
		/// <param name="node">The SearchNode to check.</param>
		/// <param name="expressedChart">The ExpressedChart so we can check GraphLinks.</param>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <returns>
		/// True if this SearchNode has a step that occurs at the same time as a release on the same arrow.
		/// </returns>
		private static bool DoesNodeStepOnReleaseAtSamePosition(SearchNode node, ExpressedChart expressedChart, int numArrows)
		{
			var previousNode = node.PreviousNode;
			if (previousNode == null)
				return false;
			var previousPreviousNode = previousNode.PreviousNode;
			if (previousPreviousNode == null)
				return false;

			// This node and the previous node must occur at the same time for the problem to arise.
			if (expressedChart.StepEvents[previousNode.Depth - 1].Position != expressedChart.StepEvents[node.Depth - 1].Position)
				return false;

			// Determine what actions are performed for both the current and previous node.
			var currentActions = GetActionsForNode(
				node.GraphNode,
				node.GraphLinkFromPreviousNode,
				numArrows);
			var previousActions = GetActionsForNode(
				previousNode.GraphNode,
				previousNode.GraphLinkFromPreviousNode,
				numArrows);

			// Check if the previous node released on the same arrow tha the current node is stepping on.
			for (var a = 0; a < numArrows; a++)
			{
				if (previousActions[a] == PerformanceFootAction.Release &&
				    currentActions[a] != PerformanceFootAction.None && currentActions[a] != PerformanceFootAction.Release)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Add Mines to the PerformedChart. Done after the steps are added since mine placement is
		/// relative to arrows in the chart. Mines are added to the end of the PerformanceNode list
		/// and sorted later.
		/// </summary>
		/// <param name="performedChart">PerformedChart to add mines for.</param>
		/// <param name="stepGraph">
		/// StepGraph representing all possible states that can be traversed.
		/// </param>
		/// <param name="expressedChart">ExpressedChart being used to generate the PerformedChart.</param>
		/// <param name="lastPerformanceNode">
		/// Last StepPerformanceNode in the PerformedChart. Used to append MinePerformanceNodes to
		/// the end.
		/// </param>
		private static void AddMinesToPerformedChart(
			PerformedChart performedChart,
			StepGraph stepGraph,
			ExpressedChart expressedChart,
			PerformanceNode lastPerformanceNode)
		{
			// Record which lanes have arrows in them.
			var numLanesWithArrows = 0;
			var lanesWithNoArrows = new bool[stepGraph.NumArrows];
			for (var a = 0; a < stepGraph.NumArrows; a++)
				lanesWithNoArrows[a] = true;
			var currentPerformanceNode = performedChart.Root;
			while (currentPerformanceNode != null)
			{
				if (currentPerformanceNode is StepPerformanceNode stepNode)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (stepNode.GraphNodeInstance.Node.State[f, p].Arrow != InvalidArrowIndex)
							{
								if (lanesWithNoArrows[stepNode.GraphNodeInstance.Node.State[f, p].Arrow])
								{
									lanesWithNoArrows[stepNode.GraphNodeInstance.Node.State[f, p].Arrow] = false;
									numLanesWithArrows++;
								}
							}
						}
					}
					if (numLanesWithArrows == stepGraph.NumArrows)
						break;
				}
				currentPerformanceNode = currentPerformanceNode.Next;
			}

			// Get the first lane with no arrow, if one exists.
			var firstLaneWithNoArrow = InvalidArrowIndex;
			for (var a = 0; a < stepGraph.NumArrows; a++)
			{
				if (!lanesWithNoArrows[a])
					continue;
				firstLaneWithNoArrow = a;
				break;
			}

			// Create sorted lists of releases.
			var stepEvents = new List<StepPerformanceNode>();
			currentPerformanceNode = performedChart.Root;
			while (currentPerformanceNode != null)
			{
				if (currentPerformanceNode is StepPerformanceNode stepNode)
					stepEvents.Add(stepNode);
				currentPerformanceNode = currentPerformanceNode.Next;
			}
			var (releases, steps) = MineUtils.GetReleasesAndSteps(stepEvents, stepGraph.NumArrows);

			// Add the MinePerformanceNodes to the PerformedChart.
			// For simplicity add all the nodes to the end. They will be sorted later.
			var stepIndex = 0;
			var releaseIndex = 0;
			MetricPosition previousMinePosition = null;
			var arrowsOccupiedByMines = new bool[stepGraph.NumArrows];
			foreach (var mineEvent in expressedChart.MineEvents)
			{
				// Advance the step and release indices to follow and precede the event respectively.
				while (stepIndex < steps.Count && steps[stepIndex].Position <= mineEvent.Position)
					stepIndex++;
				while (releaseIndex + 1 < releases.Count && releases[releaseIndex + 1].Position < mineEvent.Position)
					releaseIndex++;

				// Reset arrows occupied by mines if this mine is at a new position.
				if (previousMinePosition == null || previousMinePosition < mineEvent.Position)
				{
					for (var a = 0; a < stepGraph.NumArrows; a++)
						arrowsOccupiedByMines[a] = false;
				}
				previousMinePosition = mineEvent.Position;

				switch (mineEvent.Type)
				{
					case MineType.AfterArrow:
					case MineType.BeforeArrow:
					{
						var bestArrow = MineUtils.FindBestNthMostRecentArrow(
							mineEvent.Type == MineType.AfterArrow,
							mineEvent.ArrowIsNthClosest,
							mineEvent.FootAssociatedWithPairedNote,
							stepGraph.NumArrows,
							releases,
							releaseIndex,
							steps,
							stepIndex,
							arrowsOccupiedByMines,
							mineEvent.Position);
						if (bestArrow != InvalidArrowIndex)
						{
							// Add mine event
							var newNode = new MinePerformanceNode
							{
								Position = mineEvent.Position,
								Arrow = bestArrow,
								Prev = lastPerformanceNode
							};
							lastPerformanceNode.Next = newNode;
							lastPerformanceNode = newNode;

							arrowsOccupiedByMines[bestArrow] = true;
						}
						else
						{
							performedChart.LogWarn($"Skipping {mineEvent.Type:G} mine event at {mineEvent.Position}. Unable to determine best arrow to associate with this mine.");
						}
						break;
					}
					case MineType.NoArrow:
					{
						// If this PerformedChart has a lane with no arrows in it, use that for this mine.
						// If it doesn't then just skip the mine.
						if (firstLaneWithNoArrow >= 0)
						{
							var newNode = new MinePerformanceNode
							{
								Position = mineEvent.Position,
								Arrow = firstLaneWithNoArrow,
								Prev = lastPerformanceNode
							};
							lastPerformanceNode.Next = newNode;
							lastPerformanceNode = newNode;

							arrowsOccupiedByMines[firstLaneWithNoArrow] = true;
						}
						else
						{
							performedChart.LogWarn($"Skipping {mineEvent.Type:G} mine event at {mineEvent.Position}. No empty lanes.");
						}
						break;
					}
				}
			}
		}

		/// <summary>
		/// Given a GraphNodeInstance and the GraphLinkInstance to that node, returns a
		/// representation of what actions should be performed on what arrows
		/// to arrive at the node. The actions are returned in an array indexed by arrow.
		/// This is a helper method used when generating an SM Chart.
		/// </summary>
		/// <param name="graphNode">GraphNode.</param>
		/// <param name="graphLinkToNode">GraphLink to GraphNode.</param>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <returns>Array of actions.</returns>
		private static PerformanceFootAction[] GetActionsForNode(
			GraphNodeInstance graphNode,
			GraphLinkInstance graphLinkToNode,
			int numArrows)
		{
			// Initialize actions.
			var actions = new PerformanceFootAction[numArrows];
			for (var a = 0; a < numArrows; a++)
				actions[a] = PerformanceFootAction.None;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (graphLinkToNode.GraphLink.Links[f, p].Valid)
					{
						var arrow = graphNode.Node.State[f, p].Arrow;
						switch(graphLinkToNode.GraphLink.Links[f, p].Action)
						{
							case FootAction.Release:
								actions[arrow] = PerformanceFootAction.Release;
								break;
							case FootAction.Hold:
								if (graphNode.InstanceTypes[f, p] == InstanceStepType.Roll)
									actions[arrow] = PerformanceFootAction.Roll;
								else
									actions[arrow] = PerformanceFootAction.Hold;
								break;
							case FootAction.Tap:
								if (graphNode.InstanceTypes[f, p] == InstanceStepType.Fake)
									actions[arrow] = PerformanceFootAction.Fake;
								else if (graphNode.InstanceTypes[f, p] == InstanceStepType.Lift)
									actions[arrow] = PerformanceFootAction.Lift;
								else
									actions[arrow] = PerformanceFootAction.Tap;
								break;
						}
					}
				}
			}

			return actions;
		}

		/// <summary>
		/// Given a GraphNode and the GraphLink to that node, returns a
		/// representation of what actions should be performed on what arrows
		/// to arrive at the node. The actions are returned in an array indexed by arrow.
		/// This is a helper method used when searching to determine which arrows were stepped on,
		/// and for determining if steps and releases occur at the same time on the same arrows.
		/// This method will not return PerformanceFootActions based on InstanceStepTypes.
		/// Specifically, it will only set None, Release, Hold, or Tap.
		/// This method is static and takes the number of arrows as a parameter because it can be used prior to
		/// instantiating the PerformedChart.
		/// </summary>
		/// <param name="graphNode">GraphNode.</param>
		/// <param name="graphLinkToNode">GraphLink to GraphNode.</param>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <returns>Array of actions.</returns>
		private static PerformanceFootAction[] GetActionsForNode(
			GraphNode graphNode,
			GraphLink graphLinkToNode,
			int numArrows)
		{
			// Initialize actions.
			var actions = new PerformanceFootAction[numArrows];
			for (var a = 0; a < numArrows; a++)
				actions[a] = PerformanceFootAction.None;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (graphLinkToNode.Links[f, p].Valid)
					{
						var arrow = graphNode.State[f, p].Arrow;
						switch (graphLinkToNode.Links[f, p].Action)
						{
							case FootAction.Release:
								actions[arrow] = PerformanceFootAction.Release;
								break;
							case FootAction.Hold:
								actions[arrow] = PerformanceFootAction.Hold;
								break;
							case FootAction.Tap:
								actions[arrow] = PerformanceFootAction.Tap;
								break;
						}
					}
				}
			}

			return actions;
		}

		/// <summary>
		/// Creates a List of Events representing the Events of an SM Chart.
		/// </summary>
		/// <returns>List of Events that represent the Events of a SM Chart.</returns>
		public List<Event> CreateSMChartEvents()
		{
			var events = new List<Event>();

			var currentNode = Root;
			// Skip first rest position node.
			currentNode = currentNode.Next;
			while (currentNode != null)
			{
				// StepPerformanceNode
				if (currentNode is StepPerformanceNode stepNode)
				{
					var actions = GetActionsForNode(
						stepNode.GraphNodeInstance,
						stepNode.GraphLinkInstance,
						NumArrows);

					for (var arrow = 0; arrow < NumArrows; arrow++)
					{
						var action = actions[arrow];
						switch (action)
						{
							case PerformanceFootAction.Release:
								events.Add(new LaneHoldEndNote
								{
									Position = stepNode.Position,
									Lane = arrow,
									Player = 0,
									SourceType = SMCommon.NoteChars[(int) SMCommon.NoteType.HoldEnd].ToString()
								});
								break;
							case PerformanceFootAction.Tap:
							case PerformanceFootAction.Fake:
							case PerformanceFootAction.Lift:
							{
								var instanceAction = SMCommon.NoteType.Tap;
								if (action == PerformanceFootAction.Fake)
									instanceAction = SMCommon.NoteType.Fake;
								else if (action == PerformanceFootAction.Lift)
									instanceAction = SMCommon.NoteType.Lift;
								events.Add(new LaneTapNote
								{
									Position = stepNode.Position,
									Lane = arrow,
									Player = 0,
									SourceType = SMCommon.NoteChars[(int)instanceAction].ToString()
								});
								break;
							}
							case PerformanceFootAction.Hold:
							case PerformanceFootAction.Roll:
							{
								// Hold or Roll Start
								var holdRollType = action == PerformanceFootAction.Hold
									? SMCommon.NoteType.HoldStart
									: SMCommon.NoteType.RollStart;
								events.Add(new LaneHoldStartNote
								{
									Position = stepNode.Position,
									Lane = arrow,
									Player = 0,
									SourceType = SMCommon.NoteChars[(int)holdRollType].ToString()
								});
								break;
							}
						}
					}
				}

				// MinePerformanceNode
				else if (currentNode is MinePerformanceNode mineNode)
				{
					events.Add(new LaneNote
					{
						Position = mineNode.Position,
						Lane = mineNode.Arrow,
						Player = 0,
						SourceType = SMCommon.NoteChars[(int)SMCommon.NoteType.Mine].ToString()
					});
				}

				// Advance
				currentNode = currentNode.Next;
			}

			return events;
		}

		#region GraphLink Cache
		/// <summary>
		/// Determines and caches all replacement GraphLinks for the given GraphLinks.
		/// Caches into GraphLinkReplacementCache.
		/// </summary>
		/// <remarks>
		/// Expected that the given GraphLinks represent the set of all GraphLinks in
		/// the output StepGraph.
		/// Expected that this method is called to populate the cache prior to calling
		/// CreateFromExpressedChart.
		/// </remarks>
		/// <param name="graphLinks">Collection of GraphLinks</param>
		public static void CacheGraphLinks(IEnumerable<GraphLink> graphLinks)
		{
			foreach (var graphLink in graphLinks)
			{
				if (!GraphLinkReplacementCache.ContainsKey(graphLink))
					GraphLinkReplacementCache.Add(graphLink, FindAllAcceptableLinks(graphLink));
			}
		}

		/// <summary>
		/// Given a GraphLink from an ExpressedChart, return all acceptable GraphLinks that can be used
		/// in its place in the PerformedChart.
		/// Replacements are specified in Config.StepTypeReplacements.
		/// </summary>
		/// <param name="originalGraphLinkToNextNode">Original GraphLink.</param>
		/// <returns>List of all valid GraphLink replacements.</returns>
		private static List<GraphLink> FindAllAcceptableLinks(GraphLink originalGraphLinkToNextNode)
		{
			var acceptableLinks = new List<GraphLink>();
			if (originalGraphLinkToNextNode == null)
				return acceptableLinks;

			var originalLinks = originalGraphLinkToNextNode.Links;

			// Accumulate states to turn into GraphLinks.
			// Loop over each foot and portion, updating tempStates with valid replacements.
			var tempStates = new List<GraphLink.FootArrowState[,]>();
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					// Get the acceptable steps for the step at this Foot and FootPortion.
					if (!originalLinks[f, p].Valid)
						continue;
					if (!Config.Instance.StepTypeReplacements.TryGetValue(originalLinks[f, p].Step, out var acceptableSteps))
						continue;

					// If no temp states exist yet, create new ones for this Foot and FootPortion.
					if (tempStates.Count == 0)
					{
						foreach (var stepType in acceptableSteps)
						{
							var state = new GraphLink.FootArrowState[NumFeet, NumFootPortions];
							state[f, p] = new GraphLink.FootArrowState(stepType, originalLinks[f, p].Action);
							tempStates.Add(state);
						}
					}

					// If temp states exist, loop over them and add to their states.
					else
					{
						// Accumulate new states taking the state from the current tempStates and
						// adding the new step to them.
						var newStates = new List<GraphLink.FootArrowState[,]>();
						foreach (var tempState in tempStates)
						{
							foreach (var stepType in acceptableSteps)
							{
								// Don't create invalid brackets. In a bracket, both FootPortions
								// must use the same StepType.
								if (p > 0
									&& tempState[f, 0].Valid
									&& StepData.Steps[(int)tempState[f, 0].Step].IsBracket
									&& stepType != tempState[f, 0].Step)
								{
									continue;
								}

								// Create a new state, copied from the current temp state.
								var state = new GraphLink.FootArrowState[NumFeet, NumFootPortions];
								for (var f2 = 0; f2 < NumFeet; f2++)
									for (var p2 = 0; p2 < NumFootPortions; p2++)
										state[f2, p2] = tempState[f2, p2];
								// Update the new state with the new StepType.
								state[f, p] = new GraphLink.FootArrowState(stepType, originalLinks[f, p].Action);
								newStates.Add(state);
							}
						}

						// Update the tempStates with the newStates.
						tempStates = newStates;
					}
				}
			}

			// Accumulate all the states into GraphLinks.
			foreach (var state in tempStates)
			{
				var g = new GraphLink();
				for (var f = 0; f < NumFeet; f++)
					for (var p = 0; p < NumFootPortions; p++)
						g.Links[f, p] = state[f, p];
				acceptableLinks.Add(g);
			}

			return acceptableLinks;
		}
		#endregion GraphLink Cache

		#region Logging
		private static void LogError(string message, string logIdentifier)
		{
			Logger.Error($"[{LogTag}] {logIdentifier} {message}");
		}

		private static void LogWarn(string message, string logIdentifier)
		{
			Logger.Warn($"[{LogTag}] {logIdentifier} {message}");
		}

		private static void LogInfo(string message, string logIdentifier)
		{
			Logger.Info($"[{LogTag}] {logIdentifier} {message}");
		}

		private void LogError(string message)
		{
			LogError(message, LogIdentifier);
		}

		private void LogWarn(string message)
		{
			LogWarn(message, LogIdentifier);
		}

		private void LogInfo(string message)
		{
			LogInfo(message, LogIdentifier);
		}
		#endregion Logging
	}
}
