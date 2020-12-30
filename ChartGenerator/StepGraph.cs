//#define DEBUG_STEPGRAPH

using System;
using System.Collections.Generic;
using System.Linq;
using static ChartGenerator.Constants;
using Fumen;

#if DEBUG_STEPGRAPH
using System.Diagnostics;
#endif // DEBUG_STEPGRAPH

namespace ChartGenerator
{
	/// <summary>
	/// A graph of GraphNodes connected by GraphLinks representing all the positions on a set
	/// of arrows and the ways in which one can move between those positions.
	/// </summary>
	public class StepGraph
	{
		/// <summary>
		/// Data for each StepType.
		/// </summary>
		private class StepTypeFillData
		{
			/// <summary>
			/// Cached FootAction combinations for a step.
			/// Steps that involve one arrow will have an array of length 1 arrays.
			/// Steps that involve multiple arrows (brackets) will have an array of length 2 arrays.
			/// First index is the set of actions.
			///  For example, for length 2 FootAction combinations we would have roughly 9 entries
			///  since it is combining the 3 FootActions with 3 more.
			/// Second index is the arrow for the foot in question.
			///  For most StepTypes this will be a length 1 arrow. For brackets this will be a
			///  length 2 array.
			/// </summary>
			public FootAction[][] ActionSets;
			
			/// <summary>
			/// Which foot portions to use when filling the graph.
			/// Most StepTypes use just one foot portion, DefaultFootPortion.
			/// Bracket individual steps use one foot portion, Heel or Toe.
			/// Brackets use two foot portions, Heel then Toe.
			/// We do not need to include other combinations (Toe then Heel) since the
			/// ActionCombinations will apply all FootAction combinations to Heel then Toe.
			/// For example it will apply Hold, Tap to Heel, Toe, and also Tap, Hold to Heel Toe.
			/// </summary>
			public int[] FootPortionsForStep;

			/// <summary>
			/// Whether or not this StepType can be used in a jump.
			/// </summary>
			public bool CanBeUsedInJump;

			/// <summary>
			/// If true then when filling this StepType only consider filling from currently
			/// valid State entries in the parent GraphNode.
			/// If false, then loop over all arrows to try and fill for each.
			/// </summary>
			public bool OnlyConsiderCurrentArrowsWhenFilling;
		}

		/// <summary>
		/// Static cached StepTypeFillData for each StepType.
		/// </summary>
		private static readonly StepTypeFillData[] FillData;
		/// <summary>
		/// Static cached StepType combinations that constitute valid jumps.
		/// First index is the index of the combination.
		/// Second index is foot for that combination.
		/// </summary>
		private static readonly StepType[][] JumpCombinations;
		/// <summary>
		/// Static cached Foot order to use when filling jumps.
		/// First index is the index of the order to use when looping over all orders.
		/// Second index is array of foot indexes (e.g. [L,R] or [R,L]).
		/// </summary>
		private static readonly int[][] JumpFootOrder;

		/// <summary>
		/// Cached array of all arrow indices.
		/// </summary>
		private readonly int[] AllArrows;
		/// <summary>
		/// Functions to call per StepType to fill steps.
		/// </summary>
		private readonly Func<GraphNode, int, int, int, FootAction[], List<GraphNode>>[] FillFuncs;

		/// <summary>
		/// Structure to keep track of visited nodes when filling the StepGraph.
		/// </summary>
		private HashSet<GraphNode> VisitedNodes;

		/// <summary>
		/// Number of arrows in this StepGraph.
		/// </summary>
		public int NumArrows { get; }
		/// <summary>
		/// The root GraphNode for this StepGraph.
		/// </summary>
		public GraphNode Root { get; }
		/// <summary>
		/// ArrowData associated with this StepGraph.
		/// </summary>
		public ArrowData[] ArrowData { get; }

		/// <summary>
		/// Static initializer.
		/// </summary>
		static StepGraph()
		{
			// Create lists of combinations of actions for each number of foot portions.
			// Create one list with releases and one without.
			var actionCombinations = new FootAction[MaxArrowsPerFoot][][];
			var actionCombinationsWithoutReleases = new FootAction[MaxArrowsPerFoot][][];
			for (var i = 0; i < MaxArrowsPerFoot; i++)
			{
				var combinations = Combinations.CreateCombinations<FootAction>(i + 1);

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
				actionCombinations[i] = combinations.ToArray();

				// Update the combinations with no releases.
				combinations.RemoveAll(actions =>
				{
					foreach (var action in actions)
					{
						if (action == FootAction.Release)
							return true;
					}

					return false;
				});
				actionCombinationsWithoutReleases[i] = combinations.ToArray();
			}

			var steps = Enum.GetValues(typeof(StepType)).Cast<StepType>().ToList();

			// Configure the FillData.
			FillData = new StepTypeFillData[steps.Count];
			FillData[(int)StepType.SameArrow] = new StepTypeFillData
			{
				ActionSets = actionCombinations[0],
				FootPortionsForStep = new[] { DefaultFootPortion },
				OnlyConsiderCurrentArrowsWhenFilling = true,
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.NewArrow] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion },
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.CrossoverFront] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion }
			};
			FillData[(int)StepType.CrossoverBehind] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion }
			};
			FillData[(int)StepType.InvertFront] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion }
			};
			FillData[(int)StepType.InvertBehind] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion }
			};
			FillData[(int)StepType.FootSwap] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { DefaultFootPortion }
			};
			FillData[(int)StepType.BracketBothNew] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[1],
				FootPortionsForStep = new[] { Heel, Toe },
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketHeelNew] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[1],
				FootPortionsForStep = new[] { Heel, Toe },
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketToeNew] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[1],
				FootPortionsForStep = new[] { Heel, Toe },
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketBothSame] = new StepTypeFillData
			{
				ActionSets = actionCombinations[1],
				FootPortionsForStep = new[] { Heel, Toe },
				OnlyConsiderCurrentArrowsWhenFilling = true,
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketOneArrowHeelSame] = new StepTypeFillData
			{
				ActionSets = actionCombinations[0],
				FootPortionsForStep = new[] { Heel },
				OnlyConsiderCurrentArrowsWhenFilling = true,
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketOneArrowHeelNew] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { Heel },
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketOneArrowToeSame] = new StepTypeFillData
			{
				ActionSets = actionCombinations[0],
				FootPortionsForStep = new[] { Toe },
				OnlyConsiderCurrentArrowsWhenFilling = true,
				CanBeUsedInJump = true
			};
			FillData[(int)StepType.BracketOneArrowToeNew] = new StepTypeFillData
			{
				ActionSets = actionCombinationsWithoutReleases[0],
				FootPortionsForStep = new[] { Toe },
				CanBeUsedInJump = true
			};

			// Initialize JumpCombinations.
			var jumpSingleSteps = new List<StepType>();
			for (var stepType = 0; stepType < steps.Count; stepType++)
			{
				if (FillData[stepType].CanBeUsedInJump)
					jumpSingleSteps.Add((StepType)stepType);
			}
			JumpCombinations = Combinations.CreateCombinations(jumpSingleSteps, NumFeet).ToArray();

			// Initialize JumpFootOrder.
			JumpFootOrder = new[]
			{
				new []{L, R},
				new []{R, L}
			};
		}

		/// <summary>
		/// Private constructor.
		/// StepGraphs are publicly created using CreateStepGraph.
		/// </summary>
		/// <param name="arrowData">ArrowData this StepGraph is for.</param>
		/// <param name="root">Root GraphNode.</param>
		private StepGraph(ArrowData[] arrowData, GraphNode root)
		{
			Root = root;
			ArrowData = arrowData;
			NumArrows = ArrowData.Length;

			AllArrows = new int[NumArrows];
			for (var i = 0; i < NumArrows; i++)
				AllArrows[i] = i;

			// Configure the fill Functions.
			FillFuncs = new Func<GraphNode, int, int, int, FootAction[], List<GraphNode>>[]
			{
				FillSameArrow,
				FillNewArrow,
				FillCrossoverFront,
				FillCrossoverBack,
				FillInvertFront,
				FillInvertBack,
				FillFootSwap,
				FillBracketBothNew,
				FillBracketHeelNew,
				FillBracketToeNew,
				FillBracketBothSame,
				FillBracketOneArrowHeelSame,
				FillBracketOneArrowHeelNew,
				FillBracketOneArrowToeSame,
				FillBracketOneArrowToeNew
			};
		}

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
			var stepGraph = new StepGraph(arrowData, root);
			stepGraph.Fill();
			return stepGraph;
		}

		private void Fill()
		{
			Logger.Info($"Generating {ArrowData.Length}-panel StepGraph.");

			VisitedNodes = new HashSet<GraphNode>();
			var completeNodes = new HashSet<GraphNode>();
			var currentNodes = new List<GraphNode> { Root };
			var level = 0;

			var allArrows = new int[ArrowData.Length];
			for (var i = 0; i < allArrows.Length; i++)
				allArrows[i] = i;

			while (currentNodes.Count > 0)
			{
				Logger.Info($"Level {level + 1}: Searching {currentNodes.Count} nodes...");

				var allChildren = new HashSet<GraphNode>();
				foreach (var currentNode in currentNodes)
				{
					VisitedNodes.Add(currentNode);

					// Fill node
					foreach (var stepType in Enum.GetValues(typeof(StepType)).Cast<StepType>())
						FillSingleFootStep(currentNode, stepType);
					foreach (var jump in JumpCombinations)
						FillJump(currentNode, jump);

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

			Logger.Info($"{ArrowData.Length}-panel StepGraph generation complete. {completeNodes.Count} Nodes.");
		}

		private void AddNode(GraphNode currentNode, GraphNode newNode, GraphLink link)
		{
			if (VisitedNodes.TryGetValue(newNode, out var visitedNode))
				newNode = visitedNode;
			else
				VisitedNodes.Add(newNode);

			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);
		}

		private void FillSingleFootStep(GraphNode currentNode, StepType stepType)
		{
			// Get the foot portions needed for this step.
			// For brackets this is two portions and for other steps it is one.
			// We do not need to exhaust combinations here since we will do it for
			// ActionCombinations.
			var footPortions = FillData[(int)stepType].FootPortionsForStep;
			// Get the appropriate function to use for filling this StepType.
			var fillFunc = FillFuncs[(int)stepType];
			// Get the list of combinations of FootActions for the number arrows used by this StepType.
			// If this StepType uses two arrows then this actionSets array will include
			// arrays for every combination of FootActions for two feet.
			// This includes, for example, Tap then Hold and Hold then Tap.
			var actionSets = FillData[(int)stepType].ActionSets;
			var onlyConsiderCurrent = FillData[(int)stepType].OnlyConsiderCurrentArrowsWhenFilling;

			var arrows = onlyConsiderCurrent ? new int[1] : AllArrows;

			for (var foot = 0; foot < NumFeet; foot++)
			{
				for (var startingFootPortion = 0; startingFootPortion < MaxArrowsPerFoot; startingFootPortion++)
				{
					if (currentNode.State[foot, startingFootPortion].Arrow == InvalidArrowIndex)
						continue;
					for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
					{
						var actions = actionSets[setIndex];
						if (onlyConsiderCurrent)
							arrows[0] = currentNode.State[foot, startingFootPortion].Arrow;
						foreach (var newArrow in arrows)
						{
							// Call the fill function to get the List of GraphNodes that can be reached
							// from the current Node for the given StepType and FootActions.
							var newNodes = fillFunc(currentNode, foot, startingFootPortion, newArrow, actions);
							if (newNodes == null || newNodes.Count == 0)
								continue;
							// Add each new GraphNode with a new GraphLink
							foreach (var newNode in newNodes)
							{
								// Fill the GraphLink Links at the correct foot portions.
								// For example, if this is BracketToeNew StepType we want to only
								// fill the Link at the Toe foot portion index.
								// We do not want to index the actions by the foot portion.
								// For example, if this is BracketToeNew StepType the foot portion
								// will be Toe (index 1) with only one FootAction in the actions
								// array (at index 0).
								var link = new GraphLink();
								var actionIndex = 0;
								foreach (var footPortion in footPortions)
								{
									link.Links[foot, footPortion] = new GraphLink.FootArrowState(stepType, actions[actionIndex++]);
								}

								AddNode(currentNode, newNode, link);
							}
						}
					}
				}
			}
		}

		private void FillJump(GraphNode currentNode, StepType[] stepTypes)
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

				// Get the foot portions needed for this step.
				// For brackets this is two portions and for other steps it is one.
				// We do not need to exhaust combinations here since we will do it for
				// ActionCombinations.
				var footPortionsF1 = FillData[(int)stepTypes[f1]].FootPortionsForStep;
				var footPortionsF2 = FillData[(int)stepTypes[f2]].FootPortionsForStep;

				// Find all the states from moving the first foot.
				var actionsToNodesF1 = FillJumpStep(currentNode, stepTypes[f1], f1);
				foreach (var actionNodeF1 in actionsToNodesF1)
				{
					foreach (var newNodeF1 in actionNodeF1.Value)
					{
						// Using the state from the first foot, find all the states from moving the second foot.
						var actionsToNodesF2 = FillJumpStep(newNodeF1, stepTypes[f2], f2);
						foreach (var actionNodeF2 in actionsToNodesF2)
						{
							foreach (var newNodeF2 in actionNodeF2.Value)
							{
								// Fill the GraphLink Links at the correct foot portions for each foot.
								// For example, if this is BracketToeNew StepType we want to only
								// fill the Link at the Toe foot portion index.
								// We do not want to index the actions by the foot portion.
								// For example, if this is BracketToeNew StepType the foot portion
								// will be Toe (index 1) with only one FootAction in the actions
								// array (at index 0).
								var link = new GraphLink();
								var actionIndex = 0;
								foreach (var footPortion in footPortionsF1)
								{
									link.Links[f1, footPortion] = new GraphLink.FootArrowState(stepTypes[f1], actionNodeF1.Key[actionIndex++]);
								}
								actionIndex = 0;
								foreach (var footPortion in footPortionsF2)
								{
									link.Links[f2, footPortion] = new GraphLink.FootArrowState(stepTypes[f2], actionNodeF2.Key[actionIndex++]);
								}

								AddNode(currentNode, newNodeF2, link);
							}
						}
					}
				}
			}
		}

		private Dictionary<FootAction[], List<GraphNode>> FillJumpStep(
			GraphNode currentNode,
			StepType stepType,
			int foot)
		{
			var result = new Dictionary<FootAction[], List<GraphNode>>();

			// Get the appropriate function to use for filling this StepType.
			var fillFunc = FillFuncs[(int)stepType];
			// Get the list of combinations of FootActions for the number arrows used by this StepType.
			// If this StepType uses two arrows then this actionSets array will include
			// arrays for every combination of FootActions for two feet.
			// This includes, for example, Tap then Hold and Hold then Tap.
			var actionSets = FillData[(int)stepType].ActionSets;
			var onlyConsiderCurrent = FillData[(int)stepType].OnlyConsiderCurrentArrowsWhenFilling;

			var arrows = onlyConsiderCurrent ? new int[1] : AllArrows;

			for (var startingFootPortion = 0; startingFootPortion < MaxArrowsPerFoot; startingFootPortion++)
			{
				if (currentNode.State[foot, startingFootPortion].Arrow == InvalidArrowIndex)
					continue;
				for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
				{
					var actions = actionSets[setIndex];
					if (onlyConsiderCurrent)
						arrows[0] = currentNode.State[foot, startingFootPortion].Arrow;
					foreach (var newArrow in arrows)
					{
						// Call the fill function to get the List of GraphNodes that can be reached
						// from the current Node for the given StepType and FootActions.
						var newNodes = fillFunc(currentNode, foot, startingFootPortion, newArrow, actions);
						if (newNodes == null || newNodes.Count == 0)
							continue;

						if (!result.TryGetValue(actions, out var nodeLists))
							nodeLists = newNodes;
						else
							nodeLists.AddRange(newNodes);
						result[actions] = nodeLists;
					}
				}
			}

			return result;
		}

		private List<GraphNode> FillNewArrow(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				DefaultFootPortion,
				footActions,
				false);
		}

		private List<GraphNode> FillBracketOneArrowHeelNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(currentNode, foot, currentFootPortion, newArrow, Heel, footActions, true);
		}

		private List<GraphNode> FillBracketOneArrowToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(currentNode, foot, currentFootPortion, newArrow, Toe, footActions, true);
		}

		private List<GraphNode> FillNewArrowInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			int destinationFootPortion,
			FootAction[] footActions,
			bool bracket)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentState[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;

			// BracketOneArrowHeelNew and BracketOneArrowToeNew checks.
			if (bracket)
			{
				// Skip if the new arrow is not a valid bracketable pairing.
				if (destinationFootPortion == Heel && !ArrowData[currentArrow].BracketablePairingsOtherHeel[foot][newArrow])
					return null;
				if (destinationFootPortion == Toe && !ArrowData[currentArrow].BracketablePairingsOtherToe[foot][newArrow])
					return null;

				var numHeld = NumHeld(currentState, foot);

				// Must be holding on an arrow to bracket another.
				if (numHeld < 1)
					return null;
				// Must have one free foot.
				if (numHeld == MaxArrowsPerFoot)
					return null;
			}
			// NewArrow checks.
			else
			{
				// Skip if the new arrow isn't a valid next arrow for the current placement.
				if (!ArrowData[currentArrow].ValidNextArrows[newArrow])
					return null;

				// Cannot step on a new arrow if already holding.
				if (AnyHeld(currentState, foot))
					return null;
			}
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, newArrow))
				return null;
			// Skip if the new arrow is not a valid pairing for any other foot arrows.
			if (!IsValidPairingWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			// Set up the state for a new node.
			// Copy the previous state, but lift from any resting arrows for the given foot.
			// Leave holds.
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

			// If stepping on a new bracket with the Heel (0) from a state where only one foot portion is
			// resting (DefaultFootPortion, also 0), then we need to make the existing held arrow held with
			// the Toe.
			if (bracket
			    && destinationFootPortion == DefaultFootPortion
			    && currentState[foot, destinationFootPortion].Arrow != InvalidArrowIndex)
			{
				newState[foot, OtherFootPortion(destinationFootPortion)] = currentState[foot, destinationFootPortion];
			}

			newState[foot, destinationFootPortion] = new GraphNode.FootArrowState(newArrow, StateAfterAction(footAction));

			// Stepping on a new arrow implies a normal orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private List<GraphNode> FillSameArrow(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				DefaultFootPortion,
				footActions,
				false);
		}

		private List<GraphNode> FillBracketOneArrowHeelSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(currentNode, foot, currentFootPortion, newArrow, Heel, footActions, true);
		}

		private List<GraphNode> FillBracketOneArrowToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(currentNode, foot, currentFootPortion, newArrow, Toe, footActions, true);
		}

		private List<GraphNode> FillSameArrowInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			int destinationFootPortion,
			FootAction[] footActions,
			bool bracket)
		{
#if DEBUG_STEPGRAPH
			Debug.Assert(currentNode.State[foot, currentFootPortion].Arrow == newArrow);
#endif // DEBUG_STEPGRAPH

			// Only for brackets ensure that the foot portions match.
			// We do not want to do this for non-bracket steps because you could be resting
			// with both foot portions and we want to allow SameArrow steps from either portion
			// to the DefaultFootPortion.
			if (bracket && currentFootPortion != destinationFootPortion)
				return null;

			var footAction = footActions[0];
			var release = footAction == FootAction.Release;
			var currentState = currentNode.State;

			// Release logic. Lift portion from hold.
			if (release && currentState[foot, destinationFootPortion].State != GraphArrowState.Held)
				return null;
			// Step logic. Placement action on an arrow resting under the heel or toe.
			if (!release && currentState[foot, destinationFootPortion].State != GraphArrowState.Resting)
				return null;

			// Count number held and held or resting so we can determine if this step is appropriate
			// for the given type of step (release or not and also bracket or not).
			var numHeld = 0;
			var numHeldOrResting = 0;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (currentState[foot, a].Arrow != InvalidArrowIndex)
				{
					numHeldOrResting++;
					if (currentState[foot, a].State == GraphArrowState.Held)
						numHeld++;
				}
			}

			// Bracket (BracketOneArrowHeelSame and BracketOneArrowToeSame) logic.
			if (bracket)
			{
				// For both releasing and stepping there needs to be at least one hold.
				// For releasing this is because the hold needs to be released.
				// For stepping this is because if none were held this would not be a bracket step.
				if (numHeld < 1)
					return null;
				// For both releasing and stepping both foot portions need to be on arrows.
				// For releasing this is because if only one were held and the other was not resting or held then
				// the step would not be a bracket release.
				// For stepping this is because to step on the same arrow as a bracket, the foot needs to be resting
				// on the arrow while the other portion is held.
				if (numHeldOrResting < MaxArrowsPerFoot)
					return null;
			}
			// SameArrow logic.
			else
			{
				// Releasing. Different logic than stepping for a non-bracket step.
				if (release)
				{
					// Releasing on the SameArrow when not bracketing requires exactly one
					// arrow to be held.
					if (numHeld != 1)
						return null;
					// When releasing only one foot portion can be holding. If the other foot
					// portion were resting then this release would be a bracket release.
					if (numHeldOrResting == MaxArrowsPerFoot)
						return null;
				}
				// Stepping. Different logic than releasing for a non-bracket step.
				else
				{
					// When stepping no arrow can be held with this foot. If an arrow was
					// held then this step should be a bracket.
					if (numHeld != 0)
						return null;
				}
			}

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
			if (!release && IsResting(currentState, newArrow, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				newState[otherFoot, a] = currentState[otherFoot, a];

				if (a == destinationFootPortion)
					newState[foot, a] = new GraphNode.FootArrowState(newArrow, StateAfterAction(footAction));
				// When stepping is is necessary to lift all resting portions.
				else if (!release && IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			// Stepping on the same arrow maintains the previous state's orientation.
			return new List<GraphNode> { new GraphNode(newState, currentNode.Orientation) };
		}

		private List<GraphNode> FillFootSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			// Cannot release on a new arrow.
			if (footAction == FootAction.Release)
				return null;
			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;

			var otherFoot = OtherFoot(foot);

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (currentNode.State[foot, currentFootPortion].State != GraphArrowState.Resting)
				return null;
			// The new arrow must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState, newArrow, otherFoot))
				return null;
			// Disallow foot swap if this foot is holding or rolling.
			if (AnyHeld(currentState, foot))
				return null;
			// Disallow foot swap if the other foot is holding or rolling.
			if (AnyHeld(currentState, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == DefaultFootPortion)
				{
					// The other foot should remain resting on the new arrow, even though it is slightly lifted.
					newState[otherFoot, a] = new GraphNode.FootArrowState(newArrow, GraphArrowState.Resting);
					// The DefaultFootPortion for the foot in the new state should be on the new arrow,
					// with the appropriate state.
					newState[foot, a] = new GraphNode.FootArrowState(newArrow, StateAfterAction(footAction));
				}
				else
				{
					// All other arrows should be lifted.
					newState[otherFoot, a] = GraphNode.InvalidFootArrowState;
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				}
			}

			// Footswaps correct inverted orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private List<GraphNode> FillCrossoverFront(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, foot, currentFootPortion, newArrow, footActions, true);
		}

		private List<GraphNode> FillCrossoverBack(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, foot, currentFootPortion, newArrow, footActions, false);
		}

		private List<GraphNode> FillCrossoverInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;
			// Cannot crossover if any arrows are held by this foot.
			if (AnyHeld(currentState, foot))
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!ArrowData[currentArrow].ValidNextArrows[newArrow])
				return null;
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is not a crossover.
			if (front && !FootCrossesOverInFrontWithAnyOtherFoot(currentState, foot, newArrow))
				return null;
			if (!front && !FootCrossesOverInBackWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			var otherFoot = OtherFoot(foot);

			// If the current state is already a crossover of the same type (front or back)
			// with the other foot then we cannot create another crossover of that type
			// with this foot as the legs would cross through each other (or you would be spun).
			if (front && FootCrossedOverInFront(currentState, otherFoot))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot))
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

				if (a == DefaultFootPortion)
					newState[foot, a] = new GraphNode.FootArrowState(newArrow, StateAfterAction(footAction));
				// Lift any resting arrows for the given foot.
				else if (IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			// Crossovers are not inverted.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private List<GraphNode> FillInvertFront(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, foot, currentFootPortion, newArrow, footActions, true);
		}

		private List<GraphNode> FillInvertBack(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, foot, currentFootPortion, newArrow, footActions, false);
		}

		private List<GraphNode> FillInvertInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;
			// Cannot invert if any arrows are held by this foot.
			if (AnyHeld(currentState, foot))
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!ArrowData[currentArrow].ValidNextArrows[newArrow])
				return null;
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is not an inversion.
			if (!FootInvertsWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			// Determine the orientation this inversion will result in.
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
			if (front && FootCrossedOverInFront(currentState, otherFoot))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				// Copy previous state for other foot.
				newState[otherFoot, a] = currentState[otherFoot, a];

				if (a == DefaultFootPortion)
					newState[foot, a] = new GraphNode.FootArrowState(newArrow, StateAfterAction(footAction));
				// Lift any resting arrows for the given foot.
				else if (IsStateRestingAtIndex(currentState, a, foot))
					newState[foot, a] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, a] = currentState[foot, a];
			}

			return new List<GraphNode> { new GraphNode(newState, orientation) };
		}

		private List<GraphNode> FillBracketBothNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentState, foot))
				return null;
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, newArrow))
				return null;
			// Skip if the new arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, newArrow);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var heelAction = footActions[Heel];
			var toeAction = footActions[Toe];

			var allResults = new List<GraphNode>();

			// Fill steps where the heel arrow precedes the toe arrow.
			var results = FillBracketBothNewInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Heel, Toe },
				new[] { heelAction, toeAction },
				newIndexOtherFootValidPairings);
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			// Fill steps where the toe arrow precedes the heel arrow.
			results = FillBracketBothNewInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Toe, Heel },
				new[] { toeAction, heelAction },
				newIndexOtherFootValidPairings);
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			return allResults;
		}

		private List<GraphNode> FillBracketBothNewInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions,
			List<int> newIndexOtherFootValidPairings)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;
			var firstNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[firstNewArrow];

			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + ChartGenerator.ArrowData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;
				// Skip if this next arrow is occupied.
				if (!IsFree(currentState, secondNewArrow))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, secondNewArrow))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, secondNewArrow);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[secondNewArrow];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				return new List<GraphNode>
					{CreateNewBracketNode(currentState, foot, firstNewArrow, secondNewArrow, newFootPortions, footActions)};
			}

			return null;
		}

		private List<GraphNode> FillBracketHeelNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var results = new List<GraphNode>();
			var resultFirst = FillBracketFirstNew(currentNode, foot, currentFootPortion, newArrow,
				new[] { Heel, Toe },
				footActions);
			var resultSecond = FillBracketSecondNew(currentNode, foot, currentFootPortion, newArrow,
				new[] { Toe, Heel },
				new[] { footActions[Toe], footActions[Heel] });
			if (resultFirst != null && resultFirst.Count > 0)
				results.AddRange(resultFirst);
			if (resultSecond != null && resultSecond.Count > 0)
				results.AddRange(resultSecond);

			return results;
		}

		private List<GraphNode> FillBracketToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow.
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();
			var results = FillBracketFirstNew(currentNode, foot, currentFootPortion, newArrow,
				new[] { Toe, Heel },
				new[] { footActions[Toe], footActions[Heel] });
			if (results != null && results.Count > 0)
				allResults.AddRange(results);
			results = FillBracketSecondNew(currentNode, foot, currentFootPortion, newArrow,
				new[] { Heel, Toe },
				footActions);
			if (results != null && results.Count > 0)
				allResults.AddRange(results);
			return allResults;
		}

		private List<GraphNode> FillBracketFirstNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// The first new arrow must be a step on a new arrow.
			if (!IsFree(currentState, firstNewArrow))
				return null;
			// Skip if the first new arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, firstNewArrow))
				return null;

			// Skip if the first new arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, firstNewArrow);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			var firstNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[firstNewArrow];

			var results = new List<GraphNode>();
			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + ChartGenerator.ArrowData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;
				// The second new arrow must be a step on the same arrow (only the first is new).
				if (!IsResting(currentState, secondNewArrow, foot))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, secondNewArrow))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, secondNewArrow);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement.
				var secondNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[secondNewArrow];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows.
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(currentState, foot, firstNewArrow, secondNewArrow, newFootPortions, footActions));
			}

			return results;
		}

		private List<GraphNode> FillBracketSecondNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

			// The first new arrow must be a step on the same arrow (only the second is new).
			if (!IsResting(currentState, firstNewArrow, foot))
				return null;

			// Skip if the first new arrow is not a valid pairing for any other foot arrows.
			var newIndexOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, firstNewArrow);
			if (newIndexOtherFootValidPairings.Count == 0)
				return null;

			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			var firstNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[firstNewArrow];

			var results = new List<GraphNode>();
			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + ChartGenerator.ArrowData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;
				// The second new arrow must be a step on a new arrow.
				if (!IsFree(currentState, secondNewArrow))
					continue;
				// Skip if the second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, secondNewArrow))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, secondNewArrow);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = ArrowData[currentArrow].ValidNextArrows[secondNewArrow];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows
				if (!newIndexOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(currentState, foot, firstNewArrow, secondNewArrow, newFootPortions, footActions));
			}

			return results;
		}

		private List<GraphNode> FillBracketBothSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var heelAction = footActions[Heel];
			var toeAction = footActions[Toe];

			var allResults = new List<GraphNode>();

			var results = FillBracketBothSameInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Heel, Toe },
				new[] { heelAction, toeAction });
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			results = FillBracketBothSameInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Toe, Heel },
				new[] { toeAction, heelAction });
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			return allResults;
		}

		private List<GraphNode> FillBracketBothSameInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			// Must be all releases or all placements.
			// This check is performed when creating ActionCombinations so we do not
			// need to perform it again here.
			var releasing = footActions[0] == FootAction.Release;

			// Check to make sure we are acting on the same arrow.
			// This is to ensure we do not process the bracket twice from the caller.
			if (currentArrow != firstNewArrow)
				return null;

			// Check for state at the first new arrow matching expected state for whether releasing or placing.
			if (!releasing && !IsRestingWithFootPortion(currentState, firstNewArrow, foot, newFootPortions[0]))
				return null;
			if (releasing && !IsHeldWithFootPortion(currentState, firstNewArrow, foot, newFootPortions[0]))
				return null;

			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + ChartGenerator.ArrowData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Check for state at the second new arrow matching expected state for whether releasing or placing.
				if (!releasing && !IsRestingWithFootPortion(currentState, secondNewArrow, foot, newFootPortions[1]))
					continue;
				if (releasing && !IsHeldWithFootPortion(currentState, secondNewArrow, foot, newFootPortions[1]))
					continue;

				// Set up the state for a new node.
				return new List<GraphNode>
					{CreateNewBracketNode(currentState, foot, firstNewArrow, secondNewArrow, newFootPortions, footActions)};
			}

			return null;
		}

		private static GraphNode CreateNewBracketNode(
			GraphNode.FootArrowState[,] currentState,
			int foot,
			int firstArrow,
			int secondArrow,
			int[] footPortions,
			FootAction[] footActions)
		{
			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, MaxArrowsPerFoot];
			var otherFoot = OtherFoot(foot);
			// The other foot doesn't change
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				newState[otherFoot, a] = currentState[otherFoot, a];
			// The given foot brackets the two new arrows.
			newState[foot, footPortions[0]] = new GraphNode.FootArrowState(firstArrow, StateAfterAction(footActions[0]));
			newState[foot, footPortions[1]] = new GraphNode.FootArrowState(secondArrow, StateAfterAction(footActions[1]));
			// Brackets are not inverted.
			return new GraphNode(newState, BodyOrientation.Normal);
		}

		#region Fill Helpers
		private bool IsValidPairingWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			return GetValidPairingsWithOtherFoot(state, foot, arrow).Count > 0;
		}

		private List<int> GetValidPairingsWithOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var result = new List<int>();
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && ArrowData[otherFootArrowIndex].OtherFootPairings[otherFoot][arrow])
					result.Add(otherFootArrowIndex);
			}

			return result;
		}

		private bool FootCrossedOverInFront(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var footArrowIndex = state[foot, a].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInFrontWithAnyOtherFoot(state, foot, footArrowIndex))
					return true;
			}
			return false;
		}

		private bool FootCrossedOverInBack(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var footArrowIndex = state[foot, a].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInBackWithAnyOtherFoot(state, foot, footArrowIndex))
					return true;
			}
			return false;
		}

		private bool FootCrossesOverWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			return FootCrossesOverInFrontWithAnyOtherFoot(state, foot, arrow)
			       || FootCrossesOverInBackWithAnyOtherFoot(state, foot, arrow);
		}

		private bool FootCrossesOverInFrontWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverFront[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool FootCrossesOverInBackWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverBehind[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool FootInvertsWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				var otherFootArrowIndex = state[otherFoot, a].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && ArrowData[otherFootArrowIndex].OtherFootPairingsInverted[otherFoot][arrow])
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

		private static int NumHeld(GraphNode.FootArrowState[,] state, int foot)
		{
			var num = 0;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow != InvalidArrowIndex && state[foot, a].State == GraphArrowState.Held)
					num++;
			return num;
		}

		private static bool AnyHeld(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow != InvalidArrowIndex && state[foot, a].State == GraphArrowState.Held)
					return true;
			return false;
		}

		private static bool IsHeldWithFootPortion(GraphNode.FootArrowState[,] state, int arrow, int foot, int footPortion)
		{
			if (state[foot, footPortion].Arrow == arrow && state[foot, footPortion].State == GraphArrowState.Held)
				return true;
			return false;
		}

		private static bool IsResting(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
				if (state[foot, a].Arrow == arrow && state[foot, a].State == GraphArrowState.Resting)
					return true;
			return false;
		}

		private static bool IsRestingWithFootPortion(GraphNode.FootArrowState[,] state, int arrow, int foot, int footPortion)
		{
			if (state[foot, footPortion].Arrow == arrow && state[foot, footPortion].State == GraphArrowState.Resting)
				return true;
			return false;
		}

		private static bool IsStateRestingAtIndex(GraphNode.FootArrowState[,] state, int a, int foot)
		{
			if (state[foot, a].Arrow != InvalidArrowIndex && state[foot, a].State == GraphArrowState.Resting)
				return true;
			return false;
		}

		#endregion Fill Helpers
	}
}
