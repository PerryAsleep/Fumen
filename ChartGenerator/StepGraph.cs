using System;
using System.Collections.Generic;
using System.Linq;
using static ChartGenerator.Constants;
using Fumen;

namespace ChartGenerator
{
	/// <summary>
	/// The state a foot on an arrow in StepGraph can be in.
	/// There is no none / lifted state.
	/// Each foot is on one or more arrows each in one of these states.
	/// </summary>
	public enum GraphArrowState
	{
		Resting,
		Held,
		Rolling
	}

	/// <summary>
	/// Link between nodes in a StepGraph.
	/// Represents what each foot does to move from one GraphNode to a set of other GraphNodes.
	/// </summary>
	public class GraphLink : IEquatable<GraphLink>
	{
		/// <summary>
		/// The state of a foot and an arrow within a GraphLink.
		/// Structs provide a substantial performance gain over classes for FootArrowState.
		/// </summary>
		public struct FootArrowState
		{
			public StepType Step { get; }
			public FootAction Action { get; }
			public bool Valid { get; }

			public FootArrowState(StepType step, FootAction action)
			{
				Step = step;
				Action = action;
				Valid = true;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Step == f.Step && Action == f.Action && Valid == f.Valid;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + (int) Step);
				hash = unchecked(hash * 31 + (int) Action);
				hash = unchecked(hash * 31 + (Valid ? 1 : 0));
				return hash;
			}
			#endregion
		}

		// TODO: Document or remove the assumption that [1] will never be valid if [0] is not.
		/// <summary>
		/// The state of both feet.
		/// First index is the foot.
		/// Second index is the arrow under this foot ([0-MaxArrowsPerFoot)).
		/// </summary>
		public readonly FootArrowState[,] Links = new FootArrowState[NumFeet, MaxArrowsPerFoot];

		/// <summary>
		/// Whether or not this link represents a jump with both feet.
		/// Includes bracket jumps
		/// </summary>
		/// <returns>True if this link is a jump and false otherwise.</returns>
		public bool IsJump()
		{
			for (var f = 0; f < NumFeet; f++)
				if (!Links[f, 0].Valid || Links[f, 0].Action == FootAction.Release)
					return false;
			return true;
		}

		/// <summary>
		/// Whether or not this link represents a release with any foot.
		/// </summary>
		/// <returns>True if this link is a release and false otherwise.</returns>
		public bool IsRelease()
		{
			for (var f = 0; f < NumFeet; f++)
				if (Links[f, 0].Valid && Links[f, 0].Action == FootAction.Release)
					return true;
			return false;
		}

		/// <summary>
		/// Whether or not this link represents a step with one foot, regardless of if
		/// that is a bracket step.
		/// </summary>
		/// <param name="foot">The foot in question.</param>
		/// <returns>True if this link is a step with the given foot and false otherwise.</returns>
		public bool IsStepWithFoot(int foot)
		{
			var otherFoot = OtherFoot(foot);
			return Links[foot, 0].Valid && Links[foot, 0].Action != FootAction.Release && !Links[otherFoot, 0].Valid;
		}

		/// <summary>
		/// Whether or not this link represents a footswap.
		/// </summary>
		/// <param name="foot">
		/// Out param to store the foot which performed the swap, if this is a foot swap.
		/// </param>
		/// <returns>True if this link is a footswap and false otherwise.</returns>
		public bool IsFootSwap(out int foot)
		{
			foot = InvalidFoot;
			for (var f = 0; f < NumFeet; f++)
			{
				if (Links[f, 0].Valid && Links[f, 0].Step == StepType.FootSwap)
				{
					foot = f;
					return true;
				}
			}
			return false;
		}

		public bool Equals(GraphLink other)
		{
			if (other == null)
				return false;
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (!Links[f, a].Equals(other.Links[f, a]))
						return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphLink g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					hash = unchecked(hash * 31 + Links[f, a].GetHashCode());
			return hash;
		}
	}

	/// <summary>
	/// Node in a StepGraph.
	/// Connected to other GraphNodes by GraphLinks.
	/// One GraphLink may attach a GraphNode to multiple other GraphNodes.
	/// Represents the state of each foot. Each foot may be on one or two (MaxArrowsPerFoot) arrows.
	/// In this representation, a foot is never considered to be lifted or in the air.
	/// The GraphNodes represent where the players feet are after making a move.
	/// Mines aren't considered in this representation.
	/// Footswaps result in both feet resting on the same arrow.
	/// GraphNodes are considered equal if their state (but not necessarily GraphLinks) are equal.
	/// </summary>
	public class GraphNode : IEquatable<GraphNode>
	{
		/// <summary>
		/// The state of a foot and an arrow within a GraphNode.
		/// Structs provide a substantial performance gain over classes for FootArrowState.
		/// </summary>
		public struct FootArrowState
		{
			public int Arrow { get; }
			public GraphArrowState State { get; }

			public FootArrowState(int arrow, GraphArrowState state)
			{
				Arrow = arrow;
				State = state;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Arrow == f.Arrow && State == f.State;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + Arrow);
				hash = unchecked(hash * 31 + (int) State);
				return hash;
			}
			#endregion
		}

		/// <summary>
		/// Static FootArrowState instance for ease of setting up an invalid state in lue of null.
		/// </summary>
		public static readonly FootArrowState InvalidFootArrowState;
		static GraphNode()
		{
			InvalidFootArrowState = new FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);
		}

		/// <summary>
		/// The state of both feet.
		/// First index is the foot.
		/// Second index is the arrow under this foot ([0-MaxArrowsPerFoot)).
		/// </summary>
		public readonly FootArrowState[,] State;

		/// <summary>
		/// 
		/// </summary>
		public readonly BodyOrientation Orientation;

		/// <summary>
		/// GraphLinks to other GraphNodes.
		/// A GraphLink may connection more than one GraphNode.
		/// </summary>
		public Dictionary<GraphLink, List<GraphNode>> Links = new Dictionary<GraphLink, List<GraphNode>>();

		/// <summary>
		/// Constructor requiring the State for the GraphNode.
		/// Side effect: Sorts the given state.
		/// </summary>
		/// <param name="state">State for the GraphNode.</param>
		/// <param name="orientation">BodyOrientation for the GraphNode.</param>
		public GraphNode(FootArrowState[,] state, BodyOrientation orientation)
		{
			State = state;
			Orientation = orientation;

			// Sort so that comparisons are easier
			for (var foot = 0; foot < NumFeet; foot++)
			{
				// TODO: Remove assumption that MaxArrowsPerFoot == 2.
				if (State[foot, 0].Arrow <= State[foot, 1].Arrow)
					continue;
				var swap = State[foot, 0];
				State[foot, 0] = State[foot, 1];
				State[foot, 1] = swap;
			}
		}

		public bool Equals(GraphNode other)
		{
			if (other == null)
				return false;
			if (State.Length != other.State.Length)
				return false;
			// Relies on sorted State.
			for(var f = 0; f < NumFeet; f++)
				for(var a = 0; a < MaxArrowsPerFoot; a++)
					if (!State[f, a].Equals(other.State[f, a]))
						return false;
			if (Orientation != other.Orientation)
				return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphNode g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					hash = unchecked(hash * 31 + State[f, a].GetHashCode());
			hash = unchecked(hash * 31 + Orientation.GetHashCode());
			return hash;
		}
	}

	/// <summary>
	/// A graph of GraphNodes connected by GraphLinks representing all the positions on a set
	/// of arrows and the ways in which one can move between those positions.
	/// </summary>
	public class StepGraph
	{
		/// <summary>
		/// Cached StepType combinations that constitute valid jumps.
		/// First index is the index of the combination.
		/// Second index is foot for that combination.
		/// </summary>
		private static readonly StepType[][] JumpCombinations;
		/// <summary>
		/// Cached Foot order to use when filling jumps.
		/// First index is the index of the order to use when looping over all orders.
		/// Second index is array of foot indexes (e.g. [L,R] or [R,L]).
		/// </summary>
		private static readonly int[][] JumpFootOrder;
		/// <summary>
		/// Cached FootActions for a step.
		/// First index is the number of arrows of a single foot step in question.
		///  Length 2.
		///  Most StepTypes have 1 arrow, but brackets have 2.
		///  [0]: Length 1 FootAction combinations
		///  [1]: Length 2 FootAction combinations
		/// Second index is the set of actions.
		///  For example, for length 2 FootAction combinations we would have roughly 16 entries
		///  since it is combining the 4 FootActions with 4 more.
		/// Third index is the arrow for the foot in question.
		///  For most StepTypes this will be a length 1 arrow. For brackets this will be a
		///  length 2 array.
		/// </summary>
		private static readonly FootAction[][][] ActionCombinations;
		/// <summary>
		/// Cached number of arrows for each StepType.
		/// Most StepTypes are for one arrow. Brackets are for two.
		/// This array is used to provide the value for the first index in ActionCombinations.
		/// </summary>
		private static readonly int[] NumArrowsForStepType;
		/// <summary>
		/// Cached functions used to fill nodes of the StepGraph.
		/// Index is StepType.
		/// </summary>
		private static readonly Func<GraphNode, ArrowData[], int, int, int, FootAction[], List<GraphNode>>[] FillFuncs;

		/// <summary>
		/// Number of arrows in this StepGraph.
		/// </summary>
		public int NumArrows { get; private set; }
		/// <summary>
		/// The root GraphNode for this StepGraph.
		/// </summary>
		public GraphNode Root { get; private set; }
		/// <summary>
		/// ArrowData associated with this StepGraph.
		/// </summary>
		public ArrowData[] ArrowData { get; private set; }

		/// <summary>
		/// Static initializer. Caches data to static structures to improve search performance when
		/// creating a StepGraph instance.
		/// </summary>
		static StepGraph()
		{
			// Initialize JumpCombinations
			var jumpSingleSteps = new[]
			{
				StepType.SameArrow,
				StepType.NewArrow,
				StepType.BracketBothNew,
				StepType.BracketOneNew,
				StepType.BracketBothSame,
			};
			JumpCombinations = Combinations(jumpSingleSteps, NumFeet).ToArray();

			// Initialize JumpFootOrder
			JumpFootOrder = new []
			{
				new []{L, R},
				new []{R, L}
			};

			// Initialize ActionCombination
			ActionCombinations = new FootAction[MaxArrowsPerFoot][][];
			for (var i = 0; i < MaxArrowsPerFoot; i++)
			{
				var combinations = Combinations<FootAction>(i + 1);

				// For brackets you can never release and place at the same time
				// This would be split into two events, a release first, and a step after.
				// Prune those combinations out now so we don't loop over them and need to check for them
				// when searching.
				if (i >= 1)
				{
					combinations.RemoveAll(actions =>
					{
						var hasAtLeastOneRelease = false;
						var isAllReleases = true;
						foreach (var action in actions)
						{
							if (action == FootAction.Release)
								hasAtLeastOneRelease = true;
							else
								isAllReleases = false;
						}
						return hasAtLeastOneRelease != isAllReleases;
					});
				}

				ActionCombinations[i] = combinations.ToArray();
			}

			// Initialize NumArrowsForStepType
			var steps = Enum.GetValues(typeof(StepType)).Cast<StepType>().ToList();
			NumArrowsForStepType = new int[steps.Count];
			NumArrowsForStepType[(int)StepType.SameArrow] = 1;
			NumArrowsForStepType[(int)StepType.NewArrow] = 1;
			NumArrowsForStepType[(int)StepType.CrossoverFront] = 1;
			NumArrowsForStepType[(int)StepType.CrossoverBehind] = 1;
			NumArrowsForStepType[(int)StepType.InvertFront] = 1;
			NumArrowsForStepType[(int)StepType.InvertBehind] = 1;
			NumArrowsForStepType[(int)StepType.FootSwap] = 1;
			NumArrowsForStepType[(int)StepType.BracketBothNew] = 2;
			NumArrowsForStepType[(int)StepType.BracketOneNew] = 2;
			NumArrowsForStepType[(int)StepType.BracketBothSame] = 2;

			// Initialize FillFuncs
			FillFuncs = new Func<GraphNode, ArrowData[], int, int, int, FootAction[],
				List<GraphNode>>[steps.Count];
			FillFuncs[(int)StepType.SameArrow] = FillSameArrow;
			FillFuncs[(int)StepType.NewArrow] = FillNewArrow;
			FillFuncs[(int)StepType.CrossoverFront] = FillCrossoverFront;
			FillFuncs[(int)StepType.CrossoverBehind] = FillCrossoverBack;
			FillFuncs[(int)StepType.InvertFront] = FillInvertFront;
			FillFuncs[(int)StepType.InvertBehind] = FillInvertBack;
			FillFuncs[(int)StepType.FootSwap] = FillFootSwap;
			FillFuncs[(int)StepType.BracketBothNew] = FillBracketBothNew;
			FillFuncs[(int)StepType.BracketOneNew] = FillBracketOneNew;
			FillFuncs[(int)StepType.BracketBothSame] = FillBracketBothSame;
		}

		/// <summary>
		/// Private constructor.
		/// StepGraphs should be created using CreateStepGraph.
		/// </summary>
		private StepGraph() { }

		/// <summary>
		/// Creates a new StepGraph satisfying all movements for the given ArrowData with
		/// a root position at the given starting arrows.
		/// </summary>
		/// <param name="arrowData">ArrowData for the set of arrows to create a StepGraph for.</param>
		/// <param name="leftStartingArrow">Starting arrow for the left foot.</param>
		/// <param name="rightStartingArrow">Starting arrow for the right foot.</param>
		/// <returns>
		/// StepGraph satisfying all movements for the given ArrowData with a root position
		/// at the given starting arrows.
		/// </returns>
		public static StepGraph CreateStepGraph(ArrowData[] arrowData, int leftStartingArrow, int rightStartingArrow)
		{
			// Set up state for root node.
			var state = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					state[L, a] = new GraphNode.FootArrowState(leftStartingArrow, GraphArrowState.Resting);
					state[R, a] = new GraphNode.FootArrowState(rightStartingArrow, GraphArrowState.Resting);
				}
				else
				{
					state[L, a] = GraphNode.InvalidFootArrowState;
					state[R, a] = GraphNode.InvalidFootArrowState;
				}
			}

			var root = new GraphNode(state, BodyOrientation.Normal);
			FillStepGraph(root, arrowData);
			return new StepGraph
			{
				NumArrows = arrowData.Length,
				Root = root,
				ArrowData = arrowData
			};
		}

		private static void FillStepGraph(GraphNode root, ArrowData[] arrowData)
		{
			Logger.Info($"Generating {arrowData.Length}-panel StepGraph.");

			var completeNodes = new HashSet<GraphNode>();
			var visitedNodes = new HashSet<GraphNode>();
			var currentNodes = new List<GraphNode> {root};
			int level = 0;
			while (currentNodes.Count > 0)
			{
				Logger.Info($"Level {level + 1}: Searching {currentNodes.Count} nodes...");

				var allChildren = new HashSet<GraphNode>();
				foreach (var currentNode in currentNodes)
				{
					visitedNodes.Add(currentNode);

					// Fill node
					foreach (var stepType in Enum.GetValues(typeof(StepType)).Cast<StepType>())
						FillSingleFootStep(currentNode, visitedNodes, arrowData, stepType);
					foreach (var jump in JumpCombinations)
						FillJump(currentNode, visitedNodes, arrowData, jump);

					// Collect children
					foreach (var linkEntry in currentNode.Links)
						foreach (var childNode in linkEntry.Value)
							allChildren.Add(childNode);

					// Mark node complete
					completeNodes.Add(currentNode);
				}

				// Remove all complete nodes
				var previousCount = allChildren.Count;
				allChildren.RemoveWhere(n => completeNodes.Contains(n));

				Logger.Info($"Level {level + 1}: Found {allChildren.Count} children (pruned from {previousCount}).");

				// Search one level deeper
				currentNodes = allChildren.ToList();
				level++;
			}

			Logger.Info($"{arrowData.Length}-panel StepGraph generation complete. {completeNodes.Count} Nodes.");
		}

		private static void AddNode(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			GraphNode newNode,
			GraphLink link)
		{
			if (visitedNodes.TryGetValue(newNode, out var visitedNode))
				newNode = visitedNode;
			else
				visitedNodes.Add(newNode);

			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);
		}

		private static void FillSingleFootStep(GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			ArrowData[] arrowData,
			StepType stepType)
		{
			var numStepArrows = NumArrowsForStepType[(int)stepType];
			var fillFunc = FillFuncs[(int)stepType];
			var actionSets = ActionCombinations[numStepArrows - 1];
			var numArrows = arrowData.Length;
			for (var currentIndex = 0; currentIndex < numArrows; currentIndex++)
			{
				for (var newIndex = 0; newIndex < numArrows; newIndex++)
				{
					for (int foot = 0; foot < NumFeet; foot++)
					{
						for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
						{
							var newNodes = fillFunc(currentNode, arrowData, currentIndex, newIndex, foot,
								actionSets[setIndex]);
							if (newNodes == null || newNodes.Count == 0)
								continue;

							foreach (var newNode in newNodes)
							{
								var link = new GraphLink();
								for (var f = 0; f < numStepArrows; f++)
								{
									link.Links[foot, f] = new GraphLink.FootArrowState(stepType, actionSets[setIndex][f]);
								}

								AddNode(currentNode, visitedNodes, newNode, link);
							}
						}
					}
				}
			}
		}

		private static void FillJump(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			ArrowData[] arrowData,
			StepType[] stepTypes)
		{
			// When filling a jump we fill one foot at a time, then pass the state that has been altered by the first foot
			// to the next foot to complete the jump.
			// We need to loop over the feet in both orders to ensure all valid states are hit.
			// For example, if we only process left then right we will miss a jump where both feet us NewArrow and the jump
			// goes from LU to UR since the right foot would not have moved yet to make room for the left.
			foreach (var footOrder in JumpFootOrder)
			{
				var f1 = footOrder[0];
				var f2 = footOrder[1];

				// Find all the states from moving the first foot.
				var actionsToNodesF1 = FillJumpStep(currentNode, arrowData, stepTypes[f1], f1);
				foreach (var actionNodeF1 in actionsToNodesF1)
				{
					foreach (var newNodeF1 in actionNodeF1.Value)
					{
						// Using the state from the first foot, find all the states from moving the second foot.
						var actionsToNodesF2 = FillJumpStep(newNodeF1, arrowData, stepTypes[f2], f2);
						foreach (var actionNodeF2 in actionsToNodesF2)
						{
							foreach (var newNodeF2 in actionNodeF2.Value)
							{
								// Set up a link for the final state and add a node.
								var link = new GraphLink();
								for (var f = 0; f < NumArrowsForStepType[(int)stepTypes[f1]]; f++)
									link.Links[f1, f] = new GraphLink.FootArrowState(stepTypes[f1], actionNodeF1.Key[f]);
								for (var f = 0; f < NumArrowsForStepType[(int)stepTypes[f2]]; f++)
									link.Links[f2, f] = new GraphLink.FootArrowState(stepTypes[f2], actionNodeF2.Key[f]);
								AddNode(currentNode, visitedNodes, newNodeF2, link);
							}
						}
					}
				}
			}
		}

		private static Dictionary<FootAction[], List<GraphNode>> FillJumpStep(
			GraphNode currentNode,
			ArrowData[] arrowData,
			StepType stepType,
			int foot)
		{
			var result = new Dictionary<FootAction[], List<GraphNode>>();

			var numArrows = arrowData.Length;
			var numStepArrows = NumArrowsForStepType[(int)stepType];
			var fillFunc = FillFuncs[(int)stepType];
			var actionSets = ActionCombinations[numStepArrows - 1];
			for (var currentIndex = 0; currentIndex < numArrows; currentIndex++)
			{
				for (var newIndex = 0; newIndex < numArrows; newIndex++)
				{
					for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
					{
						var newNodes = fillFunc(currentNode, arrowData, currentIndex, newIndex, foot, actionSets[setIndex]);
						if (newNodes == null || newNodes.Count == 0)
							continue;
						if (!result.TryGetValue(actionSets[setIndex], out var nodeLists))
							nodeLists = newNodes;
						else
							nodeLists.AddRange(newNodes);
						result[actionSets[setIndex]] = nodeLists;
					}
				}
			}

			return result;
		}

		private static List<GraphNode> FillNewArrow(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;

			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;
			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Only consider moving to a new arrow when the currentIndex is on an arrow
			if (!IsOn(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new arrow if already holding on MaxArrowsPerFoot
			var numHeld = NumHeldOrRolling(currentState, foot);
			if (numHeld >= MaxArrowsPerFoot)
				return null;
			// If bracketing, skip if this is not a valid bracketable pairing.
			if (numHeld == 1 && !arrowData[currentIndex].BracketablePairings[foot][newIndex])
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;
			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			if (!IsValidPairingWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Set up the state for a new node.
			// Copy the previous state, but lift from any resting arrows for the given foot.
			// Leave holds/rolls.
			var otherFoot = OtherFoot(foot);
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				newState[otherFoot, a] = currentState[otherFoot, a];

				if (IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[foot, a].Arrow == InvalidArrowIndex)
				{
					newState[foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			// Stepping on a new arrow implies a normal orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private static List<GraphNode> FillSameArrow(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;

			// Must be same arrow.
			if (currentIndex != newIndex)
				return null;
			// Release logic. Lift from a hold or roll.
			if (footAction == FootAction.Release && !IsHeldOrRolling(currentState, currentIndex, foot))
				return null;
			// Normal logic. Placement action on a resting arrow.
			if (footAction != FootAction.Release && !IsResting(currentState, currentIndex, foot))
				return null;

			var otherFoot = OtherFoot(foot);

			// If this follows a footswap then you cannot perform this action.
			// Ideally we should only prevent a same arrow step after a foot swap with the other foot since
			// there is nothing wrong with tapping twice after a swap, but we are unable to that when
			// making the StepGraph as we cannot know how the currentState was entered. The only options
			// are to forbid any same arrow step if it follows a swap with either feet, or allow it
			// all the time and then when making an ExpressedChart give a same arrow step movement following
			// a swap with the other foot a high cost. I can't think of any scenario where it would
			// actually be beneficial to do some kind of paradiddle pattern on one arrow so for now I am
			// going to prevent it always by not including it in the StepGraph.
			if (footAction != FootAction.Release && IsResting(currentState, currentIndex, otherFoot))
				return null;

			// Set up the state for a new node.
			// Copy the previous state and if placing a new foot, lift from any resting arrows.
			// It is necessary to lift for, e.g. the step after an SP quad.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				newState[otherFoot, a] = currentState[otherFoot, a];

				if (footAction != FootAction.Release && IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else if (footAction == FootAction.Release && IsHeldOrRollingOnArrowAtIndex(currentState, a, newIndex, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[foot, a].Arrow == InvalidArrowIndex)
				{
					newState[foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			// Stepping on the same arrow maintains the previous state's orientation.
			return new List<GraphNode> { new GraphNode(newState, currentNode.Orientation) };
		}

		private static List<GraphNode> FillFootSwap(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;

			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;

			var otherFoot = OtherFoot(foot);

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// The new index must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState, newIndex, otherFoot))
				return null;
			// Disallow foot swap if this foot is holding or rolling
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Disallow foot swap if the other foot is holding or rolling
			if (NumHeldOrRolling(currentState, otherFoot) > 0)
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// The other foot should remain Resting on the newIndex, even though it is slightly lifted
				if (currentState[otherFoot, a].Arrow == newIndex)
					newState[otherFoot, a] = new GraphNode.FootArrowState(newIndex, GraphArrowState.Resting);
				// All other arrows under the other foot should be lifted.
				else
					newState[otherFoot, a] = GraphNode.InvalidFootArrowState;
				// The first arrow under the foot in the new state should be at the newIndex, with the appropriate state.
				if (a == 0)
					newState[foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
				// All other arrows under the foot should be lifted. 
				else
					newState[foot, a] = GraphNode.InvalidFootArrowState;
			}

			// Footswaps correct inverted orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private static List<GraphNode> FillCrossoverFront(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, arrowData, currentIndex, newIndex, foot, footActions, true);
		}

		private static List<GraphNode> FillCrossoverBack(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, arrowData, currentIndex, newIndex, foot, footActions, false);
		}

		private static List<GraphNode> FillCrossoverInternal(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;

			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;
			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot crossover if any arrows are held by this foot.
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is not a crossover
			if (front && !FootCrossesOverInFrontWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;
			if (!front && !FootCrossesOverInBackWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			var otherFoot = OtherFoot(foot);

			// If the current state is already a crossover of the same type (front or back)
			// with the other foot then we cannot create another crossover of that type
			// with this foot as the legs would cross through each other (or you would be spun).
			if (front && FootCrossedOverInFront(currentState, otherFoot, arrowData))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot, arrowData))
				return null;

			// Skip if the current state is inverted and this crossover faces the player in
			// the other direction since this would cross their legs through each other.
			if (currentNode.Orientation == BodyOrientation.InvertedRightOverLeft
			    && ((front && foot == L) || (!front && foot == R)))
				return null;
			if (currentNode.Orientation == BodyOrientation.InvertedLeftOverRight
				&& ((front && foot == R) || (!front && foot == L)))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// Copy previous state for other foot
				newState[otherFoot, a] = currentState[otherFoot, a];

				// Lift any resting arrows for the given foot.
				if (IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			// Set up the FootArrowState for the new arrow
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[foot, a].Arrow == InvalidArrowIndex)
				{
					newState[foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			// Crossovers are not inverted.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private static List<GraphNode> FillInvertFront(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, arrowData, currentIndex, newIndex, foot, footActions, true);
		}

		private static List<GraphNode> FillInvertBack(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, arrowData, currentIndex, newIndex, foot, footActions, false);
		}

		private static List<GraphNode> FillInvertInternal(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;

			// Must be new arrow.
			if (currentIndex == newIndex)
				return null;
			// Cannot release on a new arrow
			if (footAction == FootAction.Release)
				return null;
			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot invert if any arrows are held by this foot.
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!arrowData[currentIndex].ValidNextArrows[newIndex])
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is not an inversion
			if (!FootInvertsWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			var otherFoot = OtherFoot(foot);
			var orientation = ((front && foot == R) || (!front && foot == L)) ?
				BodyOrientation.InvertedRightOverLeft : BodyOrientation.InvertedLeftOverRight;

			// If the current state is already inverted in the opposite orientation, skip.
			// The player needs to right themselves first.
			if (currentNode.Orientation != BodyOrientation.Normal && currentNode.Orientation != orientation)
				return null;

			// If the current state is crossed over with the same type (front or back)
			// with the other foot then we cannot invert with the same type with
			// this foot as the legs would cross through each other.
			if (front && FootCrossedOverInFront(currentState, otherFoot, arrowData))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot, arrowData))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// Copy previous state for other foot
				newState[otherFoot, a] = currentState[otherFoot, a];

				// Lift any resting arrows for the given foot.
				if (IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			// Set up the FootArrowState for the new arrow
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (newState[foot, a].Arrow == InvalidArrowIndex)
				{
					newState[foot, a] = new GraphNode.FootArrowState(newIndex, StateAfterAction(footAction));
					break;
				}
			}

			return new List<GraphNode> { new GraphNode(newState, orientation) };
		}

		/// <summary>
		/// Assumes that footActions length is MaxArrowsPerFoot.
		/// Assumes that if one FootAction is a release, they are all a release.
		/// </summary>
		private static List<GraphNode> FillBracketBothNew(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// Cannot release on a new arrow.
			// If one is a release they are both a release due to ActionCombinations creation logic.
			if (footActions[0] == FootAction.Release)
				return null;

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;
			// Skip if this next arrow is occupied.
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[foot][secondIndex])
					continue;
				// Skip if this next arrow is occupied.
				if (!IsFree(currentState, secondIndex))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, secondIndex))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, secondIndex);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[secondIndex];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				return new List<GraphNode>
					{CreateNewBracketNode(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static List<GraphNode> FillBracketOneNew(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// Cannot release on a new arrow.
			// If one is a release they are both a release due to ActionCombinations creation logic.
			if (footActions[0] == FootAction.Release)
				return null;

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (!IsResting(currentState, currentIndex, foot))
				return null;
			// Cannot step on a new bracket if already holding on an arrow
			if (NumHeldOrRolling(currentState, foot) > 0)
				return null;

			var results = new List<GraphNode>();
			var resultFirst = FillBracketFirstNew(currentNode, arrowData, currentIndex, newIndex, foot, footActions);
			var resultSecond = FillBracketSecondNew(currentNode, arrowData, currentIndex, newIndex, foot, footActions);
			if (resultFirst != null && resultFirst.Count > 0)
				results.AddRange(resultFirst);
			if (resultSecond != null && resultSecond.Count > 0)
				results.AddRange(resultSecond);
			return results;
		}

		private static List<GraphNode> FillBracketFirstNew(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// The first index must be a step on a new arrow
			if (!IsFree(currentState, newIndex))
				return null;
			// Skip if this next arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, newIndex))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var results = new List<GraphNode>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[foot][secondIndex])
					continue;
				// The second index must be a step on the same arrow (only the first is new)
				if (!IsResting(currentState, secondIndex, foot))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, secondIndex))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, secondIndex);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[secondIndex];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(currentState, foot, newIndex, secondIndex, footActions));
			}

			return results;
		}

		private static List<GraphNode> FillBracketSecondNew(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// The first index must be a step on the same arrow (only the second is new)
			if (!IsResting(currentState, newIndex, foot))
				return null;

			// Skip if this next arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, newIndex);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var firstNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[newIndex];

			var results = new List<GraphNode>();
			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (!arrowData[newIndex].BracketablePairings[foot][secondIndex])
					continue;
				// The second index must be a step on a new arrow
				if (!IsFree(currentState, secondIndex))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, arrowData, secondIndex))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, arrowData, secondIndex);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = arrowData[currentIndex].ValidNextArrows[secondIndex];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(currentState, foot, newIndex, secondIndex, footActions));
			}

			return results;
		}

		private static List<GraphNode> FillBracketBothSame(
			GraphNode currentNode,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// Must be all releases or all placements.
			// This check is performed when creating ActionCombinations so we do not
			// need to perform it again here.
			var releasing = footActions[0] == FootAction.Release;

			// Check to make sure we are acting on the same arrow.
			// This is to ensure we do not process the bracket twice from the caller.
			if (currentIndex != newIndex)
				return null;

			// Check for state at newIndex matching expected state for whether releasing or placing
			if (!releasing && !IsResting(currentState, newIndex, foot))
				return null;
			if (releasing && !IsHeldOrRolling(currentState, newIndex, foot))
				return null;

			for (var secondIndex = newIndex + 1; secondIndex < arrowData.Length; secondIndex++)
			{
				// Check for state at secondIndex matching expected state for whether releasing or placing
				if (!releasing && !IsResting(currentState, secondIndex, foot))
					continue;
				if (releasing && !IsHeldOrRolling(currentState, secondIndex, foot))
					continue;

				// Set up the state for a new node.
				return new List<GraphNode>
					{CreateNewBracketNode(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static GraphNode CreateNewBracketNode(
			GraphNode.FootArrowState[,] currentState,
			int foot,
			int firstIndex,
			int secondIndex,
			FootAction[] footActions)
		{
			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			var otherFoot = OtherFoot(foot);
			// The other foot doesn't change
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				newState[otherFoot, a] = currentState[otherFoot, a];
			// The given foot brackets the two new arrows
			newState[foot, 0] = new GraphNode.FootArrowState(firstIndex, StateAfterAction(footActions[0]));
			newState[foot, 1] = new GraphNode.FootArrowState(secondIndex, StateAfterAction(footActions[1]));
			// Brackets are not inverted.
			return new GraphNode(newState, BodyOrientation.Normal);
		}

		private static bool IsValidPairingWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, ArrowData[] arrowData,
			int arrow)
		{
			return GetValidPairingsWithOtherFoot(state, foot, arrowData, arrow).Count > 0;
		}

		private static List<int> GetValidPairingsWithOtherFoot(GraphNode.FootArrowState[,] state, int foot,
			ArrowData[] arrowData, int arrow)
		{
			var result = new List<int>();
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairings[otherFoot][arrow])
					result.Add(otherFootArrowIndex);
			}

			return result;
		}

		private static bool FootCrossedOverInFront(GraphNode.FootArrowState[,] state, int foot, ArrowData[] arrowData)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var footArrowIndex = state[foot, a].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInFrontWithAnyOtherFoot(state, foot, arrowData, footArrowIndex))
					return true;
			}
			return false;
		}

		private static bool FootCrossedOverInBack(GraphNode.FootArrowState[,] state, int foot, ArrowData[] arrowData)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var footArrowIndex = state[foot, a].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInBackWithAnyOtherFoot(state, foot, arrowData, footArrowIndex))
					return true;
			}
			return false;
		}

		private static bool FootCrossesOverWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, ArrowData[] arrowData,
			int arrow)
		{
			return FootCrossesOverInFrontWithAnyOtherFoot(state, foot, arrowData, arrow)
			       || FootCrossesOverInBackWithAnyOtherFoot(state, foot, arrowData, arrow);
		}

		private static bool FootCrossesOverInFrontWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot,
			ArrowData[] arrowData, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverFront[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private static bool FootCrossesOverInBackWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot,
			ArrowData[] arrowData, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverBehind[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private static bool FootInvertsWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot,
			ArrowData[] arrowData, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && arrowData[otherFootArrowIndex].OtherFootPairingsInverted[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private static bool IsFree(GraphNode.FootArrowState[,] state, int arrow)
		{
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (state[f, a].Arrow == arrow)
						return false;
			return true;
		}

		private static int NumHeldOrRolling(GraphNode.FootArrowState[,] state, int foot)
		{
			var num = 0;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow != InvalidArrowIndex
				    && (state[foot, a].State == GraphArrowState.Held
				        || state[foot, a].State == GraphArrowState.Rolling))
					num++;
			return num;
		}

		private static bool IsHeldOrRolling(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow == arrow
				    && (state[foot, a].State == GraphArrowState.Held
				        || state[foot, a].State == GraphArrowState.Rolling))
					return true;
			return false;
		}

		private static bool IsHeldOrRollingOnArrowAtIndex(GraphNode.FootArrowState[,] state, int a, int arrow, int foot)
		{
			return state[foot, a].Arrow == arrow 
			       && (state[foot, a].State == GraphArrowState.Held
			           || state[foot, a].State == GraphArrowState.Rolling);
		}

		private static bool IsResting(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow == arrow && state[foot, a].State == GraphArrowState.Resting)
					return true;
			return false;
		}

		private static bool IsStateRestingAtIndex(GraphNode.FootArrowState[,] state, int a, int foot)
		{
			if (state[foot, a].Arrow != InvalidArrowIndex && state[foot, a].State == GraphArrowState.Resting)
				return true;
			return false;
		}

		private static bool IsOn(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow == arrow)
					return true;
			return false;
		}

		private static List<T[]> Combinations<T>(int size) where T : Enum
		{
			return Combinations(Enum.GetValues(typeof(T)).Cast<T>().ToList(), size);
		}

		private static List<T[]> Combinations<T>(IEnumerable<T> elements, int size)
		{
			var result = new List<T[]>();
			if (size < 1)
				return result;

			var elementList = elements.ToList();
			var len = elementList.Count;
			var indices = new int[size];

			bool Inc()
			{
				var i = size - 1;
				while (i >= 0 && indices[i] == len - 1)
				{
					indices[i] = 0;
					i--;
				}

				if (i < 0)
					return false;
				indices[i]++;
				return true;
			}

			do
			{
				var r = new T[size];
				for (var i = 0; i < size; i++)
					r[i] = elementList[indices[i]];
				result.Add(r);
			} while (Inc());

			return result;
		}
	}
}
