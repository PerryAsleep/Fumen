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

		private class StepPerformanceNode : PerformanceNode
		{
			public GraphNode GraphNode;
			public GraphLink GraphLink;
		}

		private class MinePerformanceNode : PerformanceNode
		{
			public int Arrow;
		}

		private readonly PerformanceNode Root = new PerformanceNode();
		private readonly int NumArrows;

		private PerformedChart() {}

		private PerformedChart(int numArrows)
		{
			NumArrows = numArrows;
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
			var performedChart = new PerformedChart(stepGraph.NumArrows);

			var random = new Random();

			var root = new SearchNode
			{
				GraphNode = stepGraph.Root
			};

			var currentNode = root;
			while (true)
			{
				// Finished
				if (currentNode.Depth >= expressedChart.StepEvents.Count)
					break;

				// Dead end
				while (currentNode.GraphNode.Links[expressedChart.StepEvents[currentNode.Depth].Link] == null
					   || currentNode.CurrentIndex >
					   currentNode.GraphNode.Links[expressedChart.StepEvents[currentNode.Depth].Link].Count - 1)
				{
					// Back up
					var prevNode = currentNode.PreviousNode;
					if (prevNode != null)
					{
						prevNode.NextNode = null;
						currentNode = prevNode;
					}
					else
					{
						// Failed to find a path
						return null;
					}
				}

				var links = currentNode.GraphNode.Links[expressedChart.StepEvents[currentNode.Depth].Link];

				// If this node's indices have not been set up, set them up
				if (currentNode.CurrentIndex == 0)
				{
					for (var i = 0; i < links.Count; i++)
						currentNode.Indices.Add(i);
					currentNode.Indices = currentNode.Indices.OrderBy(a => random.Next()).ToList();
				}

				// We can search further, pick a new index and advance
				var nextGraphNode = links[currentNode.Indices[currentNode.CurrentIndex++]];
				var newNode = new SearchNode
				{
					PreviousNode = currentNode,
					Depth = currentNode.Depth + 1,
					GraphNode = nextGraphNode
				};
				currentNode.NextNode = newNode;
				currentNode = newNode;
			}

			// TODO: Mines
			foreach (var mineEvent in expressedChart.MineEvents)
			{
				switch (mineEvent.Type)
				{
					case MineType.AfterArrow:
					{
						break;
					}

					case MineType.BeforeArrow:
					{
						// Scan ahead to find which arrow is in the nth closest.


						// If there are more than one arrow, prefer the one matching the foot for this mine event


						// Make sure that arrow is free now

						// If the arrow is not free, repeat the scan for n - 1 until an arrow is found.

						// If no arrow is found, skip the mine?


						break;
					}
					case MineType.NoArrow:
					{
						break;
					}
				}

			}

			currentNode = root;
			while (currentNode != null)
			{

				currentNode = currentNode.NextNode;
			}

			return performedChart;
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
