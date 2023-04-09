﻿using Fumen.Converters;
using Fumen;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StepManiaLibrary
{
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
		/// Configuration for controlling facing.
		/// See Config.md for more information.
		/// </summary>
		public class FacingConfig
		{
			/// <summary>
			/// Maximum percentage of steps which should be Inward Facing.
			/// </summary>
			[JsonInclude] public double MaxInwardPercentage = -1.0;

			/// <summary>
			/// Maximum percentage of steps which should be Outward Facing.
			/// </summary>
			[JsonInclude] public double MaxOutwardPercentage = -1.0;

			/// <summary>
			/// Sets this FacingConfig to be an override of the the given other FacingConfig.
			/// Any values in this FacingConfig which are at their default, invalid values will
			/// be replaced with the corresponding values in the given other FacingConfig.
			/// </summary>
			/// <param name="other">Other FacingConfig to use as as a base.</param>
			public void SetAsOverrideOf(FacingConfig other)
			{
				if (MaxInwardPercentage.DoubleEquals(-1.0))
					MaxInwardPercentage = other.MaxInwardPercentage;
				if (MaxOutwardPercentage.DoubleEquals(-1.0))
					MaxOutwardPercentage = other.MaxOutwardPercentage;
			}

			/// <summary>
			/// Log errors if any values are not valid and return whether or not there are errors.
			/// </summary>
			/// <param name="pccId">Identifier for logging.</param>
			/// <returns>True if errors were found and false otherwise.</returns>
			public bool Validate(string pccId)
			{
				var errors = false;
				if (MaxInwardPercentage < 0.0)
				{
					LogError(
						$"Negative value \"{MaxInwardPercentage}\" specified for "
						+ "MaxInwardPercentage. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (MaxInwardPercentage > 1.0)
				{
					LogError(
						$"MaxInwardPercentage \"{MaxInwardPercentage}\" is greater 1.0. "
						+ "MaxInwardPercentage must be less than or equal to 1.0.",
						pccId);
					errors = true;
				}

				if (MaxOutwardPercentage < 0.0)
				{
					LogError(
						$"Negative value \"{MaxOutwardPercentage}\" specified for "
						+ "MaxOutwardPercentage. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (MaxOutwardPercentage > 1.0)
				{
					LogError(
						$"MaxOutwardPercentage \"{MaxOutwardPercentage}\" is greater 1.0. "
						+ "MaxOutwardPercentage must be less than or equal to 1.0.",
						pccId);
					errors = true;
				}
				return errors;
			}
		}

		/// <summary>
		/// Configuration for tightening steps.
		/// See Config.md for more information.
		/// </summary>
		public class StepTighteningConfig
		{
			/// <summary>
			/// When limiting travel speed, the lower time for the tightening range.
			/// Time in seconds between steps for one foot.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double TravelSpeedMinTimeSeconds = -1.0;
			/// <summary>
			/// When limiting travel speed, the higher time for the tightening range.
			/// Time in seconds between steps for one foot.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double TravelSpeedMaxTimeSeconds = -1.0;

			/// <summary>
			/// When limiting travel distance, the lower distance for the tightening range.
			/// Distance is in panel widths.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double TravelDistanceMin = -1.0;
			/// <summary>
			/// When limiting travel distance, the higher distance for the tightening range.
			/// Distance is in panel widths.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double TravelDistanceMax = -1.0;

			/// <summary>
			/// When limiting stretch, the lower distance for the tightening range.
			/// Distance is in panels width.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double StretchDistanceMin = -1.0;
			/// <summary>
			/// When limiting stretch, the higher distance for the tightening range.
			/// Distance is in panels width.
			/// See Config.md for more information.
			/// </summary>
			[JsonInclude] public double StretchDistanceMax = -1.0;

			/// <summary>
			/// Sets this StepTighteningConfig to be an override of the the given other StepTighteningConfig.
			/// Any values in this StepTighteningConfig which are at their default, invalid values will
			/// be replaced with the corresponding values in the given other StepTighteningConfig.
			/// </summary>
			/// <param name="other">Other StepTighteningConfig to use as as a base.</param>
			public void SetAsOverrideOf(StepTighteningConfig other)
			{
				if (TravelSpeedMinTimeSeconds.DoubleEquals(-1.0))
					TravelSpeedMinTimeSeconds = other.TravelSpeedMinTimeSeconds;
				if (TravelSpeedMaxTimeSeconds.DoubleEquals(-1.0))
					TravelSpeedMaxTimeSeconds = other.TravelSpeedMaxTimeSeconds;
				if (TravelDistanceMin.DoubleEquals(-1.0))
					TravelDistanceMin = other.TravelDistanceMin;
				if (TravelDistanceMax.DoubleEquals(-1.0))
					TravelDistanceMax = other.TravelDistanceMax;
				if (StretchDistanceMin.DoubleEquals(-1.0))
					StretchDistanceMin = other.StretchDistanceMin;
				if (StretchDistanceMax.DoubleEquals(-1.0))
					StretchDistanceMax = other.StretchDistanceMax;
			}

			/// <summary>
			/// Log errors if any values are not valid and return whether or not there are errors.
			/// </summary>
			/// <param name="pccId">Identifier for logging.</param>
			/// <returns>True if errors were found and false otherwise.</returns>
			public bool Validate(string pccId)
			{
				var errors = false;
				if (TravelSpeedMinTimeSeconds < 0.0)
				{
					LogError(
						$"Negative value \"{TravelSpeedMinTimeSeconds}\" "
						+ "specified for TravelSpeedMinTimeSeconds. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (TravelSpeedMaxTimeSeconds < 0.0)
				{
					LogError(
						$"Negative value \"{TravelSpeedMaxTimeSeconds}\" "
						+ "specified for TravelSpeedMaxTimeSeconds. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (TravelSpeedMinTimeSeconds > TravelSpeedMaxTimeSeconds)
				{
					LogError(
						$"TravelSpeedMinTimeSeconds \"{TravelSpeedMinTimeSeconds}\" "
						+ $"is greater than TravelSpeedMaxTimeSeconds \"{TravelSpeedMaxTimeSeconds}\". "
						+ "TravelSpeedMinTimeSeconds must be less than or equal to TravelSpeedMaxTimeSeconds.",
						pccId);
					errors = true;
				}

				if (TravelDistanceMin < 0.0)
				{
					LogError(
						$"Negative value \"{TravelDistanceMin}\" "
						+ "specified for TravelDistanceMin. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (TravelDistanceMax < 0.0)
				{
					LogError(
						$"Negative value \"{TravelDistanceMax}\" "
						+ "specified for TravelDistanceMax. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (TravelDistanceMin > TravelDistanceMax)
				{
					LogError(
						$"TravelDistanceMin \"{TravelDistanceMin}\" "
						+ $"is greater than TravelDistanceMax \"{TravelDistanceMax}\". "
						+ "TravelDistanceMin must be less than or equal to TravelDistanceMax.",
						pccId);
					errors = true;
				}

				if (StretchDistanceMin < 0.0)
				{
					LogError(
						$"Negative value \"{StretchDistanceMin}\" "
						+ "specified for StretchDistanceMin. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (StretchDistanceMax < 0.0)
				{
					LogError(
						$"Negative value \"{StretchDistanceMax}\" "
						+ "specified for StretchDistanceMax. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (StretchDistanceMin > StretchDistanceMax)
				{
					LogError(
						$"StretchDistanceMin \"{StretchDistanceMin}\" "
						+ $"is greater than StretchDistanceMax \"{StretchDistanceMax}\". "
						+ "StretchDistanceMin must be less than or equal to StretchDistanceMax.",
						pccId);
					errors = true;
				}
				return errors;
			}
		}

		/// <summary>
		/// Configuration for tightening lateral body movement.
		/// See Config.md for more information.
		/// </summary>
		public class LateralTighteningConfig
		{
			/// <summary>
			/// Pattern length at which to start apply lateral tightening.
			/// </summary>
			[JsonInclude] public int PatternLength = -1;

			/// <summary>
			/// The relative notes per second over which patterns should cost more.
			/// </summary>
			[JsonInclude] public double RelativeNPS = -1.0;

			/// <summary>
			/// The absolute notes per second over which patterns should cost more.
			/// </summary>
			[JsonInclude] public double AbsoluteNPS = -1.0;

			/// <summary>
			/// The lateral body speed in arrows per second over which patterns should cost more.
			/// </summary>
			[JsonInclude] public double Speed = -1.0;

			/// <summary>
			/// Sets this LateralTighteningConfig to be an override of the the given other LateralTighteningConfig.
			/// Any values in this LateralTighteningConfig which are at their default, invalid values will
			/// be replaced with the corresponding values in the given other LateralTighteningConfig.
			/// </summary>
			/// <param name="other">Other LateralTighteningConfig to use as as a base.</param>
			public void SetAsOverrideOf(LateralTighteningConfig other)
			{
				if (PatternLength == -1)
					PatternLength = other.PatternLength;
				if (RelativeNPS.DoubleEquals(-1.0))
					RelativeNPS = other.RelativeNPS;
				if (AbsoluteNPS.DoubleEquals(-1.0))
					AbsoluteNPS = other.AbsoluteNPS;
				if (Speed.DoubleEquals(-1.0))
					Speed = other.Speed;
			}

			/// <summary>
			/// Log errors if any values are not valid and return whether or not there are errors.
			/// </summary>
			/// <param name="pccId">Identifier for logging.</param>
			/// <returns>True if errors were found and false otherwise.</returns>
			public bool Validate(string pccId)
			{
				var errors = false;
				if (PatternLength < 0)
				{
					LogError(
						$"Negative value \"{PatternLength}\" specified for "
						+ "PatternLength. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (RelativeNPS < 0.0)
				{
					LogError(
						$"Negative value \"{RelativeNPS}\" specified for "
						+ "RelativeNPS. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (AbsoluteNPS < 0.0)
				{
					LogError(
						$"Negative value \"{AbsoluteNPS}\" specified for "
						+ "AbsoluteNPS. Expected non-negative value.",
						pccId);
					errors = true;
				}

				if (Speed < 0.0)
				{
					LogError(
						$"Negative value \"{Speed}\" specified for "
						+ "Speed. Expected non-negative value.",
						pccId);
					errors = true;
				}
				return errors;
			}
		}

		[JsonInclude] public FacingConfig Facing = new FacingConfig();
		[JsonInclude] public LateralTighteningConfig LateralTightening = new LateralTighteningConfig();
		[JsonInclude] public StepTighteningConfig StepTightening = new StepTighteningConfig();

		/// <summary>
		/// Dictionary of StepMania StepsType to a List of integers representing weights
		/// for each lane. When generating a PerformedChart we should try to match these weights
		/// for distributing arrows.
		/// </summary>
		[JsonInclude] public Dictionary<string, List<int>> ArrowWeights = new Dictionary<string, List<int>>();

		/// <summary>
		/// Normalized ArrowWeights.
		/// Values sum to 1.0.
		/// </summary>
		[JsonIgnore] public Dictionary<string, List<double>> ArrowWeightsNormalized = new Dictionary<string, List<double>>();

		/// <summary>
		/// Gets the desired arrow weights for the given chart type.
		/// Values normalized to sum to 1.0.
		/// </summary>
		/// <returns>List of normalized weights.</returns>
		public List<double> GetArrowWeightsNormalized(string chartType)
		{
			if (ArrowWeightsNormalized.TryGetValue(chartType, out var weights))
				return weights;
			return new List<double>();
		}

		/// <summary>
		/// Sets this PerformedChartConfig to be an override of the the given other PerformedChartConfig.
		/// Any values in this PerformedChartConfig which are at their default, invalid values will
		/// be replaced with the corresponding values in the given other PerformedChartConfig.
		/// </summary>
		/// <param name="other">Other PerformedChartConfig to use as as a base.</param>
		public void SetAsOverrideOf(PerformedChartConfig other)
		{
			LateralTightening.SetAsOverrideOf(other.LateralTightening);
			StepTightening.SetAsOverrideOf(other.StepTightening);
			Facing.SetAsOverrideOf(other.Facing);

			foreach (var kvp in other.ArrowWeights)
			{
				if (!ArrowWeights.ContainsKey(kvp.Key))
				{
					ArrowWeights.Add(kvp.Key, new List<int>(kvp.Value));
				}
			}
		}

		/// <summary>
		/// Perform post-load initialization.
		/// </summary>
		public void Init()
		{
			if (ArrowWeights != null)
			{
				ArrowWeightsNormalized = new Dictionary<string, List<double>>();
				foreach (var entry in ArrowWeights)
				{
					ArrowWeightsNormalized[entry.Key] = new List<double>();
					var sum = 0;
					foreach (var weight in entry.Value)
						sum += weight;
					foreach (var weight in entry.Value)
						ArrowWeightsNormalized[entry.Key].Add((double)weight / sum);
				}
			}
		}

		/// <summary>
		/// Log errors if any values are not valid and return whether or not there are errors.
		/// </summary>
		/// <param name="pccId">Identifier for logging.</param>
		/// <returns>True if errors were found and false otherwise.</returns>
		public bool Validate(string pccId = null)
		{
			var errors = false;
			errors = LateralTightening.Validate(pccId) || errors;
			errors = StepTightening.Validate(pccId) || errors;
			errors = Facing.Validate(pccId) || errors;
			return !errors;
		}

		public bool ValidateArrowWeights(
			string chartType,
			SMCommon.ChartType smChartType,
			bool smChartTypeValid,
			string pccId = null)
		{
			var errors = false;

			var desiredWeightsValid = ArrowWeights != null
									  && ArrowWeights.ContainsKey(chartType);
			if (!desiredWeightsValid)
			{
				LogError($"No ArrowWeights specified for \"{chartType}\".", pccId);
				errors = true;
			}

			if (smChartTypeValid && desiredWeightsValid)
			{
				var expectedNumArrows = SMCommon.Properties[(int)smChartType].NumInputs;
				if (ArrowWeights[chartType].Count != expectedNumArrows)
				{
					LogError($"ArrowWeights[\"{chartType}\"] has "
							 + $"{ArrowWeights[chartType].Count} entries. Expected {expectedNumArrows}.",
						pccId);
					errors = true;
				}

				foreach (var weight in ArrowWeights[chartType])
				{
					if (weight < 0)
					{
						LogError($"Negative weight \"{weight}\" in ArrowWeights[\"{chartType}\"].",
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
}
