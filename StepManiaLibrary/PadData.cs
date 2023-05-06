using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fumen;
using static StepManiaLibrary.Constants;

namespace StepManiaLibrary
{
	/// <summary>
	/// Information about how a pad or set of pads is laid out.
	/// Deserialized from json.
	/// </summary>
	public class PadData
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "PadData";

		/// <summary>
		/// Stepmania StepsType that this PadData is used for.
		/// </summary>
		[JsonIgnore] public string StepsType;

		/// <summary>
		/// Number of arrows / lanes for this PadData.
		/// Set after deserialization.
		/// </summary>
		[JsonIgnore] public int NumArrows;

		/// <summary>
		/// Valid starting positions.
		/// First array is tier, with lower indices preferred to higher indices when choosing a start position.
		/// Second array is all equally preferred positions of the same tier.
		/// Third array is lane indices, one for each foot.
		/// </summary>
		[JsonInclude] public int[][][] StartingPositions;

		/// <summary>
		/// Data for each arrow on the pad.
		/// </summary>
		[JsonInclude] public ArrowData[] ArrowData;

		/// <summary>
		/// When the foot travels in Y it needs to travel less than when it travels in X
		/// since the foot is longer than it is wide. Movements in Y are substantially shorter
		/// as a result.
		/// values.
		/// </summary>
		[JsonInclude] public double YTravelDistanceCompensation = 0.5;

		/// <summary>
		/// Loads the PadData from the given file.
		/// </summary>
		/// <param name="stepsType">Stepmania StepsType for the PadData.</param>
		/// <param name="fileName">Filename to load.</param>
		/// <returns>PadData Instance or null if it failed to load or did contained invalid data.</returns>
		public static async Task<PadData> LoadPadData(string stepsType, string fileName)
		{
			var options = new JsonSerializerOptions
			{
				Converters =
				{
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
				},
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				IncludeFields = true,
			};

			PadData padData;
			try
			{
				var fileFileName = Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
				if (!File.Exists(fileFileName))
				{
					LogErrorStatic(
						$"[{stepsType}] Could not find PadData file {fileName}. Please create a PadData file for {stepsType}.");
					return null;
				}

				using (FileStream openStream = File.OpenRead(fileFileName))
				{
					padData = await JsonSerializer.DeserializeAsync<PadData>(openStream, options);
					if (padData == null)
						throw new Exception("Null PadData.");
					padData.StepsType = stepsType;
					if (!padData.Validate())
						return null;
					padData.SetFlippedAndMirroredPositions();
				}
			}
			catch (Exception e)
			{
				LogErrorStatic($"[{stepsType}] Failed to load {fileName}. {e}");
				return null;
			}

			return padData;
		}

		/// <summary>
		/// Validates the PadData, logging errors on unexpected data.
		/// Side-effects of setting NumArrows on PadData and Lane on each ArrowData.
		/// </summary>
		/// <returns>True of no errors were found and false otherwise.</returns>
		private bool Validate()
		{
			var errors = false;
			if (ArrowData == null)
			{
				LogError("Null ArrowData. Expected an array.");
				return true;
			}

			NumArrows = ArrowData.Length;
			if (NumArrows < 1)
			{
				LogError("Empty ArrowData. Expected an array.");
				errors = true;
			}

			var lane = 0;
			foreach (var arrowData in ArrowData)
			{
				arrowData.Lane = lane;
				lane++;
				errors |= !ValidateArrowDataArrays(arrowData.BracketablePairingsHeel, lane, "BracketablePairingsHeel");
				errors |= !ValidateArrowDataArrays(arrowData.BracketablePairingsToe, lane, "BracketablePairingsToe");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairings, lane, "OtherFootPairings");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsStretch, lane, "OtherFootPairingsStretch");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsCrossoverFront, lane, "OtherFootPairingsCrossoverFront");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsCrossoverFrontStretch, lane, "OtherFootPairingsCrossoverFrontStretch");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsCrossoverBehind, lane, "OtherFootPairingsCrossoverBehind");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsCrossoverBehindStretch, lane, "OtherFootPairingsCrossoverBehindStretch");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsInverted, lane, "OtherFootPairingsInverted");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsInvertedStretch, lane, "OtherFootPairingsInvertedStretch");
			}

			if (StartingPositions == null || StartingPositions.Length < 1)
			{
				LogError("Empty StartingPositions. At least one valid starting position is required.");
				errors = true;
			}
			else
			{
				var tierIndex = 0;
				foreach (var tier in StartingPositions)
				{
					if (tier == null || tier.Length < 1)
					{
						LogError($"Empty array at StartingPositions[{tierIndex}].");
						errors = true;
					}
					else
					{
						if (tierIndex == 0 && tier.Length != 1)
						{
							LogError($"The first tier of StartingPositions has {tier.Length} entries. It should only have 1.");
							errors = true;
						}

						var positionIndex = 0;
						foreach (var position in tier)
						{
							if (position == null || position.Length != NumFeet)
							{
								LogError(
									$"StartingPositions[{tierIndex}][{positionIndex}] {position?.Length ?? 0} entries found. Expected {NumFeet}, one for each foot.");
								errors = true;
							}
							else
							{
								var laneIndex = 0;
								foreach (var spLane in position)
								{
									if (spLane < 0 || spLane >= NumArrows)
									{
										LogError(
											$"StartingPositions[{tierIndex}][{positionIndex}][{laneIndex}] lane {spLane} out of bounds Must be within [0, {NumArrows - 1}].");
										errors = true;
									}

									laneIndex++;
								}
							}

							positionIndex++;
						}
					}

					tierIndex++;
				}
			}

			return !errors;
		}

		/// <summary>
		/// Helper method for Validate to validate one of the many bool[][] arrays on ArrowData.
		/// </summary>
		/// <param name="arrowDataArray">Array from ArrowData. First index is foot, second is lane.</param>
		/// <param name="lane">Lane of the ArrowData. Used for logging.</param>
		/// <param name="name">Name of the bool[][] property. Used for logging.</param>
		/// <returns>True of no errors were found and false otherwise.</returns>
		private bool ValidateArrowDataArrays(bool[][] arrowDataArray, int lane, string name)
		{
			var errors = false;
			if (arrowDataArray == null || arrowDataArray.Length != NumFeet)
			{
				LogError(
					$"Lane {lane}: {arrowDataArray?.Length ?? 0} {name} entries found. Expected {NumFeet} arrays, one for each foot.");
				errors = true;
			}
			else
			{
				for (var f = 0; f < NumFeet; f++)
				{
					if (arrowDataArray[f] == null || arrowDataArray[f].Length != NumArrows)
					{
						LogError(
							$"Lane {lane}: {name}[{f}] {arrowDataArray[f]?.Length ?? 0} entries found. Expected {NumArrows}.");
						errors = true;
					}
				}
			}

			return !errors;
		}

		/// <summary>
		/// Sets MirroredLane and FlippedLane on the ArrowData based on the individual ArrowData
		/// X and Y values.
		/// </summary>
		private void SetFlippedAndMirroredPositions()
		{
			var minX = int.MaxValue;
			var maxX = int.MinValue;
			var minY = int.MaxValue;
			var maxY = int.MinValue;
			for (var a = 0; a < NumArrows; a++)
			{
				minX = Math.Min(minX, ArrowData[a].X);
				maxX = Math.Max(maxX, ArrowData[a].X);
				minY = Math.Min(minY, ArrowData[a].Y);
				maxY = Math.Max(maxY, ArrowData[a].Y);
			}
			var numColumns = maxX - minX;
			var numRows = maxY - minY;

			Func<int, int> mirror = (int x) =>
			{
				return minX + (numColumns - x);
			};
			Func<int, int> flip = (int y) =>
			{
				return minY + (numRows - y);
			};

			for (var a = 0; a < NumArrows; a++)
			{
				for (var a2 = 0; a2 < NumArrows; a2++)
				{
					if (mirror(ArrowData[a].X) == ArrowData[a2].X && ArrowData[a].Y == ArrowData[a2].Y)
					{
						ArrowData[a].MirroredLane = a2;
					}
					if (ArrowData[a].X == ArrowData[a2].X && flip(ArrowData[a].Y) == ArrowData[a2].Y)
					{
						ArrowData[a].FlippedLane = a2;
					}
				}
			}
		}

		/// <summary>
		/// Gets the X distance between two points on the pads.
		/// </summary>
		/// <param name="x1">Point 1 X.</param>
		/// <param name="x2">Point 2 X.</param>
		/// <returns>X distance between the two points.</returns>
		public double GetXDistance(double x1, double x2)
		{
			return Math.Abs(x1 - x2);
		}

		/// <summary>
		/// Gets the Y distance between two points on the pads, taking into account the configured
		/// YTravelDistanceCompensation.
		/// </summary>
		/// <param name="y1">Point 1 Y.</param>
		/// <param name="y2">Point 2 Y.</param>
		/// <returns>Y distance between the two points.</returns>
		public double GetYDistance(double y1, double y2)
		{
			return Math.Max(0.0, Math.Abs(y1 - y2) - YTravelDistanceCompensation);
		}

		/// <summary>
		/// Gets the distance between two points on the pads, taking into account the configured
		/// YTravelDistanceCompensation.
		/// </summary>
		/// <param name="x1">Point 1 X.</param>
		/// <param name="y1">Point 1 Y.</param>
		/// <param name="x2">Point 2 X.</param>
		/// <param name="y2">Point 2 Y.</param>
		/// <returns>Distance between the two points.</returns>
		public double GetDistance(double x1, double y1, double x2, double y2)
		{
			var dx = GetXDistance(x1, x2);
			var dy = GetYDistance(y1, y2);
			return Math.Sqrt(dx * dx + dy * dy);
		}

		private (int, int, int, int) GetBounds()
		{
			var minX = int.MaxValue;
			var maxX = int.MinValue;
			var minY = int.MaxValue;
			var maxY = int.MinValue;

			for (var i = 0; i < NumArrows; i++)
			{
				minX = Math.Min(minX, ArrowData[i].X);
				maxX = Math.Max(maxX, ArrowData[i].X);
				minY = Math.Min(minY, ArrowData[i].Y);
				maxY = Math.Max(maxY, ArrowData[i].Y);
			}

			return (minX, maxX, minY, maxY);
		}

		private int FindArrowAt(int x, int y)
		{
			for (int a = 0; a < NumArrows; a++)
				if (ArrowData[a].X == x && ArrowData[a].Y == y)
					return a;
			return InvalidArrowIndex;
		}

		/// <summary>
		/// Returns whether this PadData can fit within the given other PadData.
		/// For this to fit within the other data, all of it's arrows must be able to be represented in
		/// the other data, and all the moves between those arrows must be able to be represented in the
		/// other data.
		/// For example, dance-single fits within dance-double (it is a clear subset) and smx-beginner fits
		/// within dance-solo (it can be shifted upwards so the three in a row overlap the three top solo
		/// arrows).
		/// </summary>
		/// <param name="other">Other PadData to check.</param>
		/// <returns>True if this PadData fits within the other PadData and false otherwise.</returns>
		public bool CanFitWithin(PadData other)
		{
			// PadData always overlaps itself.
			if (this == other)
				return true;
			// Early out on arrow count.
			if (NumArrows > other.NumArrows)
				return false;

			// Get the bounds and ensure the bounds of this PadData fit within the other's bounds.
			var (minX, maxX, minY, maxY) = GetBounds();
			var dx = maxX + 1 - minX;
			var dy = maxY + 1 - minY;
			var (otherMinX, otherMaxX, otherMinY, otherMaxY) = other.GetBounds();
			var otherDx = otherMaxX + 1 - otherMinX;
			var otherDy = otherMaxY + 1 - otherMinY;
			var xExtraRoom = otherDx - dx;
			var yExtraRoom = otherDy - dy;
			if (xExtraRoom < 0 || yExtraRoom < 0)
				return false;

			// Try every valid coordinate overlap.
			for (int otherStartX = otherMinX; otherStartX <= otherMinX + xExtraRoom; otherStartX++)
			{
				for (int otherStartY = otherMinY; otherStartY <= otherMinY + yExtraRoom; otherStartY++)
				{
					// For this overlap coordinate, ensure this PadData's arrows can lay in the same
					// pattern on the other PadData's arrows.
					var validOverlap = true;
					var arrowMapping = new Dictionary<int, int>();
					for (int thisX = minX, otherX = otherStartX; thisX <= maxX; thisX++, otherX++)
					{
						for (int thisY = minY, otherY = otherStartY; thisY <= maxY; thisY++, otherY++)
						{
							var thisA = FindArrowAt(thisX, thisY);
							if (thisA == InvalidArrowIndex)
								continue;
							var otherA = other.FindArrowAt(otherX, otherY);
							if (otherA == InvalidArrowIndex)
							{
								validOverlap = false;
								break;
							}
							arrowMapping[thisA] = otherA;
						}
						if (!validOverlap)
							break;
					}
					if (!validOverlap)
						continue;

					// At this point the arrows overlap.
					// Ensure that all the moves between the arrows are also preserved.
					// This is expected to be true unless the PadData used the same coordinates but had
					// for example unbracketable arrows or arrows of a different size such that stretch boundaries were different.
					var pairingsMatch = true;
					for (var a1 = 0; a1 < NumArrows; a1++)
					{
						for (var a2 = 0; a2 < NumArrows; a2++)
						{
							for (var f = 0; f < NumFeet; f++)
							{
								if (ArrowData[a1].BracketablePairingsHeel[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].BracketablePairingsHeel[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].BracketablePairingsToe[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].BracketablePairingsToe[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairings[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairings[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsStretch[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsStretch[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsCrossoverFront[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsCrossoverFront[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsCrossoverFrontStretch[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsCrossoverFrontStretch[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsCrossoverBehind[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsCrossoverBehind[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsCrossoverBehindStretch[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsCrossoverBehindStretch[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsInverted[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsInverted[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
								if (ArrowData[a1].OtherFootPairingsInvertedStretch[f][a2]
									&& !other.ArrowData[arrowMapping[a1]].OtherFootPairingsInvertedStretch[f][arrowMapping[a2]])
								{
									pairingsMatch = false;
									break;
								}
							}
							if (!pairingsMatch)
								break;
						}
						if (!pairingsMatch)
							break;
					}

					// If all the pairings match then this PadData fits within the other PadData.
					if (pairingsMatch)
						return true;
				}
			}

			// No valid fit was found.
			return false;
		}

		#region Logging

		private void LogError(string message)
		{
			LogErrorStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private void LogWarn(string message)
		{
			LogWarnStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private void LogInfo(string message)
		{
			LogInfoStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private static void LogErrorStatic(string message)
		{
			Logger.Error($"[{LogTag}] {message}");
		}

		private static void LogWarnStatic(string message)
		{
			Logger.Warn($"[{LogTag}] {message}");
		}

		private static void LogInfoStatic(string message)
		{
			Logger.Info($"[{LogTag}] {message}");
		}

		#endregion Logging
	}
}