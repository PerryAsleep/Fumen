using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static ChartGenerator.Constants;
using static Fumen.Converters.SMCommon;
using Fumen;
using Fumen.Converters;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChartGenerator
{
	// WIP
	class Program
	{
		private static StepGraph InputStepGraph;
		private static StepGraph OutputStepGraph;

		private static List<string> SupportedInputTypes = new List<string> { ChartTypeString(ChartType.dance_single) };
		private static List<string> SupportedOutputTypes = new List<string> { ChartTypeString(ChartType.dance_single), ChartTypeString(ChartType.dance_double) };

		private const double Version = 0.1;
		private const string FumenGeneratedFormattedVersion = "[FG v{0:0.00}]";
		private const string FumenGeneratedFormattedVersionRegexPattern = @"^\[FG v([0-9]+\.[0-9]+)\]";

		private static DateTime ExportTime;
		private static string VisualizationSubDir;
		private static string VisualizationDir;

		static async Task Main(string[] args)
		{
			ExportTime = DateTime.Now;
			VisualizationSubDir = ExportTime.ToString("yyyy-MM-dd HH-mm-ss");

			// Load Config.
			var config = await Config.Load();
			if (config == null)
				return;

			// Validate ChartTypes.
			if (string.IsNullOrEmpty(config.InputChartType))
			{
				Logger.Error($"No InputChartType found in {Config.FileName}.");
				return;
			}
			if (!SupportedInputTypes.Contains(config.InputChartType))
			{
				Logger.Error($"Unsupported InputChartType \"{config.InputChartType}\" found in {Config.FileName}. Expected value in [{string.Join(", ", SupportedInputTypes)}].");
				return;
			}
			if (string.IsNullOrEmpty(config.OutputChartType))
			{
				Logger.Error($"No OutputChartType found in {Config.FileName}.");
				return;
			}
			if (!SupportedOutputTypes.Contains(config.OutputChartType))
			{
				Logger.Error($"Unsupported OutputChartType \"{config.OutputChartType}\" found in {Config.FileName}. Expected value in [{string.Join(", ", SupportedOutputTypes)}].");
				return;
			}

			// Create StepGraphs.
			if (config.InputChartType == config.OutputChartType)
			{
				Logger.Info("Creating StepGraph.");
				InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R);
				OutputStepGraph = InputStepGraph;
				Logger.Info("Finished creating StepGraph.");
			}
			else
			{
				Logger.Info("Creating StepGraphs.");
				var t1 = new Thread(() => { InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R); });
				var t2 = new Thread(() => { OutputStepGraph = StepGraph.CreateStepGraph(ArrowData.DPArrowData, P1R, P2L); });
				t1.Start();
				t2.Start();
				t1.Join();
				t2.Join();
				Logger.Info("Finished creating StepGraphs.");
			}

			// Create a directory for outputting visualizations.
			if (Config.Instance.OutputVisualizations)
			{
				if (string.IsNullOrEmpty(Config.Instance.VisualizationsDirectory))
				{
					Logger.Error($"No VisualizationsDirectory provided in {Config.FileName}.");
					return;
				}

				try
				{
					var pathSep = Path.DirectorySeparatorChar.ToString();
					VisualizationDir = Config.Instance.VisualizationsDirectory;
					if (!VisualizationDir.EndsWith(pathSep))
						VisualizationDir += pathSep;
					VisualizationDir += VisualizationSubDir;
					Logger.Info($"Creating directory for outputting visualizations: {VisualizationDir}.");
					Directory.CreateDirectory(VisualizationDir);
				}
				catch (Exception e)
				{
					Logger.Error("Failed to find or create directory for outputting visualizations.");
					Logger.Error(e.ToString());
					return;
				}
			}

			FindCharts();

			// Hack. Wait for input.
			// TODO: Wait for all threads to complete.
			Logger.Info("Done.");
			Console.ReadLine();
		}

		static void FindCharts()
		{
			if (!Directory.Exists(Config.Instance.InputDirectory))
			{
				Logger.Error($"Could not find InputDirectory '{Config.Instance.InputDirectory}'.");
				return;
			}

			var pathSep = Path.DirectorySeparatorChar.ToString();
			var dirs = new Stack<string>();
			dirs.Push(Config.Instance.InputDirectory);

			while (dirs.Count > 0)
			{
				var currentDir = dirs.Pop();

				// Get sub directories.
				try
				{
					var subDirs = Directory.GetDirectories(currentDir);
					foreach (var str in subDirs)
						dirs.Push(str);
				}
				catch (Exception e)
				{
					Logger.Warn($"Could not get directories in '{currentDir}'.");
					Logger.Warn(e.ToString());
					continue;
				}

				var relativePath = currentDir.Substring(
					Config.Instance.InputDirectory.Length,
					currentDir.Length - Config.Instance.InputDirectory.Length);
				if (relativePath.StartsWith(pathSep))
					relativePath = relativePath.Substring(1, relativePath.Length - 1);
				if (!relativePath.EndsWith(pathSep))
					relativePath += pathSep;

				// Get files.
				string[] files;
				try
				{
					files = Directory.GetFiles(currentDir);
				}
				catch (Exception e)
				{
					Logger.Warn($"Could not get files in '{currentDir}'.");
					Logger.Warn(e.ToString());
					continue;
				}

				// Check each file.
				foreach (var file in files)
				{
					// Get the FileInfo for this file so we can check its name.
					FileInfo fi = null;
					try
					{
						fi = new FileInfo(file);
					}
					catch (Exception e)
					{
						Logger.Warn($"Could not get file info for '{file}'.");
						Logger.Warn(e.ToString());
						continue;
					}

					// Check if the matches the expression for files to convert.
					if (!Config.Instance.InputNameMatches(fi.Name))
						continue;

					var songArgs = new SongArgs
					{
						FileInfo = fi,
						CurrentDir = currentDir,
						RelativePath = relativePath
					};
					if (!ThreadPool.QueueUserWorkItem(ProcessSong, songArgs))
					{
						Logger.Error($"Failed to queue work thread for '{fi.Name}'.");
						continue;
					}
				}
			}
		}

		public class SongArgs
		{
			public FileInfo FileInfo;
			public string CurrentDir;
			public string RelativePath;
		}


		static async void ProcessSong(object args)
		{
			if (!(args is SongArgs songArgs))
				return;

			var fileNameNoExtension = songArgs.FileInfo.Name;
			if (!string.IsNullOrEmpty(songArgs.FileInfo.Extension))
			{
				fileNameNoExtension = fileNameNoExtension.Substring(0, songArgs.FileInfo.Name.Length - songArgs.FileInfo.Extension.Length);
			}

			// Load the song.
			Song song;
			try
			{
				var reader = Reader.CreateReader(songArgs.FileInfo);
				if (reader == null)
				{
					Logger.Error($"[{fileNameNoExtension}] No Reader for file. Cannot parse.");
					return;
				}
				song = await reader.Load();
				
			}
			catch (Exception e)
			{
				Logger.Error($"[{fileNameNoExtension}] Failed to load '{songArgs.FileInfo.Name}'.");
				Logger.Error(e.ToString());
				return;
			}

			// Add new charts.
			AddCharts(song, songArgs.CurrentDir, songArgs.RelativePath, songArgs.FileInfo.Name, fileNameNoExtension);

			// Save
			var pathSep = Path.DirectorySeparatorChar.ToString();
			var saveDir = Config.Instance.OutputDirectory;
			if (!saveDir.EndsWith(pathSep))
				saveDir += pathSep;
			saveDir += songArgs.RelativePath;
			Directory.CreateDirectory(saveDir);
			var smWriter = new SMWriter(new SMWriter.SMWriterConfig
			{
				FilePath = saveDir + songArgs.FileInfo.Name,
				Song = song,
				MeasureSpacingBehavior = SMWriter.MeasureSpacingBehavior.UseUnmodifiedChartSubDivisions
			});
			smWriter.Save();

			// TODO: Copy all files in the song dir.
		}

		static void AddCharts(Song song, string songDir, string relativePath, string fileName, string fileNameNoExtension)
		{
			Logger.Info($"[{fileNameNoExtension}] Processing '{relativePath}{fileName}'.");

			var pathSep = Path.DirectorySeparatorChar.ToString();
			var newCharts = new List<Chart>();
			var chartsIndicesToRemove = new List<int>();
			var chartIndex = 0;
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == Config.Instance.InputChartType
				    && chart.NumPlayers == 1
				    && chart.NumInputs == InputStepGraph.NumArrows
				    && Config.Instance.DifficultyMatches(chart.DifficultyType))
				{
					// Check if there is an existing chart.
					var (currentChart, currentChartIndex) = FindChart(
						song,
						Config.Instance.OutputChartType,
						chart.DifficultyType,
						OutputStepGraph.NumArrows);
					if (currentChart != null)
					{
						var fumenGenerated = GetFumenGeneratedVersion(chart, out var version);

						// Check if we should skip or overwrite the chart.
						switch (Config.Instance.OverwriteBehavior)
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
					}

					// Create an ExpressedChart.
					var (expressedChart, rootSearchNode) = ExpressedChart.CreateFromSMEvents(chart.Layers[0].Events, InputStepGraph);
					if (expressedChart == null)
					{
						Logger.Error($"[{fileNameNoExtension}] Failed to create ExpressedChart for {chart.DifficultyType} chart for '{relativePath}{fileName}'.");
						continue;
					}

					// Create a PerformedChart.
					var performedChart = PerformedChart.CreateFromExpressedChart(OutputStepGraph, expressedChart);
					if (performedChart == null)
					{
						Logger.Error($"[{fileNameNoExtension}] Failed to create PerformedChart for {chart.DifficultyType} chart for '{relativePath}{fileName}'.");
						continue;
					}

					// At this point we have succeeded, so add the chart index to remove if appropriate.
					if (currentChart != null)
						chartsIndicesToRemove.Add(currentChartIndex);

					// Create Events for the new Chart.
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
						Type = Config.Instance.OutputChartType,
						NumPlayers = 1,
						NumInputs = OutputStepGraph.NumArrows
					};
					newChart.Layers.Add(new Layer {Events = events});
					newCharts.Add(newChart);

					Logger.Info($"[{fileNameNoExtension}] Generated {Config.Instance.OutputChartType} {chart.DifficultyType} chart for '{relativePath}{fileName}'.");

					// Write a visualization.
					if (Config.Instance.OutputVisualizations)
					{
						var visualizationDirectory = VisualizationDir + Path.DirectorySeparatorChar.ToString() + relativePath;
						if (!visualizationDirectory.EndsWith(pathSep))
							visualizationDirectory += pathSep;
						Directory.CreateDirectory(visualizationDirectory);
						var saveFile = visualizationDirectory + $"{fileNameNoExtension}-{chart.DifficultyType}.html";

						var renderer = new Renderer(
							songDir,
							saveFile,
							song,
							chart,
							expressedChart,
							rootSearchNode,
							performedChart,
							newChart
						);
						renderer.Write();
					}
				}
				chartIndex++;
			}

			Logger.Info($"[{fileNameNoExtension}] Generated {newCharts.Count} new charts (replaced {chartsIndicesToRemove.Count}) for '{relativePath}{fileName}'.");

			// Remove overwritten charts.
			foreach (var i in chartsIndicesToRemove)
				song.Charts.RemoveAt(i);

			// Add new charts.
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

		private static (Chart, int) FindChart(Song song, string chartType, string difficultyType, int numArrows)
		{
			int index = 0;
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == chartType
					&& chart.NumPlayers == 1
				    && chart.NumInputs == numArrows
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
