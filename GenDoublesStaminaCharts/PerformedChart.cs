using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.Converters;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	public class PerformedChart
	{
		/// <summary>
		/// Search node for performing a search through an ExpressedChart.
		/// </summary>
		private class SearchNode
		{
			/// <summary>
			/// The GraphNode at this SearchNode.
			/// </summary>
			public GraphNode GraphNode;
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

		private class PerformanceNode
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

		private class StepPerformanceNode : PerformanceNode, MineUtils.IChartNode
		{
			public GraphNode GraphNode;
			public GraphLink GraphLink;

			#region MineUtils.IChartNode Implementation
			public GraphNode GetGraphNode() { return GraphNode; }
			public GraphLink GetGraphLinkToNode() { return GraphLink; }
			public MetricPosition GetPosition() { return Position; }
			#endregion
		}

		private class MinePerformanceNode : PerformanceNode
		{
			public int Arrow;
		}

		private readonly PerformanceNode Root;
		private readonly int NumArrows;

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
			var rootSearchNode = new SearchNode { GraphNode = stepGraph.Root };
			var currentSearchNode = rootSearchNode;
			while (true)
			{
				// Finished
				if (currentSearchNode.Depth >= expressedChart.StepEvents.Count)
					break;

				// Dead end
				while (!currentSearchNode.GraphNode.Links.ContainsKey(expressedChart.StepEvents[currentSearchNode.Depth].Link)
					   || currentSearchNode.CurrentIndex >
					   currentSearchNode.GraphNode.Links[expressedChart.StepEvents[currentSearchNode.Depth].Link].Count - 1)
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

				var links = currentSearchNode.GraphNode.Links[expressedChart.StepEvents[currentSearchNode.Depth].Link];

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
					GraphNode = nextGraphNode
				};
				currentSearchNode.NextNode = newNode;
				currentSearchNode = newNode;
			}

			// Set up a new PerformedChart
			var performedChart = new PerformedChart(
				stepGraph.NumArrows,
				new StepPerformanceNode
				{
					Position = new MetricPosition(),
					GraphNode = stepGraph.Root
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
					GraphLink = expressedChart.StepEvents[currentSearchNode.Depth - 1].Link,
					GraphNode = currentSearchNode.GraphNode,
					Prev = currentPerformanceNode
				};
				currentPerformanceNode.Next = newNode;
				currentPerformanceNode = newNode;
				currentSearchNode = currentSearchNode.NextNode;
			}
			var lastPerformanceNode = currentPerformanceNode;

			// Add Mines
			AddMinesToPerformedChart(performedChart, stepGraph, expressedChart, rootSearchNode, lastPerformanceNode);
			return performedChart;
		}

		private static void AddMinesToPerformedChart(
			PerformedChart performedChart,
			StepGraph stepGraph,
			ExpressedChart expressedChart,
			SearchNode rootSearchNode,
			PerformanceNode lastPerformanceNode)
		{
			// Record which lanes have arrows in them.
			var lanesWithNoArrows = new bool[stepGraph.NumArrows];
			for (var a = 0; a < stepGraph.NumArrows; a++)
				lanesWithNoArrows[a] = true;
			var currentSearchNode = rootSearchNode;
			while (currentSearchNode != null)
			{
				for (var f = 0; f < NumFeet; f++)
					for (var a = 0; a < MaxArrowsPerFoot; a++)
						if (currentSearchNode.GraphNode.State[f, a].Arrow != InvalidArrowIndex)
							lanesWithNoArrows[currentSearchNode.GraphNode.State[f, a].Arrow] = false;
				currentSearchNode = currentSearchNode.NextNode;
			}

			// Get the first lane with no arrow, if one exists.
			var firstLaneWithNoArrow = -1;
			for (var a = 0; a < stepGraph.NumArrows; a++)
			{
				if (!lanesWithNoArrows[a])
					continue;
				firstLaneWithNoArrow = a;
				break;
			}

			// Create sorted lists of releases.
			var stepEvents = new List<StepPerformanceNode>();
			var currentPerformanceNode = performedChart.Root;
			while (currentPerformanceNode != null)
			{
				if (currentPerformanceNode is StepPerformanceNode stepNode)
					stepEvents.Add(stepNode);
				currentPerformanceNode = currentPerformanceNode.Next;
			}
			var (releases, steps) = MineUtils.GetReleasesAndSteps(stepEvents);

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
							arrowsOccupiedByMines);
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
					var previousStepNode = stepNode.GetPreviousStepNode();
					for (var arrow = 0; arrow < NumArrows; arrow++)
					{
						// Determine the state of this arrow previously and now
						GraphNode.FootArrowState currentArrowState = GraphNode.InvalidFootArrowState;
						var currentArrowFoot = InvalidFoot;
						GraphNode.FootArrowState previousArrowState = GraphNode.InvalidFootArrowState;
						var previousArrowFoot = InvalidFoot;
						for (var f = 0; f < NumFeet; f++)
						{
							for (var a = 0; a < MaxArrowsPerFoot; a++)
							{
								if (stepNode.GraphNode.State[f, a].Arrow == arrow)
								{
									currentArrowState = stepNode.GraphNode.State[f, a];
									currentArrowFoot = f;
								}

								if (previousStepNode.GraphNode.State[f, a].Arrow == arrow)
								{
									previousArrowState = previousStepNode.GraphNode.State[f, a];
									previousArrowFoot = f;
								}
							}
						}

						var addNormalStep = false;

						// A foot was on this arrow previously and on this arrow now
						if (previousArrowFoot != InvalidFoot && currentArrowFoot != InvalidArrowIndex)
						{
							// The same foot was on the arrow previously and now
							if (previousArrowFoot == currentArrowFoot)
							{
								// Currently resting
								if (currentArrowState.State == GraphArrowState.Resting)
								{
									// Previous was holding or rolling
									if (previousArrowState.State == GraphArrowState.Held
										|| previousArrowState.State == GraphArrowState.Rolling)
									{
										// Release
										events.Add(new LaneHoldEndNote
										{
											Position = stepNode.Position,
											Lane = arrow,
											Player = 0,
											SourceType = SMCommon.SNoteChars[(int)SMCommon.NoteType.HoldEnd].ToString()
										});
									}
									// Previous was resting
									else
									{
										// Check link to see if we tapped the same arrow
										for (var a = 0; a < MaxArrowsPerFoot; a++)
										{
											if (stepNode.GraphLink.Links[currentArrowFoot, a].Valid
												&& stepNode.GraphLink.Links[currentArrowFoot, a].Action == FootAction.Tap
												&& (stepNode.GraphLink.Links[currentArrowFoot, a].Step == SingleStepType.SameArrow
													|| stepNode.GraphLink.Links[currentArrowFoot, a].Step == SingleStepType.BracketOneNew
													|| stepNode.GraphLink.Links[currentArrowFoot, a].Step == SingleStepType.BracketBothSame))
											{
												// Tap on the arrow again
												addNormalStep = true;
												break;
											}
										}
									}
								}
								// Currently holding or rolling
								else
								{
									// Previous was resting
									if (previousArrowState.State == GraphArrowState.Resting)
									{
										addNormalStep = true;
									}

									// If previous was not resting then previous was also holding or rolling.
									// Cannot switch from hold to roll without a release, so nothing to do here.
								}
							}

							// The foot on this arrow changed due to a foot swap
							else
							{
								addNormalStep = true;
							}
						}
						// A foot was not on this arrow previously but is on this arrow now
						else if (previousArrowFoot == InvalidFoot && currentArrowFoot != InvalidArrowIndex)
						{
							addNormalStep = true;
						}

						// If a foot was on this arrow previously but is not on this arrow now
						// we do not need to add an event because even if held previously, a release event
						// will already have been added by the logic above.

						// Add a normal step. Either a tap, hold, or roll based off of the current state.
						if (addNormalStep)
						{
							switch (currentArrowState.State)
							{
								case GraphArrowState.Resting:
								{
									events.Add(new LaneTapNote
									{
										Position = stepNode.Position,
										Lane = arrow,
										Player = 0,
										SourceType = SMCommon.SNoteChars[(int)SMCommon.NoteType.Tap].ToString()
									});
									break;
								}
								case GraphArrowState.Held:
								case GraphArrowState.Rolling:
								{
									// Hold or Roll Start
									var holdRollType = currentArrowState.State == GraphArrowState.Held
										? SMCommon.NoteType.HoldStart
										: SMCommon.NoteType.RollStart;
									events.Add(new LaneHoldStartNote
									{
										Position = stepNode.Position,
										Lane = arrow,
										Player = 0,
										SourceType = SMCommon.SNoteChars[(int)holdRollType].ToString()
									});
									break;
								}
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
						SourceType = SMCommon.SNoteChars[(int)SMCommon.NoteType.Mine].ToString()
					});
				}

				// Advance
				currentNode = currentNode.Next;
			}

			return events;
		}
	}
}
