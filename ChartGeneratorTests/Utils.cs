using System;
using System.Threading;
using System.Threading.Tasks;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Unit Test Utilities.
	/// </summary>
	class Utils
	{
		/// <summary>
		/// Gets the path with extension to a test sm or ssc file in the given folder.
		/// </summary>
		/// <param name="songFolder">Name of the folder containing the test sm or ssc file.</param>
		/// <param name="fileName">
		/// Optional sm or ssc file name without extension.
		/// Defaults to "test".
		/// </param>
		/// <param name="extension">Option extension. Defaults to "sm".</param>
		/// <returns>String representation of path to sm file with extension.</returns>
		public static string GetTestChartPath(string songFolder, string fileName = "test", string extension = "sm")
		{
			return Fumen.Path.Combine(new[]{
				AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ChartGeneratorTests", "TestData", songFolder,
				$"{fileName}.{extension}"});
		}

		/// <summary>
		/// Load an sm file.
		/// </summary>
		/// <param name="smFile">SM file with path and extension.</param>
		/// <returns>Song</returns>
		public static Song LoadSMSong(string smFile)
		{
			Song song = null;
			Task.Run(async () => { song = await new SMReader(smFile).LoadAsync(CancellationToken.None); }).Wait();
			return song;
		}

		/// <summary>
		/// Load an ssc file.
		/// </summary>
		/// <param name="sscFile">SSC file with path and extension.</param>
		/// <returns>Song</returns>
		public static Song LoadSSCSong(string sscFile)
		{
			Song song = null;
			Task.Run(async () => { song = await new SSCReader(sscFile).LoadAsync(CancellationToken.None); }).Wait();
			return song;
		}

		/// <summary>
		/// Load an sm file and return the ExpressedChart representation.
		/// </summary>
		/// <param name="smFile">SM file with path and extension.</param>
		/// <param name="chartDifficultyType">
		/// Optional difficulty type string of chart in SM file to load.
		/// If omitted, the first chart found will be used.
		/// </param>
		/// <returns></returns>
		public static Chart LoadSMChart(string smFile, string chartDifficultyType = null)
		{
			var song = LoadSMSong(smFile);

			// Use the specified chart, if present.
			if (chartDifficultyType != null)
			{
				foreach (var chart in song.Charts)
				{
					if (chart.DifficultyType == chartDifficultyType)
					{
						return chart;
					}
				}
			}

			// Default to the first chart.
			return song.Charts[0];
		}

		/// <summary>
		/// Asserts that a given Event's position and time information match expected values.
		/// Assumes MetricPosition Beat and SubDivision are 0.
		/// </summary>
		/// <param name="chartEvent">Event to check</param>
		/// <param name="expectedIntegerPosition">Expected IntegerPosition of Event.</param>
		/// <param name="expectedTimeSeconds">Expected TimeSeconds of Event.</param>
		/// <param name="expectedMeasure">Expected MetricPosition Measure of Event.</param>
		public static void AssertPositionMatches(
			Event chartEvent,
			int expectedIntegerPosition,
			double expectedTimeSeconds,
			int expectedMeasure)
		{
			Assert.AreEqual(chartEvent.IntegerPosition, expectedIntegerPosition);
			Assert.AreEqual(chartEvent.TimeSeconds, expectedTimeSeconds);
			Assert.AreEqual(chartEvent.MetricPosition, new MetricPosition(expectedMeasure, 0));
		}

		/// <summary>
		/// Asserts that a given Event's position and time information match expected values.
		/// Assumes MetricPosition SubDivision is 0.
		/// </summary>
		/// <param name="chartEvent">Event to check</param>
		/// <param name="expectedIntegerPosition">Expected IntegerPosition of Event.</param>
		/// <param name="expectedTimeSeconds">Expected TimeSeconds of Event.</param>
		/// <param name="expectedMeasure">Expected MetricPosition Measure of Event.</param>
		/// <param name="expectedBeat">Expected MetricPosition Beat of Event.</param>
		public static void AssertPositionMatches(
			Event chartEvent,
			int expectedIntegerPosition,
			double expectedTimeSeconds,
			int expectedMeasure,
			int expectedBeat)
		{
			Assert.AreEqual(chartEvent.IntegerPosition, expectedIntegerPosition);
			Assert.AreEqual(chartEvent.TimeSeconds, expectedTimeSeconds);
			Assert.AreEqual(chartEvent.MetricPosition, new MetricPosition(expectedMeasure, expectedBeat));
		}

		/// <summary>
		/// Asserts that a given Event's position and time information match expected values.
		/// </summary>
		/// <param name="chartEvent">Event to check</param>
		/// <param name="expectedIntegerPosition">Expected IntegerPosition of Event.</param>
		/// <param name="expectedTimeSeconds">Expected TimeSeconds of Event.</param>
		/// <param name="expectedMeasure">Expected MetricPosition Measure of Event.</param>
		/// <param name="expectedBeat">Expected MetricPosition Beat of Event.</param>
		/// <param name="expectedSubDivisionNumerator">Expected MetricPosition SubDivision Numerator of Event.</param>
		/// <param name="expectedSubDivisionDenominator">Expected MetricPosition SubDivision Denominator of Event.</param>
		public static void AssertPositionMatches(
			Event chartEvent,
			int expectedIntegerPosition,
			double expectedTimeSeconds,
			int expectedMeasure,
			int expectedBeat,
			int expectedSubDivisionNumerator,
			int expectedSubDivisionDenominator)
		{
			Assert.AreEqual(chartEvent.IntegerPosition, expectedIntegerPosition);
			Assert.IsTrue(chartEvent.TimeSeconds.DoubleEquals(expectedTimeSeconds));
			Assert.AreEqual(chartEvent.MetricPosition, new MetricPosition(expectedMeasure, expectedBeat, expectedSubDivisionNumerator, expectedSubDivisionDenominator));
		}
	}
}
