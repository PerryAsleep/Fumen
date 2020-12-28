using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static ChartGenerator.Constants;
using static Fumen.Converters.SMCommon;
using Fumen;
using Fumen.Converters;

namespace ChartGenerator
{
	// WIP
	class Program
	{
		private static StepGraph SPGraph;
		private static StepGraph DPGraph;

		private const double Version = 0.1;
		private const string FumenGeneratedFormattedVersion = "[FG v{0:0.00}]";
		private const string FumenGeneratedFormattedVersionRegexPattern = @"^\[FG v([0-9]+\.[0-9]+)\]";

		private const string HackChart = @"C:\Games\StepMania 5\Songs\Fumen\TestBracketHoldRoll\test.sm";
		private const string HackChartDir = @"C:\Games\StepMania 5\Songs\Fumen\TestBracketHoldRoll\";
		private const string HackDifficulty = @"Beginner";
		//private const string HackChart = @"C:\Games\StepMania 5\Songs\Technical Showcase 4\GIGA VIOLATE\GIGA VIOLATE.sm";
		//private const string HackChartDir = @"C:\Games\StepMania 5\Songs\Technical Showcase 4\GIGA VIOLATE";
		//private const string HackDifficulty = @"Challenge";
		//private const string HackChart = @"C:\Games\StepMania 5\Songs\Customs\Hey Sexy Lady (Skrillex Remix)\hey.sm";
		//private const string HackChartDir = @"C:\Games\StepMania 5\Songs\Customs\Hey Sexy Lady (Skrillex Remix)\";
		//private const string HackDifficulty = @"Challenge";

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

			var song = SMReader.Load(HackChart);
			AddDoublesCharts(song, OverwriteBehavior.IfFumenGenerated);

			// HACK
			//SMWriter.Save(song,
			//	@"C:\Games\StepMania 5\Songs\Customs\GIGA VIOLATE\GIGA VIOLATE.sm");
		}

		static void AddDoublesCharts(Song song, OverwriteBehavior overwriteBehavior)
		{
			var newCharts = new List<Chart>();
			var chartsIndicesToRemove = new List<int>();
			var chartIndex = 0;
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == ChartTypeString(ChartType.dance_single)
				    && chart.NumPlayers == 1
				    && chart.NumInputs == NumSPArrows
				    //HACK
				    && chart.DifficultyType == HackDifficulty)
				{
					// Check if there is an existing doubles chart corresponding to this singles chart.
					var (currentDoublesChart, dpChartIndex) = FindDoublesChart(song, chart.DifficultyType);
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

						chartsIndicesToRemove.Add(dpChartIndex);
					}

					// Generate a new series of Events for this Chart from the singles Chart.
					var (expressedChart, rootSearchNode) = ExpressedChart.CreateFromSMEvents(chart.Layers[0].Events, SPGraph);
					// HACK
					var performedChart = PerformedChart.CreateFromExpressedChart(SPGraph, expressedChart);
					var events = performedChart.CreateSMChartEvents();
					CopyNonPerformanceEvents(chart.Layers[0].Events, events);
					events.Sort(new SMEventComparer());

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
						Type = ChartTypeString(ChartType.dance_double),
						NumPlayers = 1,
						NumInputs = 8
					};

					// HACK
					newChart.NumInputs = 4;
					newChart.Type = ChartTypeString(ChartType.dance_single);

					newChart.Layers.Add(new Layer {Events = events});
					newCharts.Add(newChart);

					var renderer = new Renderer(
						HackChartDir,
						song,
						chart,
						expressedChart,
						rootSearchNode,
						performedChart,
						newChart
					);
					renderer.Write();
					return;
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

		private static (Chart, int) FindDoublesChart(Song song, string difficultyType)
		{
			int index = 0;
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == ChartTypeString(ChartType.dance_double)
				    && chart.NumPlayers == 1
				    && chart.NumInputs == NumDPArrows
					&& chart.DifficultyType == difficultyType)
				{
					return (chart, index);
				}
				index++;
			}

			return (null, 0);
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
