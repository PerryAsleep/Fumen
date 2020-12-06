using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChartGenerator;
using static ChartGenerator.Constants;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Tests for ArrowData.
	/// </summary>
	[TestClass]
	public class TestArrowData
	{
		/// <summary>
		/// ArrowData arrays should be symmetric.
		/// Asymmetric data likely means a typo in construction.
		/// </summary>
		[TestMethod]
		public void TestSymmetry()
		{
			TestSymmetry(ArrowData.SPArrowData);
			TestSymmetry(ArrowData.DPArrowData);
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

					Assert.AreEqual(arrowData[a].ValidNextArrows[a2],
						arrowData[oppositeA].ValidNextArrows[oppositeA2]);

					for (var f = 0; f < NumFeet; f++)
					{
						var oppositeF = NumFeet - f - 1;
						Assert.AreEqual(arrowData[a].BracketablePairings[f][a2],
							arrowData[oppositeA].BracketablePairings[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairings[f][a2],
							arrowData[oppositeA].OtherFootPairings[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsOtherFootCrossoverBehind[f][a2],
							arrowData[oppositeA].OtherFootPairingsOtherFootCrossoverFront[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsSameFootCrossoverBehind[f][a2],
							arrowData[oppositeA].OtherFootPairingsSameFootCrossoverFront[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsOtherFootCrossoverFront[f][a2],
							arrowData[oppositeA].OtherFootPairingsOtherFootCrossoverBehind[oppositeF][oppositeA2]);
						Assert.AreEqual(arrowData[a].OtherFootPairingsSameFootCrossoverFront[f][a2],
							arrowData[oppositeA].OtherFootPairingsSameFootCrossoverBehind[oppositeF][oppositeA2]);
					}
				}
			}
		}
	}
}
