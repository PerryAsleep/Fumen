using Fumen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StepManiaLibrary.PerformedChart
{
	public class StepTypeFallbacks
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "StepTypeFallbacks";

		/// <summary>
		/// Filename of the default fallbacks.
		/// </summary>
		public const string DefaultFallbacksFileName = "default-steptype-fallbacks.json";

		/// <summary>
		/// StepTypeFallbacks from config as strings.
		/// </summary>
		[JsonInclude, JsonPropertyName("StepTypeFallbacks")] public Dictionary<StepType, List<string>> FallbacksStrings = new Dictionary<StepType, List<string>>();
		/// <summary>
		/// Parsed StepTypeFallbacks.
		/// </summary>
		[JsonIgnore] private Dictionary<StepType, List<StepType>> Fallbacks = new Dictionary<StepType, List<StepType>>();
		/// <summary>
		/// Cached indexes of StepType fallbacks per StepType.
		/// </summary>
		[JsonIgnore] private Dictionary<StepType, Dictionary<StepType, int>> FallbackIndexes = new Dictionary<StepType, Dictionary<StepType, int>>();

		/// <summary>
		/// Loads StepTypeFallbacks from the given file.
		/// </summary>
		/// <returns>Loaded StepTypeFallbacks or null if the StepTypeFallbacks failed to load.</returns>
		public static async Task<StepTypeFallbacks> Load(string fileName)
		{
			StepTypeFallbacks fallbacks;
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
				using (FileStream openStream = File.OpenRead(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)))
				{
					fallbacks = await JsonSerializer.DeserializeAsync<StepTypeFallbacks>(openStream, options);
					fallbacks?.Init();
					if (fallbacks == null || fallbacks.Validate(fileName))
						throw new Exception("Invalid StepTypeFallbacks.");
				}
			}
			catch (Exception e)
			{
				LogError($"Failed to load {fileName}. {e}", fileName);
				fallbacks = null;
			}

			return fallbacks;
		}

		private void Init()
		{
			// Init Fallbacks from FallbacksStrings.
			foreach (var kvp in FallbacksStrings)
			{
				var stepTypeList = new List<StepType>();
				var ancestors = new HashSet<StepType> { kvp.Key };
				try
				{
					ParseStepTypeList(ancestors, kvp.Value, stepTypeList);
				}
				catch (Exception e)
				{
					e.Data.Add("StepTypeFallback", kvp.Key);
					throw;
				}
				Fallbacks.Add(kvp.Key, stepTypeList);
			}

			// Init FallbackIndexes.
			foreach (var kvp in Fallbacks)
			{
				var order = new Dictionary<StepType, int>();
				var index = 0;
				foreach (var fallback in kvp.Value)
				{
					order.Add(fallback, index);
					index++;
				}
				FallbackIndexes.Add(kvp.Key, order);
			}
		}

		/// <summary>
		/// Parse a list of strings of StepTypes from the config's StepTypeFallbacks elements that may include
		/// special entries to include other lists.
		/// </summary>
		/// <param name="ancestors">The visited nodes to prevent infinite recursion due to misconfiguration.</param>
		/// <param name="stepTypeStringList">The list of strings of StepType or include entries.</param>
		/// <param name="stepTypeList">The StepType list to generate.</param>
		private void ParseStepTypeList(HashSet<StepType> ancestors, List<string> stepTypeStringList, List<StepType> stepTypeList)
		{
			foreach (var stepTypeStr in stepTypeStringList)
			{
				// This value starts with the character indicating it should expand to another list.
				if (stepTypeStr.StartsWith("*"))
				{
					if (!Enum.TryParse(stepTypeStr.Substring(1), out StepType baseStepType))
					{
						throw new Exception($"Could not parse \"{stepTypeStr}\".");
					}
					if (!FallbacksStrings.TryGetValue(baseStepType, out var baseStrings))
					{
						throw new Exception($"No \"{baseStepType:G}\" entry found for \"{stepTypeStr}\".");
					}
					if (ancestors.Contains(baseStepType))
					{
						throw new Exception($"Cycle detected on {stepTypeStr}.");
					}

					// Record this type and recurse.
					ancestors.Add(baseStepType);
					ParseStepTypeList(ancestors, baseStrings, stepTypeList);
				}

				// This is a normal value representing a StepType.
				else
				{
					if (!Enum.TryParse(stepTypeStr, out StepType stepType))
					{
						throw new Exception($"Could not parse \"{stepTypeStr}\".");
					}
					stepTypeList.Add(stepType);
				}
			}
		}

		/// <summary>
		/// Lor errors if any StepType fallbacks aren't present.
		/// </summary>
		/// <param name="id">Identifier for logging.</param>
		/// <returns>True if errors were found and false otherwise.</returns>
		public bool Validate(string id)
		{
			var stepTypes = Enum.GetValues(typeof(StepType)).Cast<StepType>().ToList();
			var errors = false;
			foreach (var stepType in stepTypes)
			{
				if (!Fallbacks.ContainsKey(stepType) || Fallbacks[stepType].Count == 0)
				{
					LogError($"No StepTypeFallbacks for {stepType:G}."
							+ $" To ignore {stepType:G} steps, include an entry for it in StepTypeFallbacks and with an"
							+ " array value containing at least one StepType to use as a replacement.",
							id);
					errors = true;
				}
			}
			return errors;
		}

		public Dictionary<StepType, List<StepType>> GetFallbacks()
		{
			return Fallbacks;
		}

		public List<StepType> GetFallbacks(StepType stepType)
		{
			return Fallbacks[stepType];
		}

		public int GetFallbackIndex(StepType stepType, StepType fallbackStepType)
		{
			return FallbackIndexes[stepType][fallbackStepType];
		}

		#region Logging

		private static void LogError(string message, string id)
		{
			if (string.IsNullOrEmpty(id))
				Logger.Error($"[{LogTag}] {message}");
			else
				Logger.Error($"[{LogTag}] [{id}] {message}");
		}

		#endregion Logging
	}
}
