using System;
using System.Threading;
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
			return Path.Combine(new[]{
				AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ChartGeneratorTests", "TestData", songFolder,
				$"{fileName}.{extension}"});
		}

		/// <summary>
		/// Load a song file.
		/// </summary>
		/// <param name="file">Song file with path and extension.</param>
		/// <returns>Song</returns>
		public static Song LoadSong(string file)
		{
			var reader = Reader.CreateReader(file);
			var task = reader.LoadAsync(CancellationToken.None);
			task.Wait();
			return task.Result;
		}

		/// <summary>
		/// Load a song file and return the first Chart matching the given parameters.
		/// </summary>
		/// <param name="file">Song file with path and extension.</param>
		/// <param name="chartDifficultyType">Optional difficulty type string of chart in song file to load.</param>
		/// <param name="type">Optional type string of chart in song file to load.</param>
		/// <returns>Chart</returns>
		public static Chart LoadChart(string file, string chartDifficultyType = null, string type = null)
		{
			var song = LoadSong(file);

			// Use the specified chart, if present.
			foreach (var chart in song.Charts)
			{
				if (chartDifficultyType != null && type != null)
				{
					if (chart.DifficultyType == chartDifficultyType && chart.Type == type)
					{
						return chart;
					}
				}
				else if (chartDifficultyType != null)
				{
					if (chart.DifficultyType == chartDifficultyType)
					{
						return chart;
					}
				}
				else if (type != null)
				{
					if (chart.Type == type)
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
