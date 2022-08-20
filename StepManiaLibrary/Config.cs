﻿using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fumen;
using Fumen.Converters;
using static StepManiaLibrary.Constants;

namespace StepManiaLibrary
{
	/// <summary>
	/// Enumeration of methods for overwriting charts.
	/// </summary>
	public enum OverwriteBehavior
	{
		/// <summary>
		/// Do no overwrite charts that match the output type.
		/// </summary>
		DoNotOverwrite,

		/// <summary>
		/// Overwrite existing charts if they were generated by this program.
		/// </summary>
		IfFumenGenerated,

		/// <summary>
		/// Overwrite existing charts if they were generated by this program and they
		/// were generated at an older version.
		/// </summary>
		IfFumenGeneratedAndNewerVersion,

		/// <summary>
		/// Always overwrite any existing charts.
		/// </summary>
		Always
	}

	/// <summary>
	/// Enumeration of methods for parsing brackets in ExpressedCharts.
	/// When encountering two arrows it can be subjective whether these
	/// arrows are meant to represent a bracket step or a jump. These behaviors
	/// allow a user to better control this behavior.
	/// </summary>
	public enum BracketParsingMethod
	{
		/// <summary>
		/// Aggressively interpret steps as brackets. In most cases brackets
		/// will preferred but in some cases jumps will still be preferred.
		/// </summary>
		Aggressive,

		/// <summary>
		/// Use a balanced method of interpreting brackets.
		/// </summary>
		Balanced,

		/// <summary>
		/// Never user brackets unless there is no other option.
		/// </summary>
		NoBrackets
	}

	/// <summary>
	/// Enumeration of methods for determining which BracketParsingMethod should
	/// be used.
	/// </summary>
	public enum BracketParsingDetermination
	{
		/// <summary>
		/// Dynamically choose the BracketParsingMethod based on configuration values.
		/// </summary>
		ChooseMethodDynamically,

		/// <summary>
		/// Use the configuration's default method.
		/// </summary>
		UseDefaultMethod,
	}

	/// <summary>
	/// Enumeration of methods for copying files.
	/// </summary>
	public enum CopyBehavior
	{
		/// <summary>
		/// Do not copy the file.
		/// </summary>
		DoNotCopy,

		/// <summary>
		/// Copy the file if it is newer than the destination file.
		/// </summary>
		IfNewer,

		/// <summary>
		/// Always copy the file.
		/// </summary>
		Always
	}
	
	/// <summary>
	/// Configuration data for ExpressedChart behavior.
	/// </summary>
	public class ExpressedChartConfig
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "ExpressedChartConfig";

		/// <summary>
		/// The default method to use for parsing brackets.
		/// </summary>
		[JsonInclude] public BracketParsingMethod DefaultBracketParsingMethod = BracketParsingMethod.Balanced;

		/// <summary>
		/// How to make the determination of which BracketParsingMethod to use.
		/// </summary>
		[JsonInclude] public BracketParsingDetermination BracketParsingDetermination = BracketParsingDetermination.ChooseMethodDynamically;

		/// <summary>
		/// When using the ChooseMethodDynamically BracketParsingDetermination, a level under which BracketParsingMethod NoBrackets
		/// will be chosen.
		/// </summary>
		[JsonInclude] public int MinLevelForBrackets;

		/// <summary>
		/// When using the ChooseMethodDynamically BracketParsingDetermination, whether or not encountering more simultaneous
		/// arrows than can be covered without brackets should result in using BracketParsingMethod Aggressive.
		/// </summary>
		[JsonInclude] public bool UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;

		/// <summary>
		/// When using the ChooseMethodDynamically BracketParsingDetermination, a Balanced bracket per minute count over which
		/// should result in BracketParsingMethod Aggressive being used.
		/// </summary>
		[JsonInclude] public double BalancedBracketsPerMinuteForAggressiveBrackets;

		/// <summary>
		/// When using the ChooseMethodDynamically BracketParsingDetermination, a Balanced bracket per minute count under which
		/// should result in BracketParsingMethod NoBrackets being used.
		/// </summary>
		[JsonInclude] public double BalancedBracketsPerMinuteForNoBrackets;

		public bool Validate(string eccId = null)
		{
			var errors = false;
			if (BalancedBracketsPerMinuteForAggressiveBrackets < 0.0)
			{
				LogError($"ExpressedChartConfig \"{eccId}\" BalancedBracketsPerMinuteForAggressiveBrackets "
				         + $" {BalancedBracketsPerMinuteForAggressiveBrackets} must be non-negative.",
					eccId);
				errors = true;
			}

			if (BalancedBracketsPerMinuteForNoBrackets < 0.0)
			{
				LogError($"ExpressedChartConfig \"{eccId}\" BalancedBracketsPerMinuteForNoBrackets "
				         + $" {BalancedBracketsPerMinuteForNoBrackets} must be non-negative.",
					eccId);
				errors = true;
			}

			if (BalancedBracketsPerMinuteForAggressiveBrackets <= BalancedBracketsPerMinuteForNoBrackets
			    && BalancedBracketsPerMinuteForAggressiveBrackets != 0.0
			    && BalancedBracketsPerMinuteForNoBrackets != 0.0)
			{
				LogError($"ExpressedChartConfig \"{eccId}\" BalancedBracketsPerMinuteForAggressiveBrackets"
				         + $" {BalancedBracketsPerMinuteForAggressiveBrackets} is not greater than"
				         + $" BalancedBracketsPerMinuteForNoBrackets {BalancedBracketsPerMinuteForNoBrackets}."
				         + " If these values are non-zero, BalancedBracketsPerMinuteForAggressiveBrackets must be"
				         + " greater than BalancedBracketsPerMinuteForNoBrackets.",
					eccId);
				errors = true;
			}

			return !errors;
		}

		#region Logging

		private static void LogError(string message, string eccId)
		{
			if (string.IsNullOrEmpty(eccId))
				Logger.Error($"[{LogTag}] {message}");
			else
				Logger.Error($"[{LogTag}] [{eccId}] {message}");
		}

		#endregion Logging
	}

	/// <summary>
	/// Configuration data for PerformedChart behavior.
	/// </summary>
	public class PerformedChartConfig
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "PerformedChartConfig";

		/// <summary>
		/// Dictionary of StepMania StepsType to a List of integers representing weights
		/// for each lane. When generating a PerformedChart we should try to match these weights
		/// for distributing arrows.
		/// </summary>
		[JsonInclude] public Dictionary<string, List<int>> DesiredArrowWeights;

		/// <summary>
		/// When assigning costs for tightening individual steps, the lower time for the tightening range.
		/// See Config.md for more information.
		/// Time in seconds between steps for one foot.
		/// </summary>
		[JsonInclude] public double IndividualStepTighteningMinTimeSeconds;

		/// <summary>
		/// When assigning costs for tightening individual steps, the higher time for the tightening range.
		/// See Config.md for more information.
		/// Time in seconds between steps for one foot.
		/// </summary>
		[JsonInclude] public double IndividualStepTighteningMaxTimeSeconds;

		/// <summary>
		/// When assigning costs lateral body movement tightening, the pattern length to use.
		/// See Config.md for more information.
		/// </summary>
		[JsonInclude] public int LateralTighteningPatternLength;

		/// <summary>
		/// When assigning costs lateral body movement tightening, the relative NPS over which patterns should cost more.
		/// See Config.md for more information.
		/// </summary>
		[JsonInclude] public double LateralTighteningRelativeNPS;

		/// <summary>
		/// When assigning costs lateral body movement tightening, the absolute NPS over which patterns should cost more.
		/// See Config.md for more information.
		/// </summary>
		[JsonInclude] public double LateralTighteningAbsoluteNPS;

		/// <summary>
		/// When assigning costs lateral body movement tightening, the lateral body speed in arrows per second over
		/// which patterns should cost more.
		/// See Config.md for more information.
		/// </summary>
		[JsonInclude] public double LateralTighteningSpeed;

		/// <summary>
		/// Normalized DesiredArrowWeights.
		/// Values sum to 1.0.
		/// </summary>
		[JsonIgnore] public Dictionary<string, List<double>> DesiredArrowWeightsNormalized;

		/// <summary>
		/// Gets the desired arrow weights for the given chart type.
		/// Values normalized to sum to 1.0.
		/// </summary>
		/// <returns>List of normalized weights.</returns>
		public List<double> GetDesiredArrowWeightsNormalized(string chartType)
		{
			if (DesiredArrowWeightsNormalized.TryGetValue(chartType, out var weights))
				return weights;
			return new List<double>();
		}

		public void Init()
		{
			if (DesiredArrowWeights != null)
			{
				DesiredArrowWeightsNormalized = new Dictionary<string, List<double>>();
				foreach (var entry in DesiredArrowWeights)
				{
					DesiredArrowWeightsNormalized[entry.Key] = new List<double>();
					var sum = 0;
					foreach (var weight in entry.Value)
						sum += weight;
					foreach (var weight in entry.Value)
						DesiredArrowWeightsNormalized[entry.Key].Add((double)weight / sum);
				}
			}
		}

		public bool Validate(string pccId = null)
		{
			var errors = false;
			if (IndividualStepTighteningMinTimeSeconds < 0.0)
			{
				LogError(
					$"Negative value \"{IndividualStepTighteningMinTimeSeconds}\" "
					+ "specified for IndividualStepTighteningMinTimeSeconds. Expected non-negative value.",
					pccId);
				errors = true;
			}

			if (IndividualStepTighteningMaxTimeSeconds < 0.0)
			{
				LogError(
					$"Negative value \"{IndividualStepTighteningMaxTimeSeconds}\" "
					+ "specified for IndividualStepTighteningMaxTimeSeconds. Expected non-negative value.",
					pccId);
				errors = true;
			}

			if (IndividualStepTighteningMinTimeSeconds > IndividualStepTighteningMaxTimeSeconds)
			{
				LogError(
					$"IndividualStepTighteningMinTimeSeconds \"{IndividualStepTighteningMinTimeSeconds}\" "
					+ $"is greater than IndividualStepTighteningMaxTimeSeconds \"{IndividualStepTighteningMaxTimeSeconds}\". "
					+ "IndividualStepTighteningMinTimeSeconds must be less than or equal to IndividualStepTighteningMaxTimeSeconds.",
					pccId);
				errors = true;
			}

			if (LateralTighteningPatternLength < 0)
			{
				LogError(
					$"Negative value \"{LateralTighteningPatternLength}\" specified for "
					+ "LateralTighteningPatternLength. Expected non-negative value.",
					pccId);
				errors = true;
			}

			if (LateralTighteningRelativeNPS < 0.0)
			{
				LogError(
					$"Negative value \"{LateralTighteningRelativeNPS}\" specified for "
					+ "LateralTighteningRelativeNPS. Expected non-negative value.",
					pccId);
				errors = true;
			}

			if (LateralTighteningAbsoluteNPS < 0.0)
			{
				LogError(
					$"Negative value \"{LateralTighteningAbsoluteNPS}\" specified for "
					+ "LateralTighteningAbsoluteNPS. Expected non-negative value.",
					pccId);
				errors = true;
			}

			if (LateralTighteningSpeed < 0.0)
			{
				LogError(
					$"Negative value \"{LateralTighteningSpeed}\" specified for "
					+ "LateralTighteningSpeed. Expected non-negative value.",
					pccId);
				errors = true;
			}

			return !errors;
		}

		public bool ValidateDesiredArrowWeights(
			string chartType,
			SMCommon.ChartType smChartType,
			bool smChartTypeValid,
			string pccId = null)
		{
			var errors = false;

			var desiredWeightsValid = DesiredArrowWeights != null
			                          && DesiredArrowWeights.ContainsKey(chartType);
			if (!desiredWeightsValid)
			{
				LogError($"No DesiredArrowWeights specified for \"{chartType}\".", pccId);
				errors = true;
			}

			if (smChartTypeValid && desiredWeightsValid)
			{
				var expectedNumArrows = SMCommon.Properties[(int)smChartType].NumInputs;
				if (DesiredArrowWeights[chartType].Count != expectedNumArrows)
				{
					LogError($"DesiredArrowWeights[\"{chartType}\"] has "
					         + $"{DesiredArrowWeights[chartType].Count} entries. Expected {expectedNumArrows}.",
						pccId);
					errors = true;
				}

				foreach (var weight in DesiredArrowWeights[chartType])
				{
					if (weight < 0)
					{
						LogError($"Negative weight \"{weight}\" in DesiredArrowWeights[\"{chartType}\"].",
							pccId);
						errors = true;
					}
				}
			}

			return !errors;
		}

		#region Logging

		private static void LogError(string message, string pccId)
		{
			if (string.IsNullOrEmpty(pccId))
				Logger.Error($"[{LogTag}] {message}");
			else
				Logger.Error($"[{LogTag}] [{pccId}] {message}");
		}

		#endregion Logging
	}

	public enum FillConfigStartFootChoice
	{
		AutomaticSameLane,
		AutomaticNewLane,
		SpecifiedLane
	}

	public enum FillConfigEndFootChoice
	{
		AutomaticIgnoreFollowingSteps,
		AutomaticSameLaneAsFollowing,
		AutomaticNewLaneFromFollowing,
		SpecifiedLane
	}

	public enum FillConfigStartingFootChoice
	{
		Random,
		Automatic,
		Specified
	}

	public class FillConfig
	{
		[JsonInclude] public int StartPosition;
		[JsonInclude] public int EndPosition;

		[JsonInclude] public int RandomSeed;

		[JsonInclude] public int BeatSubDivisionToFill = 4;

		[JsonInclude] public FillConfigStartingFootChoice StartingFootChoice;
		[JsonInclude] public int StartingFootSpecified = InvalidArrowIndex;

		[JsonInclude] public FillConfigStartFootChoice LeftFootStartChoice;
		[JsonInclude] public int LeftFootStartLaneSpecified = InvalidArrowIndex;
		[JsonInclude] public FillConfigEndFootChoice LeftFootEndChoice;
		[JsonInclude] public int LeftFootEndLaneSpecified = InvalidArrowIndex;

		[JsonInclude] public FillConfigStartFootChoice RightFootStartChoice;
		[JsonInclude] public int RightFootStartLaneSpecified = InvalidArrowIndex;
		[JsonInclude] public FillConfigEndFootChoice RightFootEndChoice;
		[JsonInclude] public int RightFootEndLaneSpecified = InvalidArrowIndex;

		[JsonInclude] public int SameArrowStepWeight;
		[JsonInclude] public int NewArrowStepWeight;
		[JsonInclude] public int NewArrowBracketableWeight = 1;
		[JsonInclude] public int NewArrowNonBracketableWeight = 0;

		[JsonInclude] public int MaxSameArrowsInARowPerFoot = -1;

		// Facing controls

		[JsonInclude] public PerformedChartConfig PerformedChartConfig;

		[JsonIgnore] public double SameArrowStepWeightNormalized;
		[JsonIgnore] public double NewArrowStepWeightNormalized;
		[JsonIgnore] public double NewArrowBracketableWeightNormalized;

		public bool Validate()
		{
			var errors = false;
			// TODO: validate.

			if (!PerformedChartConfig.Validate())
				errors = true;
			return !errors;
		}

		public void Init()
		{
			double totalStepTypeWeight = SameArrowStepWeight + NewArrowStepWeight;
			SameArrowStepWeightNormalized = SameArrowStepWeight / totalStepTypeWeight;
			NewArrowStepWeightNormalized = NewArrowStepWeight / totalStepTypeWeight;

			double totalBracketableTypeWeight = NewArrowBracketableWeight + NewArrowNonBracketableWeight;
			NewArrowBracketableWeightNormalized = NewArrowBracketableWeight / totalBracketableTypeWeight;

			PerformedChartConfig.Init();
		}
	}

	public class LoggerConfig
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "LoggerConfig";

		/// <summary>
		/// Logging log level.
		/// </summary>
		[JsonInclude] public LogLevel LogLevel = LogLevel.Info;

		/// <summary>
		/// Whether or not log to a file.
		/// </summary>
		[JsonInclude] public bool LogToFile;

		/// <summary>
		/// Directory to use for writing the log file.
		/// </summary>
		[JsonInclude] public string LogDirectory;

		/// <summary>
		/// Interval in seconds after which to flush the log file to disk.
		/// </summary>
		[JsonInclude] public int LogFlushIntervalSeconds;

		/// <summary>
		/// Log buffer size in bytes. When full the buffer log will flush to disk.
		/// </summary>
		[JsonInclude] public int LogBufferSizeBytes;

		/// <summary>
		/// Whether or not to log to the console.
		/// </summary>
		[JsonInclude] public bool LogToConsole;

		public bool Validate()
		{
			var errors = false;

			if (LogToFile)
			{
				if (string.IsNullOrEmpty(LogDirectory))
				{
					LogError("LogToFile is true, but no LogDirectory specified.");
					errors = true;
				}

				if (LogBufferSizeBytes <= 0)
				{
					LogError("Expected a non-negative LogBufferSizeBytes.");
					errors = true;
				}
			}

			return !errors;
		}

		#region Logging

		private static void LogError(string message)
		{
			Logger.Error($"[{LogTag}] {message}");
		}

		#endregion Logging
	}
}
