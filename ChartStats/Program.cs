using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fumen;
using Fumen.Converters;

namespace ChartStats
{
	/// <summary>
	/// Parses charts based off of the Config settings and writes CSVs with statistics useful
	/// for quantifying qualities that could help aid in constructing good PerformedCharts.
	/// Quick and dirty.
	/// Some hardcoded assumptions that doubles charts will be used as the input.
	/// Some hardcoded assumptions that the time signature will be 4/4.
	/// Some copy-paste file system parsing from ChartGenerator.
	/// </summary>
	class Program
	{
		/// <summary>
		/// StringBuilder for accumulating csv data for a song stats spreadsheet.
		/// </summary>
		private static StringBuilder SBStats = new StringBuilder();
		/// <summary>
		/// StringBuilder for accumulating csv data about steps taken on each side of the pads.
		/// </summary>
		private static StringBuilder SBStepsPerSide = new StringBuilder();

		static async Task Main(string[] args)
		{
			// Load Config.
			var config = await Config.Load();
			if (config == null)
				return;

			// Set the Log Level before validating Config.
			Logger.LogLevel = config.LogLevel;

			// Write headers.
			// Assumption that charts are doubles.
			SBStats.AppendLine("Path,File,Song,Type,Difficulty,Rating,Notes Per Second,Total Steps,L,D,U,R,L,D,U,R,%L,%D,%U,%R,%L,%D,%U,%R");
			SBStepsPerSide.Append("Path,File,Song,Type,Difficulty,Rating,Time");
			foreach(var denominator in SMCommon.ValidDenominators)
				SBStepsPerSide.Append($",1_{denominator * SMCommon.NumBeatsPerMeasure} Steps");
			SBStepsPerSide.Append(",Variable Steps");
			SBStepsPerSide.Append(",NPS");
			foreach (var denominator in SMCommon.ValidDenominators)
				SBStepsPerSide.Append($",1_{denominator * SMCommon.NumBeatsPerMeasure} Steps NPS");
			SBStepsPerSide.AppendLine(",Variable Steps NPS");

			// Parse all charts.
			await FindCharts();

			// Write the files.
			File.WriteAllText(config.OutputFileStats, SBStats.ToString());
			File.WriteAllText(config.OutputFileStepsPerSide, SBStepsPerSide.ToString());

			Logger.Info("Done.");
		}

		/// <summary>
		/// Loops over all files specified by the Config and records stats
		/// into the appropriate CSV StringBuilders for matching Charts.
		/// </summary>
		static async Task FindCharts()
		{
			if (!Directory.Exists(Config.Instance.InputDirectory))
			{
				Logger.Error($"Could not find InputDirectory \"{Config.Instance.InputDirectory}\".");
				return;
			}

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
					Logger.Warn($"Could not get directories in \"{currentDir}\". {e}");
					continue;
				}

				// Get files.
				string[] files;
				try
				{
					files = Directory.GetFiles(currentDir);
				}
				catch (Exception e)
				{
					Logger.Warn($"Could not get files in \"{currentDir}\". {e}");
					continue;
				}

				// Check each file.
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
						Logger.Warn($"Could not get file info for \"{file}\". {e}");
						continue;
					}

					// Check if the matches the expression for files to convert.
					if (!Config.Instance.InputNameMatches(fi.DirectoryName, fi.Name))
						continue;
					
					await ProcessSong(fi);
				}
			}
		}

		/// <summary>
		/// Process the song represented by the given FileInfo.
		/// Record stats for each chart into the appropriate CSV StringBuilders.
		/// </summary>
		/// <param name="fileInfo">FileInfo representing a song.</param>
		static async Task ProcessSong(FileInfo fileInfo)
		{
			Logger.Info($"Processing {fileInfo.Name}.");

			var reader = Reader.CreateReader(fileInfo);
			var song = await reader.Load();
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == Config.Instance.InputChartType
				    && chart.NumPlayers == 1
				    && Config.Instance.DifficultyMatches(chart.DifficultyType))
				{
					// Variable to record steps per side change with variable spacing.
					// This data will go in its own column in the csv for ease of graphing.
					// Tuple first value is time for the series of steps.
					// Tuple second value is hte number of steps in the series.
					var stepsBetweenSideChangesVariableSpacing = new List<Tuple<double, int>>();

					// Variable to record steps per side change with constant spacing (streams).
					// The key in this dictionary is the denominator of the beat subdivision.
					// This helps group by note type (quarter, eighth, etc.) for graphing.
					// Each note type will go in its own column in the csv for ease of graphing.
					// Tuple first value is time for the series of steps.
					// Tuple second value is hte number of steps in the series.
					var stepsBetweenSideChanges = new Dictionary<int, List<Tuple<double, int>>>();
					foreach (var denom in SMCommon.ValidDenominators)
						stepsBetweenSideChanges[denom] = new List<Tuple<double, int>>();

					var totalSteps = 0;
					var steps = new int[chart.NumInputs];
					var onLeftSide = false;
					var onRightSide = false;
					var currentStepCountOnSide = 0;
					var previousStepWasFullyOnLeft = false;
					var previousStepWasFullyOnRight = false;
					var currentHolds = new bool[chart.NumInputs];
					var firstNote = true;
					var firstNoteTimeMicros = 0L;
					var lastNoteTimeMicros = 0L;
					var previousSteps = new bool[chart.NumInputs];
					var currentSteps = new bool[chart.NumInputs];
					var timeOfFirstStepOnCurrentSide = 0.0;
					var previousTimeBetweenSteps = 0.0;
					var previousTime = 0.0;
					var previousPosition = new MetricPosition();
					var currentStepsBetweenSideUseVariableTiming = false;
					var currentStepsBetweenSideGreatestDenominator = 0;

					// Parse each event in the chart. Loop index incremented in internal while loop
					// to capture jumps.
					for (var i = 0; i < chart.Layers[0].Events.Count;)
					{
						var currentStepsOnLeft = false;
						var currentStepsOnRight = false;
						double currentTime;
						MetricPosition currentPosition;
						var currentTimeBetweenSteps = 0.0;
						var wasAStep = false;
						for (var s = 0; s < chart.NumInputs; s++)
							currentSteps[s] = false;

						// Process each note at the same position (capture jumps).
						do
						{
							var chartEvent = chart.Layers[0].Events[i];
							currentPosition = chartEvent.Position;
							currentTime = chartEvent.TimeMicros / 1000000.0;

							// Record data about the step.
							var lane = -1;
							if (chartEvent is LaneHoldStartNote lhsn)
							{
								lane = lhsn.Lane;
								currentHolds[lane] = true;
								if (firstNote)
									firstNoteTimeMicros = lhsn.TimeMicros;
								lastNoteTimeMicros = lhsn.TimeMicros;
								firstNote = false;
								currentSteps[lane] = true;
							}
							else if (chartEvent is LaneHoldEndNote lhen)
							{
								currentHolds[lhen.Lane] = false;
							}
							else if (chartEvent is LaneTapNote ltn)
							{
								lane = ltn.Lane;
								if (firstNote)
									firstNoteTimeMicros = ltn.TimeMicros;
								lastNoteTimeMicros = ltn.TimeMicros;
								firstNote = false;
								currentSteps[lane] = true;
							}

							// This note was a step on an arrow.
							if (lane >= 0)
							{
								if (lane < chart.NumInputs >> 1)
									currentStepsOnLeft = true;
								else
									currentStepsOnRight = true;

								totalSteps++;
								steps[lane]++;
								if (currentStepCountOnSide == 0)
									timeOfFirstStepOnCurrentSide = currentTime;
								currentStepCountOnSide++;
								wasAStep = true;
								currentStepsBetweenSideGreatestDenominator = Math.Max(
									currentStepsBetweenSideGreatestDenominator,
									chartEvent.Position.SubDivision.Reduce().Denominator
								);
							}

							i++;
						}
						// Continue looping if the next event is at the same position.
						while (i < chart.Layers[0].Events.Count
						       && chart.Layers[0].Events[i].Position == chart.Layers[0].Events[i - 1].Position);

						if (wasAStep)
						{
							currentTimeBetweenSteps = currentTime - previousTime;
							if (Math.Abs(currentTimeBetweenSteps - previousTimeBetweenSteps) > 0.001)
								currentStepsBetweenSideUseVariableTiming = true;
						}

						// Quick and somewhat sloppy check to determine if this step is a jack.
						var jack = true;
						for (var s = 0; s < chart.NumInputs; s++)
							if (currentSteps[s] && !previousSteps[s])
								jack = false;

						var currentStepIsFullyOnLeft = currentStepsOnLeft && !currentStepsOnRight;
						var currentStepIsFullyOnRight = currentStepsOnRight && !currentStepsOnLeft;
						
						// Determine if any are held on each side so we don't consider steps on the other side
						// to be the start of a new sequence.
						var anyHeldOnLeft = false;
						var anyHeldOnRight = false;
						for (var a = 0; a < chart.NumInputs; a++)
						{
							if (currentHolds[a])
							{
								if (a < chart.NumInputs >> 1)
									anyHeldOnLeft = true;
								else
									anyHeldOnRight = true;
							}
						}

						// Check for the steps representing the player being fully on the left.
						if (currentStepIsFullyOnLeft && previousStepWasFullyOnLeft && !anyHeldOnRight && !jack)
						{
							// If we were on the right, swap sides.
							if (onRightSide)
								SwapSides(
									ref currentStepsBetweenSideGreatestDenominator,
									currentPosition,
									previousPosition,
									ref currentStepsBetweenSideUseVariableTiming,
									stepsBetweenSideChangesVariableSpacing,
									stepsBetweenSideChanges,
									currentTime,
									ref timeOfFirstStepOnCurrentSide,
									ref currentStepCountOnSide);
							onLeftSide = true;
							onRightSide = false;
						}

						// Check for the steps representing the player being fully on the right.
						if (currentStepIsFullyOnRight && previousStepWasFullyOnRight && !anyHeldOnLeft && !jack)
						{
							// If we were on the left, swap sides.
							if (onLeftSide)
								SwapSides(
									ref currentStepsBetweenSideGreatestDenominator,
									currentPosition,
									previousPosition,
									ref currentStepsBetweenSideUseVariableTiming,
									stepsBetweenSideChangesVariableSpacing,
									stepsBetweenSideChanges,
									currentTime,
									ref timeOfFirstStepOnCurrentSide,
									ref currentStepCountOnSide);
							onRightSide = true;
							onLeftSide = false;
						}

						// Record data about the previous step for the next iteration if this was a step.
						if (wasAStep)
						{
							previousStepWasFullyOnLeft = currentStepIsFullyOnLeft;
							previousStepWasFullyOnRight = currentStepIsFullyOnRight;
							for (var s = 0; s < chart.NumInputs; s++)
								previousSteps[s] = currentSteps[s];
							previousTime = currentTime;
							previousTimeBetweenSteps = currentTimeBetweenSteps;
							previousPosition = currentPosition;
						}
					}

					// Don't record anything if there are no steps. This prevents unhelpful data (and NaNs) from showing up.
					if (totalSteps == 0)
						continue;

					var playTime = (lastNoteTimeMicros - firstNoteTimeMicros) / 1000000.0;
					var nps = totalSteps / playTime;

					// Record song stats.
					SBStats.Append($"{CSVEscape(fileInfo.Directory.FullName)},{CSVEscape(fileInfo.Name)},");
					SBStats.Append(
						$"{CSVEscape(song.Title)},{CSVEscape(chart.Type)},{CSVEscape(chart.DifficultyType)},{chart.DifficultyRating},");
					SBStats.Append($"{nps},");
					SBStats.Append($"{totalSteps},");
					for (var i = 0; i < chart.NumInputs; i++)
						SBStats.Append($"{steps[i]},");
					for (var i = 0; i < chart.NumInputs; i++)
						SBStats.Append($"{(double) steps[i] / totalSteps},");
					SBStats.AppendLine("");

					// Record data about the steps taken per each side of the pads.
					foreach (var denominator in SMCommon.ValidDenominators)
					{
						foreach (var sideChange in stepsBetweenSideChanges[denominator])
						{
							SBStepsPerSide.Append($"{CSVEscape(fileInfo.Directory.FullName)},{CSVEscape(fileInfo.Name)},");
							SBStepsPerSide.Append(
								$"{CSVEscape(song.Title)},{CSVEscape(chart.Type)},{CSVEscape(chart.DifficultyType)},{chart.DifficultyRating},");
							SBStepsPerSide.Append($"{sideChange.Item1}");

							// Count pass
							foreach (var lineDenominator in SMCommon.ValidDenominators)
							{
								if (lineDenominator == denominator)
									SBStepsPerSide.Append($",{sideChange.Item2}");
								else
									SBStepsPerSide.Append(",");
							}
							SBStepsPerSide.Append(",");

							// NPS pass
							SBStepsPerSide.Append($",{sideChange.Item2 / sideChange.Item1}");
							foreach (var lineDenominator in SMCommon.ValidDenominators)
							{
								if (lineDenominator == denominator)
									SBStepsPerSide.Append($",{sideChange.Item2 / sideChange.Item1}");
								else
									SBStepsPerSide.Append(",");
							}
							SBStepsPerSide.AppendLine(",");
						}
					}
					foreach (var sideChange in stepsBetweenSideChangesVariableSpacing)
					{
						SBStepsPerSide.Append($"{CSVEscape(fileInfo.Directory.FullName)},{CSVEscape(fileInfo.Name)},");
						SBStepsPerSide.Append(
							$"{CSVEscape(song.Title)},{CSVEscape(chart.Type)},{CSVEscape(chart.DifficultyType)},{chart.DifficultyRating},");
						SBStepsPerSide.Append($"{sideChange.Item1}");

						// Count pass
						foreach (var _ in SMCommon.ValidDenominators)
							SBStepsPerSide.Append(",");
						SBStepsPerSide.Append($",{sideChange.Item2}");

						// NPS pass
						SBStepsPerSide.Append($",{sideChange.Item2 / sideChange.Item1}");
						foreach (var _ in SMCommon.ValidDenominators)
							SBStepsPerSide.Append(",");
						SBStepsPerSide.AppendLine($",{sideChange.Item2 / sideChange.Item1}");
					}
				}
			}
		}

		/// <summary>
		/// Helper to encapsulate the logic around swapping sides and recording data into the right
		/// variables.
		/// </summary>
		private static void SwapSides(
			ref int currentStepsBetweenSideGreatestDenominator,
			MetricPosition currentPosition,
			MetricPosition previousPosition,
			ref bool currentStepsBetweenSideUseVariableTiming,
			List<Tuple<double, int>> stepsBetweenSideChangesVariableSpacing,
			Dictionary<int, List<Tuple<double, int>>> stepsBetweenSideChanges,
			double currentTime,
			ref double timeOfFirstStepOnCurrentSide,
			ref int currentStepCountOnSide)
		{
			// If the notes are spaced greater than expected (e.g. eighth note up beats, or half/whole notes) then
			// treat as variable timing.
			if (currentStepsBetweenSideGreatestDenominator > 0)
			{
				// Assumption that time signature is 4/4.
				var expectedBeatsForConsistentTiming = 1.0 / currentStepsBetweenSideGreatestDenominator;
				var currentBeats =
					currentPosition.Measure * 4
					+ currentPosition.Beat
					+ (currentPosition.SubDivision.Denominator == 0 ? 0 : currentPosition.SubDivision.ToDouble());
				var previousBeats =
					previousPosition.Measure * 4
					+ previousPosition.Beat
					+ (previousPosition.SubDivision.Denominator == 0 ? 0 : previousPosition.SubDivision.ToDouble());

				// Notes are spaced too far apart to be a proper stream.
				if (Math.Abs(currentBeats - (previousBeats + expectedBeatsForConsistentTiming)) > 0.001)
					currentStepsBetweenSideUseVariableTiming = true;
			}

			var time = currentTime - timeOfFirstStepOnCurrentSide;
			var entry = new Tuple<double, int>(time, currentStepCountOnSide);

			if (currentStepsBetweenSideUseVariableTiming)
				stepsBetweenSideChangesVariableSpacing.Add(entry);
			else if (stepsBetweenSideChanges.ContainsKey(currentStepsBetweenSideGreatestDenominator))
				stepsBetweenSideChanges[currentStepsBetweenSideGreatestDenominator].Add(entry);
			// If we recorded a time signature, but it isn't a valid subdivision, just use the highest subdivision.
			else
				stepsBetweenSideChanges[48].Add(entry);

			// Reset state.
			currentStepCountOnSide = 0;
			timeOfFirstStepOnCurrentSide = 0.0;
			currentStepsBetweenSideUseVariableTiming = false;
			currentStepsBetweenSideGreatestDenominator = 0;
		}

		/// <summary>
		/// Escapes the given string for use in a csv.
		/// </summary>
		/// <param name="input">String to escape.</param>
		/// <returns>Escaped string.</returns>
		private static string CSVEscape(string input)
		{
			return $"\"{input.Replace("\"", "\"\"")}\"";
		}
	}
}
