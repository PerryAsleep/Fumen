﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.Converters;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	public class PerformedChart
	{
		private enum PerformanceFootAction
		{
			None,
			Tap,
			Hold,
			Roll,
			Release
		}

		/// <summary>
		/// Search node for performing a search through an ExpressedChart.
		/// </summary>
		private class SearchNode
		{
			/// <summary>
			/// The GraphNodeInstance at this SearchNode.
			/// </summary>
			public GraphNodeInstance GraphNodeInstance;
			/// <summary>
			/// The depth of this SearchNode.
			/// This depth can also index the ExpressedChart StepEvents for accessing the StepType.
			/// </summary>
			public int Depth;
			/// <summary>
			/// Randomly ordered indices into the GraphNode's Links for the StepEvent in the ExpressedChart at this Depth.
			/// </summary>
			public List<int> Indices = new List<int>();
			/// <summary>
			/// CurrentIndex into Indices, beginning at 0 and increasing by one as new paths are searched.
			/// </summary>
			public int CurrentIndex;

			/// <summary>
			/// The previous SearchNode.
			/// Used for backing up when hitting a dead end in a search.
			/// </summary>
			public SearchNode PreviousNode;
			/// <summary>
			/// The next SearchNode.
			/// </summary>
			public SearchNode NextNode;
		}

		public class PerformanceNode
		{
			public MetricPosition Position;
			public PerformanceNode Next;
			public PerformanceNode Prev;

			public StepPerformanceNode GetPreviousStepNode()
			{
				var node = Prev;
				while (true)
				{
					if (node == null)
						return null;
					if (node is StepPerformanceNode stepNode)
						return stepNode;
					node = node.Prev;
				}
			}
		}

		public class StepPerformanceNode : PerformanceNode, MineUtils.IChartNode
		{
			public GraphNodeInstance GraphNodeInstance;
			public GraphLinkInstance GraphLinkInstance;

			#region MineUtils.IChartNode Implementation
			public GraphNode GetGraphNode() { return GraphNodeInstance?.Node; }
			public GraphLink GetGraphLinkToNode() { return GraphLinkInstance?.GraphLink; }
			public MetricPosition GetPosition() { return Position; }
			#endregion
		}

		public class MinePerformanceNode : PerformanceNode
		{
			public int Arrow;
		}

		public readonly PerformanceNode Root;
		public readonly int NumArrows;

		private PerformedChart() {}

		private PerformedChart(int numArrows, PerformanceNode root)
		{
			NumArrows = numArrows;
			Root = root;
		}

		/// <summary>
		/// Creates a PerformedChart by iteratively searching for a series of GraphNodes that satisfy
		/// the given ExpressedChart's StepEvents.
		/// </summary>
		/// <param name="stepGraph">
		/// StepGraph representing all possible states that can be traversed.
		/// </param>
		/// <param name="expressedChart">ExpressedChart to search.</param>
		/// <returns>
		/// PerformedChart satisfying the given ExpressedChart for the given StepGraph.
		/// </returns>
		public static PerformedChart CreateFromExpressedChart(StepGraph stepGraph, ExpressedChart expressedChart)
		{
			// Find a path of SearchNodes through the ExpressedChart
			// HACK consistent seed
			var random = new Random(1);
			var rootSearchNode = new SearchNode { GraphNodeInstance = new GraphNodeInstance { Node = stepGraph.Root }};
			var currentSearchNode = rootSearchNode;
			while (true)
			{
				// Finished
				if (currentSearchNode.Depth >= expressedChart.StepEvents.Count)
					break;

				// Dead end
				while (DoesNodeStepOnReleaseAtSamePosition(currentSearchNode, expressedChart, stepGraph.NumArrows)
				       || !currentSearchNode.GraphNodeInstance.Node.Links.ContainsKey(expressedChart.StepEvents[currentSearchNode.Depth].LinkInstance.GraphLink)
				       || currentSearchNode.CurrentIndex >
				       currentSearchNode.GraphNodeInstance.Node.Links[expressedChart.StepEvents[currentSearchNode.Depth].LinkInstance.GraphLink].Count - 1)
				{
					// Back up
					var prevNode = currentSearchNode.PreviousNode;
					if (prevNode != null)
					{
						prevNode.NextNode = null;
						currentSearchNode = prevNode;
					}
					else
					{
						// Failed to find a path
						return null;
					}
				}

				var linkInstance = expressedChart.StepEvents[currentSearchNode.Depth].LinkInstance;
				var links = currentSearchNode.GraphNodeInstance.Node.Links[linkInstance.GraphLink];

				// If this node's indices have not been set up, set them up
				if (currentSearchNode.CurrentIndex == 0)
				{
					for (var i = 0; i < links.Count; i++)
						currentSearchNode.Indices.Add(i);
					currentSearchNode.Indices = currentSearchNode.Indices.OrderBy(a => random.Next()).ToList();
				}

				// We can search further, pick a new index and advance
				var nextGraphNode = links[currentSearchNode.Indices[currentSearchNode.CurrentIndex++]];
				var newNode = new SearchNode
				{
					PreviousNode = currentSearchNode,
					Depth = currentSearchNode.Depth + 1,
					GraphNodeInstance = new GraphNodeInstance {Node = nextGraphNode}
				};
				// Set up rolls
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						if (linkInstance.Rolls[f, p])
						{
							newNode.GraphNodeInstance.Rolls[f, p] = true;
						}
					}
				}

				currentSearchNode.NextNode = newNode;
				currentSearchNode = newNode;
			}

			// Set up a new PerformedChart
			var performedChart = new PerformedChart(
				stepGraph.NumArrows,
				new StepPerformanceNode
				{
					Position = new MetricPosition(),
					GraphNodeInstance = new GraphNodeInstance { Node = stepGraph.Root }
				});

			// Add the StepPerformanceNodes to the PerformedChart
			var currentPerformanceNode = performedChart.Root;
			currentSearchNode = rootSearchNode;
			currentSearchNode = currentSearchNode.NextNode;
			while (currentSearchNode != null)
			{
				// Add new StepPerformanceNode and advance
				var newNode = new StepPerformanceNode
				{
					Position = expressedChart.StepEvents[currentSearchNode.Depth - 1].Position,
					GraphLinkInstance = expressedChart.StepEvents[currentSearchNode.Depth - 1].LinkInstance,
					GraphNodeInstance = currentSearchNode.GraphNodeInstance,
					Prev = currentPerformanceNode
				};

				currentPerformanceNode.Next = newNode;
				currentPerformanceNode = newNode;
				currentSearchNode = currentSearchNode.NextNode;
			}
			var lastPerformanceNode = currentPerformanceNode;

			// Add Mines
			AddMinesToPerformedChart(performedChart, stepGraph, expressedChart, lastPerformanceNode);

			return performedChart;
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

			// This node and the previous node must occur at the same time for the problem to arise.
			if (expressedChart.StepEvents[previousNode.Depth - 1].Position != expressedChart.StepEvents[node.Depth - 1].Position)
				return false;

			// Determine what actions are performed for both the current and previous node.
			var currentActions = GetActionsForNode(
				node.GraphNodeInstance,
				expressedChart.StepEvents[node.Depth - 1].LinkInstance,
				numArrows);
			var previousActions = GetActionsForNode(
				previousNode.GraphNodeInstance,
				expressedChart.StepEvents[previousNode.Depth - 1].LinkInstance,
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
						break;
					}
					case MineType.NoArrow:
					{
						// If this PerformedChart has a lane with no arrows in it, use that for this mine.
						// If it doesn't then just skip the mine.
						// TODO: Log warning
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
		/// Given a node and the previous node, returns a representation of what actions should be performed on what arrows
		/// to arrive at the node. The actions are returned in an array indexed by arrow.
		/// This is a helper method used when generating an SM Chart and when determining if steps and releases occur
		/// at the same time on the same arrows when generating the PerformedChart.
		/// This method is static and takes the number of arrows as a parameter because it can be used prior to instantiating
		/// the PerformedChart.
		/// </summary>
		/// <param name="currentNode">Current GraphNode.</param>
		/// <param name="currentLink">GraphLink to current GraphNode.</param>
		/// <param name="numArrows">Number of arrows in the Chart.</param>
		/// <returns>Array of actions.</returns>
		private static PerformanceFootAction[] GetActionsForNode(
			GraphNodeInstance currentNode,
			GraphLinkInstance currentLink,
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
					if (currentLink.GraphLink.Links[f, p].Valid)
					{
						var arrow = currentNode.Node.State[f, p].Arrow;
						switch(currentLink.GraphLink.Links[f, p].Action)
						{
							case FootAction.Release:
								actions[arrow] = PerformanceFootAction.Release;
								break;
							case FootAction.Hold:
								actions[arrow] = currentNode.Rolls[f, p] ? PerformanceFootAction.Roll : PerformanceFootAction.Hold;
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
							{
								events.Add(new LaneTapNote
								{
									Position = stepNode.Position,
									Lane = arrow,
									Player = 0,
									SourceType = SMCommon.NoteChars[(int)SMCommon.NoteType.Tap].ToString()
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
	}
}