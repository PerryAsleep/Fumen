//#define DEBUG_STEPGRAPH

using System;
using System.Collections.Generic;
using System.Linq;
using static StepManiaLibrary.Constants;
using Fumen;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fumen.Compression;
using System.Diagnostics;

namespace StepManiaLibrary
{
	/// <summary>
	/// A graph of GraphNodes connected by GraphLinks representing all the positions on a set
	/// of arrows and the ways in which one can move between those positions.
	/// </summary>
	public class StepGraph
	{
		/// <summary>
		/// Number of arrows in this StepGraph.
		/// </summary>
		public readonly int NumArrows;

		/// <summary>
		/// Identifier for logs.
		/// </summary>
		private readonly string LogIdentifier;

		/// <summary>
		/// The root GraphNode for this StepGraph.
		/// </summary>
		private GraphNode Root;

		/// <summary>
		/// PadData associated with this StepGraph.
		/// </summary>
		public readonly PadData PadData;

		/// <summary>
		/// Private constructor.
		/// StepGraphs are publicly created using CreateStepGraph.
		/// </summary>
		/// <param name="padData">PadData this StepGraph is for.</param>
		private StepGraph(PadData padData)
		{
			PadData = padData;
			NumArrows = PadData.NumArrows;
			LogIdentifier = PadData.StepsType;
		}

		/// <summary>
		/// Creates a new StepGraph satisfying all movements for the given ArrowData with
		/// a root position at the given starting arrows.
		/// </summary>
		/// <param name="padData">PadData for the set of arrows to create a StepGraph for.</param>
		/// <param name="leftStartingArrow">Starting arrow for the left foot.</param>
		/// <param name="rightStartingArrow">Starting arrow for the right foot.</param>
		/// <returns>
		/// StepGraph satisfying all movements for the given ArrowData with a root position
		/// at the given starting arrows.
		/// </returns>
		public static StepGraph CreateStepGraph(PadData padData, int leftStartingArrow, int rightStartingArrow)
		{
			// Set up state for root node.
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					state[L, p] = new GraphNode.FootArrowState(leftStartingArrow, GraphArrowState.Resting);
					state[R, p] = new GraphNode.FootArrowState(rightStartingArrow, GraphArrowState.Resting);
				}
				else
				{
					state[L, p] = GraphNode.InvalidFootArrowState;
					state[R, p] = GraphNode.InvalidFootArrowState;
				}
			}
			var root = new GraphNode(state, BodyOrientation.Normal);
			
			var stepGraph = new StepGraph(padData);
			if (!stepGraph.CreateGraph(root))
				return null;
			return stepGraph;
		}

		public GraphNode GetRoot()
		{
			return Root;
		}

		#region Serialization

		private static bool DoesEnumMatchExpectedValues<T>(T[] expectedValues) where T : Enum
		{
			var expectedCount = expectedValues.Length;
			var actualValues = Enum.GetValues(typeof(T)).Cast<T>();
			var actualCount = 0;
			foreach (var actualValue in actualValues)
			{
				if (actualCount > expectedCount)
					return false;
				if (Convert.ToInt32(actualValue) != Convert.ToInt32(expectedValues[actualCount]))
					return false;
				actualCount++;
			}
			if (actualCount != expectedCount)
				return false;
			return true;
		}

		private static bool DoStructuresMatchSerializedV1Data()
		{
			var expectedStepTypes = new StepType[]
			{
				StepType.SameArrow,
				StepType.NewArrow,
				StepType.CrossoverFront,
				StepType.CrossoverBehind,
				StepType.InvertFront,
				StepType.InvertBehind,
				StepType.FootSwap,
				StepType.Swing,
				StepType.NewArrowStretch,
				StepType.CrossoverFrontStretch,
				StepType.CrossoverBehindStretch,
				StepType.InvertFrontStretch,
				StepType.InvertBehindStretch,
				StepType.FootSwapCrossoverFront,
				StepType.FootSwapCrossoverBehind,
				StepType.FootSwapInvertFront,
				StepType.FootSwapInvertBehind,
				StepType.BracketHeelNewToeNew,
				StepType.BracketHeelNewToeSame,
				StepType.BracketHeelSameToeNew,
				StepType.BracketHeelSameToeSame,
				StepType.BracketHeelSameToeSwap,
				StepType.BracketHeelNewToeSwap,
				StepType.BracketHeelSwapToeSame,
				StepType.BracketHeelSwapToeNew,
				StepType.BracketHeelSwapToeSwap,
				StepType.BracketSwing,
				StepType.BracketCrossoverFrontHeelNewToeNew,
				StepType.BracketCrossoverFrontHeelNewToeSame,
				StepType.BracketCrossoverFrontHeelSameToeNew,
				StepType.BracketCrossoverBehindHeelNewToeNew,
				StepType.BracketCrossoverBehindHeelNewToeSame,
				StepType.BracketCrossoverBehindHeelSameToeNew,
				StepType.BracketInvertFrontHeelNewToeNew,
				StepType.BracketInvertFrontHeelNewToeSame,
				StepType.BracketInvertFrontHeelSameToeNew,
				StepType.BracketInvertBehindHeelNewToeNew,
				StepType.BracketInvertBehindHeelNewToeSame,
				StepType.BracketInvertBehindHeelSameToeNew,
				StepType.BracketStretchHeelNewToeNew,
				StepType.BracketStretchHeelNewToeSame,
				StepType.BracketStretchHeelSameToeNew,
				StepType.BracketOneArrowHeelSame,
				StepType.BracketOneArrowHeelNew,
				StepType.BracketOneArrowHeelSwap,
				StepType.BracketOneArrowToeSame,
				StepType.BracketOneArrowToeNew,
				StepType.BracketOneArrowToeSwap,
				StepType.BracketCrossoverFrontOneArrowHeelNew,
				StepType.BracketCrossoverFrontOneArrowToeNew,
				StepType.BracketCrossoverBehindOneArrowHeelNew,
				StepType.BracketCrossoverBehindOneArrowToeNew,
				StepType.BracketInvertFrontOneArrowHeelNew,
				StepType.BracketInvertFrontOneArrowToeNew,
				StepType.BracketInvertBehindOneArrowHeelNew,
				StepType.BracketInvertBehindOneArrowToeNew,
				StepType.BracketStretchOneArrowHeelNew,
				StepType.BracketStretchOneArrowToeNew,
			};
			if (!DoesEnumMatchExpectedValues(expectedStepTypes))
				return false;

			var expectedFootActions = new FootAction[]
			{
				FootAction.Tap,
				FootAction.Hold,
				FootAction.Release
			};
			if (!DoesEnumMatchExpectedValues(expectedFootActions))
				return false;

			var expectedGraphArrowStates = new GraphArrowState[]
			{
				GraphArrowState.Resting,
				GraphArrowState.Held,
				GraphArrowState.Lifted,
			};
			if (!DoesEnumMatchExpectedValues(expectedGraphArrowStates))
				return false;

			return true;
		}

		public async Task<bool> SaveAsync(string filePath)
		{
			return await Task.Run(() => Save(filePath));
		}

		public bool Save(string filePath)
		{
			try
			{
				using (var fileStream = File.Open(filePath, FileMode.Create))
				{
					using (var memoryStream = new MemoryStream())
					{
						using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, false))
						{
							WriteGraphV1(writer);
							memoryStream.Seek(0, SeekOrigin.Begin);
							Compression.CompressLzma(memoryStream, fileStream);
						}
					}
				}
			}
			catch (Exception e)
			{
				LogError($"Failed to save StepGraph: {e}");
				return false;
			}
			return true;
		}

		private void WriteGraphV1(BinaryWriter writer)
		{
			var nodeIds = new Dictionary<GraphNode, int>();
			var nodesToWrite = new List<GraphNode>();
			var recordedNodes = new HashSet<GraphNode>();
			var writtenNodesList = new List<GraphNode>();
			var nodeId = 0;

			// Ensure that serialized enums have not changed
			if (!DoStructuresMatchSerializedV1Data())
				throw new Exception("Programmer Error: Data required for StepGraph V1 serialization has changed.");

			// Write version;
			writer.Write(1);

			recordedNodes.Add(Root);
			nodesToWrite.Add(Root);

			// Reserve an int to write the number of nodes in later.
			var positionBeforeNodes = (int)writer.BaseStream.Position;
			writer.Write(0);

			while (nodesToWrite.Count > 0)
			{
				var node = nodesToWrite[nodesToWrite.Count - 1];
				nodesToWrite.RemoveAt(nodesToWrite.Count - 1);

				var id = nodeId;
				nodeId++;
				nodeIds.Add(node, id);
				writtenNodesList.Add(node);

				// Write the state of this node.
				writer.Write(id);
				writer.Write((byte)node.Orientation);
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						writer.Write((byte)node.State[f, p].Arrow);
						writer.Write((byte)node.State[f, p].State);
					}
				}

				// Gather new nodes to write.
				foreach (var link in node.Links)
				{
					var linkedNodes = link.Value;
					foreach (var linkedNode in linkedNodes)
					{
						if (!recordedNodes.Contains(linkedNode))
						{
							recordedNodes.Add(linkedNode);
							nodesToWrite.Add(linkedNode);
						}
					}
				}
			}

			// Write the number of nodes.
			var positionAfterNodes = (int)writer.BaseStream.Position;
			writer.Seek(positionBeforeNodes, SeekOrigin.Begin);
			writer.Write(writtenNodesList.Count);
			writer.Seek(positionAfterNodes, SeekOrigin.Begin);

			// Now that the nodes have all been written, write the links.
			foreach (var node in writtenNodesList)
			{
				var numLinks = node.Links.Count;
				writer.Write(numLinks);
				foreach (var link in node.Links)
				{
					// Write the link state.
					var graphLink = link.Key;
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							writer.Write(graphLink.Links[f, p].Valid);
							writer.Write((byte)graphLink.Links[f, p].Step);
							writer.Write((byte)graphLink.Links[f, p].Action);
						}
					}

					// Write all the ids of the nodes this link links to.
					var linkedNodes = link.Value;
					writer.Write(linkedNodes.Count);
					foreach (var linkedNode in linkedNodes)
					{
						var linkedNodeId = nodeIds[linkedNode];
						writer.Write(linkedNodeId);
					}
				}
			}
		}

		public static async Task<StepGraph> LoadAsync(string filePath, PadData padData)
		{
			return await Task.Run(() => Load(filePath, padData));
		}

		public static StepGraph Load(string filePath, PadData padData)
		{
			StepGraph stepGraph = null;
			try
			{
				using (var fileStream = File.Open(filePath, FileMode.Open))
				{
					using (var stream = Compression.DecompressLzma(fileStream))
					{
						using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
						{
							var version = reader.ReadInt32();
							if (version != 1)
							{
								throw new Exception($"Unsupported StepGraph version {version}. Expected 1.");
							}
							var root = ReadGraphV1(reader);
							stepGraph = new StepGraph(padData);
							stepGraph.Root = root;
						}
					}
				}
			}
			catch (Exception e)
			{
				LogError(padData.StepsType, padData.NumArrows, $"Failed to load StepGraph: {e}");
				return null;
			}

			return stepGraph;
		}

		private static GraphNode ReadGraphV1(BinaryReader reader)
		{
			var nodes = new Dictionary<int, GraphNode>();
			var nodesList = new List<GraphNode>();

			// Ensure that serialized enums have not changed.
			if (!DoStructuresMatchSerializedV1Data())
				throw new Exception("Programmer Error: Data required for StepGraph V1 deserialization has changed.");

			var numNodes = reader.ReadInt32();
			for(var nodeIndex = 0; nodeIndex < numNodes; nodeIndex++)
			{
				var id = reader.ReadInt32();
				var orientation = (BodyOrientation)reader.ReadByte();
				var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						var arrow = (int)reader.ReadByte();
						if (arrow == 255)
							arrow = InvalidArrowIndex;
						var graphArrowState = (GraphArrowState)reader.ReadByte();
						state[f, p] = new GraphNode.FootArrowState(arrow, graphArrowState);
					}
				}
				var node = new GraphNode(state, orientation);
				nodes.Add(id, node);
				nodesList.Add(node);
			}

			foreach(var node in nodesList)
			{
				var numLinks = reader.ReadInt32();
				for (var linkIndex = 0; linkIndex < numLinks; linkIndex++)
				{
					var graphLink = new GraphLink();
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							var valid = reader.ReadBoolean();
							var stepType = (StepType)reader.ReadByte();
							var footAction = (FootAction)reader.ReadByte();
							graphLink.Links[f, p] = new GraphLink.FootArrowState(stepType, footAction, valid);
						}
					}

					var numLinkedNodes = reader.ReadInt32();
					var linkedNodesList = new List<GraphNode>(numLinkedNodes);
					for (var linkedNodeIndex = 0; linkedNodeIndex < numLinkedNodes; linkedNodeIndex++)
					{
						var linkedNodeId = reader.ReadInt32();
						linkedNodesList.Add(nodes[linkedNodeId]);
					}
					node.Links.Add(graphLink, linkedNodesList);
				}
			}

			return nodesList[0];
		}

		#endregion Serialization

		#region Public Search Methods

		/// <summary>
		/// Searches the StepGraph for a GraphNode matching the given left and right
		/// foot states using DefaultFootPortions.
		/// </summary>
		/// <param name="leftArrow">Arrow the left foot should be on.</param>
		/// <param name="leftState">GraphArrowState for the left foot.</param>
		/// <param name="rightArrow">Arrow the right foot should be on.</param>
		/// <param name="rightState">GraphArrowState for the right foot.</param>
		/// <returns>GraphNode matching parameters or null if none was found.</returns>
		public GraphNode FindGraphNode(
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var trackedNodes = new HashSet<GraphNode>();
			var nodes = new HashSet<GraphNode> {Root};
			trackedNodes.Add(Root);
			while (true)
			{
				var newNodes = new HashSet<GraphNode>();

				foreach (var node in nodes)
				{
					if (StateMatches(node, leftArrow, leftState, rightArrow, rightState))
						return node;

					foreach (var l in node.Links)
					{
						foreach (var g in l.Value)
						{
							if (!trackedNodes.Contains(g))
							{
								trackedNodes.Add(g);
								newNodes.Add(g);
							}
						}
					}
				}

				nodes = newNodes;
				if (nodes.Count == 0)
					break;
			}

			return null;
		}

		/// <summary>
		/// Checks if the given GraphNode matches the state represented by the given
		/// arrows and GraphArrowStates for DefaultFootPortions for the left and right foot.
		/// Helper for FindGraphNode.
		/// </summary>
		/// <param name="node">GraphNode to check.</param>
		/// <param name="leftArrow">Arrow the left foot should be on.</param>
		/// <param name="leftState">GraphArrowState for the left foot.</param>
		/// <param name="rightArrow">Arrow the right foot should be on.</param>
		/// <param name="rightState">GraphArrowState for the right foot.</param>
		/// <returns>True if the state matches and false otherwise.</returns>
		private static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					state[f, p] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			state[L, DefaultFootPortion] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[R, DefaultFootPortion] = new GraphNode.FootArrowState(rightArrow, rightState);
			var newNode = new GraphNode(state, BodyOrientation.Normal);
			return node.Equals(newNode);
		}

		#endregion Public Search Methods

		#region Fill

		private bool CreateGraph(GraphNode root)
		{
			// Create all the nodes in the graph.
			var allNodes = CreateNodes();

			// Craete all the links between nodes.
			CreateLinks(allNodes);

			// Assign the root.
			foreach(var node in allNodes)
			{
				if (node.Equals(root))
				{
					Root = node;
					break;
				}
			}
			if (Root == null)
			{
				LogError("Could not find root.");
				return false;
			}

			EnsureAllNodesReachable(allNodes);
			return true;
		}

		/// <summary>
		/// Generates a list of all possible GraphNodes for this StepGraph.
		/// </summary>
		private List<GraphNode> CreateNodes()
		{
			var allNodes = new List<GraphNode>();

			var graphArrowStateValues = Enum.GetValues(typeof(GraphArrowState)).Cast<GraphArrowState>();
			var numGasValues = graphArrowStateValues.Count();
			var graphArrowStates = new GraphArrowState[numGasValues];
			var gasIndex = 0;
			foreach (var gas in graphArrowStateValues)
			{
				graphArrowStates[gasIndex] = gas;
				gasIndex++;
			}

			var bodyOrientationValues = Enum.GetValues(typeof(BodyOrientation)).Cast<BodyOrientation>();
			var numBodyOrientationValues = bodyOrientationValues.Count();
			var bodyOrientations = new BodyOrientation[numBodyOrientationValues];
			var boIndex = 0;
			foreach (var bo in bodyOrientationValues)
			{
				bodyOrientations[boIndex] = bo;
				boIndex++;
			}

			// There must be a better way to describe this, but we need to loop over all possible
			// foot arrow states for all combinations of feet and portions. A way to do that is to
			// effectively have a N-digit base-M number where N is 4 (NumFeet * NumFootPortions) and
			// M is 3 * NumArrows (3 being the number of unique GraphArrowStates). Using that number
			// we can loop over all values to get all the combinations.
			var numFootArrowStates = (NumArrows + 1) * numGasValues;
			var numStatesPerNode = NumFeet * NumFootPortions;
			var digits = new int[numStatesPerNode];
			for (var i = 0; i < numStatesPerNode; i++)
				digits[i] = 0;

			// Assumptions below:
			Debug.Assert(InvalidArrowIndex == -1);
			Debug.Assert(NumFeet == 2);
			Debug.Assert(NumFootPortions == 2);
			Debug.Assert(Heel == 0);

			var done = false;
			var occupiedArrows = new bool[NumArrows];
			var liftedUnoccupiedArrows = new bool[NumArrows];
			var footInvolvesLift = new bool[NumFeet];
			var footInvolvesBracket = new bool[NumFeet];
			LogInfo("Creating Nodes...");
			while (true)
			{
				for (var a = 0; a < NumArrows; a++)
				{
					occupiedArrows[a] = false;
					liftedUnoccupiedArrows[a] = false;
				}
				for (var f = 0; f < NumFeet; f++)
				{
					footInvolvesLift[f] = false;
					footInvolvesBracket[f] = false;
				}

				var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
				var stateDigitIndex = 0;
				var isValidState = true;
				var inverted = false;
				var crossover = false;
				var totalNumArrowsOccupied = 0;
				for (var f = 0; f < NumFeet; f++)
				{
					for (var p = 0; p < NumFootPortions; p++)
					{
						var arrow = digits[stateDigitIndex] / numGasValues;
						var gas = graphArrowStates[digits[stateDigitIndex] - (arrow * numGasValues)];
						// Subtract one because we include the InvalidArrowIndex in the number of total arrows to consider.
						arrow--;

						// Cannot hold or lift on InvalidArrowIndex. The default state for an InvalidArrowIndex is Resting.
						if (arrow == InvalidArrowIndex && gas != GraphArrowState.Resting)
						{
							LogState("Held or Lifted on InvalidArrowIndex", f, p, state, arrow, gas);
							isValidState = false;
							break;
						}

						if (arrow != InvalidArrowIndex && (gas == GraphArrowState.Held || gas == GraphArrowState.Resting))
						{
							// Cannot rest or hold if another foot portion is already resting or holding. Two feet can
							// only occupy the same arrow if one is Lifted after a swap.
							if (occupiedArrows[arrow])
							{
								LogState("Hold or Rest on Occupied Arrow", f, p, state, arrow, gas);
								isValidState = false;
								break;
							}
							totalNumArrowsOccupied++;
							occupiedArrows[arrow] = true;
							liftedUnoccupiedArrows[arrow] = false;
						}

						if (arrow != InvalidArrowIndex && gas == GraphArrowState.Lifted)
						{
							if (liftedUnoccupiedArrows[arrow])
							{
								LogState("Multiple Lifted feet", f, p, state, arrow, gas);
								isValidState = false;
								break;
							}
							if (!occupiedArrows[arrow])
								liftedUnoccupiedArrows[arrow] = true;
							footInvolvesLift[f] = true;
						}

						// Either there is only one foot portion for this foot (the DefaultFootPortion), or there are two
						// foot portions for this foot. In either case the 0th index (DefaultFootPortion) must be on a valid arrow.
						if (arrow == InvalidArrowIndex && p == DefaultFootPortion)
						{
							LogState("InvalidArrowIndex on DefaultFootPortion", f, p, state, arrow, gas);
							isValidState = false;
							break;
						}

						// For a bracket, the two arrows must be valid bracketable pairings.
						if (p > 0 && arrow != InvalidArrowIndex)
						{
							footInvolvesBracket[f] = true;
							var heelArrow = state[f, Heel].Arrow;
							if (!PadData.ArrowData[arrow].BracketablePairingsHeel[f][heelArrow])
							{
								LogState($"Foot {f} Pair Not Bracketable", f, p, state, arrow, gas);
								isValidState = false;
								break;
							}
						}

						// Update the state.
						state[f, p] = new GraphNode.FootArrowState(arrow, gas);

						// And the end of the loop over feet and portions, determine if this is a valid combination.
						if (f > 0 && p > 0)
						{
							// There can only be a lifted foot portion if the arrow has the other foot resting or holding on it.
							for (int a = 0; a < NumArrows; a++)
							{
								if (liftedUnoccupiedArrows[a])
								{
									LogState($"Lifted foot with no other foot occupying arrow", f, p, state, arrow, gas);
									isValidState = false;
									break;
								}
							}

							// Cannot have a state where one foot is resting or holding with one portion and lifted with the other
							// while the other foot is on the same two arrows also with one lift and one rest or hold. For example,
							// on 8 panel doubles, you cannot have L with heel on 4 resting and toe on 3 lifted and R with heel on
							// 4 lifted and toe on 3 resting. This can't occur because it would require a jump to enter with that
							// jump being two bracket one arrow swap steps, and swaps can't be used in jumps.
							// We already know from above checks that two lifts can't occur on the same arrow, so we can check for
							// two brackets on the same two arrows where each foot has one lift.
							if (totalNumArrowsOccupied == NumFootPortions
								&& footInvolvesLift[L] && footInvolvesLift[R]
								&& footInvolvesBracket[L] && footInvolvesBracket[R])
							{
								LogState($"Jump with two one arrow bracket swaps.", f, p, state, arrow, gas);
								isValidState = false;
								break;
							}

							// We don't currently support moves which invert or crossover and also stretch and bracket.
							// We do however support holding one foot with a bracket and doing a normal stretch
							// invert or crossover. We don't want to generate states which can't be linked to. To catch
							// invert or crossover stretch brackets we need to disallow states where all four portions
							// are down in that position, as the only way that state could be entered is through one of
							// those disallowed moves.
							if (totalNumArrowsOccupied == (NumFeet * NumFootPortions))
							{
								if (IsInvertedWithStretch(state))
								{
									LogState($"Stretch invert bracket.", f, p, state, arrow, gas);
									isValidState = false;
									break;
								}
								if (IsCrossoverWithStretch(state))
								{
									LogState($"Stretch crossover bracket.", f, p, state, arrow, gas);
									isValidState = false;
									break;
								}
							}

							inverted = IsInvertedWithOrWithoutStretch(state);
							crossover = IsCrossoverWithOrWithoutStretch(state);

							// The steps must be an invert, crossover, stretch, or normal position.
							if (!inverted && !crossover)
							{
								for (var lfp = 0; lfp < NumFootPortions; lfp++)
								{
									for (var rfp = 0; rfp < NumFootPortions; rfp++)
									{
										if (IsInvalidNormalPairing(state, lfp, rfp))
										{
											LogState($"L,{lfp} [{state[L, lfp].Arrow}] to R,{rfp} [{state[R, rfp].Arrow}] Not Valid", f, p, state, arrow, gas);
											isValidState = false;
											break;
										}
									}
								}
							}
						}

						stateDigitIndex++;
					}
					if (!isValidState)
						break;
				}

				if (isValidState)
				{
					for (var b = 0; b < numBodyOrientationValues; b++)
					{
						// Ensure the body orientation matches the steps.
						var bo = bodyOrientations[b];
						if (bo == BodyOrientation.Normal && inverted)
							continue;
						else if (bo != BodyOrientation.Normal && !inverted)
							continue;

						LogState($"Valid", 1, 1, state, state[1, 1].Arrow, state[1, 1].State);
						var gn = new GraphNode(state, bodyOrientations[b]);
						allNodes.Add(gn);
					}
				}

				// Advance.
				var digitIndex = 0;
				while (true)
				{
					digits[digitIndex]++;
					if (digits[digitIndex] != numFootArrowStates)
						break;
					digits[digitIndex] = 0;
					digitIndex++;
					if (digitIndex == digits.Length)
					{
						done = true;
						break;
					}
				}
				if (done)
					break;
			}

			LogInfo($"Created {allNodes.Count} Nodes.");
			return allNodes;
		}

		/// <summary>
		/// Creates GraphLinks between all given GraphNodes.
		/// </summary>
		private void CreateLinks(List<GraphNode> allNodes)
		{
			// For every node, compare it to every other node and find the step that would link the two.
			LogInfo($"Creating Links...");
			var nodeCount = allNodes.Count;

			var ad = PadData.ArrowData;
			var footIsSame = new bool[NumFeet];
			var footIsSameExceptForLifts = new bool[NumFeet];
			var fromFootIsBracket = new bool[NumFeet];
			var fromFootAnyHeld = new bool[NumFeet];
			var toFootIsBracket = new bool[NumFeet];
			var toStateHasLifts = new bool[NumFeet];
			for (var n1i = 0; n1i < nodeCount; n1i++)
			{
				var from = allNodes[n1i];
				var fromState = from.State;

				for (var f = 0; f < NumFeet; f++)
				{
					fromFootIsBracket[f] = true;
					fromFootAnyHeld[f] = false;
					for (var p = 0; p < NumFootPortions; p++)
					{
						if (fromState[f, p].Arrow == InvalidArrowIndex)
						{
							fromFootIsBracket[f] = false;
							toFootIsBracket[f] = false;
						}
						if (fromState[f, p].Arrow != InvalidArrowIndex && fromState[f, p].State == GraphArrowState.Held)
							fromFootAnyHeld[f] = true;
					}
				}

				for (var n2i = 0; n2i < nodeCount; n2i++)
				{
					var to = allNodes[n2i];
					var toState = to.State;

					for (var f = 0; f < NumFeet; f++)
					{
						footIsSame[f] = true;
						footIsSameExceptForLifts[f] = true;
						toFootIsBracket[f] = true;
						toStateHasLifts[f] = false;
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (fromState[f, p].Arrow != toState[f, p].Arrow || fromState[f, p].State != toState[f, p].State)
							{
								footIsSame[f] = false;
							}

							if (fromState[f, p].Arrow != toState[f, p].Arrow ||
								(fromState[f, p].State != toState[f, p].State && toState[f, p].State != GraphArrowState.Lifted))
							{
								footIsSameExceptForLifts[f] = false;
							}

							if (toState[f, p].Arrow != InvalidArrowIndex && toState[f, p].State == GraphArrowState.Lifted)
							{
								toStateHasLifts[f] = true;
							}

							if (toState[f, p].Arrow == InvalidArrowIndex)
								toFootIsBracket[f] = false;
						}
					}

					// Cannot do a 360. Other aggressive patterns like going from an invert to an opposing crossover are allowed as
					// there are certain patterns in modes like dance-double where it is the only way to perform a pattern.
					if (IsTransitionBetweenOpposingInverts(from, to))
						continue;

					var swing = IsSwing(from, to);

					var footPerformedValidFootSwap = new bool[NumFeet];

					var linksPerFoot = new List<GraphLink>[NumFeet];
					// Gather links to the second node from this node considering only one foot at a time.
					for (var f = 0; f < NumFeet; f++)
					{
						linksPerFoot[f] = new List<GraphLink>();
						var links = linksPerFoot[f];

						var otherFoot = OtherFoot(f);
						var otherFootUnchanged = footIsSame[otherFoot];
						var otherFootUnchangedExceptForLifts = footIsSameExceptForLifts[otherFoot];
						var otherFootSamePosition = otherFootUnchanged || otherFootUnchangedExceptForLifts;

						// Simple steps with no brackets involved.
						// It doesn't matter whether the foot is coming from a bracket or a single step.
						// If the previous state is bracketing and holding any arrow, then the only valid
						// next steps are bracket steps, either by releasing the hold which would be a one-arrow
						// bracket move, or by pressing on another arrow which would also be a one-arrow bracket
						// move, or by releasing both holds, which would be a full bracket move.
						if (!toFootIsBracket[f] && !(fromFootIsBracket[f] && fromFootAnyHeld[f]))
						{
							var p = DefaultFootPortion;
							var fs = fromState[f, p];
							var ts = toState[f, p];
							if (!CanFootActionTransition(fs.State, ts.State))
								continue;
							var action = GetActionForStates(fs.State, ts.State);

							var isValidFootSwap = IsFootSwap(fromState, toState, f, ts.Arrow, true) && action != FootAction.Release;

							// Swing
							if (swing)
							{
								if (otherFootUnchanged && action != FootAction.Release)
									RecordLink(links, f, p, StepType.Swing, action);
							}

							// Stepping on the same arrow is either a SameArrow step or a FootSwap.
							else if (IsSameArrowStep(fromState, f, ts.Arrow) && from.Orientation == to.Orientation)
							{
								if (isValidFootSwap)
								{
									RecordLink(links, f, p, StepType.FootSwap, action);
									footPerformedValidFootSwap[f] = true;
								}
								else
								{
									RecordLink(links, f, p, StepType.SameArrow, action);
								}
							}

							// New arrow step.
							else
							{
								// Cannot release on a new arrow.
								if (action == FootAction.Release)
									continue;

								// Inverted steps. Process these first because every other step type
								// other than inverted steps require the orientation to be normal.
								var invertedWithStretch = IsInvertedWithStretch(toState, f, ts.Arrow);
								var invertedWithoutStretch = IsInvertedWithoutStretch(toState, f, ts.Arrow);
								if ((invertedWithStretch || invertedWithoutStretch) && otherFootSamePosition)
								{
									// Inverting right over left.
									if (to.Orientation == BodyOrientation.InvertedRightOverLeft)
									{
										// Inverting right over left when moving the left foot requires
										// bringing the left foot from a position further back on the pads
										// upwards and behind the right foot.
										if (f == L && ad[fs.Arrow].Y >= ad[ts.Arrow].Y)
										{
											if (otherFootUnchanged)
											{
												if (invertedWithStretch)
													RecordLink(links, f, p, StepType.InvertBehindStretch, action);
												else
													RecordLink(links, f, p, StepType.InvertBehind, action);
											}
											else if (isValidFootSwap)
											{
												if (!invertedWithStretch)
												{
													footPerformedValidFootSwap[f] = true;
													RecordLink(links, f, p, StepType.FootSwapInvertBehind, action);
												}
											}
										}
										// Inverting right over left when moving the right foot requires
										// bringing the right foot from a position further up on the pads
										// downwards and in front of the left foot.
										if (f == R && ad[fs.Arrow].Y <= ad[ts.Arrow].Y)
										{
											if (otherFootUnchanged)
											{
												if (invertedWithStretch)
													RecordLink(links, f, p, StepType.InvertFrontStretch, action);
												else
													RecordLink(links, f, p, StepType.InvertFront, action);
											}
											else if (isValidFootSwap)
											{
												if (!invertedWithStretch)
												{
													footPerformedValidFootSwap[f] = true;
													RecordLink(links, f, p, StepType.FootSwapInvertFront, action);
												}
											}
										}
									}
									// Inverting left over right.
									else if (to.Orientation == BodyOrientation.InvertedLeftOverRight)
									{
										// Inverting left over right when moving the left foot requires
										// bringing the left foot from a position further up on the pads
										// downwards and in front of the right foot.
										if (f == L && ad[fs.Arrow].Y <= ad[ts.Arrow].Y)
										{
											if (otherFootUnchanged)
											{
												if (invertedWithStretch)
													RecordLink(links, f, p, StepType.InvertFrontStretch, action);
												else
													RecordLink(links, f, p, StepType.InvertFront, action);
											}
											else if (isValidFootSwap)
											{
												if (!invertedWithStretch)
												{
													footPerformedValidFootSwap[f] = true;
													RecordLink(links, f, p, StepType.FootSwapInvertFront, action);
												}
											}
										}
										// Inverting left over right when moving the right foot requires
										// bringing the right foot from a position further back on the pads
										// upwards and behind the left foot.
										if (f == R && ad[fs.Arrow].Y >= ad[ts.Arrow].Y)
										{
											if (otherFootUnchanged)
											{
												if (invertedWithStretch)
													RecordLink(links, f, p, StepType.InvertBehindStretch, action);
												else
													RecordLink(links, f, p, StepType.InvertBehind, action);
											}
											else if (isValidFootSwap)
											{
												if (!invertedWithStretch)
												{
													footPerformedValidFootSwap[f] = true;
													RecordLink(links, f, p, StepType.FootSwapInvertBehind, action);
												}
											}
										}
									}
								}

								// Now that inverted steps have been checked we can early out on inverted orientations.
								if (to.Orientation != BodyOrientation.Normal)
									continue;

								var normalCrossoverInBack = IsCrossoverInBackWithoutStretch(toState, f, ts.Arrow);
								var normalCrossoverInFront = IsCrossoverInFrontWithoutStretch(toState, f, ts.Arrow);

								// Other simple step types.
								if (IsCrossoverInBackWithStretch(toState, f, ts.Arrow) && otherFootUnchanged)
								{
									RecordLink(links, f, p, StepType.CrossoverBehindStretch, action);
								}
								else if (normalCrossoverInBack && otherFootUnchanged)
								{
									RecordLink(links, f, p, StepType.CrossoverBehind, action);
								}
								else if (normalCrossoverInBack && isValidFootSwap)
								{
									RecordLink(links, f, p, StepType.FootSwapCrossoverBehind, action);
									footPerformedValidFootSwap[f] = true;
								}
								else if (IsCrossoverInFrontWithStretch(toState, f, ts.Arrow) && otherFootUnchanged)
								{
									RecordLink(links, f, p, StepType.CrossoverFrontStretch, action);
								}
								else if (normalCrossoverInFront && otherFootUnchanged)
								{
									RecordLink(links, f, p, StepType.CrossoverFront, action);
								}
								else if (normalCrossoverInFront && isValidFootSwap)
								{
									RecordLink(links, f, p, StepType.FootSwapCrossoverFront, action);
									footPerformedValidFootSwap[f] = true;
								}
								else if (isValidFootSwap)
								{
									RecordLink(links, f, p, StepType.FootSwap, action);
									footPerformedValidFootSwap[f] = true;
								}
								else if (IsStretch(toState))
								{
									RecordLink(links, f, p, StepType.NewArrowStretch, action);
								}
								else if (IsNormalStep(toState, f, ts.Arrow))
								{
									RecordLink(links, f, p, StepType.NewArrow, action);
								}
							}
						}

						// The foot is moving to a bracket position, from either a simple position or a bracket position
						else
						{
							// Bracketing single arrows is complicated as the foot portion can swap when this occurs.
							// For example if you were holding with your heel and your toe was resting from a previous
							// bracket, you could still do a BracketOneArrowHeelNew from this position by sliding your
							// foot to now hold the held arrow with your toe. These variables help make the conditionals
							// below easier to understand.
							var holdingWithDefaultPortionInNonBracketFromStateWithToStateHeelArrow =
								!fromFootIsBracket[f]
								&& fromState[f, DefaultFootPortion].State == GraphArrowState.Held
								&& fromState[f, DefaultFootPortion].Arrow == toState[f, Heel].Arrow;
							var holdingWithOnlyHeelInBracketFromStateWithToStateHeelArrow =
								fromFootIsBracket[f]
								&& fromState[f, Heel].State == GraphArrowState.Held
								&& fromState[f, Heel].Arrow == toState[f, Heel].Arrow
								&& fromState[f, Toe].State != GraphArrowState.Held;
							var holdingWithOnlyToeInBracketFromStateWithToStateHeelArrow =
								fromFootIsBracket[f]
								&& fromState[f, Toe].State == GraphArrowState.Held
								&& fromState[f, Toe].Arrow == toState[f, Heel].Arrow
								&& fromState[f, Heel].State != GraphArrowState.Held;
							var holdingWithBothPortionsInBracketFromStateWithHeelMatchingToStateHeelArrow =
								fromFootIsBracket[f]
								&& fromState[f, Heel].State == GraphArrowState.Held
								&& fromState[f, Toe].State == GraphArrowState.Held
								&& fromState[f, Heel].Arrow == toState[f, Heel].Arrow;
							var heelUnchangedToeChanged =
								fromFootIsBracket[f]
								&& toFootIsBracket[f]
								&& fromState[f, Heel].State == toState[f, Heel].State
								&& fromState[f, Heel].Arrow == toState[f, Heel].Arrow
								&& (fromState[f, Toe].State != toState[f, Toe].State
								|| fromState[f, Toe].Arrow != toState[f, Toe].Arrow);
							var heelUnchangedHeelSameArrow =
								fromFootIsBracket[f]
								&& toFootIsBracket[f]
								&& fromState[f, Heel].State == toState[f, Heel].State
								&& fromState[f, Heel].Arrow == toState[f, Heel].Arrow
								&& fromState[f, Toe].Arrow == toState[f, Toe].Arrow;

							var holdingWithDefaultPortionInNonBracketFromStateWithToStateToeArrow =
								!fromFootIsBracket[f]
								&& fromState[f, DefaultFootPortion].State == GraphArrowState.Held
								&& fromState[f, DefaultFootPortion].Arrow == toState[f, Toe].Arrow;
							var holdingWithOnlyHeelInBracketFromStateWithToStateToeArrow =
								fromFootIsBracket[f]
								&& fromState[f, Heel].State == GraphArrowState.Held
								&& fromState[f, Heel].Arrow == toState[f, Toe].Arrow
								&& fromState[f, Toe].State != GraphArrowState.Held;
							var holdingWithOnlyToeInBracketFromStateWithToStateToeArrow =
								fromFootIsBracket[f]
								&& fromState[f, Toe].State == GraphArrowState.Held
								&& fromState[f, Toe].Arrow == toState[f, Toe].Arrow
								&& fromState[f, Heel].State != GraphArrowState.Held;
							var holdingWithBothPortionsInBracketFromStateWithToeMatchingToStateToeArrow =
								fromFootIsBracket[f]
								&& fromState[f, Heel].State == GraphArrowState.Held
								&& fromState[f, Toe].State == GraphArrowState.Held
								&& fromState[f, Toe].Arrow == toState[f, Toe].Arrow;
							var toeUnchangedHeelChanged =
								fromFootIsBracket[f]
								&& toFootIsBracket[f]
								&& fromState[f, Toe].State == toState[f, Toe].State
								&& fromState[f, Toe].Arrow == toState[f, Toe].Arrow
								&& (fromState[f, Heel].State != toState[f, Heel].State
								|| fromState[f, Heel].Arrow != toState[f, Heel].Arrow);
							var toeUnchangedHeelSameArrow =
								fromFootIsBracket[f]
								&& toFootIsBracket[f]
								&& fromState[f, Toe].State == toState[f, Toe].State
								&& fromState[f, Toe].Arrow == toState[f, Toe].Arrow
								&& fromState[f, Heel].Arrow == toState[f, Heel].Arrow;

							// Holding with Heel and bracketing with the Toe.
							if ((heelUnchangedToeChanged && fromFootAnyHeld[f])
								|| (toState[f, Heel].State == GraphArrowState.Held
								&& (holdingWithDefaultPortionInNonBracketFromStateWithToStateHeelArrow
								|| holdingWithBothPortionsInBracketFromStateWithHeelMatchingToStateHeelArrow
								|| holdingWithOnlyHeelInBracketFromStateWithToStateHeelArrow
								|| holdingWithOnlyToeInBracketFromStateWithToStateHeelArrow
								|| heelUnchangedToeChanged)))
							{
								// The from state for the toe depends on how the from state was oriented.
								// If we were coming from a state where only the toe was holding and now we
								// are transitioning to a state where the heel has replaced the toe on the arrow
								// which is holding then we want to consider the toe as coming from a default state.
								var toeFromState = GraphArrowState.Resting;
								var toeFromArrow = InvalidArrowIndex;
								if (heelUnchangedToeChanged
									|| heelUnchangedHeelSameArrow
									|| holdingWithOnlyHeelInBracketFromStateWithToStateHeelArrow
									|| holdingWithBothPortionsInBracketFromStateWithHeelMatchingToStateHeelArrow)
								{
									toeFromState = fromState[f, Toe].State;
									toeFromArrow = fromState[f, Toe].Arrow;
								}

								if (!CanFootActionTransition(toeFromState, toState[f, Toe].State))
									continue;

								var toeAction = GetActionForStates(toeFromState, toState[f, Toe].State);
								var toeArrow = toState[f, Toe].Arrow;

								var isValidFootSwap = IsFootSwap(fromState, toState, f, toeArrow, true) && toeAction != FootAction.Release;

								// Swing
								if (swing)
								{
									// There is no valid swing step with bracketing one arrow.
								}

								// Toe bracketing the same arrow.
								else if (toeFromArrow != InvalidArrowIndex && toeFromArrow == toeArrow)
								{
									if (isValidFootSwap)
									{
										RecordLink(links, f, Toe, StepType.BracketOneArrowToeSwap, toeAction);
										footPerformedValidFootSwap[f] = true;
									}
									else
									{
										RecordLink(links, f, Toe, StepType.BracketOneArrowToeSame, toeAction);
									}
								}

								// Toe bracketing a new arrow.
								else if (toeArrow != InvalidArrowIndex && toeFromArrow != toeArrow)
								{
									// Cannot release on a new arrow.
									if (toeAction == FootAction.Release)
										continue;

									// Inverted steps caused by toe bracketing a new arrow.
									if (IsInvertedWithoutStretch(toState, f, toeArrow) && otherFootUnchanged)
									{
										// Inverting toe bracket right over left.
										if (to.Orientation == BodyOrientation.InvertedRightOverLeft)
										{
											if (f == L)
												RecordLink(links, f, Toe, StepType.BracketInvertBehindOneArrowToeNew, toeAction);
											else
												RecordLink(links, f, Toe, StepType.BracketInvertFrontOneArrowToeNew, toeAction);
										}
										// Inverting toe bracket left over right.
										else if (to.Orientation == BodyOrientation.InvertedLeftOverRight)
										{
											if (f == L)
												RecordLink(links, f, Toe, StepType.BracketInvertFrontOneArrowToeNew, toeAction);
											else
												RecordLink(links, f, Toe, StepType.BracketInvertBehindOneArrowToeNew, toeAction);
										}
									}

									// Now that inverted steps have been checked we can early out on inverted orientations.
									if (to.Orientation != BodyOrientation.Normal)
										continue;

									var xoBackStr = IsCrossoverInBackWithStretch(toState, f, toeArrow);
									var xoBack = IsCrossoverInBackWithoutStretch(toState, f, toeArrow);
									var xoFrontStr = IsCrossoverInFrontWithStretch(toState, f, toeArrow);
									var xoFront = IsCrossoverInFrontWithoutStretch(toState, f, toeArrow);

									// Other toe bracket states.
									// Using if/else here because a normal bracket step of one arrow by itself might look
									// like stretch. In other words, IsNormalStep() would return false for that step. We already
									// from the checks above that the new arrow is a valid bracketable pairing, so if it isn't 
									// any kind of special step then we know it is a normal step.
									if (xoBack || xoBackStr)
									{
										if (otherFootUnchanged && xoBack)
											RecordLink(links, f, Toe, StepType.BracketCrossoverBehindOneArrowToeNew, toeAction);
									}
									else if (xoFront || xoFrontStr)
									{
										if (otherFootUnchanged && xoFront)
											RecordLink(links, f, Toe, StepType.BracketCrossoverFrontOneArrowToeNew, toeAction);
									}
									else if (isValidFootSwap)
									{
										RecordLink(links, f, Toe, StepType.BracketOneArrowToeSwap, toeAction);
										footPerformedValidFootSwap[f] = true;
									}
									else if (IsStretch(toState))
									{
										RecordLink(links, f, Toe, StepType.BracketStretchOneArrowToeNew, toeAction);
									}
									else
									{
										RecordLink(links, f, Toe, StepType.BracketOneArrowToeNew, toeAction);
									}
								}
							}

							// Holding with Toe and bracketing with the Heel.
							else if ((toeUnchangedHeelChanged && fromFootAnyHeld[f])
								|| (toState[f, Toe].State == GraphArrowState.Held
								&& (holdingWithDefaultPortionInNonBracketFromStateWithToStateToeArrow
								|| holdingWithBothPortionsInBracketFromStateWithToeMatchingToStateToeArrow
								|| holdingWithOnlyHeelInBracketFromStateWithToStateToeArrow
								|| holdingWithOnlyToeInBracketFromStateWithToStateToeArrow)))
							{
								// The from state for the heel depends on how the from state was oriented.
								// If we were coming from a state where only the heel was holding and now we
								// are transitioning to a state where the toe has replaced the heel on the arrow
								// which is holding then we want to consider the heel as coming from a default state.
								var heelFromState = GraphArrowState.Resting;
								var heelFromArrow = InvalidArrowIndex;
								if (toeUnchangedHeelChanged
									|| toeUnchangedHeelSameArrow
									|| holdingWithOnlyHeelInBracketFromStateWithToStateHeelArrow
									|| holdingWithBothPortionsInBracketFromStateWithToeMatchingToStateToeArrow)
								{
									heelFromState = fromState[f, Heel].State;
									heelFromArrow = fromState[f, Heel].Arrow;
								}

								if (!CanFootActionTransition(heelFromState, toState[f, Heel].State))
									continue;

								var heelAction = GetActionForStates(heelFromState, toState[f, Heel].State);
								var heelArrow = toState[f, Heel].Arrow;

								var isValidFootSwap = IsFootSwap(fromState, toState, f, heelArrow, true) && heelAction != FootAction.Release;

								// Swing
								if (swing)
								{
									// There is no valid swing step with bracketing one arrow.
								}

								// Heel bracketing the same arrow.
								else if (heelFromArrow != InvalidArrowIndex && heelFromArrow == heelArrow)
								{
									if (isValidFootSwap)
									{
										RecordLink(links, f, Heel, StepType.BracketOneArrowHeelSwap, heelAction);
										footPerformedValidFootSwap[f] = true;
									}
									else
									{
										RecordLink(links, f, Heel, StepType.BracketOneArrowHeelSame, heelAction);
									}
								}

								// Heel bracketing a new arrow.
								else if (heelArrow != InvalidArrowIndex && heelFromArrow != heelArrow)
								{
									// Cannot release on a new arrow.
									if (heelAction == FootAction.Release)
										continue;

									// Inverted steps caused by heel bracketing a new arrow.
									if (IsInvertedWithoutStretch(toState, f, heelArrow) && otherFootUnchanged)
									{
										// Inverting heel bracket right over left.
										if (to.Orientation == BodyOrientation.InvertedRightOverLeft)
										{
											if (f == L)
												RecordLink(links, f, Heel, StepType.BracketInvertBehindOneArrowHeelNew, heelAction);
											else
												RecordLink(links, f, Heel, StepType.BracketInvertFrontOneArrowHeelNew, heelAction);
										}
										// Inverting heel bracket left over right.
										else if (to.Orientation == BodyOrientation.InvertedLeftOverRight)
										{
											if (f == L)
												RecordLink(links, f, Heel, StepType.BracketInvertFrontOneArrowHeelNew, heelAction);
											else
												RecordLink(links, f, Heel, StepType.BracketInvertBehindOneArrowHeelNew, heelAction);
										}
									}

									// Now that inverted steps have been checked we can early out on inverted orientations.
									if (to.Orientation != BodyOrientation.Normal)
										continue;

									var xoBackStr = IsCrossoverInBackWithStretch(toState, f, heelArrow);
									var xoBack = IsCrossoverInBackWithoutStretch(toState, f, heelArrow);
									var xoFrontStr = IsCrossoverInFrontWithStretch(toState, f, heelArrow);
									var xoFront = IsCrossoverInFrontWithoutStretch(toState, f, heelArrow);

									// Other heel bracket states.
									// Using if/else here because a normal bracket step of one arrow by itself might look
									// like stretch. In other words, IsNormalStep() would return false for that step. We already
									// from the checks above that the new arrow is a valid bracketable pairing, so if it isn't 
									// any kind of special step then we know it is a normal step.
									if (xoBack || xoBackStr)
									{
										if (otherFootUnchanged && xoBack)
											RecordLink(links, f, Heel, StepType.BracketCrossoverBehindOneArrowHeelNew, heelAction);
									}
									else if (xoFront || xoFrontStr)
									{
										if (otherFootUnchanged && xoFront)
											RecordLink(links, f, Heel, StepType.BracketCrossoverFrontOneArrowHeelNew, heelAction);
									}
									else if (isValidFootSwap)
									{
										RecordLink(links, f, Heel, StepType.BracketOneArrowHeelSwap, heelAction);
										footPerformedValidFootSwap[f] = true;
									}
									else if (IsStretch(toState))
									{
										RecordLink(links, f, Heel, StepType.BracketStretchOneArrowHeelNew, heelAction);
									}
									else
									{
										RecordLink(links, f, Heel, StepType.BracketOneArrowHeelNew, heelAction);
									}
								}
							}

							// Bracketing two arrows with one foot.
							else
							{
								var actions = new FootAction[NumFootPortions];
								var numReleases = 0;

								// Ensure the transition can be made.
								var canTransition = true;
								for (var p = 0; p < NumFootPortions; p++)
								{
									if (!CanFootActionTransition(fromState[f, p].State, toState[f, p].State))
									{
										canTransition = false;
										break;
									}
								}
								if (!canTransition)
									continue;

								for (var p = 0; p < NumFootPortions; p++)
								{
									actions[p] = GetActionForStates(fromState[f, p].State, toState[f, p].State);
									if (actions[p] == FootAction.Release)
										numReleases++;
								}

								// Either both portions must release, or none must release.
								if (!(numReleases == 0 || numReleases == NumFootPortions))
									continue;

								var heelArrow = toState[f, Heel].Arrow;
								var toeArrow = toState[f, Toe].Arrow;
								var heelSame =
									(!fromFootIsBracket[f] && heelArrow == fromState[f, DefaultFootPortion].Arrow)
									|| (fromFootIsBracket[f] && heelArrow == fromState[f, Heel].Arrow);
								var toeSame =
									(!fromFootIsBracket[f] && toeArrow == fromState[f, DefaultFootPortion].Arrow)
									|| (fromFootIsBracket[f] && toeArrow == fromState[f, Toe].Arrow);

								var isHeelValidIndependentSwap =
									actions[Heel] != FootAction.Release && IsFootSwap(fromState, toState, f, toState[f, Heel].Arrow, true);
								var isToeValidIndependentSwap =
									actions[Toe] != FootAction.Release && IsFootSwap(fromState, toState, f, toState[f, Toe].Arrow, true);
								var isValidFullBracketSwap =
									actions[Heel] != FootAction.Release && IsFootSwap(fromState, toState, f, toState[f, Heel].Arrow, false)
									&& actions[Toe] != FootAction.Release && IsFootSwap(fromState, toState, f, toState[f, Toe].Arrow, false);

								// Swing
								if (swing)
								{
									if (otherFootUnchanged && numReleases == 0)
										RecordLink(links, f, StepType.BracketSwing, actions);
								}

								// Bracketing the same two arrows.
								else if (fromFootIsBracket[f] && heelSame && toeSame)
								{
									if (isValidFullBracketSwap)
									{
										RecordLink(links, f, StepType.BracketHeelSwapToeSwap, actions);
										footPerformedValidFootSwap[f] = true;
									}
									else if (isHeelValidIndependentSwap && !isToeValidIndependentSwap)
									{
										RecordLink(links, f, StepType.BracketHeelSwapToeSame, actions);
										footPerformedValidFootSwap[f] = true;
									}
									else if (!isHeelValidIndependentSwap && isToeValidIndependentSwap)
									{
										RecordLink(links, f, StepType.BracketHeelSameToeSwap, actions);
										footPerformedValidFootSwap[f] = true;
									}
									else
									{
										RecordLink(links, f, StepType.BracketHeelSameToeSame, actions);
									}
								}

								// Bracketing with at least one new arrow
								else
								{
									// From this point on neither action for the bracket can be release. Releases can only occur one at a time
									// in which case they would be two separate SameArrow steps, or together in which case they would be one
									// BracketHeelSameToeSame step.
									if (numReleases != 0)
										continue;

									// Inverted bracket steps.
									if (IsInvertedWithoutStretch(toState) && otherFootUnchanged)
									{
										// Inverting bracket right over left.
										if (to.Orientation == BodyOrientation.InvertedRightOverLeft)
										{
											// Invert in back.
											if (f == L)
											{
												if (heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelSameToeNew, actions);
												else if (toeSame && !heelSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelNewToeSame, actions);
												else if (!heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelNewToeNew, actions);
											}
											// Invert in front.
											else
											{
												if (heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelSameToeNew, actions);
												else if (toeSame && !heelSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelNewToeSame, actions);
												else if (!heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelNewToeNew, actions);
											}
										}
										// Inverting bracket left over right.
										else if (to.Orientation == BodyOrientation.InvertedLeftOverRight)
										{
											// Invert in front.
											if (f == L)
											{
												if (heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelSameToeNew, actions);
												else if (toeSame && !heelSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelNewToeSame, actions);
												else if (!heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertFrontHeelNewToeNew, actions);
											}
											// Invert in back.
											else
											{
												if (heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelSameToeNew, actions);
												else if (toeSame && !heelSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelNewToeSame, actions);
												else if (!heelSame && !toeSame)
													RecordLink(links, f, StepType.BracketInvertBehindHeelNewToeNew, actions);
											}
										}
									}

									// Now that inverted steps have been checked we can early out on inverted orientations.
									if (to.Orientation != BodyOrientation.Normal)
										continue;

									var xoBackStr = IsCrossoverInBackWithStretch(toState, f);
									var xoBack = IsCrossoverInBackWithoutStretch(toState, f);
									var xoFrontStr = IsCrossoverInFrontWithStretch(toState, f);
									var xoFront = IsCrossoverInFrontWithoutStretch(toState, f);

									// Other bracket states.
									if (xoBack || xoBackStr)
									{
										if (xoBack && otherFootUnchanged)
										{
											if (!heelSame && !toeSame)
												RecordLink(links, f, StepType.BracketCrossoverBehindHeelNewToeNew, actions);
											else if (heelSame && !toeSame)
												RecordLink(links, f, StepType.BracketCrossoverBehindHeelSameToeNew, actions);
											else if (!heelSame && toeSame)
												RecordLink(links, f, StepType.BracketCrossoverBehindHeelNewToeSame, actions);
										}
									}
									else if (xoFront || xoFrontStr)
									{
										if (xoFront && otherFootUnchanged)
										{
											if (!heelSame && !toeSame)
												RecordLink(links, f, StepType.BracketCrossoverFrontHeelNewToeNew, actions);
											else if (heelSame && !toeSame)
												RecordLink(links, f, StepType.BracketCrossoverFrontHeelSameToeNew, actions);
											else if (!heelSame && toeSame)
												RecordLink(links, f, StepType.BracketCrossoverFrontHeelNewToeSame, actions);
										}
									}
									else if (isValidFullBracketSwap)
									{
										RecordLink(links, f, StepType.BracketHeelSwapToeSwap, actions);
										footPerformedValidFootSwap[f] = true;
									}
									else if (!isHeelValidIndependentSwap && isToeValidIndependentSwap)
									{
										if (heelSame)
										{
											RecordLink(links, f, StepType.BracketHeelSameToeSwap, actions);
											footPerformedValidFootSwap[f] = true;
										}
										else
										{
											RecordLink(links, f, StepType.BracketHeelNewToeSwap, actions);
											footPerformedValidFootSwap[f] = true;
										}
									}
									else if (isHeelValidIndependentSwap && !isToeValidIndependentSwap)
									{
										if (toeSame)
										{
											RecordLink(links, f, StepType.BracketHeelSwapToeSame, actions);
											footPerformedValidFootSwap[f] = true;
										}
										else
										{
											RecordLink(links, f, StepType.BracketHeelSwapToeNew, actions);
											footPerformedValidFootSwap[f] = true;
										}
									}
									else if (IsStretch(toState))
									{
										if (!heelSame && !toeSame)
											RecordLink(links, f, StepType.BracketStretchHeelNewToeNew, actions);
										else if (heelSame && !toeSame)
											RecordLink(links, f, StepType.BracketStretchHeelSameToeNew, actions);
										else if (!heelSame && toeSame)
											RecordLink(links, f, StepType.BracketStretchHeelNewToeSame, actions);
									}
									else
									{
										if (!heelSame && !toeSame)
											RecordLink(links, f, StepType.BracketHeelNewToeNew, actions);
										else if (heelSame && !toeSame)
											RecordLink(links, f, StepType.BracketHeelSameToeNew, actions);
										else if (!heelSame && toeSame)
											RecordLink(links, f, StepType.BracketHeelNewToeSame, actions);
									}
								}
							}
						}
					}

					// Take the links gathered above and add them as single foot steps if appropriate.
					var validFeetCount = 0;
					for (var f = 0; f < NumFeet; f++)
					{
						// The other foot must be in the same state before and after for a single step to be valid.
						// Or it adjusted due to being swapped over.
						var of = OtherFoot(f);
						if (!(footIsSame[of] || footPerformedValidFootSwap[f]))
							continue;

						validFeetCount++;
						foreach (var link in linksPerFoot[f])
							AddLink(from, to, link);
					}

					// Combine the links gathered above into jump links for valid pairs.
					foreach (var leftLink in linksPerFoot[L])
					{
						if (!CanBeUsedInJump(leftLink))
							continue;

						foreach (var rightLink in linksPerFoot[R])
						{
							if (!CanBeUsedInJump(rightLink))
								continue;

							var jumpLink = new GraphLink();
							for (var p = 0; p < NumFootPortions; p++)
							{
								jumpLink.Links[L, p] = leftLink.Links[L, p];
								jumpLink.Links[R, p] = rightLink.Links[R, p];
							}
							AddLink(from, to, jumpLink);
						}
					}
				}
			}
			LogInfo($"Created Links.");
		}

		private void EnsureAllNodesReachable(List<GraphNode> allNodes)
		{
			var expectedCount = allNodes.Count();

			var trackedNodes = new HashSet<GraphNode>();
			var nodes = new HashSet<GraphNode> { Root };
			trackedNodes.Add(Root);
			while (true)
			{
				var newNodes = new HashSet<GraphNode>();

				foreach (var node in nodes)
				{
					foreach (var l in node.Links)
					{
						foreach (var g in l.Value)
						{
							if (!trackedNodes.Contains(g))
							{
								trackedNodes.Add(g);
								newNodes.Add(g);
							}
						}
					}
				}

				nodes = newNodes;
				if (nodes.Count == 0)
					break;
			}

			if (expectedCount != trackedNodes.Count)
			{
				LogError("Unreachable nodes found.");
				foreach (var node in allNodes)
				{
					if (!trackedNodes.Contains(node))
					{
						LogNode("Unreachable node", node);
					}
				}
			}
		}

		private static bool CanBeUsedInJump(GraphLink link)
		{
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (link.Links[f, p].Valid && !StepData.Steps[(int)link.Links[f, p].Step].CanBeUsedInJump)
						return false;
				}
			}
			return true;
		}

		private static bool CanFootActionTransition(GraphArrowState from, GraphArrowState to)
		{
			// No FootAction will transition from Held to Held.
			if (from == GraphArrowState.Held && to == GraphArrowState.Held)
				return false;
			// No FootAction will transition to Lifted.
			if (to == GraphArrowState.Lifted)
				return false;
			return true;
		}

		private static FootAction GetActionForStates(GraphArrowState from, GraphArrowState to)
		{
			if ((from == GraphArrowState.Resting || from == GraphArrowState.Lifted) && to == GraphArrowState.Held)
				return FootAction.Hold;
			if (from == GraphArrowState.Held && to == GraphArrowState.Resting)
				return FootAction.Release;
			return FootAction.Tap;
		}

		private void RecordLink(List<GraphLink> links, int f, int p, StepType step, FootAction action)
		{
			var link = new GraphLink();
			link.Links[f, p] = new GraphLink.FootArrowState(step, action);
			links.Add(link);
		}

		private void RecordLink(List<GraphLink> links, int f, StepType step, FootAction[] actions)
		{
			var link = new GraphLink();
			for (var p = 0; p < NumFootPortions; p++)
				link.Links[f, p] = new GraphLink.FootArrowState(step, actions[p]);
			links.Add(link);
		}

		private void AddLink(GraphNode from, GraphNode to, GraphLink link)
		{
			if (!from.Links.ContainsKey(link))
				from.Links.Add(link, new List<GraphNode>());
			from.Links[link].Add(to);
		}

		#endregion Fill

		#region Public State Helpers

		public bool IsStretch(GraphNode node)
		{
			return IsStretch(node.State);
		}

		public bool IsCrossoverWithOrWithoutStretch(GraphNode node)
		{
			return IsCrossoverWithOrWithoutStretch(node.State);
		}

		public bool IsCrossoverWithStretch(GraphNode node)
		{
			return IsCrossoverWithStretch(node.State);
		}

		public bool IsCrossoverWithoutStretch(GraphNode node)
		{
			return IsCrossoverWithoutStretch(node.State);
		}

		public bool IsInvertedWithOrWithoutStretch(GraphNode node)
		{
			return IsInvertedWithOrWithoutStretch(node.State);
		}

		public bool IsInvertedWithStretch(GraphNode node)
		{
			return IsInvertedWithStretch(node.State);
		}

		public bool IsInvertedWithoutStretch(GraphNode node)
		{
			return IsInvertedWithoutStretch(node.State);
		}

		public bool IsTransitionBetweenOpposingInverts(GraphNode from, GraphNode to)
		{
			if (from.Orientation == BodyOrientation.InvertedRightOverLeft && to.Orientation == BodyOrientation.InvertedLeftOverRight)
				return true;
			if (from.Orientation == BodyOrientation.InvertedLeftOverRight && to.Orientation == BodyOrientation.InvertedRightOverLeft)
				return true;
			return false;
		}

		public bool IsSwing(GraphNode from, GraphNode to)
		{
			return IsTransitionBetweenInvertAndOpposingCrossover(from, to)
				|| IsTransitionBetweenOpposingCrossovers(from, to)
				|| IsTransitionBetweenInvertAndStepOnOtherSide(from, to);
		}

		public bool IsTransitionBetweenInvertAndOpposingCrossover(GraphNode from, GraphNode to)
		{
			if (IsCrossoverInBackWithOrWithoutStretch(from.State, L) && to.Orientation == BodyOrientation.InvertedLeftOverRight)
				return true;
			if (IsCrossoverInFrontWithOrWithoutStretch(from.State, L) && to.Orientation == BodyOrientation.InvertedRightOverLeft)
				return true;
			if (IsCrossoverInBackWithOrWithoutStretch(to.State, L) && from.Orientation == BodyOrientation.InvertedLeftOverRight)
				return true;
			if (IsCrossoverInFrontWithOrWithoutStretch(to.State, L) && from.Orientation == BodyOrientation.InvertedRightOverLeft)
				return true;
			return false;
		}

		public bool IsTransitionBetweenOpposingCrossovers(GraphNode from, GraphNode to)
		{
			if (IsCrossoverInBackWithOrWithoutStretch(from.State, L) && IsCrossoverInFrontWithOrWithoutStretch(to.State, L))
				return true;
			if (IsCrossoverInFrontWithOrWithoutStretch(from.State, L) && IsCrossoverInBackWithOrWithoutStretch(to.State, L))
				return true;
			return false;
		}

		public bool IsTransitionBetweenInvertAndStepOnOtherSide(GraphNode from, GraphNode to)
		{
			if (from.Orientation == BodyOrientation.InvertedRightOverLeft && to.Orientation == BodyOrientation.Normal)
			{
				var (_, ly) = GetFootPosition(to, L);
				var (_, ry) = GetFootPosition(to, R);
				if (ly < ry)
					return true;
			}
			if (to.Orientation == BodyOrientation.InvertedRightOverLeft && from.Orientation == BodyOrientation.Normal)
			{
				var (_, ly) = GetFootPosition(from, L);
				var (_, ry) = GetFootPosition(from, R);
				if (ly < ry)
					return true;
			}
			if (from.Orientation == BodyOrientation.InvertedLeftOverRight && to.Orientation == BodyOrientation.Normal)
			{
				var (_, ly) = GetFootPosition(to, L);
				var (_, ry) = GetFootPosition(to, R);
				if (ly > ry)
					return true;
			}
			if (to.Orientation == BodyOrientation.InvertedLeftOverRight && from.Orientation == BodyOrientation.Normal)
			{
				var (_, ly) = GetFootPosition(from, L);
				var (_, ry) = GetFootPosition(from, R);
				if (ly > ry)
					return true;
			}
			return false;
		}

		public bool Matches(GraphNode node, int leftArrow, int rightArrow)
		{
			return StateMatches(node.State, leftArrow, rightArrow);
		}

		public bool Matches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			return StateMatches(node.State, leftArrow, leftState, rightArrow, rightState);
		}

		public bool Matches(GraphNode node,
			int leftHeelArrow, GraphArrowState leftHeelState,
			int leftToeArrow, GraphArrowState leftToeState,
			int rightHeelArrow, GraphArrowState rightHeelState,
			int rightToeArrow, GraphArrowState rightToeState)
		{
			return StateMatches(node.State,
				leftHeelArrow, leftHeelState, leftToeArrow, leftToeState,
				rightHeelArrow, rightHeelState, rightToeArrow, rightToeState);
		}

		/// <summary>
		/// Gets the position of the given foot on the pads in the given GraphNode.
		/// For brackets, this position is the average of the bracketed arrows.
		/// </summary>
		/// <param name="node">GraphNode to get the foot position of.</param>
		/// <param name="foot">Foot to get the position of.</param>
		/// <returns>Position represented as a tuple of doubles representing the X and Y positions.</returns>
		public (double, double) GetFootPosition(GraphNode node, int foot)
		{
			var x = 0.0;
			var y = 0.0;
			var num = 0;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (node.State[foot, p].Arrow != InvalidArrowIndex)
				{
					num++;
					x += PadData.ArrowData[node.State[foot, p].Arrow].X;
					y += PadData.ArrowData[node.State[foot, p].Arrow].Y;
				}
			}
			if (num > 1)
			{
				x /= num;
				y /= num;
			}
			return (x, y);
		}

		/// <summary>
		/// Gets the average position of the given arrows. Assumes the given array represents
		/// arrows corresponding to the FootPortions for one foot on a GraphNode.
		/// </summary>
		/// <param name="footArrows">Array of arrows being stepped on.</param>
		/// <returns>Position represented as a tuple of doubles representing the X and Y positions.</returns>
		public (double, double) GetFootPosition(int[] footArrows)
		{
			var x = 0.0;
			var y = 0.0;
			var num = 0;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (footArrows[p] != InvalidArrowIndex)
				{
					num++;
					x += PadData.ArrowData[footArrows[p]].X;
					y += PadData.ArrowData[footArrows[p]].Y;
				}
			}
			if (num > 1)
			{
				x /= num;
				y /= num;
			}
			return (x, y);
		}

		#endregion Public State Helpers

		#region Fill Helpers

		private bool IsInvalidNormalPairing(GraphNode.FootArrowState[,] state, int lfp, int rfp)
		{
			if (state[L, lfp].Arrow != InvalidArrowIndex
				&& state[L, lfp].State != GraphArrowState.Lifted
				&& state[R, rfp].Arrow != InvalidArrowIndex
				&& state[R, lfp].State != GraphArrowState.Lifted)
			{
				// It is valid if the feet are on the same arrow (due to e.g. a swap).
				if (state[L, lfp].Arrow == state[R, rfp].Arrow)
					return false;

				// It is valid if the combination is a valid other foot pairing with or without stretch.
				if (PadData.ArrowData[state[L, lfp].Arrow].OtherFootPairings[L][state[R, rfp].Arrow]
					|| PadData.ArrowData[state[L, lfp].Arrow].OtherFootPairingsStretch[L][state[R, rfp].Arrow])
				{
					return false;
				}

				// If the arrows are not invalid and the above checks failed, it is not a valid pairing.
				return true;
			}

			// If the arrows can't be compared, it is not invalid.
			return false;
		}

		private bool IsSameArrowStep(GraphNode.FootArrowState[,] fromState, int foot, int arrow)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (fromState[foot, p].Arrow == arrow)
					return true;
			}
			return false;
		}

		private bool IsOtherFootOnArrow(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (state[otherFoot, p].Arrow == arrow)
					return true;
			}
			return false;
		}

		private bool IsOtherFootOnArrowWithoutLift(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (state[otherFoot, p].Arrow == arrow && state[otherFoot, p].State != GraphArrowState.Lifted)
					return true;
			}
			return false;
		}

		private bool IsNormalStep(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			// An individual step should not be considered normal if the entire state represents stretch.
			if (IsStretch(state))
				return false;

			if (IsOtherFootOnArrow(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairings[otherFoot][arrow])
					return true;
			}
			return false;
		}

		/// <summary>
		/// Returns whether a state as whole represents a stretch position.
		/// For a state to be considered stretch at least 2 of all foot portion combinations must be stretch,
		/// or if there is only one combination (no brackets with either foot), then it must be stretch.
		/// </summary>
		private bool IsStretch(GraphNode.FootArrowState[,] state)
		{
			var numStretchPairs = 0;
			var numTotalPairs = 0;
			for (var leftFootPortion = 0; leftFootPortion < NumFootPortions; leftFootPortion++)
			{
				if (state[L, leftFootPortion].Arrow != InvalidArrowIndex)
				{
					for (var rightFootPortion = 0; rightFootPortion < NumFootPortions; rightFootPortion++)
					{
						if (state[R, rightFootPortion].Arrow != InvalidArrowIndex)
						{
							if (PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsStretch[L][state[R, rightFootPortion].Arrow])
								numStretchPairs++;
							numTotalPairs++;
						}
					}
				}
			}
			return numStretchPairs == numTotalPairs || numStretchPairs > 1;
		}

		private bool IsCrossoverInFrontWithStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex && IsCrossoverInFrontWithStretch(state, foot, footArrowIndex))
					return true;
			}

			return false;
		}

		private bool IsCrossoverInFrontWithoutStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex && IsCrossoverInFrontWithoutStretch(state, foot, footArrowIndex))
					return true;
			}

			return false;
		}

		private bool IsCrossoverInFrontWithOrWithoutStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			return IsCrossoverInFrontWithStretch(state, foot) || IsCrossoverInFrontWithoutStretch(state, foot);
		}

		private bool IsCrossoverInFrontWithStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsCrossoverFrontStretch[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool IsCrossoverInFrontWithoutStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsCrossoverFront[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool IsCrossoverInBackWithStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex && IsCrossoverInBackWithStretch(state, foot, footArrowIndex))
					return true;
			}

			return false;
		}

		private bool IsCrossoverInBackWithoutStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex && IsCrossoverInBackWithoutStretch(state, foot, footArrowIndex))
					return true;
			}

			return false;
		}

		private bool IsCrossoverInBackWithOrWithoutStretch(GraphNode.FootArrowState[,] state, int foot)
		{
			return IsCrossoverInBackWithStretch(state, foot) || IsCrossoverInBackWithoutStretch(state, foot);
		}

		private bool IsCrossoverInBackWithStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsCrossoverBehindStretch[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool IsCrossoverInBackWithoutStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsCrossoverBehind[otherFoot][arrow])
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the given state represents a crossover position without stretch.
		/// If any portion of any foot is crossed over with any portion of any other foot it is considered a crossover.
		/// </summary>
		private bool IsCrossoverWithoutStretch(GraphNode.FootArrowState[,] state)
		{
			for (var leftFootPortion = 0; leftFootPortion < NumFootPortions; leftFootPortion++)
			{
				if (state[L, leftFootPortion].Arrow != InvalidArrowIndex)
				{
					for (var rightFootPortion = 0; rightFootPortion < NumFootPortions; rightFootPortion++)
					{
						if (state[R, rightFootPortion].Arrow != InvalidArrowIndex)
						{
							if (PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsCrossoverFront[L][state[R, rightFootPortion].Arrow]
								|| PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsCrossoverBehind[L][state[R, rightFootPortion].Arrow])
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given state represents a crossover position with stretch.
		/// If any portion of any foot is crossed over with any portion of any other foot it is considered a crossover.
		/// </summary>
		private bool IsCrossoverWithStretch(GraphNode.FootArrowState[,] state)
		{
			// For brackets, both feet must be stretching for it to be considered a stretch crossover.
			if (IsCrossoverWithoutStretch(state))
				return false;

			for (var leftFootPortion = 0; leftFootPortion < NumFootPortions; leftFootPortion++)
			{
				if (state[L, leftFootPortion].Arrow != InvalidArrowIndex)
				{
					for (var rightFootPortion = 0; rightFootPortion < NumFootPortions; rightFootPortion++)
					{
						if (state[R, rightFootPortion].Arrow != InvalidArrowIndex)
						{
							if (PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsCrossoverFrontStretch[L][state[R, rightFootPortion].Arrow]
								|| PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsCrossoverBehindStretch[L][state[R, rightFootPortion].Arrow])
								return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given state represents a crossover position with or without stretch.
		/// If any portion of any foot is crossed over with any portion of any other foot it is considered a crossover.
		/// </summary>
		private bool IsCrossoverWithOrWithoutStretch(GraphNode.FootArrowState[,] state)
		{
			return IsCrossoverWithStretch(state) || IsCrossoverWithoutStretch(state);
		}

		/// <summary>
		/// Returns true if the given foot at the given arrow represents an inverted step for the given state with stretch.
		/// If the given foot inverts with any portion of the other foot in this state, it is considered inverted.
		/// Lifted states do not count.
		/// </summary>
		private bool IsInvertedWithStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsInvertedStretch[otherFoot][arrow])
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the given state represents an inverted position with stretch.
		/// If any portion of any foot is inverted with any portion of any other foot it is considered inverted.
		/// Lifted states do not count.
		/// </summary>
		private bool IsInvertedWithStretch(GraphNode.FootArrowState[,] state)
		{
			if (IsInvertedWithoutStretch(state))
				return false;

			// For brackets, both feet must be inverted with stretch for it to be considered a stretch invert.
			for (var leftFootPortion = 0; leftFootPortion < NumFootPortions; leftFootPortion++)
			{
				if (state[L, leftFootPortion].Arrow != InvalidArrowIndex && state[L, leftFootPortion].State != GraphArrowState.Lifted)
				{
					for (var rightFootPortion = 0; rightFootPortion < NumFootPortions; rightFootPortion++)
					{
						if (state[R, rightFootPortion].Arrow != InvalidArrowIndex && state[R, rightFootPortion].State != GraphArrowState.Lifted)
						{
							if (PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsInvertedStretch[L][state[R, rightFootPortion].Arrow])
								return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given foot at the given arrow represents an inverted step for the given state without stretch.
		/// If the given foot inverts with any portion of the other foot in this state, it is considered inverted.
		/// Lifted states do not count.
		/// </summary>
		private bool IsInvertedWithoutStretch(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			if (IsOtherFootOnArrowWithoutLift(state, foot, arrow))
				return false;

			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
					&& state[otherFoot, p].State != GraphArrowState.Lifted
					&& PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsInverted[otherFoot][arrow])
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the given state represents an inverted position without stretch.
		/// If any portion of any foot is inverted with any portion of any other foot it is considered inverted.
		/// Lifted states do not count.
		/// </summary>
		private bool IsInvertedWithoutStretch(GraphNode.FootArrowState[,] state)
		{
			for (var leftFootPortion = 0; leftFootPortion < NumFootPortions; leftFootPortion++)
			{
				if (state[L, leftFootPortion].Arrow != InvalidArrowIndex && state[L, leftFootPortion].State != GraphArrowState.Lifted)
				{
					for (var rightFootPortion = 0; rightFootPortion < NumFootPortions; rightFootPortion++)
					{
						if (state[R, rightFootPortion].Arrow != InvalidArrowIndex && state[R, rightFootPortion].State != GraphArrowState.Lifted)
						{
							if (PadData.ArrowData[state[L, leftFootPortion].Arrow].OtherFootPairingsInverted[L][state[R, rightFootPortion].Arrow])
								return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given state represents an inverted position with or without stretch.
		/// If any portion of any foot is inverted with any portion of any other foot it is considered inverted.
		/// Lifted states do not count.
		/// </summary>
		private bool IsInvertedWithOrWithoutStretch(GraphNode.FootArrowState[,] state)
		{
			return IsInvertedWithStretch(state) || IsInvertedWithoutStretch(state);
		}

		/// <summary>
		/// Returns true if the given foot at the given arrow is a foot swap for the given state.
		/// A foot swap requires the current state to have its other foot lifted on the given arrow
		/// due to being swapped onto, and it requires the other foot to have been down on the
		/// given arrow in the previous state.
		/// </summary>
		private bool IsFootSwap(
			GraphNode.FootArrowState[,] fromstate,
			GraphNode.FootArrowState[,] toState,
			int foot, int arrow, bool footIsSingleArrowStep)
		{
			if (arrow == InvalidArrowIndex)
				return false;
			var otherFoot = OtherFoot(foot);

			// The given arrow needs to be stepped on by this foot.
			var steppedOnArrow = false;
			for (var p = 0; p < NumFootPortions; p++)
				if (toState[foot, p].Arrow == arrow)
					steppedOnArrow = true;
			if (!steppedOnArrow)
				return false;

			// The other foot needs to have not moved and to have become lifted.
			var steppedOnLiftedArrow = false;
			for (var p = 0; p < NumFootPortions; p++)
			{
				// The other foot needs to become lifted on this arrow during this transition.
				if (toState[otherFoot, p].Arrow == arrow)
				{
					if (fromstate[otherFoot, p].Arrow != arrow)
						return false;
					if (toState[otherFoot, p].State == GraphArrowState.Lifted && fromstate[otherFoot, p].State != GraphArrowState.Lifted)
						steppedOnLiftedArrow = true;
				}

				// The other foot's other portion needs to have not moved.
				else
				{
					// The arrow needs to be the same.
					if (toState[otherFoot, p].Arrow != fromstate[otherFoot, p].Arrow)
						return false;

					// If the other foot other portion is actually on another arrow that hasn't changed...
					if (toState[otherFoot, p].Arrow != InvalidArrowIndex)
					{
						// With a single step, the other foot's other portion needs to be identical before and after.
						if (footIsSingleArrowStep)
						{
							if(toState[otherFoot, p].State != fromstate[otherFoot, p].State)
							{
								return false;
							}
						}
						// With a bracket step, the other foot's other arrow could also lift.
						else
						{
							if (!(toState[otherFoot, p].State == fromstate[otherFoot, p].State
								|| (toState[otherFoot, p].State == GraphArrowState.Lifted && fromstate[otherFoot, p].State != GraphArrowState.Lifted)))
							{
								return false;
							}
						}
					}
				}
			}
			if (!steppedOnLiftedArrow)
				return false;

			return true;
		}

		private static bool StateMatches(GraphNode.FootArrowState[,] state,
			int leftHeelArrow, GraphArrowState leftHeelState,
			int leftToeArrow, GraphArrowState leftToeState,
			int rightHeelArrow, GraphArrowState rightHeelState,
			int rightToeArrow, GraphArrowState rightToeState)
		{
			return
				state[L, Heel].Arrow == leftHeelArrow && state[L, Heel].State == leftHeelState
				&& state[L, Toe].Arrow == leftToeArrow && state[L, Toe].State == leftToeState
				&& state[R, Heel].Arrow == rightHeelArrow && state[R, Heel].State == rightHeelState
				&& state[R, Toe].Arrow == rightToeArrow && state[R, Toe].State == rightToeState;
		}

		private static bool StateMatches(GraphNode.FootArrowState[,] state,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			return
				state[L, DefaultFootPortion].Arrow == leftArrow && state[L, DefaultFootPortion].State == leftState
				&& state[R, DefaultFootPortion].Arrow == rightArrow && state[R, DefaultFootPortion].State == rightState;
		}

		private static bool StateMatches(GraphNode.FootArrowState[,] state, int leftArrow, int rightArrow)
		{
			return
				state[L, DefaultFootPortion].Arrow == leftArrow
				&& state[R, DefaultFootPortion].Arrow == rightArrow;
		}

		private bool StateMatches(
			GraphNode.FootArrowState[,] fromState, int lfa, GraphArrowState lfs, int rfa, GraphArrowState rfs,
			GraphNode.FootArrowState[,] toState, int lta, GraphArrowState lts, int rta, GraphArrowState rts)
		{
			var ds = GraphArrowState.Resting;
			var da = InvalidArrowIndex;

			return StateMatches(
				fromState,
				lfa, lfs,
				da, ds,
				rfa, rfs,
				da, ds,
				toState,
				lta, lts,
				da, ds,
				rta, rts,
				da, ds);
		}

		private bool StateMatches(
			GraphNode.FootArrowState[,] fromState,
			int lhfa, GraphArrowState lhfs,
			int ltfa, GraphArrowState ltfs,
			int rhfa, GraphArrowState rhfs,
			int rtfa, GraphArrowState rtfs,
			GraphNode.FootArrowState[,] toState,
			int lhta, GraphArrowState lhts,
			int ltta, GraphArrowState ltts,
			int rhta, GraphArrowState rhts,
			int rtta, GraphArrowState rtts)
		{
			return (fromState[L, Heel].Arrow == lhfa && fromState[L, Heel].State == lhfs
				&& fromState[L, Toe].Arrow == ltfa && fromState[L, Toe].State == ltfs
				&& toState[L, Heel].Arrow == lhta && toState[L, Heel].State == lhts
				&& toState[L, Toe].Arrow == ltta && toState[L, Toe].State == ltts
				&& fromState[R, Heel].Arrow == rhfa && fromState[R, Heel].State == rhfs
				&& fromState[R, Toe].Arrow == rtfa && fromState[R, Toe].State == rtfs
				&& toState[R, Heel].Arrow == rhta && toState[R, Heel].State == rhts
				&& toState[R, Toe].Arrow == rtta && toState[R, Toe].State == rtts);
		}

		#endregion Fill Helpers

		#region Logging

		private void LogInfo(string message)
		{
			Logger.Info($"[StepGraph] [{LogIdentifier} ({NumArrows})] {message}");
		}

		private void LogError(string message)
		{
			LogError(LogIdentifier, NumArrows, message);
		}

		private static void LogError(string logIdentifier, int numArrows, string message)
		{
			Logger.Error($"[StepGraph] [{logIdentifier} ({numArrows})] {message}");
		}

		private void LogNode(string message, GraphNode node)
		{
			LogState(message, node.State);
		}

		private void LogState(string message, GraphNode.FootArrowState[,] state)
		{
			LogStateHelper(message, state, NumFeet - 1, NumFootPortions - 1);
		}

		private void LogState(string message, int f, int p, GraphNode.FootArrowState[,] state, int arrow, GraphArrowState gas)
		{
			state[f, p] = new GraphNode.FootArrowState(arrow, gas);
			LogStateHelper(message, state, f, p);
		}

		private void LogStateHelper(string message, GraphNode.FootArrowState[,] state, int f, int p)
		{
#if DEBUG_STEPGRAPH
			var sb = new StringBuilder();
			sb.Append("[");
			for (int fi = 0; fi <= f; fi++)
			{
				if (fi == 0)
				{
					sb.Append("L:");
				}
				else
				{
					sb.Append(" R:");
				}

				var maxPi = fi == f ? p : NumFootPortions - 1;
				for (int pi = 0; pi <= maxPi; pi++)
				{
					if (pi != 0)
					{
						sb.Append(",");
					}

					if (state[fi, pi].Arrow == InvalidArrowIndex)
						sb.Append("XX");
					else
						sb.Append(state[fi, pi].Arrow.ToString("D2"));
					sb.Append("-");
					if (state[fi, pi].State == GraphArrowState.Resting)
						sb.Append("N");
					else if (state[fi, pi].State == GraphArrowState.Lifted)
						sb.Append("L");
					else
						sb.Append("H");
				}
			}
			sb.Append("]");

			if (!string.IsNullOrEmpty(message))
				sb.Append($" {message}");

			LogInfo(sb.ToString());
#endif
		}

		#endregion Logging
	}
}
