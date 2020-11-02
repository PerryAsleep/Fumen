using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Fumen.Converters;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	/// <summary>
	/// Search node for performing a search through an ExpressedChart.
	/// </summary>
	public class SearchNode
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
		public List<int> Indices = new List<int> ();
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

	public class ChartSearch
	{
		/// <summary>
		/// Iteratively searches for a series of GraphNodes that satisfy the ExpressedChart's StepEvents.
		/// </summary>
		/// <param name="stepGraphRoot">
		/// GraphNode representing the root of a graph of all possible states that can be traversed.
		/// </param>
		/// <param name="chart">ExpressedChart to search.</param>
		/// <param name="random">Random for traversing links in the Graph in a random order at each depth.</param>
		/// <returns>
		/// SearchNode representing the first node in a series that represents a valid series of GraphNodes
		/// through the ExpressedChart.
		/// </returns>
		static SearchNode SearchExpressedChart(GraphNode stepGraphRoot, ExpressedChart chart, Random random)
		{
			var root = new SearchNode
			{
				GraphNode = stepGraphRoot
			};

			var currentNode = root;
			while(true)
			{
				// Finished
				if (currentNode.Depth >= chart.StepEvents.Count)
					break;

				// Dead end
				while (currentNode.GraphNode.Links[chart.StepEvents[currentNode.Depth].Link] == null
				       || currentNode.CurrentIndex > currentNode.GraphNode.Links[chart.StepEvents[currentNode.Depth].Link].Count - 1)
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

				var links = currentNode.GraphNode.Links[chart.StepEvents[currentNode.Depth].Link];

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

			return root;
		}

		//public class ChartSearchNode
		//{
		//	public GraphNode GraphNode;
		//	public int Depth;
		//	public int Cost;

		//	public Dictionary<StepType, ChartSearchNode> NextNodes;
		//	public ChartSearchNode PreviousNode;
		//}

		private static void ParseNextEvents(
			List<Event> events,
			ref int eventIndex,
			out List<Event> releases,
			out List<Event> mines,
			out List<Event> steps)
		{
			releases = new List<Event>();
			mines = new List<Event>();
			steps = new List<Event>();

			if (eventIndex >= events.Count)
				return;

			var pos = events[eventIndex].Position;
			while (eventIndex < events.Count && events[eventIndex].Position == pos)
			{
				if (events[eventIndex] is LaneHoldEndNote)
					releases.Add(events[eventIndex]);
				else if (events[eventIndex] is LaneNote ln && ln.SourceType == SMCommon.NoteType.Mine.ToString())
					mines.Add(events[eventIndex]);
				else if (events[eventIndex] is LaneHoldStartNote || events[eventIndex] is LaneTapNote)
					steps.Add(events[eventIndex]);
				eventIndex++;
			}
		}

		//public static ExpressedChart FindExpressedChart(List<Event> events, GraphNode stepGraphRoot)
		//{
		//	var root = new ChartSearchNode
		//	{
		//		GraphNode = stepGraphRoot
		//	};

		//	int eventIndex = 0;
		//	int numEvents = events.Count;
		//	var currentLeaves = new List<ChartSearchNode> {root};
		//	while (true)
		//	{
		//		if (eventIndex >= numEvents)
		//		{
		//			// choose path with lowest cost
		//			break;
		//		}

		//		ParseNextEvents(events, ref eventIndex, out var releases, out var mines, out var steps);

		//		// Get releases
		//			// create an event for all the releases
		//			// same, looping path, though the loop will be small because releases are so restrictive
		//		switch (releases.Count)
		//		{
		//			case 4:
		//				break;
		//			case 3:
		//				break;
		//			case 2:
		//				break;
		//			case 1:
		//			{
		//				var possibleSteps = new StepType[]
		//				{
		//					StepType.LSameArrow,
		//					StepType.RSameArrow,
		//					StepType.RBracketSameArrow,
		//					StepType.LBracketSameArrow,

		//				};
		//				var newLeaves = new List<ChartSearchNode>();
		//				foreach (var node in currentLeaves)
		//				{
		//					// LSameArrow

		//					// I am thinking now it makes more sense to incorporate the
		//					// releases into the StepType enum.
		//					//	because without it I can't just use the GraphNode to search for the next state here
		//					//	I need to know which next states are actually for releases otherwise we might pick
		//					//	a state that actually steps down again
		//					//		wait would it, or would it be correct because the only next state with 1 held from
		//					//		LSameArrow is a release?
		//					//			but I don't even want to check if the arrows are held in the first place, right?
		//					//	Is there a solution where you keep the normal StepType but the links in the graph aren't
		//					//	by just StepType alone, but by StepType AND FootAction?
		//					//		so then on chord jumps, you loop over the FootAction (tap/roll/hold) for EACH arrow
		//					// But that would also mean holds? and that is going to explode.
		//					// Fuck I think it is already going to explode with like, a 3 note bracket and 1 is
		//					// a hold and two arent. You need to go a new state that actually represents that
		//					// - 1 hold, 2 resting
		//					// here is a problem,
		//					// what if the original chart has a jump hold with one roll and one hold
		//					// the roll is 1 measure long, the hold is 2. It matters which is which
		//					// how do we capture that?

		//					if (node.GraphNode.State.Count(s => s == GraphArrowState.LHeld) == 1)
		//					{
		//						for (int arrow = 0; arrow < node.GraphNode.State.Length; arrow++)
		//						{
		//							if (node.GraphNode.State[arrow] == GraphArrowState.LHeld)
		//							{
		//								var nextNode = new ChartSearchNode
		//								{
		//									Depth = node.Depth + 1,
		//									Cost = node.Cost + 1,
		//									PreviousNode = node,
		//									GraphNode = 
		//								};
		//								node.NextNodes.Add(StepType.LSameArrow, nextNode);
		//								newLeaves.Add(nextNode);
		//							}
		//						}
		//					}
		//				}

		//				currentLeaves = newLeaves;
		//				break;
		//			}
		//		}


		//		// Get mines and record them

		//		// Get taps/holds/rolls

		//		// For each path
		//		// Loop over every step type's links
		//		// If that link takes you to a graph node that matches the current event, add a path
		//		// need to weight the path
		//		// this is where you take into account was there a mine or a hold which indicates a nebulous
		//		// arrow, or a foot swap


		//		// Prune paths with the shared state but higher costs
		//	}

		//	return null;
		//}
	}
}
