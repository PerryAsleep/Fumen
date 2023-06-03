using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fumen;
using System.Text.RegularExpressions;

namespace ChartStats
{
	/// <summary>
	/// Configuration for ChartStats.
	/// Deserialized from json config file.
	/// Quick and dirty.
	/// Copy-paste from StepManiaChartGenerator.
	/// </summary>
	public class Config
	{
		private const string FileName = "config.json";
		private const string LogTag = "Config";

		public static Config Instance { get; private set; }

		[JsonInclude] public string InputDirectory;
		[JsonInclude] public string InputNameRegex;
		[JsonInclude] public List<string> InputDisallowList;
		[JsonInclude] public string InputChartType;
		[JsonInclude] public string DifficultyRegex;
		[JsonInclude] public string OutputFileStats;
		[JsonInclude] public string OutputFileStepsPerSide;
		[JsonInclude] public LogLevel LogLevel = LogLevel.Info;

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
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
				},
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				IncludeFields = true,
			};

			try
			{
				using (var openStream = File.OpenRead(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName)))
				{
					Instance = await JsonSerializer.DeserializeAsync<Config>(openStream, options);
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
		/// Determines whether the difficulty represented by the given string
		/// matches the Config DifficultyRegex.
		/// </summary>
		/// <param name="difficulty">String representing the difficulty to check.</param>
		/// <returns>True if this difficulty matches and false otherwise.</returns>
		public bool DifficultyMatches(string difficulty)
		{
			var matches = false;
			try
			{
				matches = Regex.IsMatch(difficulty, DifficultyRegex, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
			}
			catch (Exception e)
			{
				LogError(
					$"Failed to determine if difficulty \"{difficulty}\" matches DifficultyRegex \"{DifficultyRegex}\". {e}");
			}

			return matches;
		}

		/// <summary>
		/// Determines whether the file name represented by the given string
		/// matches the Config InputNameRegex.
		/// </summary>
		/// <param name="fullPath">File full path.</param>
		/// <param name="inputFileName">String representing the input file name to check.</param>
		/// <returns>True if this file name matches and false otherwise.</returns>
		public bool InputNameMatches(string fullPath, string inputFileName)
		{
			foreach (var disallowList in InputDisallowList)
			{
				if (fullPath.StartsWith(disallowList))
					return false;
			}

			var matches = false;
			try
			{
				matches = Regex.IsMatch(inputFileName, InputNameRegex, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
			}
			catch (Exception e)
			{
				LogError($"Failed to determine if file \"{inputFileName}\" matches InputNameRegex \"{InputNameRegex}\". {e}");
			}

			return matches;
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
