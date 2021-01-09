using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fumen;
using System.Text.RegularExpressions;

namespace ChartGenerator
{
	public enum OverwriteBehavior
	{
		DoNotOverwrite,
		IfFumenGenerated,
		IfFumenGeneratedAndNewerVersion,
		Always
	}

	public class Config
	{
		public const string FileName = "config.json";

		public static Config Instance { get; private set; }

		[JsonInclude] public string InputDirectory;
		[JsonInclude] public string InputNameRegex;
		[JsonInclude] public string InputChartType;
		[JsonInclude] public string DifficultyRegex;

		[JsonInclude] public string OutputChartType;
		[JsonInclude] public string OutputDirectory;
		[JsonInclude] public OverwriteBehavior OverwriteBehavior;
		[JsonInclude] public bool OutputVisualizations;
		[JsonInclude] public string VisualizationsDirectory;

		[JsonInclude] public Dictionary<StepType, HashSet<StepType>> StepTypeReplacements;

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
				using (FileStream openStream = File.OpenRead($@"{AppDomain.CurrentDomain.BaseDirectory}\{FileName}"))
				{
					Instance = await JsonSerializer.DeserializeAsync<Config>(openStream, options);
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to load {FileName}.");
				Logger.Error(e.ToString());
				Instance = null;
			}
			return Instance;
		}

		public bool DifficultyMatches(string difficulty)
		{
			var matches = false;
			try
			{
				matches = Regex.IsMatch(difficulty, DifficultyRegex, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to determine if difficulty '{difficulty}' matches DifficultyRegex '{DifficultyRegex}'.");
				Logger.Error(e.ToString());
			}
			return matches;
		}

		public bool InputNameMatches(string inputFileName)
		{
			var matches = false;
			try
			{
				matches = Regex.IsMatch(inputFileName, InputNameRegex, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to determine if file '{inputFileName}' matches InputNameRegex '{InputNameRegex}'.");
				Logger.Error(e.ToString());
			}
			return matches;
		}
	}
}
