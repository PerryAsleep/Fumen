using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StepManiaLibrary;
using static StepManiaLibrary.Constants;

namespace StepManiaLibraryTests
{
	/// <summary>
	/// Tests for ArrowData.
	/// </summary>
	[TestClass]
	public class TestArrowData
	{
		private static PadData SPPadData;
		private static PadData DPPadData;

		static TestArrowData()
		{
			var spPadDataTask = PadData.LoadPadData("dance-single", "dance-single.json");
			var dpPadDataTask = PadData.LoadPadData("dance-double", "dance-double.json");
			Task.WaitAll(spPadDataTask, dpPadDataTask);
			SPPadData = spPadDataTask.Result;
			DPPadData = dpPadDataTask.Result;
		}

		/// <summary>
		/// ArrowData arrays should be symmetric.
		/// Asymmetric data likely means a typo in construction.
		/// </summary>
		[TestMethod]
		public void TestSymmetry()
		{
			TestSymmetry(SPPadData.ArrowData);
			TestSymmetry(DPPadData.ArrowData);
		}

		private void TestSymmetry(ArrowData[] arrowData)
		{
			var numArrows = arrowData.Length;
			for (var a = 0; a < numArrows; a++)
			{
				for (var a2 = 0; a2 < numArrows; a2++)
				{
					var oppositeA = numArrows - a - 1;
					var oppositeA2 = numArrows - a2 - 1;

					for (var f = 0; f < NumFeet; f++)
					{
						var oppositeF = NumFeet - f - 1;
						Assert.AreEqual(arrowData[a].BracketablePairingsOtherHeel[f][a2],
							arrowData[oppositeA].BracketablePairingsOtherToe[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].BracketablePairingsOtherToe[f][a2],
							arrowData[oppositeA].BracketablePairingsOtherHeel[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairings[f][a2],
							arrowData[oppositeA].OtherFootPairings[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsStretch[f][a2],
							arrowData[oppositeA].OtherFootPairingsStretch[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsCrossoverBehind[f][a2],
							arrowData[oppositeA].OtherFootPairingsCrossoverFront[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsCrossoverBehindStretch[f][a2],
							arrowData[oppositeA].OtherFootPairingsCrossoverFrontStretch[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsCrossoverFront[f][a2],
							arrowData[oppositeA].OtherFootPairingsCrossoverBehind[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsCrossoverFrontStretch[f][a2],
							arrowData[oppositeA].OtherFootPairingsCrossoverBehindStretch[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsInverted[f][a2],
							arrowData[oppositeA].OtherFootPairingsInverted[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsInvertedStretch[f][a2],
							arrowData[oppositeA].OtherFootPairingsInvertedStretch[oppositeF][oppositeA2]);
					}
				}
			}
		}
	}
}
