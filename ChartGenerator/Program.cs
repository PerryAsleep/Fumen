using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static ChartGenerator.Constants;
using static Fumen.Converters.SMCommon;
using Fumen;
using Fumen.Converters;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChartGenerator
{
	class Program
	{
		private static StepGraph InputStepGraph;
		private static StepGraph OutputStepGraph;
		private static List<List<GraphNode>> OutputStartNodes = new List<List<GraphNode>>();

		private static List<string> SupportedInputTypes = new List<string> { ChartTypeString(ChartType.dance_single) };
		private static List<string> SupportedOutputTypes = new List<string> { ChartTypeString(ChartType.dance_single), ChartTypeString(ChartType.dance_double) };

		private const double Version = 0.1;
		private const string FumenGeneratedFormattedVersion = "[FG v{0:0.00}]";
		private const string FumenGeneratedFormattedVersionRegexPattern = @"^\[FG v([0-9]+\.[0-9]+)\]";
		private const string LogTag = "Main";

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

			// Set the Log Level before validating Config.
			Logger.LogLevel = config.LogLevel;

			// Validate Config.
			if (!config.Validate(SupportedInputTypes, SupportedOutputTypes))
				return;

			// Create StepGraphs.
			if (config.InputChartType == config.OutputChartType)
			{
				LogInfo("Creating StepGraph.");
				InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R);
				OutputStepGraph = InputStepGraph;
				OutputStartNodes.Add(new List<GraphNode> { OutputStepGraph.Root });
				LogInfo("Finished creating StepGraph.");
			}
			else
			{
				LogInfo("Creating StepGraphs.");
				var t1 = new Thread(() => { InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R); });
				var t2 = new Thread(() => { OutputStepGraph = StepGraph.CreateStepGraph(ArrowData.DPArrowData, P1R, P2L); });
				t1.Start();
				t2.Start();
				t1.Join();
				t2.Join();

				// The first start node we should try is the root, centered on the middles.
				OutputStartNodes.Add(new List<GraphNode> { OutputStepGraph.Root });

				// Failing that, try nodes with one foot moved outward.
				OutputStartNodes.Add(new List<GraphNode>
				{
					OutputStepGraph.FindGraphNode(P1U, GraphArrowState.Resting, P2L, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P1D, GraphArrowState.Resting, P2L, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P1R, GraphArrowState.Resting, P2U, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P1R, GraphArrowState.Resting, P2D, GraphArrowState.Resting)
				});

				// Failing that, try nodes close to the middle.
				OutputStartNodes.Add(new List<GraphNode>
				{
					OutputStepGraph.FindGraphNode(P1U, GraphArrowState.Resting, P1R, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P1D, GraphArrowState.Resting, P1R, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P2L, GraphArrowState.Resting, P2U, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P2L, GraphArrowState.Resting, P2D, GraphArrowState.Resting)
				});

				// Last resort, try the SP start nodes. This is guaranteed to catch any SP chart start.
				OutputStartNodes.Add(new List<GraphNode>
				{
					OutputStepGraph.FindGraphNode(P1L, GraphArrowState.Resting, P1R, GraphArrowState.Resting),
					OutputStepGraph.FindGraphNode(P2L, GraphArrowState.Resting, P2R, GraphArrowState.Resting),
				});

				LogInfo("Finished creating StepGraphs.");
			}

			// Create a directory for outputting visualizations.
			if (Config.Instance.OutputVisualizations)
			{
				try
				{
					var pathSep = Path.DirectorySeparatorChar.ToString();
					VisualizationDir = Config.Instance.VisualizationsDirectory;
					if (!VisualizationDir.EndsWith(pathSep))
						VisualizationDir += pathSep;
					VisualizationDir += VisualizationSubDir;
					LogInfo($"Creating directory for outputting visualizations: {VisualizationDir}.");
					Directory.CreateDirectory(VisualizationDir);
				}
				catch (Exception e)
				{
					LogError($"Failed to find or create directory for outputting visualizations. {e}");
					return;
				}
			}

			FindCharts();

			// Hack. Wait for input.
			// TODO: Wait for all threads to complete.
			LogInfo("Done.");
			Console.ReadLine();
		}

		static void FindCharts()
		{
			if (!Directory.Exists(Config.Instance.InputDirectory))
			{
				LogError($"Could not find InputDirectory \"{Config.Instance.InputDirectory}\".");
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
					LogWarn($"Could not get directories in \"{currentDir}\". {e}");
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
					LogWarn($"Could not get files in \"{currentDir}\". {e}");
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
						LogWarn($"Could not get file info for \"{file}\". {e}");
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
						LogError("Failed to queue work thread.", fi, relativePath);
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

			// Load the song.
			Song song;
			try
			{
				var reader = Reader.CreateReader(songArgs.FileInfo);
				if (reader == null)
				{
					LogError("No Reader for file. Cannot parse.", songArgs.FileInfo, songArgs.RelativePath);
					return;
				}
				song = await reader.Load();
				
			}
			catch (Exception e)
			{
				LogError($"Failed to load file. {e}", songArgs.FileInfo, songArgs.RelativePath);
				return;
			}

			// Add new charts.
			AddCharts(song, songArgs);

			// Save
			var pathSep = Path.DirectorySeparatorChar.ToString();
			var saveDir = Config.Instance.OutputDirectory;
			if (!saveDir.EndsWith(pathSep))
				saveDir += pathSep;
			saveDir += songArgs.RelativePath;
			Directory.CreateDirectory(saveDir);
			var config = new SMWriterBase.SMWriterBaseConfig
			{
				FilePath = saveDir + songArgs.FileInfo.Name,
				Song = song,
				MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseUnmodifiedChartSubDivisions,
				PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource
			};

			var extension = songArgs.FileInfo.Extension.ToLower();
			if (extension.StartsWith("."))
				extension = extension.Substring(1);
			switch (extension)
			{
				case "sm":
					new SMWriter(config).Save();
					break;
				case "ssc":
					new SSCWriter(config).Save();
					break;
			}

			// TODO: Copy all files in the song dir.
		}

		static void AddCharts(Song song, SongArgs songArgs)
		{
			LogInfo("Processing Song.", songArgs.FileInfo, songArgs.RelativePath, song);

			var fileNameNoExtension = songArgs.FileInfo.Name;
			if (!string.IsNullOrEmpty(songArgs.FileInfo.Extension))
			{
				fileNameNoExtension = fileNameNoExtension.Substring(0, songArgs.FileInfo.Name.Length - songArgs.FileInfo.Extension.Length);
			}

			var extension = songArgs.FileInfo.Extension.ToLower();
			if (extension.StartsWith("."))
				extension = extension.Substring(1);

			var pathSep = Path.DirectorySeparatorChar.ToString();
			var newCharts = new List<Chart>();
			var chartsIndicesToRemove = new List<int>();
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
					var (expressedChart, rootSearchNode) = ExpressedChart.CreateFromSMEvents(
						chart.Layers[0].Events,
						InputStepGraph,
						GetLogIdentifier(songArgs.FileInfo, songArgs.RelativePath, song, chart));
					if (expressedChart == null)
					{
						LogError("Failed to create ExpressedChart.", songArgs.FileInfo, songArgs.RelativePath, song, chart);
						continue;
					}

					// Create a PerformedChart.
					var performedChart = PerformedChart.CreateFromExpressedChart(
						OutputStepGraph,
						OutputStartNodes,
						expressedChart,
						GetLogIdentifier(songArgs.FileInfo, songArgs.RelativePath, song, chart));
					if (performedChart == null)
					{
						LogError("Failed to create PerformedChart.", songArgs.FileInfo, songArgs.RelativePath, song, chart);
						continue;
					}

					// At this point we have succeeded, so add the chart index to remove if appropriate.
					if (currentChart != null)
						chartsIndicesToRemove.Add(currentChartIndex);

					// Create Events for the new Chart.
					var events = performedChart.CreateSMChartEvents();
					CopyNonPerformanceEvents(chart.Layers[0].Events, events);
					events.Sort(new SMEventComparer());

					// Sanity check
					if (events.Count != chart.Layers[0].Events.Count)
					{
						var mineString = NoteChars[(int)NoteType.Mine].ToString();
						// Disregard discrepancies in mine counts
						var newChartNonMineEventCount = 0;
						foreach (var newEvent in events)
						{
							if (newEvent.SourceType != mineString)
								newChartNonMineEventCount++;
						}

						var oldChartNonMineEventCount = 0;
						foreach (var oldEvent in chart.Layers[0].Events)
						{
							if (oldEvent.SourceType != mineString)
								oldChartNonMineEventCount++;
						}

						if (newChartNonMineEventCount != oldChartNonMineEventCount)
						{
							MetricPosition firstDiscrepancyPosition = null;
							var i = 0;
							while (i < events.Count && i < chart.Layers[0].Events.Count)
							{
								if (events[i].SourceType != chart.Layers[0].Events[i].SourceType
								    || events[i].Position != chart.Layers[0].Events[i].Position)
								{
									firstDiscrepancyPosition = chart.Layers[0].Events[i].Position;
									break;
								}
								i++;
							}
							LogError(
								"Programmer error. Discrepancy in non-mine Event counts." 
								+ $" Old: {oldChartNonMineEventCount}, New: {newChartNonMineEventCount}."
								+ $" First discrepancy position: {firstDiscrepancyPosition}.",
								songArgs.FileInfo, songArgs.RelativePath, song, chart);
							continue;
						}
					}

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
						DestExtras = chart.DestExtras,
						Type = Config.Instance.OutputChartType,
						NumPlayers = 1,
						NumInputs = OutputStepGraph.NumArrows
					};
					newChart.Layers.Add(new Layer {Events = events});
					newCharts.Add(newChart);

					LogInfo($"Generated new Chart from {chart.Type} {chart.DifficultyType} Chart.",
						songArgs.FileInfo, songArgs.RelativePath, song, newChart);

					// Write a visualization.
					if (Config.Instance.OutputVisualizations)
					{
						var visualizationDirectory = VisualizationDir + Path.DirectorySeparatorChar + songArgs.RelativePath;
						if (!visualizationDirectory.EndsWith(pathSep))
							visualizationDirectory += pathSep;
						Directory.CreateDirectory(visualizationDirectory);
						var saveFile = visualizationDirectory + $"{fileNameNoExtension}-{chart.DifficultyType}-{extension}.html";

						var renderer = new Renderer(
							songArgs.CurrentDir,
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
			}

			LogInfo($"Generated {newCharts.Count} new Charts (replaced {chartsIndicesToRemove.Count}).",
				songArgs.FileInfo, songArgs.RelativePath, song);

			// Remove overwritten charts.
			foreach (var i in chartsIndicesToRemove)
				song.Charts.RemoveAt(i);

			// Add new charts.
			song.Charts.AddRange(newCharts);
		}

		private static string GetFormattedVersionStringForChart()
		{
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
			var index = 0;
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

		#region Logging
		private static string GetLogIdentifier(FileInfo fi, string relativePath, Song song = null, Chart chart = null)
		{
			var sb = new StringBuilder();
			sb.Append($"[{relativePath}{fi.Name}");
			if (song != null)
				sb.Append($" \"{song.Title}\"");
			if (chart != null)
				sb.Append($" {chart.Type} {chart.DifficultyType}");
			sb.Append("]");
			return sb.ToString();
		}
		
		private static void LogError(string message)
		{
			Logger.Error($"[{LogTag}] {message}");
		}

		private static void LogWarn(string message)
		{
			Logger.Warn($"[{LogTag}] {message}");
		}

		private static void LogInfo(string message)
		{
			Logger.Info($"[{LogTag}] {message}");
		}

		private static void LogError(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
		{
			LogError($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
		}

		private static void LogWarn(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
		{
			LogWarn($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
		}

		private static void LogInfo(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
		{
			LogInfo($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
		}
		#endregion Logging
	}
}
