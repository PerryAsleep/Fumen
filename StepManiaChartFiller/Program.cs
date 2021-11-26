using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static StepManiaLibrary.Constants;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using StepManiaLibrary;

namespace StepManiaChartFiller
{
	class Program
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "Main";

		/// <summary>
		/// StepGraph to use for parsing input Charts.
		/// </summary>
		private static StepGraph StepGraph;

		/// <summary>
		/// Time of the start of the export.
		/// </summary>
		private static DateTime ExportTime;

		/// <summary>
		/// Main entry point into the program.
		/// </summary>
		/// <remarks>See Config for configuration.</remarks>
		private static async Task Main()
		{
			ExportTime = DateTime.Now;

			// Create a temporary logger for logging exceptions from loading Config.
			Logger.StartUp(LogLevel.Error);

			// Load Config.
			var config = await Config.Load();
			if (config == null)
				Exit(false);

			// Create the logger as soon as possible. We need to load Config first for Logger configuration.
			var loggerSuccess = CreateLogger();

			// Validate Config, even if creating the logger failed. This will still log errors to the console.
			if (!config.Validate() || !loggerSuccess)
				Exit(false);

			// Create StepGraphs.
			var stepGraphCreationSuccess = await LoadPadDataAndCreateStepGraph();
			if (!stepGraphCreationSuccess)
				Exit(false);

			// Cache the replacement GraphLinks from the OutputStepGraph.
			var stepTypeReplacments = new Dictionary<StepType, HashSet<StepType>>();
			foreach (var stepType in Enum.GetValues(typeof(StepType)).Cast<StepType>())
			{
				var values = new HashSet<StepType>();
				values.Add(stepType);
				stepTypeReplacments[stepType] = values;
			}
			PerformedChart.CacheGraphLinks(StepGraph.FindAllGraphLinks(), stepTypeReplacments);

			await ProcessChart();

			LogInfo("Done.");
			Exit(true);
		}

		/// <summary>
		/// Exits the application.
		/// Will wait for input to close if configured to do so.
		/// </summary>
		/// <param name="bSuccess">
		/// If true then the application will exit with a 0 status code.
		/// If false, the application will exit with a 1 status code.
		/// </param>
		private static void Exit(bool bSuccess)
		{
			Logger.Shutdown();
			if (!(Config.Instance?.CloseAutomaticallyWhenComplete ?? false))
				Console.ReadLine();
			Environment.Exit(bSuccess ? 0 : 1);
		}

		/// <summary>
		/// Creates the Logger for the application.
		/// </summary>
		/// <returns>True if successful and false if any error occurred.</returns>
		private static bool CreateLogger()
		{
			try
			{
				var config = Config.Instance.LoggerConfig;
				if (config.LogToFile)
				{
					Directory.CreateDirectory(config.LogDirectory);
					var logFileName = "StepManiaChartFiller " + ExportTime.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
					var logFilePath = Fumen.Path.Combine(config.LogDirectory, logFileName);
					Logger.StartUp(
						config.LogLevel,
						logFilePath,
						config.LogFlushIntervalSeconds,
						config.LogBufferSizeBytes,
						config.LogToConsole);
				}
				else if (config.LogToConsole)
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
		/// Loads PadData and creates the InputStepGraph and OutputStepGraph.
		/// </summary>
		/// <returns>
		/// True if no errors were generated and false otherwise.
		/// </returns>
		private static async Task<bool> LoadPadDataAndCreateStepGraph()
		{
			// Load the input PadData.
			var padData = await LoadPadData(Config.Instance.InputChartType);
			if (padData == null)
				return false;

			// Create the StepGraph and use it for both the InputStepGraph and the OutputStepGraph.
			LogInfo("Creating StepGraph.");
			StepGraph = StepGraph.CreateStepGraph(padData, padData.StartingPositions[0][0][L],
				padData.StartingPositions[0][0][R]);
			LogInfo("Finished creating StepGraph.");

			return true;
		}

		/// <summary>
		/// Loads PadData for the given stepsType.
		/// </summary>
		/// <param name="stepsType">Stepmania StepsType to load PadData for.</param>
		/// <returns>Loaded PadData or null if any errors were generated.</returns>
		private static async Task<PadData> LoadPadData(string stepsType)
		{
			var fileName = $"{stepsType}.json";
			LogInfo($"Loading PadData from {fileName}.");
			var padData = await PadData.LoadPadData(stepsType, fileName);
			if (padData == null)
				return null;
			LogInfo($"Finished loading {stepsType} PadData.");
			return padData;
		}

		private static async Task ProcessChart()
		{
			FileInfo fi;
			try
			{
				fi = new FileInfo(Config.Instance.InputFile);
			}
			catch (Exception e)
			{
				LogWarn($"Could not get file info for \"{Config.Instance.InputFile}\". {e}");
				return;
			}

			// Load the song.
			Song song;
			try
			{
				var reader = Reader.CreateReader(fi);
				if (reader == null)
				{
					LogError($"Unsupported file format. Cannot parse {fi.FullName}.");
					return;
				}

				song = await reader.Load();
			}
			catch (Exception e)
			{
				LogError($"Failed to load Song {fi.FullName}. {e}");
				return;
			}

			// Fill the charts.
			FillCharts(song, fi);

			// Save
			var config = new SMWriterBase.SMWriterBaseConfig
			{
				FilePath = fi.FullName,
				Song = song,
				MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseLeastCommonMultipleFromStepmaniaEditor,
				PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource
			};
			var fileFormat = FileFormat.GetFileFormatByExtension(fi.Extension);
			switch (fileFormat.Type)
			{
				case FileFormatType.SM:
					new SMWriter(config).Save();
					break;
				case FileFormatType.SSC:
					new SSCWriter(config).Save();
					break;
				default:
					LogError($"Unsupported file format. Cannot save {fi.FullName}");
					break;
			}
		}

		private static void FillCharts(Song song, FileInfo fi)
		{
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == Config.Instance.InputChartType
				    && chart.NumPlayers == 1
				    && chart.NumInputs == StepGraph.NumArrows
				    && chart.DifficultyType == Config.Instance.InputChartDifficulty)
				{
					var performedChart = PerformedChart.CreateByFilling(
						StepGraph,
						Config.Instance.Sections,
						chart,
						GeneratePerformedChartRandomSeed(fi.Name),
						null);
					if (performedChart == null)
					{
						LogError("Failed to create PerformedChart.");
						continue;
					}

					var events = performedChart.CreateSMChartEvents();
					chart.Layers[0].Events = MergeFillEvents(chart.Layers[0].Events, events);
					chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

					LogInfo($"Filled events for {chart.Type} {chart.DifficultyType} Chart.");
				}
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
			if (Config.Instance.SeedRandomNumbersFromFile)
			{
				var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(fileName));
				return BitConverter.ToInt32(hash, 0);
			}

			return new Random().Next();
		}


		private static List<Event> MergeFillEvents(List<Event> sourceEvents, List<Event> fillEvents)
		{
			var resultEvents = new List<Event>();
			var fillSections = Config.Instance.Sections;

			var holdStartEvents = new Event[StepGraph.NumArrows];

			// Add source events.
			foreach (var sourceEvent in sourceEvents)
			{
				// Check if the event overlaps a fill section.
				var overlapsFill = false;
				foreach (var sectionConfig in fillSections)
				{
					if (sourceEvent.Position >= sectionConfig.StartPosition
					    && sourceEvent.Position < sectionConfig.EndPosition)
					{
						overlapsFill = true;
						// Add events which would not conflict with fill arrows.
						if (sourceEvent is TimeSignature || sourceEvent is TempoChange || sourceEvent is Stop)
						{
							resultEvents.Add(sourceEvent);
						}
						
						// Do not add any tap or hold notes. If this is a hold end note that occurred during the 
						// fill section, remove the start so we do not add it later.
						if (sourceEvent is LaneHoldEndNote lhen)
						{
							// do not add.
							holdStartEvents[lhen.Lane] = null;
						}
						break;
					}
				}

				// Always add events which don't overlap
				if (!overlapsFill)
				{
					// Record hold start notes for adding later only if the hold does not overlap a fill section.
					if (sourceEvent is LaneHoldStartNote lhsn)
					{
						holdStartEvents[lhsn.Lane] = sourceEvent;
					}

					// If this is a hold end, add the start and end only if it does not overlap a fill section.
					else if (sourceEvent is LaneHoldEndNote lhen)
					{
						if (holdStartEvents[lhen.Lane] != null)
						{
							var holdOverlapsFill = false;
							foreach (var sectionConfig in fillSections)
							{
								if (holdStartEvents[lhen.Lane].Position < sectionConfig.EndPosition
								    && lhen.Position >= sectionConfig.StartPosition)
								{
									holdOverlapsFill = true;
									break;
								}
							}
							if (!holdOverlapsFill)
							{
								resultEvents.Add(holdStartEvents[lhen.Lane]);
								resultEvents.Add(sourceEvent);
							}
						}
						holdStartEvents[lhen.Lane] = null;
					}

					// Always add non-holds that do not overlap.
					else
					{
						resultEvents.Add(sourceEvent);
					}
				}
			}

			// Always add the fill events.
			foreach (var fillEvent in fillEvents)
			{
				resultEvents.Add(fillEvent);
			}

			return resultEvents;
		}

		#region Logging

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

		#endregion Logging
	}
}
