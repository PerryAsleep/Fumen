using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using static StepManiaLibrary.Constants;

namespace StepManiaLibrary.PerformedChart
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
	public partial class PerformedChart
	{
		private const string LogTag = "Performed Chart";

		/// <summary>
		/// StepType cost for falling back to a completely blank link, dropping all steps for all portions of all feet.
		/// </summary>
		private const double BlankStepCost = 1000.0;
		/// <summary>
		/// StepType cost for falling back to a link with at least one foot having all of its steps dropped.
		/// </summary>
		private const double BlankSingleStepCost = 900.0;
		/// <summary>
		/// StepType cost for falling back to a link with dropped steps.
		/// </summary>
		private const double IndividualDroppedArrowStepCost = 100.0;

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
		/// Cache of fallback / replacement GraphLinkInstances.
		/// </summary>
		private static readonly GraphLinkInstanceCache LinkCache = new GraphLinkInstanceCache();

		/// <summary>
		/// List of PerformanceNodes representing the roots of each section of the PerformedChart.
		/// </summary>
		private readonly List<PerformanceNode> SectionRoots = new List<PerformanceNode>();

		/// <summary>
		/// Number of arrows in the Chart.
		/// </summary>
		private readonly int NumArrows;

		/// <summary>
		/// Identifier to use when logging messages about this PerformedChart.
		/// </summary>
		private readonly string LogIdentifier;

		/// <summary>
		/// Private constructor.
		/// </summary>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <param name="root">
		/// Root PerformanceNode of the PerformedChart. Added as the root of the first section.
		/// </param>
		/// <param name="logIdentifier">
		/// Identifier to use when logging messages about this PerformedChart.
		/// </param>
		private PerformedChart(int numArrows, PerformanceNode root, string logIdentifier)
		{
			NumArrows = numArrows;
			SectionRoots.Add(root);
			LogIdentifier = logIdentifier;
		}

		private PerformedChart(int numArrows, string logIdentifier)
		{
			NumArrows = numArrows;
			LogIdentifier = logIdentifier;
		}

		public List<PerformanceNode> GetRootNodes()
		{
			return SectionRoots;
		}

		/// <summary>
		/// Creates a PerformedChart by iteratively searching for a series of GraphNodes that satisfy
		/// the given ExpressedChart's StepEvents.
		/// </summary>
		/// <param name="stepGraph">
		/// StepGraph representing all possible states that can be traversed.
		/// </param>
		/// <param name="expressedChart">ExpressedChart to search.</param>
		/// <param name="randomSeed">
		/// Random seed to use when needing to make random choices when creating the PerformedChart.
		/// </param>
		/// <param name="logIdentifier">
		/// Identifier to use when logging messages about this PerformedChart.
		/// </param>
		/// <returns>
		/// PerformedChart satisfying the given ExpressedChart for the given StepGraph.
		/// </returns>
		public static PerformedChart CreateFromExpressedChart(
			StepGraph stepGraph,
			Config config,
			ExpressedChart expressedChart,
			int randomSeed,
			string logIdentifier)
		{
			var rootNode = stepGraph.GetRoot();
			SearchNode rootSearchNode = null;
			var nps = FindNPS(expressedChart);
			var random = new Random(randomSeed);

			// Find a path of SearchNodes through the ExpressedChart.
			if (expressedChart.StepEvents.Count > 0)
			{
				var depth = 0;

				// Set up a root search node at the root GraphNode.
				rootSearchNode = new SearchNode(
					rootNode,
					LinkCache.GetGraphLinks(expressedChart.StepEvents[0].LinkInstance, config),
					null,
					0.0,
					0.0,
					depth,
					null,
					new PerformanceFootAction[stepGraph.NumArrows],
					stepGraph,
					nps,
					random.NextDouble(),
					config,
					null);
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
							if (bestNode == null || node.CompareTo(bestNode) < 0)
								bestNode = node;

						// Remove any nodes that are not chosen so there is only one path through the chart.
						foreach (var node in currentSearchNodes)
						{
							if (node.Equals(bestNode))
								continue;
							Prune(node);
						}
						break;
					}

					// Failed to find a path.
					// This should never happen due to the allowance of blank steps.
					if (currentSearchNodes.Count == 0)
					{
						LogError($"Unable to create PerformedChart. Furthest position: {expressedChart.StepEvents[depth].Position}", logIdentifier);
						return null;
					}

					// Accumulate the next level of SearchNodes by looping over each SearchNode
					// in the current set.
					var nextDepth = depth + 1;
					var nextSearchNodes = new HashSet<SearchNode>();
					foreach (var searchNode in currentSearchNodes)
					{
						var numLinks = searchNode.GraphLinks.Count;
						for (var l = 0; l < numLinks; l++)
						{
							var graphLink = searchNode.GraphLinks[l];
							var stepTypeCost = GetStepTypeCost(searchNode, l);

							// Special case handling for a blank link for a skipped step.
							if (graphLink.GraphLink.IsBlank())
							{
								// This next node is the same as the previous node since we skipped a step.
								var nextGraphNode = searchNode.GraphNode;
								// Similarly, the links out of this node haven't changed.
								var graphLinksToNextNode = searchNode.GraphLinks;
								// Blank steps involve no actions.
								var actions = new PerformanceFootAction[stepGraph.NumArrows];
								for (var a = 0; a < stepGraph.NumArrows; a++)
									actions[a] = PerformanceFootAction.None;

								// Set up the new SearchNode.
								var nextSearchNode = new SearchNode(
									nextGraphNode,
									graphLinksToNextNode,
									graphLink,
									stepTypeCost,
									expressedChart.StepEvents[depth].Time,
									nextDepth,
									searchNode,
									actions,
									stepGraph,
									nps,
									random.NextDouble(),
									config,
									null
								);

								// Hook up the new SearchNode and store it in the nextSearchNodes for pruning.
								if (!AddChildNode(searchNode, nextSearchNode, graphLink, nextSearchNodes, stepGraph, expressedChart))
									continue;
							}
							else
							{
								// The GraphNode may not actually have this GraphLink due to
								// the StepTypeReplacements.
								if (!searchNode.GraphNode.Links.ContainsKey(graphLink.GraphLink))
									continue;

								// Check every GraphNode linked to by this GraphLink.
								var nextNodes = searchNode.GraphNode.Links[graphLink.GraphLink];
								for (var n = 0; n < nextNodes.Count; n++)
								{
									var nextGraphNode = nextNodes[n];

									// Determine new step information.
									var actions = GetActionsForNode(nextGraphNode, graphLink.GraphLink, stepGraph.NumArrows);

									// Set up the graph links leading out of this node to its next nodes.
									var graphLinksToNextNode = new List<GraphLinkInstance>();
									if (nextDepth < expressedChart.StepEvents.Count)
									{
										var sourceLinkKey = expressedChart.StepEvents[nextDepth].LinkInstance;
										graphLinksToNextNode = LinkCache.GetGraphLinks(sourceLinkKey, config);
									}

									// Set up a new SearchNode.
									var nextSearchNode = new SearchNode(
										nextGraphNode,
										graphLinksToNextNode,
										graphLink,
										stepTypeCost,
										expressedChart.StepEvents[depth].Time,
										nextDepth,
										searchNode,
										actions,
										stepGraph,
										nps,
										random.NextDouble(),
										config,
										null
									);

									// Hook up the new SearchNode and store it in the nextSearchNodes for pruning.
									if (!AddChildNode(searchNode, nextSearchNode, graphLink, nextSearchNodes, stepGraph, expressedChart))
										continue;
								}
							}
						}
					}

					// Prune all the next SearchNodes, store them in currentSearchNodes, and advance.
					currentSearchNodes = Prune(nextSearchNodes);
					depth = nextDepth;
				}
			}

			// Set up a new PerformedChart
			var performedChart = new PerformedChart(
				stepGraph.NumArrows,
				new StepPerformanceNode
				{
					Position = 0,
					GraphNodeInstance = new GraphNodeInstance {Node = rootNode},
				},
				logIdentifier);

			// Add the StepPerformanceNodes to the PerformedChart
			var currentPerformanceNode = performedChart.SectionRoots[0];
			var currentSearchNode = rootSearchNode;
			currentSearchNode = currentSearchNode?.GetNextNode();
			while (currentSearchNode != null)
			{
				if (currentSearchNode.IsBlank())
				{
					currentSearchNode = currentSearchNode.GetNextNode();
					continue;
				}

				// Create GraphNodeInstance.
				var graphLinkInstance = currentSearchNode.GraphLinkFromPreviousNode;
				var stepEventIndex = currentSearchNode.Depth - 1;
				var graphNodeInstance = new GraphNodeInstance {Node = currentSearchNode.GraphNode};
				for (var f = 0; f < NumFeet; f++)
					for (var p = 0; p < NumFootPortions; p++)
						graphNodeInstance.InstanceTypes[f, p] = graphLinkInstance.InstanceTypes[f, p];

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
			AddMinesToPerformedChart(performedChart, stepGraph, expressedChart, lastPerformanceNode, random);

			return performedChart;
		}

		/// <summary>
		/// Helper function when searching to add a new child SearchNode.
		/// Checks for whether this node would be invalid due to stepping on a previous release at the same position.
		/// Updates the parentNode's NextNodes links to include the new childNode.
		/// Updates nextSearchNodes to include the new child SearchNode for later pruning.
		/// </summary>
		private static bool AddChildNode(
			SearchNode parentNode,
			SearchNode childNode,
			GraphLinkInstance graphLink,
			HashSet<SearchNode> nextSearchNodes,
			StepGraph stepGraph,
			ExpressedChart expressedChart)
		{
			// Do not consider this next SearchNode if it results in an invalid state.
			if (DoesNodeStepOnReleaseAtSamePosition(childNode, expressedChart, stepGraph.NumArrows))
				return false;

			// Update the previous SearchNode's NextNodes to include the new SearchNode.
			if (!parentNode.NextNodes.ContainsKey(graphLink))
				parentNode.NextNodes[graphLink] = new HashSet<SearchNode>();
			parentNode.NextNodes[graphLink].Add(childNode);

			// Add this node to the set of next SearchNodes to be pruned after they are all found.
			nextSearchNodes.Add(childNode);
			return true;
		}

		/// <summary>
		/// Helper function when searching to get a child SearchNode's step
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="graphLinkIndexToChild"></param>
		/// <returns></returns>
		private static double GetStepTypeCost(
			SearchNode parentNode,
			int graphLinkIndexToChild)
		{
			var numLinks = parentNode.GraphLinks.Count;
			var graphLinkToChild = parentNode.GraphLinks[graphLinkIndexToChild];

			if (graphLinkToChild.GraphLink.IsBlank())
				return BlankStepCost;

			// Determine the step type cost for this node.
			// Assumption that the first GraphLink is the source from which the fallbacks were derived.
			var sourceLink = parentNode.GraphLinks[0];
			if (LinkCache.ContainsBlankLink(sourceLink, graphLinkToChild))
			{
				return BlankSingleStepCost;
			}
			else
			{
				var numStepsRemoved = LinkCache.GetNumStepsRemoved(parentNode.GraphLinks[0], graphLinkToChild);
				if (numStepsRemoved > 0)
				{
					return numStepsRemoved * IndividualDroppedArrowStepCost;
				}
			}

			// The first link out of this search node is the most preferred node. The
			// links at higher indexes are less preferred fallbacks that should cost more.
			return (double)graphLinkIndexToChild / numLinks;
		}

		// TODO: Rewrite
		//public static PerformedChart CreateByFilling(
		//	StepGraph stepGraph,
		//	List<FillSectionConfig> fillSections,
		//	Chart chart,
		//	int randomSeed,
		//	string logIdentifier)
		//{
		//	var performedChart = new PerformedChart(stepGraph.NumArrows, logIdentifier);
		//	var random = new Random(randomSeed);

		//	var validStepTypes = new[] {StepType.NewArrow, StepType.SameArrow};

		//	// up front, loop over all the events in the chart and the configs to make a mapping
		//	// with the cached time and positions by index.
		//	var timingPerSection = DetermineFillSectionTiming(fillSections, chart);

		//	var sectionIndex = 0;
		//	var previousSectionLastL = InvalidArrowIndex;
		//	var previousSectionLastR = InvalidArrowIndex;
		//	var previousSectionEnd = 0;
		//	var previousSectionLastFoot = InvalidFoot;
		//	foreach (var sectionConfig in fillSections)
		//	{
		//		var sectionTiming = timingPerSection[sectionIndex];
		//		var startingWherePreviousSectionEnded = false;

		//		// Get the starting position for this section.
		//		var lStart = sectionConfig.LeftFootStartLane;
		//		var rStart = sectionConfig.RightFootStartLane;
		//		GraphNode rootGraphNode;
		//		if (lStart == InvalidArrowIndex || rStart == InvalidArrowIndex)
		//		{
		//			if (previousSectionEnd == sectionConfig.StartPosition
		//			    && previousSectionLastL != InvalidArrowIndex
		//			    && previousSectionLastR != InvalidArrowIndex)
		//			{
		//				startingWherePreviousSectionEnded = true;
		//				rootGraphNode = stepGraph.FindGraphNode(previousSectionLastL, GraphArrowState.Resting, previousSectionLastR, GraphArrowState.Resting);
		//			}
		//			else
		//			{
		//				rootGraphNode = stepGraph.Root;
		//			}
		//		}
		//		else
		//		{
		//			rootGraphNode = stepGraph.FindGraphNode(lStart, GraphArrowState.Resting, rStart, GraphArrowState.Resting);
		//			if (rootGraphNode == null)
		//			{
		//				performedChart.LogError($"Section {sectionIndex}: Could not find starting node for left foot on {lStart} and right foot on {rStart}.");
		//				return null;
		//			}
		//		}

		//		var root = new StepPerformanceNode
		//		{
		//			Position = 0,
		//			GraphNodeInstance = new GraphNodeInstance { Node = rootGraphNode },
		//		};

		//		// Get the starting foot to start on.
		//		var foot = sectionConfig.FootToStartOn;
		//		if (foot == InvalidFoot)
		//		{
		//			if (previousSectionEnd == sectionConfig.StartPosition
		//			    && previousSectionLastFoot != InvalidFoot)
		//			{
		//				foot = OtherFoot(previousSectionLastFoot);
		//			}
		//			else
		//			{
		//				foot = random.Next(NumFeet);
		//			}
		//		}

		//		var depth = 0;

		//		// Set up a root search node at the root GraphNode.
		//		var possibleGraphLinksToNextNode = new List<GraphLink>();

		//		if (startingWherePreviousSectionEnded)
		//		{
		//			foreach (var stepType in validStepTypes)
		//			{
		//				var link = new GraphLink();
		//				link.Links[foot, DefaultFootPortion] = new GraphLink.FootArrowState(stepType, FootAction.Tap);
		//				possibleGraphLinksToNextNode.Add(link);
		//			}
		//		}
		//		else
		//		{
		//			var rootGraphLink = new GraphLink();
		//			rootGraphLink.Links[foot, DefaultFootPortion] = new GraphLink.FootArrowState(StepType.SameArrow, FootAction.Tap);
		//			possibleGraphLinksToNextNode.Add(rootGraphLink);
		//		}

		//		var rootSearchNode = new SearchNode(
		//			rootGraphNode,
		//			possibleGraphLinksToNextNode,
		//			null,
		//			0L,
		//			depth,
		//			null,
		//			new PerformanceFootAction[stepGraph.NumArrows],
		//			stepGraph,
		//			0,
		//			random.NextDouble(),
		//			sectionConfig.Config,
		//			sectionConfig);

		//		var currentSearchNodes = new HashSet<SearchNode>();
		//		currentSearchNodes.Add(rootSearchNode);

		//		foreach(var timingInfo in sectionTiming)
		//		{
		//			var timeSeconds = timingInfo.Item1;

		//			// Failed to find a path.
		//			if (currentSearchNodes.Count == 0)
		//			{
		//				performedChart.LogError($"Section {sectionIndex}: Failed to find path.");
		//				break;
		//			}

		//			// Accumulate the next level of SearchNodes by looping over each SearchNode
		//			// in the current set.
		//			var nextDepth = depth + 1;
		//			foot = OtherFoot(foot);
		//			var nextSearchNodes = new HashSet<SearchNode>();

		//			foreach (var searchNode in currentSearchNodes)
		//			{
		//				// Check every GraphLink out of the SearchNode.
		//				var deadEnd = true;
		//				for (var l = 0; l < searchNode.GraphLinks.Count; l++)
		//				{
		//					var graphLink = searchNode.GraphLinks[l];
		//					// The GraphNode may not actually have this GraphLink due to
		//					// the StepTypeReplacements.
		//					if (!searchNode.GraphNode.Links.ContainsKey(graphLink))
		//						continue;

		//					// Check every GraphNode linked to by this GraphLink.
		//					var nextNodes = searchNode.GraphNode.Links[graphLink];
		//					for (var n = 0; n < nextNodes.Count; n++)
		//					{
		//						var nextGraphNode = nextNodes[n];
		//						// Determine new step information.
		//						var actions = GetActionsForNode(nextGraphNode, graphLink, stepGraph.NumArrows);

		//						possibleGraphLinksToNextNode = new List<GraphLink>();
		//						foreach (var stepType in validStepTypes)
		//						{
		//							if (depth == 1 && !startingWherePreviousSectionEnded && stepType != StepType.SameArrow)
		//								continue;
		//							var link = new GraphLink();
		//							link.Links[foot, DefaultFootPortion] = new GraphLink.FootArrowState(stepType, FootAction.Tap);
		//							possibleGraphLinksToNextNode.Add(link);
		//						}

		//						// Set up a new SearchNode.
		//						var nextSearchNode = new SearchNode(
		//							nextGraphNode,
		//							possibleGraphLinksToNextNode,
		//							graphLink,
		//							timeSeconds,
		//							nextDepth,
		//							searchNode,
		//							actions,
		//							stepGraph,
		//							0,
		//							random.NextDouble(),
		//							sectionConfig.Config,
		//							sectionConfig
		//						);

		//						// Update the previous SearchNode's NextNodes to include the new SearchNode.
		//						if (!searchNode.NextNodes.ContainsKey(graphLink))
		//							searchNode.NextNodes[graphLink] = new HashSet<SearchNode>();
		//						searchNode.NextNodes[graphLink].Add(nextSearchNode);

		//						// Add this node to the set of next SearchNodes to be pruned after they are all found.
		//						nextSearchNodes.Add(nextSearchNode);
		//						deadEnd = false;
		//					}
		//				}

		//				// This SearchNode has no valid children. Prune it.
		//				if (deadEnd)
		//					Prune(searchNode);
		//			}

		//			// Prune all the next SearchNodes, store them in currentSearchNodes, and advance.
		//			currentSearchNodes = Prune(nextSearchNodes);
		//			depth = nextDepth;
		//		}

		//		// Finished
		//		{
		//			// Check for ending at the correct location.
		//			if (sectionConfig.LeftFootEndLane != InvalidArrowIndex &&
		//			    sectionConfig.RightFootEndLane != InvalidArrowIndex)
		//			{
		//				var remainingNodes = new HashSet<SearchNode>();
		//				foreach (var node in currentSearchNodes)
		//				{
		//					if (node.GraphNode.State[L, DefaultFootPortion].Arrow == sectionConfig.LeftFootEndLane
		//					    && node.GraphNode.State[R, DefaultFootPortion].Arrow == sectionConfig.RightFootEndLane)
		//					{
		//						remainingNodes.Add(node);
		//						continue;
		//					}

		//					Prune(node);
		//				}

		//				currentSearchNodes = remainingNodes;
		//				if (currentSearchNodes.Count == 0)
		//				{
		//					performedChart.LogError(
		//						$"Section {sectionIndex}: Could not find path ending with left on {sectionConfig.LeftFootEndLane} and right foot on {sectionConfig.RightFootEndLane}.");
		//				}
		//			}

		//			// Choose path with lowest cost.
		//			SearchNode bestNode = null;
		//			foreach (var node in currentSearchNodes)
		//				if (bestNode == null || node.CompareTo(bestNode) < 0)
		//					bestNode = node;

		//			// Remove any nodes that are not chosen so there is only one path through the chart.
		//			foreach (var node in currentSearchNodes)
		//			{
		//				if (node.Equals(bestNode))
		//					continue;
		//				Prune(node);
		//			}

		//			previousSectionEnd = sectionConfig.EndPosition;
		//			previousSectionLastL = bestNode.GraphNode.State[L, DefaultFootPortion].Arrow;
		//			previousSectionLastR = bestNode.GraphNode.State[R, DefaultFootPortion].Arrow;
		//			previousSectionLastFoot = OtherFoot(foot);
		//		}

		//		performedChart.SectionRoots.Add(root);
		//		sectionIndex++;

		//		// Add the StepPerformanceNodes to the PerformedChart
		//		var currentPerformanceNode = root;
		//		var currentSearchNode = rootSearchNode;
		//		currentSearchNode = currentSearchNode?.GetNextNode();
		//		var index = 0;
		//		while (currentSearchNode != null)
		//		{
		//			// Create GraphNodeInstance.
		//			var graphNodeInstance = new GraphNodeInstance { Node = currentSearchNode.GraphNode };

		//			// Create GraphLinkInstance.
		//			var graphLink = currentSearchNode.GraphLinkFromPreviousNode;
		//			GraphLinkInstance graphLinkInstance = new GraphLinkInstance { GraphLink = graphLink };

		//			// Add new StepPerformanceNode and advance.
		//			var newNode = new StepPerformanceNode
		//			{
		//				Position = sectionTiming[index].Item2,
		//				GraphLinkInstance = graphLinkInstance,
		//				GraphNodeInstance = graphNodeInstance,
		//				Prev = currentPerformanceNode
		//			};
		//			currentPerformanceNode.Next = newNode;
		//			currentPerformanceNode = newNode;
		//			currentSearchNode = currentSearchNode.GetNextNode();
		//			index++;
		//		}
		//	}

		//	return performedChart;
		//}

		//private class FillEvent
		//{
		//	public Event Event;

		//	public int SectionIndex;
		//	public int IndexWithinSection;
		//	public int Position;
		//}

		//public class FillEventComparer : IComparer<FillEvent>
		//{
		//	private SMCommon.SMEventComparer SMEventComparer = new SMCommon.SMEventComparer();

		//	int IComparer<FillEvent>.Compare(FillEvent e1, FillEvent e2)
		//	{
		//		// First, compare by position.
		//		int comparison = e1.Position.CompareTo(e2.Position);
		//		if (comparison != 0)
		//			return comparison;

		//		// If one event is for an Event from the Chart, that should come first.
		//		if ((e1.Event == null) != (e2.Event == null))
		//			return e1.Event == null ? 1 : -1;

		//		// If neither event is from the Chart, the are the same.
		//		if (e1.Event == null)
		//			return 0;

		//		// Both events are Chart Events. Compare them with the standard StepMania sort rules.
		//		return SMEventComparer.Compare(e1.Event, e2.Event);
		//	}
		//}

		// TODO: Rewrite
		//private static Tuple<double, int>[][] DetermineFillSectionTiming(
		//	List<FillSectionConfig> fillSections,
		//	Chart chart)
		//{
		//	var FillEvents = new List<FillEvent>();
		//	foreach(var chartEvent in chart.Layers[0].Events)
		//		FillEvents.Add(new FillEvent { Event = chartEvent, Position = chartEvent.IntegerPosition});

		//	var numSections = fillSections.Count;
		//	var numEventsInSection = new int[numSections];
		//	var sectionIndex = 0;
		//	foreach (var config in fillSections)
		//	{
		//		var indexWithinSection = 0;
		//		var pos = config.StartPosition;
		//		while (pos < config.EndPosition)
		//		{
		//			numEventsInSection[sectionIndex]++;
		//			FillEvents.Add(new FillEvent
		//			{
		//				SectionIndex = sectionIndex,
		//				IndexWithinSection = indexWithinSection,
		//				Position = pos
		//			});

		//			indexWithinSection++;
		//			pos += SMCommon.MaxValidDenominator / config.BeatSubDivisionToFill;
		//		}
		//		sectionIndex++;
		//	}

		//	FillEvents.Sort(new FillEventComparer());

		//	var sectionData = new Tuple<double, int>[numSections][];
		//	for (var si = 0; si < numSections; si++)
		//		sectionData[si] = new Tuple<double, int>[numEventsInSection[si]];

		//	var bpm = 0.0;
		//	var timeSignature = new Fraction(4, 4);
		//	double beatTime = 0.0;
		//	double currentTime = 0.0;

		//	var previousPosition = 0;
		//	foreach (var fillEvent in FillEvents)
		//	{
		//		if (fillEvent.Position > previousPosition)
		//		{
		//			var currentBeats =
		//				fillEvent.Position.Measure * timeSignature.Numerator
		//				+ fillEvent.Position.Beat
		//				+ (fillEvent.Position.SubDivision.Denominator == 0 ? 0 : fillEvent.Position.SubDivision.ToDouble());
		//			var previousBeats =
		//				previousPosition.Measure * timeSignature.Numerator
		//				+ previousPosition.Beat
		//				+ (previousPosition.SubDivision.Denominator == 0 ? 0 : previousPosition.SubDivision.ToDouble());
		//			currentTime += (currentBeats - previousBeats) * beatTime;
		//		}

		//		// Add the timing data.
		//		if (fillEvent.Event == null)
		//		{
		//			sectionData[fillEvent.SectionIndex][fillEvent.IndexWithinSection] =
		//				new Tuple<double, int>(currentTime, fillEvent.Position);
		//		}

		//		// Process tempo changes and stops.
		//		if (fillEvent.Event != null)
		//		{
		//			var chartEvent = fillEvent.Event;
		//			var beatTimeDirty = false;
		//			if (chartEvent is Stop stop)
		//				currentTime += stop.LengthSeconds;
		//			else if (chartEvent is TimeSignature ts)
		//			{
		//				timeSignature = ts.Signature;
		//				beatTimeDirty = true;
		//			}
		//			else if (chartEvent is Tempo tc)
		//			{
		//				bpm = tc.TempoBPM;
		//				beatTimeDirty = true;
		//			}

		//			if (beatTimeDirty)
		//			{
		//				if (bpm == 0.0 || timeSignature.Denominator == 0.0)
		//					beatTime = 0.0;
		//				else
		//					beatTime = (60 / bpm) * (4.0 / timeSignature.Denominator);
		//			}
		//		}

		//		previousPosition = fillEvent.Position;
		//	}

		//	return sectionData;
		//}

		/// <summary>
		/// Finds the notes per second of the entire Chart represented by the given ExpressedChart.
		/// </summary>
		/// <param name="expressedChart">ExpressedChart to find the notes per second of.</param>
		/// <returns>Notes per second of the Chart represented by the given ExpressedChart.</returns>
		private static double FindNPS(ExpressedChart expressedChart)
		{
			var nps = 0.0;
			var startTime = double.MaxValue;
			var endTime = 0.0;
			var numSteps = 0;
			for (var e = 0; e < expressedChart.StepEvents.Count; e++)
			{
				var stepEvent = expressedChart.StepEvents[e];
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						if (stepEvent.LinkInstance.GraphLink.Links[f, p].Valid
						    && stepEvent.LinkInstance.GraphLink.Links[f, p].Action != FootAction.Release)
						{
							if (stepEvent.Time < startTime)
								startTime = stepEvent.Time;
							numSteps++;
							endTime = stepEvent.Time;
						}
					}
				}
			}

			if (endTime > startTime)
			{
				nps = numSteps / (endTime - startTime);
			}

			return nps;
		}

		/// <summary>
		/// Prunes the given HashSet of SearchNodes to a HashSet that contains
		/// only the lowest cost SearchNode per unique GraphNode.
		/// </summary>
		/// <param name="nodes">HashSet of SearchNodes to prune.</param>
		/// <returns>Pruned SearchNodes.</returns>
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
					if (node.CompareTo(currentNode) < 1)
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
		/// Removes the given SearchNode from the tree.
		/// Removes all parents up until the first parent with other children.
		/// </summary>
		/// <param name="node">SearchNode to prune.</param>
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

			// Check if the previous node released on the same arrow tha the current node is stepping on.
			for (var a = 0; a < numArrows; a++)
			{
				if (previousNode.Actions[a] == PerformanceFootAction.Release &&
				    node.Actions[a] != PerformanceFootAction.None && node.Actions[a] != PerformanceFootAction.Release)
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
			PerformanceNode lastPerformanceNode,
			Random random)
		{
			// Record which lanes have arrows in them.
			var numLanesWithArrows = 0;
			var lanesWithNoArrows = new bool[stepGraph.NumArrows];
			for (var a = 0; a < stepGraph.NumArrows; a++)
				lanesWithNoArrows[a] = true;
			foreach (var root in performedChart.SectionRoots)
			{
				var currentPerformanceNode = root;
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
			foreach (var root in performedChart.SectionRoots)
			{
				var currentPerformanceNode = root;
				while (currentPerformanceNode != null)
				{
					if (currentPerformanceNode is StepPerformanceNode stepNode)
						stepEvents.Add(stepNode);
					currentPerformanceNode = currentPerformanceNode.Next;
				}
			}

			var (releases, steps) = MineUtils.GetReleasesAndSteps(stepEvents, stepGraph.NumArrows);

			// Add the MinePerformanceNodes to the PerformedChart.
			// For simplicity add all the nodes to the end. They will be sorted later.
			var stepIndex = 0;
			var releaseIndex = 0;
			var previousMinePosition = -1;
			var arrowsOccupiedByMines = new bool[stepGraph.NumArrows];
			var randomLaneOrder = Enumerable.Range(0, stepGraph.NumArrows).OrderBy(x => random.Next()).ToArray();
			for (var m = 0; m < expressedChart.MineEvents.Count; m++)
			{
				var mineEvent = expressedChart.MineEvents[m];
				// Advance the step and release indices to follow and precede the event respectively.
				while (stepIndex < steps.Count && steps[stepIndex].Position <= mineEvent.Position)
					stepIndex++;
				while (releaseIndex + 1 < releases.Count && releases[releaseIndex + 1].Position < mineEvent.Position)
					releaseIndex++;

				// Reset arrows occupied by mines if this mine is at a new position.
				if (previousMinePosition < 0 || previousMinePosition < mineEvent.Position)
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
							mineEvent.Position,
							randomLaneOrder);
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
						switch (graphLinkToNode.GraphLink.Links[f, p].Action)
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

			foreach (var root in SectionRoots)
			{
				var currentNode = root;
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
										IntegerPosition = stepNode.Position,
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
										IntegerPosition = stepNode.Position,
										Lane = arrow,
										Player = 0,
										SourceType = SMCommon.NoteChars[(int) instanceAction].ToString()
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
										IntegerPosition = stepNode.Position,
										Lane = arrow,
										Player = 0,
										SourceType = SMCommon.NoteChars[(int) holdRollType].ToString()
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
							IntegerPosition = mineNode.Position,
							Lane = mineNode.Arrow,
							Player = 0,
							SourceType = SMCommon.NoteChars[(int) SMCommon.NoteType.Mine].ToString()
						});
					}

					// Advance
					currentNode = currentNode.Next;
				}
			}

			return events;
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
