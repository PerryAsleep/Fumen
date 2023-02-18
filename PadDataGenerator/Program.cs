using System;
using System.Collections.Generic;
using System.IO;
using StepManiaLibrary;
using System.Text.Json;
using System.Text.Json.Serialization;
using static StepManiaLibrary.Constants;
using System.Linq;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace PadDataGenerator
{
	/// <summary>
	/// Program for generating PadData files from simplified input.
	/// </summary>
	internal class Program
	{
		private const string InputFileName = "input.json";

		/// <summary>
		/// Input to this application per ChartType.
		/// </summary>
		private class PadDataInput
		{
			/// <summary>
			/// Arrow position.
			/// </summary>
			public class Position
			{
				public int X;
				public int Y;
			}

			// TODO: Stretch
			public int MaxXSeparation = 2;
			public int MaxYSeparation = 2;
			// TODO: Wide Crossovers
			public int MaxXSeparationCrossover = 1;
			public int MaxXSeparationBracket = 1;
			public int MaxYSeparationBracket = 1;
			public double YTravelDistanceCompensation = 0.5;

			/// <summary>
			/// Positions of all arrows.
			/// </summary>
			public List<Position> Positions;
		}

		private static JsonSerializerOptions SerializationOptions = new JsonSerializerOptions()
		{
			Converters =
			{
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
			},
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
			IncludeFields = true,
			WriteIndented = true,
		};

		private static async Task Main()
		{
			// Load inputs.
			var inputs = new Dictionary<string, PadDataInput>();
			try
			{
				using (FileStream openStream = File.OpenRead(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, InputFileName)))
				{
					inputs = JsonSerializer.Deserialize<Dictionary<string, PadDataInput>>(openStream, SerializationOptions);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to load {InputFileName}: {e}");
				return;
			}

			// Load expected pad data for verification.
			var expectedPadData = await LoadExpectedPadData();

			// Create pad data for each input and write it to disk.
			foreach (var kvp in inputs)
			{
				// Create the pad data.
				var padData = CreatePadData(kvp.Value);

				// Compare it to expected data, if expected data is present.
				if (expectedPadData.ContainsKey(kvp.Key))
				{
					VerifyPadData(expectedPadData[kvp.Key], padData);
				}

				// Write to disk.
				var outputFileName = $"{kvp.Key}.json";
				try
				{
					var jsonString = JsonSerializer.Serialize(padData, SerializationOptions);
					File.WriteAllText(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outputFileName), jsonString);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to write {outputFileName}: {e}");
					continue;
				}
			}
		}

		private static async Task<Dictionary<string, PadData>> LoadExpectedPadData()
		{
			var expectedPadData = new Dictionary<string, PadData>();

			var expectedTypes = new string[] { "dance-single", "dance-double" };
			foreach (var expectedType in expectedTypes)
			{
				var padData = await PadData.LoadPadData(expectedType, $"expected-{expectedType}.json");
				if (padData == null)
					Console.WriteLine($"Failed to load expected-{expectedType}.json");
				else
					expectedPadData[expectedType] = padData;
			}
			return expectedPadData;
		}

		static PadData CreatePadData(PadDataInput input)
		{
			var numArrows = input.Positions.Count;

			var padData = new PadData();
			padData.YTravelDistanceCompensation = input.YTravelDistanceCompensation;
			padData.ArrowData = new ArrowData[numArrows];
			for (int a = 0; a < numArrows; a++)
			{
				var ad = new ArrowData();
				ad.ValidNextArrows = new bool[numArrows];
				for (var f = 0; f < NumFeet; f++)
				{
					ad.BracketablePairingsOtherHeel[f] = new bool[numArrows];
					ad.BracketablePairingsOtherToe[f] = new bool[numArrows];
					ad.OtherFootPairings[f] = new bool[numArrows];
					ad.OtherFootPairingsOtherFootCrossoverFront[f] = new bool[numArrows];
					ad.OtherFootPairingsOtherFootCrossoverBehind[f] = new bool[numArrows];
					ad.OtherFootPairingsInverted[f] = new bool[numArrows];
				}
				padData.ArrowData[a] = ad;
			}

			// Determine for each arrow if there is room to the left or right of it.
			// This is helpful for determine valid brackets below.
			var roomAtOrToLeft = new bool[numArrows];
			var roomAtOrToRight = new bool[numArrows];
			for (int a = 0; a < numArrows; a++)
			{
				var roomAtOrToLeftOfA = false;
				var roomAtOrToRightOfA = false;
				for (int a2 = 0; a2 < numArrows; a2++)
				{
					if (a == a2)
						continue;

					if (input.Positions[a2].X <= input.Positions[a].X)
						roomAtOrToLeftOfA = true;
					if (input.Positions[a2].X >= input.Positions[a].X)
						roomAtOrToRightOfA = true;
				}
				roomAtOrToLeft[a] = roomAtOrToLeftOfA;
				roomAtOrToRight[a] = roomAtOrToRightOfA;
			}

			// Set data for every arrow.
			for (int a = 0; a < numArrows; a++)
			{
				var ad = padData.ArrowData[a];

				// Position
				ad.X = input.Positions[a].X;
				ad.Y = input.Positions[a].Y;

				// Valid next arrows.
				for (int a2 = 0; a2 < numArrows; a2++)
				{
					var xd = Math.Abs(ad.X - input.Positions[a2].X);
					var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

					// For this arrow to be a valid next arrow to step on...
					padData.ArrowData[a].ValidNextArrows[a2] =
						// Arrows must be different.
						(a != a2)
						// Arrows cannot be too far apart.
						&& xd <= input.MaxXSeparation && yd <= input.MaxYSeparation;
				}

				// Bracketable pairings with the toe on this arrow and the heel on another.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.BracketablePairingsOtherHeel[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For this arrow to bracketable with the toe of the left foot...
							ad.BracketablePairingsOtherHeel[f][a2] =
								// Arrows must be different.
								(a != a2)
								// There must be room to stand at the same X or to the right of these arrows so you aren't crossed over.
								&& (roomAtOrToRight[a]) && (roomAtOrToRight[a2])
								// The arrows must be withing bracketable distance from each other.
								&& xd <= input.MaxXSeparationBracket && yd <= input.MaxYSeparationBracket
								// The other arrow must not be in front, otherwise you would be facing backwards.
								&& ad.Y <= input.Positions[a2].Y;
						}
						else
						{
							// For this arrow to bracketable with the toe of the right foot...
							ad.BracketablePairingsOtherHeel[f][a2] =
								// Arrows must be different.
								(a != a2)
								// There must be room to stand at the same X or to the left of these arrows so you aren't crossed over.
								&& (roomAtOrToLeft[a]) && (roomAtOrToLeft[a2])
								// The arrows must be withing bracketable distance from each other.
								&& xd <= input.MaxXSeparationBracket && yd <= input.MaxYSeparationBracket
								// The other arrow must not be in front, otherwise you would be facing backwards.
								&& ad.Y <= input.Positions[a2].Y;
						}
					}
				}

				// Bracketable pairings with the heel on this arrow and the toe on another.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.BracketablePairingsOtherToe[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For this arrow to bracketable with the heel of the left foot...
							ad.BracketablePairingsOtherToe[f][a2] =
								// Arrows must be different.
								(a != a2)
								// There must be room to stand at the same X or to the right of these arrows so you aren't crossed over.
								&& (roomAtOrToRight[a]) && (roomAtOrToRight[a2])
								// The arrows must be withing bracketable distance from each other.
								&& xd <= input.MaxXSeparationBracket && yd <= input.MaxYSeparationBracket
								// The other arrow must not be in back, otherwise you would be facing backwards.
								&& ad.Y >= input.Positions[a2].Y;
						}
						else
						{
							// For this arrow to bracketable with the heel of the right foot...
							ad.BracketablePairingsOtherToe[f][a2] =
								// Arrows must be different.
								(a != a2)
								// There must be room to stand at the same X or to the left of these arrows so you aren't crossed over.
								&& (roomAtOrToLeft[a]) && (roomAtOrToLeft[a2])
								// The arrows must be withing bracketable distance from each other.
								&& xd <= input.MaxXSeparationBracket && yd <= input.MaxYSeparationBracket
								// The other arrow must not be in back, otherwise you would be facing backwards.
								&& ad.Y >= input.Positions[a2].Y;
						}
					}
				}

				// Other foot pairings.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.OtherFootPairings[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For this arrow to be a valid other foot pairing...
							ad.OtherFootPairings[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparation && yd <= input.MaxYSeparation
								// The arrow must not be to the left of your left foot, otherwise you would be crossed over.
								&& input.Positions[a2].X >= ad.X;
						}
						else
						{
							// For this arrow to be a valid other foot pairing...
							ad.OtherFootPairings[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparation && yd <= input.MaxYSeparation
								// The arrow must not be to the right of your right foot, otherwise you would be crossed over.
								&& input.Positions[a2].X <= ad.X;
						}
					}
				}

				// Other foot pairings where the other foot crosses over in front.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.OtherFootPairingsOtherFootCrossoverFront[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For the right foot to cross over in front of the left on this arrow...
							ad.OtherFootPairingsOtherFootCrossoverFront[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparationCrossover && yd <= input.MaxYSeparation
								// Right foot must be in front of left foot.
								&& input.Positions[a2].Y < ad.Y
								// The arrow must be to the left of your left foot.
								&& input.Positions[a2].X < ad.X;
						}
						else
						{
							// For the left foot to cross over in front of the right on this arrow...
							ad.OtherFootPairingsOtherFootCrossoverFront[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparationCrossover && yd <= input.MaxYSeparation
								// Left foot must be in front of right foot.
								&& input.Positions[a2].Y < ad.Y
								// The arrow must be to the right of your right foot.
								&& input.Positions[a2].X > ad.X;
						}
					}
				}

				// Other foot pairings where the other foot crosses over in back.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.OtherFootPairingsOtherFootCrossoverBehind[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For the right foot to cross over in back of the left on this arrow...
							ad.OtherFootPairingsOtherFootCrossoverBehind[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparationCrossover && yd <= input.MaxYSeparation
								// Right foot must be in back of left foot.
								&& input.Positions[a2].Y > ad.Y
								// The arrow must be to the left of your left foot.
								&& input.Positions[a2].X < ad.X;
						}
						else
						{
							// For the left foot to cross over in back of the right on this arrow...
							ad.OtherFootPairingsOtherFootCrossoverBehind[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows cannot be too far apart.
								&& xd <= input.MaxXSeparationCrossover && yd <= input.MaxYSeparation
								// Left foot must be in back of right foot.
								&& input.Positions[a2].Y > ad.Y
								// The arrow must be to the right of your right foot.
								&& input.Positions[a2].X > ad.X;
						}
					}
				}

				// Other foot pairings where you are inverted and facing completely backwards.
				for (var f = 0; f < NumFeet; f++)
				{
					ad.OtherFootPairingsInverted[f] = new bool[numArrows];
					for (int a2 = 0; a2 < numArrows; a2++)
					{
						var xd = Math.Abs(ad.X - input.Positions[a2].X);
						var yd = Math.Abs(ad.Y - input.Positions[a2].Y);

						if (f == L)
						{
							// For the right foot to be inverted with the left foot...
							ad.OtherFootPairingsInverted[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows must be at the same Y position and not too far apart in X.
								&& xd <= input.MaxXSeparation && yd <= 0
								// The arrow must be to the left of your left foot.
								&& input.Positions[a2].X < ad.X;
						}
						else
						{
							// For the left foot to cross over in back of the right on this arrow...
							ad.OtherFootPairingsInverted[f][a2] =
								// Arrows must be different.
								(a != a2)
								// Arrows must be at the same Y position and not too far apart in X.
								&& xd <= input.MaxXSeparation && yd <= 0
								// The arrow must be to the right of your right foot.
								&& input.Positions[a2].X > ad.X;
						}
					}
				}
			}

			// Set valid starting positions
			SetStartingPositions(input, padData);

			return padData;
		}

		private sealed class StartingPosition
		{
			public int L;
			public int R;
			/// <summary>
			/// Rating to use for starting position tier.
			/// Many starting positions may share the same tier rating.
			/// </summary>
			public double TierRating;
			/// <summary>
			/// Unique overall rating for sorting within tiers.
			/// </summary>
			public double OverallRating;

			public override bool Equals(object obj)
			{
				if (!(obj is StartingPosition sp))
					return false;
				return sp.L == L && sp.R == R;
			}
			public override int GetHashCode()
			{
				return (L * 1000 + R).GetHashCode();
			}
		}

		private static void SetStartingPositions(PadDataInput input, PadData padData)
		{
			var numArrows = input.Positions.Count;

			var startingPositions = new HashSet<StartingPosition>();
			var minX = int.MaxValue;
			var maxX = int.MinValue;
			var minY = int.MaxValue;
			var maxY = int.MinValue;
			foreach (var pos in input.Positions)
			{
				if (pos.X < minX)
					minX = pos.X;
				if (pos.X > maxX)
					maxX = pos.X;
				if (pos.Y < minY)
					minY = pos.Y;
				if (pos.Y > maxY)
					maxY = pos.Y;
			}
			var widthOdd = ((maxX - minX) + 1) % 2 == 1;
			var heightOdd = ((maxY - minY) + 1) % 2 == 1;

			double centerX = minX + ((maxX - minX) * 0.5);
			double centerY = minY + ((maxY - minY) * 0.5);

			double ilx = centerX - 0.5;
			double ily = centerY;
			double irx = centerX + 0.5;
			double iry = centerY;

			// Loop over every valid position and compute a rating of well it is for a starting position.
			for (int a = 0; a < numArrows; a++)
			{
				var ad = padData.ArrowData[a];
				for (int a2 = 0; a2 < numArrows; a2++)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						if (ad.OtherFootPairings[f][a2])
						{
							var l = f == L ? a : a2;
							var r = f == L ? a2 : a;
							var (tierRating, overallRating) = GetStartPositionRating(centerX, maxY,
								padData.ArrowData[l].X, padData.ArrowData[l].Y, padData.ArrowData[r].X, padData.ArrowData[r].Y,
								ilx, ily, irx, iry);
							startingPositions.Add(new StartingPosition
							{
								L = l,
								R = r,
								TierRating = tierRating,
								OverallRating = overallRating,
							});
						}
					}
				}
			}

			// Convert the starting positions to a list sorted by their rating.
			var startingPositionsList = startingPositions.ToList();
			startingPositionsList.Sort((a, b) => CompareRatings(a.OverallRating, b.OverallRating));

			// Convert the sorted list into a list of lists, where the inner lists
			// share the same rating.
			var tieredPositions = new List<List<StartingPosition>>();
			double lastRating = double.MaxValue;
			foreach (var position in startingPositionsList)
			{
				if (position.TierRating != lastRating)
				{
					tieredPositions.Add(new List<StartingPosition>());
				}
				tieredPositions[tieredPositions.Count - 1].Add(position);
				lastRating = position.TierRating;
			}

			// For the first tier ensure there is only one entry by splitting it into two tiers.
			if (tieredPositions.Count > 0)
			{
				if (tieredPositions[0].Count > 1)
				{
					var tierOne = tieredPositions[0];
					var newTierTwo = new List<StartingPosition>(tierOne.Count - 1);
					for(int i = 1; i < tierOne.Count; i++)
						newTierTwo.Add(tierOne[i]);
					tierOne.RemoveRange(1, tierOne.Count - 1);
					tieredPositions.Insert(1, newTierTwo);
				}
			}

			// Copied the tiered lists to the PadData.
			padData.StartingPositions = new int[tieredPositions.Count][][];
			for (var tierIndex = 0; tierIndex < tieredPositions.Count; tierIndex++)
			{
				var positionsAtTier = tieredPositions[tierIndex];
				padData.StartingPositions[tierIndex] = new int[positionsAtTier.Count][];
				for(var positionIndex = 0; positionIndex < positionsAtTier.Count; positionIndex++)
				{
					padData.StartingPositions[tierIndex][positionIndex] = new int[NumFeet];
					padData.StartingPositions[tierIndex][positionIndex][L] = positionsAtTier[positionIndex].L;
					padData.StartingPositions[tierIndex][positionIndex][R] = positionsAtTier[positionIndex].R;
				}
			}
		}

		private static int CompareRatings(double r1, double r2)
		{
			if (Math.Abs(r1 - r2) < 0.0001)
				return 0;
			return r1.CompareTo(r2);
		}

		/// <summary>
		/// Computes ratings for a given starting position. Lower ratings are better.
		/// </summary>
		/// <returns>
		/// Tuple where first value is the tier rating and the second value is the overall rating.
		/// </returns>
		private static (double, double) GetStartPositionRating(
			double centerX, double maxY,
			int lx, int ly, int rx, int ry,
			double ilx, double ily, double irx, double iry)
		{
			// Positions closest to the ideal starting position are preferred.
			var dl = Math.Sqrt((lx - ilx) * (lx - ilx) + (ly - ily) * (ly - ily));
			var dr = Math.Sqrt((rx - irx) * (rx - irx) + (ry - iry) * (ry - iry));
			var distanceFromIdealRating = (dl + dr);

			// Positions which aren't centered are less preferred.
			// This helps Left and Right be a better start than Left and Center for SMX.
			var averageX = lx <= rx ? (lx + (rx - lx) * 0.5) : (rx + (lx - rx) * 0.5);
			var centeredXRating = Math.Abs(centerX - averageX);

			// Positions where the feet are at different y values are less preferred.
			var staggeredYRating = Math.Abs(ly - ry);

			// Positions off center and facing inwards are less preferred.
			var inwardFacingRating = 0;
			if ((lx < centerX && rx < centerX && ly < ry)
				||(lx > centerX && rx > centerX && ly > ry))
			{
				inwardFacingRating = staggeredYRating;
			}

			// As a tie-breaker, prefer starting positions closer to the back of the pads.
			var yRating = (maxY - ly) + (maxY - ry);

			// As a tie-breaker, prefer starting positions towards the left.
			var xRating = (lx + rx);

			var tierRating =
				distanceFromIdealRating * 1000
				+ centeredXRating * 100
				+ staggeredYRating * 10
				+ inwardFacingRating;
			
			var overallRating =
				distanceFromIdealRating * 100000
				+ centeredXRating * 10000
				+ staggeredYRating * 1000
				+ inwardFacingRating * 100
				+ yRating * 10
				+ xRating;

			return (tierRating, overallRating);
		}

		/// <summary>
		/// Verify generated PadData by comparing it to expected PadData.
		/// </summary>
		private static void VerifyPadData(PadData expected, PadData actual)
		{
			// Intentionally do not verify starting positions as they are expected to differ.
			var numArrows = expected.ArrowData.Length;
			Assert(numArrows == actual.ArrowData.Length);
			for (var a = 0; a < numArrows; a++)
			{
				var ed = expected.ArrowData[a];
				var ad = actual.ArrowData[a];
				Assert(ed.X == ad.X);
				Assert(ed.Y == ad.Y);

				Assert(numArrows == ed.ValidNextArrows.Length);
				Assert(numArrows == ad.ValidNextArrows.Length);
				for (var a2 = 0; a2 < numArrows; a2++)
				{
					Assert(ed.ValidNextArrows[a2] == ad.ValidNextArrows[a2]);
				}

				VerifyFootArrowArrow(ed.BracketablePairingsOtherHeel, ad.BracketablePairingsOtherHeel, numArrows);
				VerifyFootArrowArrow(ed.BracketablePairingsOtherToe, ad.BracketablePairingsOtherToe, numArrows);
				VerifyFootArrowArrow(ed.OtherFootPairings, ad.OtherFootPairings, numArrows);
				VerifyFootArrowArrow(ed.OtherFootPairingsOtherFootCrossoverFront, ad.OtherFootPairingsOtherFootCrossoverFront, numArrows);
				VerifyFootArrowArrow(ed.OtherFootPairingsOtherFootCrossoverBehind, ad.OtherFootPairingsOtherFootCrossoverBehind, numArrows);
				VerifyFootArrowArrow(ed.OtherFootPairingsInverted, ad.OtherFootPairingsInverted, numArrows);
			}
		}

		private static void VerifyFootArrowArrow(bool[][] expected, bool[][] actual, int numArrows)
		{
			Assert(NumFeet == expected.Length);
			Assert(NumFeet == actual.Length);
			for (var f = 0; f < NumFeet; f++)
			{
				Assert(numArrows == expected[f].Length);
				Assert(numArrows == actual[f].Length);
				for (var a2 = 0; a2 < numArrows; a2++)
				{
					Assert(expected[f][a2] == actual[f][a2]);
				}
			}
		}
	}
}
