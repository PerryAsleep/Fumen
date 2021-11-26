using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fumen;
using Fumen.Converters;
using StepManiaLibrary;
using static StepManiaLibrary.Constants;

namespace StepManiaChartFiller
{
	/// <summary>
	/// Configuration for StepManiaChartFiller.
	/// Deserialized from json config file.
	/// Use Load to load Config.
	/// See Config.md for detailed descriptions and examples of Config values.
	/// </summary>
	public class Config
	{
		/// <summary>
		/// File to use for deserializing Config.
		/// </summary>
		private const string FileName = "StepManiaChartFillerConfig.json";

		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "Config";

		/// <summary>
		/// Static Config instance.
		/// </summary>
		public static Config Instance { get; private set; }

		/// <summary>
		/// Logger config.
		/// </summary>
		[JsonInclude] public LoggerConfig LoggerConfig;

		/// <summary>
		/// Whether the application should close automatically or wait for input when it completes.
		/// </summary>
		[JsonInclude] public bool CloseAutomaticallyWhenComplete = false;

		/// <summary>
		/// Directory to recursively search through for finding Charts to convert.
		/// </summary>
		[JsonInclude] public string InputFile;

		/// <summary>
		/// StepMania StepsType of Charts to match for conversion.
		/// </summary>
		[JsonInclude] public string InputChartType;

		/// <summary>
		/// StepMania DifficultyNames of Chart to match for conversion.
		/// </summary>
		[JsonInclude] public string InputChartDifficulty;

		[JsonInclude] public bool SeedRandomNumbersFromFile;

		[JsonInclude] public List<FillSectionConfig> Sections;

		/// <summary>
		/// Loads the Config from the config json file.
		/// </summary>
		/// <returns>Config Instance.</returns>
		public static async Task<Config> Load()
		{
			if (Instance != null)
				return Instance;

			var options = new JsonSerializerOptions
			{
				Converters =
				{
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
				},
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				IncludeFields = true,
			};

			try
			{
				using (FileStream openStream = File.OpenRead(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName)))
				{
					Instance = await JsonSerializer.DeserializeAsync<Config>(openStream, options);
					Instance?.Init();
				}
			}
			catch (Exception e)
			{
				LogError($"Failed to load {FileName}. {e}");
				Instance = null;
			}

			return Instance;
		}

		/// <summary>
		/// Post Load initialization.
		/// </summary>
		private void Init()
		{
			foreach (var section in Sections)
				section.Init();
		}

		/// <summary>
		/// Performs validation of Config options.
		/// Will log errors and warnings.
		/// Returns true if no errors were found.
		/// </summary>
		/// <returns>True of no errors were found and false otherwise.</returns>
		public bool Validate()
		{
			var errors = false;

			if (string.IsNullOrEmpty(InputFile))
			{
				LogError("No InputFile specified.");
				errors = true;
			}

			if (string.IsNullOrEmpty(InputChartType))
			{
				LogError("No InputChartType specified.");
				errors = true;
			}
			else
			{
				var smChartTypeValid = SMCommon.TryGetChartType(InputChartType, out _);
				if (!smChartTypeValid)
				{
					LogError($"InputChartType \"{InputChartType}\" is not a valid stepmania chart type.");
					errors = true;
				}
			}

			foreach (var section in Sections)
			{
				if (!section.Validate())
					errors = true;
			}

			if (!LoggerConfig.Validate())
				errors = true;

			return !errors;
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
