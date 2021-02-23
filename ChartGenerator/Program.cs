using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static ChartGenerator.Constants;
using static Fumen.Converters.SMCommon;
using Fumen;
using Fumen.Converters;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChartGenerator
{
	/// <summary>
	/// ChartGenerator Program.
	/// Generates charts for stepmania files based on Config settings.
	/// See Config for configuring Program behavior.
	/// </summary>
	class Program
	{
		/// <summary>
		/// Version number. Recorded into a Chart's Description field using FumenGeneratedFormattedVersion.
		/// </summary>
		private const double Version = 0.2;
		/// <summary>
		/// Format for recording the Version into a Chart's Description field.
		/// </summary>
		private const string FumenGeneratedFormattedVersion = "[FG v{0:0.00}]";
		/// <summary>
		/// Regular expression for parsing the Version out of a Chart's Description field.
		/// </summary>
		private const string FumenGeneratedFormattedVersionRegexPattern = @"^\[FG v([0-9]+\.[0-9]+)\]";

		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "Main";

		/// <summary>
		/// StepGraph to use for parsing input Charts.
		/// </summary>
		private static StepGraph InputStepGraph;
		/// <summary>
		/// StepGraph to use for generating output Charts.
		/// </summary>
		private static StepGraph OutputStepGraph;
		/// <summary>
		/// GraphNodes to use as roots for trying to write output Charts.
		/// Outer List is sorted by preference of Nodes at that level. Level 0 contains the most preferable root GraphNodes.
		/// Inner Lists contain all GraphNodes of equal preference.
		/// Example for a doubles StepGraph:
		///  Tier 0 is both middles.
		///  Tier 1 is one middle and one up/down.
		///  etc.
		/// </summary>
		private static readonly List<List<GraphNode>> OutputStartNodes = new List<List<GraphNode>>();

		/// <summary>
		/// Supported file formats for reading and writing.
		/// </summary>
		private static readonly List<FileFormatType> SupportedFileFormats = new List<FileFormatType> { FileFormatType.SM, FileFormatType.SSC };
		/// <summary>
		/// Supported stepmania chart types for input.
		/// Only singles is supported as input.
		/// </summary>
		private static readonly List<string> SupportedInputTypes = new List<string> { ChartTypeString(ChartType.dance_single) };
		/// <summary>
		/// Supported stepmania chart types for output.
		/// Singles and doubles are support as output.
		/// </summary>
		private static readonly List<string> SupportedOutputTypes = new List<string> { ChartTypeString(ChartType.dance_single), ChartTypeString(ChartType.dance_double) };

		/// <summary>
		/// Time of the start of the export.
		/// </summary>
		private static DateTime ExportTime;

		/// <summary>
		/// Directory to record visualizations for this export.
		/// Export visualization directories are based on the ExportTime.
		/// </summary>
		private static string VisualizationDir;

		/// <summary>
		/// HashSet for keeping track of which song directories have had their non-chart files copied.
		/// Songs may have multiple song files (e.g. an sm and an ssc file). We want to only copy
		/// non-chart files once per song.
		/// </summary>
		private static readonly HashSet<string> CopiedDirectories = new HashSet<string>();

		/// <summary>
		/// Arguments for processing a Song.
		/// </summary>
		private class SongArgs
		{
			/// <summary>
			/// FileInfo for the Song file.
			/// </summary>
			public FileInfo FileInfo;
			/// <summary>
			/// String path of directory containing the Song file.
			/// </summary>
			public string CurrentDir;
			/// <summary>
			/// String path to the Song file relative to the Config InputDirectory.
			/// </summary>
			public string RelativePath;
			/// <summary>
			/// String path to the directory to save the Song file to.
			/// </summary>
			public string SaveDir;
		}

		/// <summary>
		/// Main entry point into the program.
		/// </summary>
		/// <remarks>See Config for configuration.</remarks>
		private static async Task Main()
		{
			ExportTime = DateTime.Now;

			// Load Config.
			var config = await Config.Load();
			if (config == null)
				return;

			// Create the logger as soon as possible. We need to load Config first for Logger configuration.
			var loggerSuccess = CreateLogger();

			// Validate Config, even if creating the logger failed. This will still log errors to the console.
			if (!config.Validate(SupportedInputTypes, SupportedOutputTypes))
				return;
			if (!loggerSuccess)
				return;

			// Create a directory for outputting visualizations.
			if (!CreateVisualizationOutputDirectory())
				return;

			// Create StepGraphs.
			await CreateStepGraphs();

			// Cache the replacement GraphLinks from the OutputStepGraph.
			PerformedChart.CacheGraphLinks(OutputStepGraph.FindAllGraphLinks());

			// Find and process all charts.
			await FindAndProcessCharts();
			
			LogInfo("Done.");
			Logger.Shutdown();
			Console.ReadLine();
		}

		/// <summary>
		/// Creates the InputStepGraph and OutputStepGraph.
		/// </summary>
		private static async Task CreateStepGraphs()
		{
			// If the types are the same, just create one graph.
			if (Config.Instance.InputChartType == Config.Instance.OutputChartType)
			{
				LogInfo("Creating StepGraph.");
				InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R);
				OutputStepGraph = InputStepGraph;
				OutputStartNodes.Add(new List<GraphNode> { OutputStepGraph.Root });
				LogInfo("Finished creating StepGraph.");
			}

			// If the types are separate, create two graphs.
			else
			{
				LogInfo("Creating StepGraphs.");

				// Create each graph on their own thread as these can take a few seconds.
				var tasks = new Task[2];
				tasks[0] = Task.Factory.StartNew(() => { InputStepGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R); });
				tasks[1] = Task.Factory.StartNew(() => { OutputStepGraph = StepGraph.CreateStepGraph(ArrowData.DPArrowData, P1R, P2L); });
				await Task.WhenAll(tasks);

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
		}

		/// <summary>
		/// Creates the Logger for the application.
		/// </summary>
		/// <returns>True if successful and false if any error occurred.</returns>
		private static bool CreateLogger()
		{
			try
			{
				var config = Config.Instance;
				if (config.LogToFile)
				{
					var logFileName = ExportTime.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
					var logFilePath = Fumen.Path.Combine(config.LogDirectory, logFileName);
					Logger.StartUp(config.LogLevel, logFilePath, config.LogFlushIntervalSeconds, config.LogBufferSizeBytes);
				}
				else
				{
					Logger.StartUp(config.LogLevel);
				}
			}
			catch (Exception e)
			{
				LogError($"Failed to create Logger. {e}");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Creates the output directory for visualizations if configured to do so.
		/// </summary>
		/// <returns>True if no errors and false otherwise.</returns>
		private static bool CreateVisualizationOutputDirectory()
		{
			if (Config.Instance.OutputVisualizations)
			{
				try
				{
					var visualizationSubDir = ExportTime.ToString("yyyy-MM-dd HH-mm-ss");
					VisualizationDir = Config.Instance.VisualizationsDirectory;
					VisualizationDir = Fumen.Path.Combine(VisualizationDir, visualizationSubDir);
					LogInfo($"Creating directory for outputting visualizations: {VisualizationDir}.");
					Directory.CreateDirectory(VisualizationDir);
				}
				catch (Exception e)
				{
					LogError($"Failed to find or create directory for outputting visualizations. {e}");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Searches for songs matching Config parameters and processes each.
		/// Will add charts, copy the charts and non-chart files to the output directory,
		/// and write visualizations for the conversion.
		/// </summary>
		private static async Task FindAndProcessCharts()
		{
			if (!Directory.Exists(Config.Instance.InputDirectory))
			{
				LogError($"Could not find InputDirectory \"{Config.Instance.InputDirectory}\".");
				return;
			}

			var songTasks = new List<Task>();
			var pathSep = System.IO.Path.DirectorySeparatorChar.ToString();

			// Search through the configured InputDirectory and all subdirectories.
			var dirs = new Stack<string>();
			dirs.Push(Config.Instance.InputDirectory);
			while (dirs.Count > 0)
			{
				// Get the directory to process.
				var currentDir = dirs.Pop();

				// Get sub directories for the next loop.
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

				// Get all files in this directory.
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

				// Cache some paths needed for processing the charts.
				var relativePath = currentDir.Substring(
					Config.Instance.InputDirectory.Length,
					currentDir.Length - Config.Instance.InputDirectory.Length);
				if (relativePath.StartsWith(pathSep))
					relativePath = relativePath.Substring(1, relativePath.Length - 1);
				if (!relativePath.EndsWith(pathSep))
					relativePath += pathSep;
				var saveDir = Fumen.Path.Combine(Config.Instance.OutputDirectory, relativePath);

				// Check each file.
				var hasSong = false;
				foreach (var file in files)
				{
					// Get the FileInfo for this file so we can check its name.
					FileInfo fi;
					try
					{
						fi = new FileInfo(file);
					}
					catch (Exception e)
					{
						LogWarn($"Could not get file info for \"{file}\". {e}");
						continue;
					}

					// Check that this is a supported file format.
					var fileFormat = FileFormat.GetFileFormatByExtension(fi.Extension);
					if (fileFormat == null || !SupportedFileFormats.Contains(fileFormat.Type))
						continue;

					// Check if the matches the expression for files to convert.
					if (!Config.Instance.InputNameMatches(fi.Name))
						continue;

					// Create the save directory before starting any Tasks which write into it.
					if (!hasSong)
					{
						hasSong = true;
						Directory.CreateDirectory(saveDir);
					}

					// Process the song.
					songTasks.Add(ProcessSong(new SongArgs 
					{
						FileInfo = fi,
						CurrentDir = currentDir,
						RelativePath = relativePath,
						SaveDir = saveDir
					}));
				}

				// TODO: Copy the song's pack assets.
			}

			// Allow the song tasks to complete.
			await Task.WhenAll(songTasks.ToArray());
		}

		/// <summary>
		/// Process one song.
		/// Song in this context is a song file.
		/// Some songs have multiple song files (an sm and an ssc version).
		/// Will add charts, copy the charts and non-chart files to the output directory,
		/// and write visualizations for the conversion.
		/// </summary>
		/// <param name="songArgs">SongArgs for the song file.</param>
		private static async Task ProcessSong(SongArgs songArgs)
		{
			// Load the song.
			Song song;
			try
			{
				var reader = Reader.CreateReader(songArgs.FileInfo);
				if (reader == null)
				{
					LogError("Unsupported file format. Cannot parse.", songArgs.FileInfo, songArgs.RelativePath);
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
			var saveFile = Fumen.Path.GetWin32FileSystemFullPath(Fumen.Path.Combine(songArgs.SaveDir, songArgs.FileInfo.Name));
			var config = new SMWriterBase.SMWriterBaseConfig
			{
				FilePath = saveFile,
				Song = song,
				MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseUnmodifiedChartSubDivisions,
				PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource
			};
			var fileFormat = FileFormat.GetFileFormatByExtension(songArgs.FileInfo.Extension);
			switch (fileFormat.Type)
			{
				case FileFormatType.SM:
					new SMWriter(config).Save();
					break;
				case FileFormatType.SSC:
					new SSCWriter(config).Save();
					break;
				default:
					LogError("Unsupported file format. Cannot save.", songArgs.FileInfo, songArgs.RelativePath);
					break;
			}

			// Copy the non-chart files.
			CopyNonChartFiles(songArgs.CurrentDir, songArgs.SaveDir);
		}

		/// <summary>
		/// Adds charts to the given song and write a visualization per chart, if configured to do so.
		/// </summary>
		/// <param name="song">Song to add charts to.</param>
		/// <param name="songArgs">SongArgs for the song file.</param>
		private static void AddCharts(Song song, SongArgs songArgs)
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
						GeneratePerformedChartRandomSeed(songArgs.FileInfo.Name),
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

					// Sanity check on note counts.
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
						var visualizationDirectory = Fumen.Path.Combine(VisualizationDir, songArgs.RelativePath);
						Directory.CreateDirectory(visualizationDirectory);
						var saveFile = Fumen.Path.GetWin32FileSystemFullPath(
							Fumen.Path.Combine(visualizationDirectory,
								$"{fileNameNoExtension}-{chart.DifficultyType}-{extension}.html"));

						// One time this write caused a DirectoryNotFoundException and I am not sure why.
						// The directory existed and nothing looked incorrect about the path or file.
						// For now, logging the exception as an error so it does not terminate the program.
						try
						{
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
						catch (Exception e)
						{
							LogError($"Failed to write visualization to \"{saveFile}\". {e}",
								songArgs.FileInfo, songArgs.RelativePath, song, newChart);
						}
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

		/// <summary>
		/// Copies the non-chart files from the given song directory into the given save directory.
		/// </summary>
		/// <remarks>
		/// Idempotent.
		/// Will not copy if already invoked with the same song directory.
		/// Will not copy if not appropriate based on Config.NonChartFileCopyBehavior.
		/// Expects that saveDir exists and is writable.
		/// Will log errors and warnings on failures.
		/// </remarks>
		/// <param name="songDir">
		/// Directory of the song to copy the non-chart files from.
		/// </param>
		/// <param name="saveDir">
		/// Directory to copy the non-chart files into.
		/// </param>
		private static void CopyNonChartFiles(string songDir, string saveDir)
		{
			if (Config.Instance.NonChartFileCopyBehavior == CopyBehavior.DoNotCopy
			    || Config.Instance.IsOutputDirectorySameAsInputDirectory())
				return;

			// Only copy the non-chart files once per song.
			lock (CopiedDirectories)
			{
				if (CopiedDirectories.Contains(songDir))
					return;
				CopiedDirectories.Add(songDir);
			}

			// Get the files in the song directory.
			string[] files;
			try
			{
				files = Directory.GetFiles(songDir);
			}
			catch (Exception e)
			{
				LogWarn($"Could not get files in \"{songDir}\". {e}");
				return;
			}

			// Check each file for copying
			foreach (var file in files)
			{
				// Get the FileInfo for this file so we can check its name.
				FileInfo fi;
				try
				{
					fi = new FileInfo(file);
				}
				catch (Exception e)
				{
					LogWarn($"Could not get file info for \"{file}\". {e}");
					continue;
				}

				// Skip this file if it is a chart.
				var fileFormat = FileFormat.GetFileFormatByExtension(fi.Extension);
				if (fileFormat != null && SupportedFileFormats.Contains(fileFormat.Type))
					continue;

				// Skip this file if it is not newer than the destination file and we
				// should only copy if newer.
				var destFilePath = saveDir + fi.Name;
				if (Config.Instance.NonChartFileCopyBehavior == CopyBehavior.IfNewer)
				{
					FileInfo dfi;
					try
					{
						dfi = new FileInfo(destFilePath);
					}
					catch (Exception e)
					{
						LogWarn($"Could not get file info for \"{destFilePath}\". {e}");
						continue;
					}

					if (dfi.Exists && fi.LastWriteTime <= dfi.LastWriteTime)
					{
						continue;
					}
				}

				// Copy the file.
				try
				{
					File.Copy(Fumen.Path.GetWin32FileSystemFullPath(fi.FullName),
						Fumen.Path.GetWin32FileSystemFullPath(destFilePath),
						true);
				}
				catch (Exception e)
				{
					LogWarn($"Failed to copy \"{fi.FullName}\" to \"{destFilePath}\". {e}");
				}
			}
		}

		/// <summary>
		/// Gets the formatted version string for recording onto a Chart.
		/// </summary>
		/// <returns>Formatted version string for recording onto a Chart.</returns>
		private static string GetFormattedVersionStringForChart()
		{
			return string.Format(FumenGeneratedFormattedVersion, Version);
		}

		/// <summary>
		/// Parses the given Chart's description to see if it was generated by this application.
		/// Returns the version if present, via the out parameter.
		/// </summary>
		/// <param name="chart">Chart to check.</param>
		/// <param name="version">
		/// Out parameter to store the version of the application used to generate the chart.
		/// </param>
		/// <returns>Whether or not the given Chart was generated by this application.</returns>
		private static bool GetFumenGeneratedVersion(Chart chart, out double version)
		{
			version = 0.0;
			if (string.IsNullOrEmpty(chart.Description))
				return false;
			
			var match = Regex.Match(chart.Description, FumenGeneratedFormattedVersionRegexPattern, RegexOptions.IgnoreCase);
			if (match.Success && match.Groups.Count == 1 && match.Groups[0].Captures.Count == 1)
				return double.TryParse(match.Groups[0].Captures[0].Value, out version);
			return false;
		}

		/// <summary>
		/// Finds the Chart matching the given parameters in the given Song.
		/// </summary>
		/// <param name="song">Song to check.</param>
		/// <param name="chartType">Chart Type sting to match.</param>
		/// <param name="difficultyType">Chart DifficultyType string to match.</param>
		/// <param name="numArrows">Number of arrows / lanes to match.</param>
		/// <returns>
		/// Chart and the index of this Chart in the Song, if the Chart was found.
		/// Returns (null, 0) if not found.
		/// </returns>
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

		/// <summary>
		/// Copies the non-performance events from one List of Events to another.
		/// Non-performance events are: TimeSignature, TempoChange, Stop.
		/// </summary>
		/// <param name="source">Event List to copy from.</param>
		/// <param name="dest">Event List to copy to.</param>
		private static void CopyNonPerformanceEvents(List<Event> source, List<Event> dest)
		{
			foreach (var e in source)
			{
				if (e is TimeSignature || e is TempoChange || e is Stop)
					dest.Add(e);
			}
		}

		/// <summary>
		/// Generates a random seed to use for a PerformedChart based on the Song's file name.
		/// Creating a PerformedChart from the same inputs more than once should produce the same result.
		/// </summary>
		/// <param name="fileName">Name of the Song file to hash to generate the seed.</param>
		/// <returns>Random seed to use.</returns>
		private static int GeneratePerformedChartRandomSeed(string fileName)
		{
			var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(fileName));
			return BitConverter.ToInt32(hash, 0);
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
