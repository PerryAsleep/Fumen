using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static GenDoublesStaminaCharts.Constants;
using Fumen;
using Fumen.Converters;

namespace GenDoublesStaminaCharts
{
	// WIP
	class Program
	{
		private static StepGraph SPGraph;
		private static StepGraph DPGraph;

		private const double Version = 0.1;
		private const string FumenGeneratedFormattedVersion = "[FG v{0:0.00}]";
		private const string FumenGeneratedFormattedVersionRegexPattern = @"^\[FG v([0-9]+\.[0-9]+)\]";

		enum OverwriteBehavior
		{
			DoNotOverwrite,
			IfFumenGenerated,
			IfFumenGeneratedAndNewerVersion,
			Always
		}

		static void Main(string[] args)
		{
			SPGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R);
			//DPGraph = StepGraph.CreateStepGraph(ArrowData.DPArrowData, P1R, P2L);

			var song = SMReader.Load(
				@"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey.sm");
			AddDoublesCharts(song, OverwriteBehavior.IfFumenGenerated);
			SMWriter.Save(song, @"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey_2.sm");
		}

		static void AddDoublesCharts(Song song, OverwriteBehavior overwriteBehavior)
		{
			var newCharts = new List<Chart>();
			var chartsIndicesToRemove = new List<int>();
			var chartIndex = 0;
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
					// TODO: Make a method to perform this replace
				    && chart.Type.Replace("-", "_") == SMCommon.ChartType.dance_single.ToString()
				    && chart.NumPlayers == 1
				    && chart.NumInputs == NumSPArrows)
				{
					// Check if there is an existing doubles chart corresponding to this singles chart.
					var currentDoublesChart = FindDoublesChart(song, chart.DifficultyType);
					if (currentDoublesChart != null)
					{
						var fumenGenerated = GetFumenGeneratedVersion(chart, out var version);

						// Check if we should skip or overwrite the chart.
						switch (overwriteBehavior)
						{
							case OverwriteBehavior.DoNotOverwrite:
								continue;
							case OverwriteBehavior.IfFumenGenerated:
								if (!fumenGenerated)
									continue;
								break;
							case OverwriteBehavior.IfFumenGeneratedAndNewerVersion:
								if (!fumenGenerated || version >= Version)
									continue;
								break;
							case OverwriteBehavior.Always:
							default:
								break;
						}

						chartsIndicesToRemove.Add(chartIndex);
					}

					// Generate a new series of Events for this Chart from the singles Chart.
					var expressedChart = ExpressedChart.CreateFromSMEvents(chart.Layers[0].Events, SPGraph);
					var performedChart = PerformedChart.CreateFromExpressedChart(SPGraph, expressedChart);
					var events = performedChart.CreateSMChartEvents();
					CopyNonPerformanceEvents(chart.Layers[0].Events, events);
					events.Sort(new SMCommon.SMEventComparer());

					// Create a new Chart for these Events.
					var formattedVersion = GetFormattedVersionStringForChart();
					var newChart = new Chart
					{
						Artist = chart.Artist,
						ArtistTransliteration = chart.ArtistTransliteration,
						Genre = chart.Genre,
						GenreTransliteration = chart.GenreTransliteration,
						Author = $"{formattedVersion} {chart.Author}",
						Description = $"{formattedVersion} {chart.Description}",
						MusicFile = chart.MusicFile,
						ChartOffsetFromMusic = chart.ChartOffsetFromMusic,
						Tempo = chart.Tempo,
						DifficultyRating = chart.DifficultyRating,
						DifficultyType = chart.DifficultyType,
						SourceExtras = chart.SourceExtras,
						Type = SMCommon.ChartType.dance_double.ToString(),
						NumPlayers = 1,
						NumInputs = 8
					};
					newChart.Layers.Add(new Layer {Events = events});
					newCharts.Add(newChart);
				}

				chartIndex++;
			}

			// Remove overwritten doubles charts.
			foreach (var i in chartsIndicesToRemove)
				song.Charts.RemoveAt(i);
			// Add new doubles charts.
			song.Charts.AddRange(newCharts);
		}

		private static string GetFormattedVersionStringForChart()
		{
			// TODO: Check that this works.
			return string.Format(FumenGeneratedFormattedVersion, Version);
		}

		private static bool GetFumenGeneratedVersion(Chart chart, out double version)
		{
			version = 0.0;
			var match = Regex.Match(chart.Description, FumenGeneratedFormattedVersionRegexPattern, RegexOptions.IgnoreCase);
			if (match.Success && match.Groups.Count == 1 && match.Groups[0].Captures.Count == 1)
				return double.TryParse(match.Groups[0].Captures[0].Value, out version);
			return false;
		}

		private static Chart FindDoublesChart(Song song, string difficultyType)
		{
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type.Replace("-", "_") == SMCommon.ChartType.dance_double.ToString()
				    && chart.NumPlayers == 1
				    && chart.NumInputs == NumDPArrows
					&& chart.DifficultyType == difficultyType)
				{
					return chart;
				}
			}

			return null;
		}

		private static void CopyNonPerformanceEvents(List<Event> source, List<Event> dest)
		{
			foreach (var e in source)
			{
				if (e is TimeSignature || e is TempoChange || e is Stop)
					dest.Add(e);
			}
		}
	}
}
