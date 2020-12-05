using System;
using System.Collections.Generic;
using System.Linq;
using static GenDoublesStaminaCharts.Constants;
using Fumen;

namespace GenDoublesStaminaCharts
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
			public SingleStepType Step { get; }
			public FootAction Action { get; }
			public bool Valid { get; }

			public FootArrowState(SingleStepType step, FootAction action)
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
		/// <returns>True if this link is a footswap and false otherwise.</returns>
		public bool IsFootSwap()
		{
			for (var f = 0; f < NumFeet; f++)
				if (!Links[f, 0].Valid || Links[f, 0].Step == SingleStepType.FootSwap)
					return true;
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
		/// GraphLinks to other GraphNodes.
		/// A GraphLink may connection more than one GraphNode.
		/// </summary>
		public Dictionary<GraphLink, List<GraphNode>> Links = new Dictionary<GraphLink, List<GraphNode>>();

		/// <summary>
		/// Constructor requiring the State for the GraphNode.
		/// Side effect: Sorts the given state.
		/// </summary>
		/// <param name="state">State for the GraphNode.</param>
		public GraphNode(FootArrowState[,] state)
		{
			State = state;

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
		/// Cached SingleStepType combinations that constitute valid jumps.
		/// First index is the index of the combination.
		/// Second index is foot for that combination.
		/// </summary>
		private static readonly SingleStepType[][] JumpCombinations;
		/// <summary>
		/// Cached FootActions for a step.
		/// First index is the number of arrows of a single foot step in question.
		///  Length 2.
		///  Most SingleStepTypes have 1 arrow, but brackets have 2.
		///  [0]: Length 1 FootAction combinations
		///  [1]: Length 2 FootAction combinations
		/// Second index is the set of actions.
		///  For example, for length 2 FootAction combinations we would have roughly 16 entries
		///  since it is combining the 4 FootActions with 4 more.
		/// Third index is the arrow for the foot in question.
		///  For most SingleStepTypes this will be a length 1 arrow. For brackets this will be a
		///  length 2 array.
		/// </summary>
		private static readonly FootAction[][][] ActionCombinations;
		/// <summary>
		/// Cached number of arrows for each SingleStepType.
		/// Most SingleStepTypes are for one arrow. Brackets are for two.
		/// This array is used to provide the value for the first index in ActionCombinations.
		/// </summary>
		private static readonly int[] NumArrowsForStepType;
		/// <summary>
		/// Cached functions used to fill nodes of the StepGraph.
		/// Index is SingleStepType.
		/// </summary>
		private static readonly Func<GraphNode.FootArrowState[,], ArrowData[], int, int, int, FootAction[],
			List<GraphNode.FootArrowState[,]>>[] FillFuncs;

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
				SingleStepType.SameArrow,
				SingleStepType.NewArrow,
				SingleStepType.BracketBothNew,
				SingleStepType.BracketOneNew,
				SingleStepType.BracketBothSame,
			};
			JumpCombinations = Combinations(jumpSingleSteps, NumFeet).ToArray();

			// Initialize ActionCombination
			ActionCombinations = new FootAction[MaxArrowsPerFoot][][];
			for (var i = 0; i < MaxArrowsPerFoot; i++)
			{
				var combinations = Combinations<FootAction>(i + 1);

				// For brackets you can never release and place at the same time
				// This would be split into two events, a release first, and a step after.
				// Prune those combinations out now so we don't loop over them and need to check for them
				// when searching.
				if (i > 1)
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
			var steps = Enum.GetValues(typeof(SingleStepType)).Cast<SingleStepType>().ToList();
			NumArrowsForStepType = new int[steps.Count];
			NumArrowsForStepType[(int)SingleStepType.SameArrow] = 1;
			NumArrowsForStepType[(int)SingleStepType.NewArrow] = 1;
			NumArrowsForStepType[(int)SingleStepType.CrossoverFront] = 1;
			NumArrowsForStepType[(int)SingleStepType.CrossoverBehind] = 1;
			NumArrowsForStepType[(int)SingleStepType.FootSwap] = 1;
			NumArrowsForStepType[(int)SingleStepType.BracketBothNew] = 2;
			NumArrowsForStepType[(int)SingleStepType.BracketOneNew] = 2;
			NumArrowsForStepType[(int)SingleStepType.BracketBothSame] = 2;

			// Initialize FillFuncs
			FillFuncs = new Func<GraphNode.FootArrowState[,], ArrowData[], int, int, int, FootAction[],
				List<GraphNode.FootArrowState[,]>>[steps.Count];
			FillFuncs[(int)SingleStepType.SameArrow] = FillSameArrow;
			FillFuncs[(int)SingleStepType.NewArrow] = FillNewArrow;
			FillFuncs[(int)SingleStepType.CrossoverFront] = FillCrossoverFront;
			FillFuncs[(int)SingleStepType.CrossoverBehind] = FillCrossoverBack;
			FillFuncs[(int)SingleStepType.FootSwap] = FillFootSwap;
			FillFuncs[(int)SingleStepType.BracketBothNew] = FillBracketBothNew;
			FillFuncs[(int)SingleStepType.BracketOneNew] = FillBracketOneNew;
			FillFuncs[(int)SingleStepType.BracketBothSame] = FillBracketBothSame;
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

			var root = new GraphNode(state);
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
					foreach (var stepType in Enum.GetValues(typeof(SingleStepType)).Cast<SingleStepType>())
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

		private static GraphNode GetOrCreateNodeByState(GraphNode.FootArrowState[,] state, HashSet<GraphNode> visitedNodes)
		{
			var node = new GraphNode(state);

			if (visitedNodes.TryGetValue(node, out var currentNode))
				node = currentNode;
			else
				visitedNodes.Add(node);

			return node;
		}

		private static void AddNode(
			GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			GraphNode.FootArrowState[,] state,
			GraphLink link)
		{
			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();

			var newNode = GetOrCreateNodeByState(state, visitedNodes);
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);
		}

		private static void FillSingleFootStep(GraphNode currentNode,
			HashSet<GraphNode> visitedNodes,
			ArrowData[] arrowData,
			SingleStepType stepType)
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
							var newStates = fillFunc(currentNode.State, arrowData, currentIndex, newIndex, foot,
								actionSets[setIndex]);
							if (newStates == null || newStates.Count == 0)
								continue;

							foreach (var newState in newStates)
							{
								var link = new GraphLink();
								for (var f = 0; f < numStepArrows; f++)
								{
									link.Links[foot, f] = new GraphLink.FootArrowState(stepType, actionSets[setIndex][f]);
								}

								AddNode(currentNode, visitedNodes, newState, link);
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
			SingleStepType[] stepTypes)
		{
			var actionsToStatesL = FillJumpStep(currentNode.State, arrowData, stepTypes[L], L);
			foreach (var actionStateL in actionsToStatesL)
			{
				foreach (var newStateL in actionStateL.Value)
				{
					var actionsToStatesR = FillJumpStep(newStateL, arrowData, stepTypes[R], R);
					foreach (var actionStateR in actionsToStatesR)
					{
						foreach (var newStateR in actionStateR.Value)
						{
							var link = new GraphLink();
							for (var f = 0; f < NumArrowsForStepType[(int)stepTypes[L]]; f++)
								link.Links[L, f] = new GraphLink.FootArrowState(stepTypes[L], actionStateL.Key[f]);
							for (var f = 0; f < NumArrowsForStepType[(int)stepTypes[R]]; f++)
								link.Links[R, f] = new GraphLink.FootArrowState(stepTypes[R], actionStateR.Key[f]);
							AddNode(currentNode, visitedNodes, newStateR, link);
						}
					}
				}
			}
		}

		private static Dictionary<FootAction[], List<GraphNode.FootArrowState[,]>> FillJumpStep(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			SingleStepType stepType,
			int foot)
		{
			var result = new Dictionary<FootAction[], List<GraphNode.FootArrowState[,]>>();

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
						var newStates = fillFunc(currentState, arrowData, currentIndex, newIndex, foot, actionSets[setIndex]);
						if (newStates == null || newStates.Count == 0)
							continue;
						if (!result.TryGetValue(actionSets[setIndex], out var stateLists))
							stateLists = newStates;
						else
							stateLists.AddRange(newStates);
						result[actionSets[setIndex]] = stateLists;
					}
				}
			}

			return result;
		}

		private static List<GraphNode.FootArrowState[,]> FillNewArrow(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];

			// TODO: Double-Step is maybe slightly different since it isn't a bracket?

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

			return new List<GraphNode.FootArrowState[,]> {newState};
		}

		private static List<GraphNode.FootArrowState[,]> FillSameArrow(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];

			// Must be same arrow.
			if (currentIndex != newIndex)
				return null;
			// Release logic. Lift from a hold or roll.
			if (footAction == FootAction.Release && !IsHeldOrRolling(currentState, currentIndex, foot))
				return null;
			// Normal logic. Placement action on a resting arrow.
			if (footAction != FootAction.Release && !IsResting(currentState, currentIndex, foot))
				return null;

			// Set up the state for a new node.
			// Copy the previous state and if placing a new foot, lift from any resting arrows.
			// It is necessary to lift for, e.g. the step after an SP quad.
			var otherFoot = OtherFoot(foot);
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

			return new List<GraphNode.FootArrowState[,]> {newState};
		}

		private static List<GraphNode.FootArrowState[,]> FillFootSwap(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			var footAction = footActions[0];

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

			return new List<GraphNode.FootArrowState[,]> {newState};
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverFront(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, true);
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverBack(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentState, arrowData, currentIndex, newIndex, foot, footActions, false);
		}

		private static List<GraphNode.FootArrowState[,]> FillCrossoverInternal(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];

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

			// Set up the state for a new node.
			var otherFoot = OtherFoot(foot);
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

			return new List<GraphNode.FootArrowState[,]> {newState};
		}

		// TODO: Apply these comments about assumptions to other methods.
		/// <summary>
		/// Assumes that footActions length is MaxArrowsPerFoot.
		/// Assumes that if one FootAction is a release, they are all a release.
		/// </summary>
		/// <param name="currentState"></param>
		/// <param name="arrowData"></param>
		/// <param name="currentIndex"></param>
		/// <param name="newIndex"></param>
		/// <param name="foot"></param>
		/// <param name="footActions"></param>
		/// <returns></returns>
		private static List<GraphNode.FootArrowState[,]> FillBracketBothNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
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
				return new List<GraphNode.FootArrowState[,]>
					{CreateNewBracketState(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketOneNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
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

			var results = new List<GraphNode.FootArrowState[,]>();
			var resultFirst = FillBracketFirstNew(currentState, arrowData, currentIndex, newIndex, foot, footActions);
			var resultSecond = FillBracketSecondNew(currentState, arrowData, currentIndex, newIndex, foot, footActions);
			if (resultFirst != null && resultFirst.Count > 0)
				results.AddRange(resultFirst);
			if (resultSecond != null && resultSecond.Count > 0)
				results.AddRange(resultSecond);
			return results;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketFirstNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
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
				return new List<GraphNode.FootArrowState[,]>
					{CreateNewBracketState(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketSecondNew(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
			// The first index must be a step on the same arrow (only the second is new)
			if (!IsResting(currentState, newIndex, foot))
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
				return new List<GraphNode.FootArrowState[,]>
					{CreateNewBracketState(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static List<GraphNode.FootArrowState[,]> FillBracketBothSame(
			GraphNode.FootArrowState[,] currentState,
			ArrowData[] arrowData,
			int currentIndex,
			int newIndex,
			int foot,
			FootAction[] footActions)
		{
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
				return new List<GraphNode.FootArrowState[,]>
					{CreateNewBracketState(currentState, foot, newIndex, secondIndex, footActions)};
			}

			return null;
		}

		private static GraphNode.FootArrowState[,] CreateNewBracketState(
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
			return newState;
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
