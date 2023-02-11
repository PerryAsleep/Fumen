using System;
using System.Collections.Generic;
using System.Reflection;
using Fumen.ChartDefinition;

namespace Fumen.Converters
{
	/// <summary>
	/// Class to assist with parsing a MSDFile.Value identified by an expected PropertyName.
	/// Abstract class with subclasses implementing the Parse method.
	/// </summary>
	public abstract class PropertyParser
	{
		/// <summary>
		/// The name of the property to parse data for.
		/// </summary>
		protected readonly string PropertyName;

		/// <summary>
		/// Optional Logger.
		/// </summary>
		protected ILogger Logger;

		protected PropertyParser(string smPropertyName)
		{
			PropertyName = smPropertyName;
		}

		public void SetLogger(ILogger logger)
		{
			Logger = logger;
		}

		/// <summary>
		/// Abstract Parse method for a MSDFile.Value.
		/// Checks the MSDFile.Value to see if it matches our PropertyName and parses
		/// the Params if does.
		/// </summary>
		/// <param name="value">MSDFile.Value to check and parse.</param>
		/// <returns>
		/// True if this MSDFile.Value matches our PropertyName and false otherwise.
		/// </returns>
		public abstract bool Parse(MSDFile.Value value);

		/// <summary>
		/// Helper to check if the MSDFile.Value matches our PropertyName.
		/// The 0th entry in the MSDFile.Value's Params is the property name.
		/// </summary>
		/// <param name="value">MSDFile.Value to check.</param>
		/// <returns>
		/// True if this MSDFile.Value matches our PropertyName and false otherwise.
		/// </returns>
		protected bool DoesValueMatchProperty(MSDFile.Value value)
		{
			return value.Params.Count > 0
				&& !string.IsNullOrEmpty(value.Params[0])
				&& value.Params[0].ToUpper() == PropertyName;
		}

		/// <summary>
		/// Helper to check if the MSDFile.Value matches our PropertyName and parse
		/// the first Param as a string.
		/// </summary>
		/// <param name="value">MSDFile.Value to check.</param>
		/// <param name="parsedValue">Out param to hold the parsed string value.</param>
		/// <returns>
		/// True if this MSDFile.Value matches our PropertyName and false otherwise.
		/// </returns>
		protected bool ParseFirstParameter(MSDFile.Value value, out string parsedValue)
		{
			parsedValue = "";

			// Only consider this line if it matches this property name.
			if (!DoesValueMatchProperty(value))
				return false;

			if (value.Params.Count > 1 && !string.IsNullOrEmpty(value.Params[1]))
				parsedValue = value.Params[1];

			return true;
		}
	}

	/// <summary>
	/// Parses a value directly into an Extras value.
	/// </summary>
	public class ExtrasPropertyParser : PropertyParser
	{
		private readonly Extras Extras;
		public ExtrasPropertyParser(Extras extras)
			: base(null)
		{
			Extras = extras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			if (value.Params.Count != 2)
				return false;
			Extras.AddSourceExtra(value.Params[0], value.Params[1]);
			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter directly into the property specified for the Song.
	/// Example:
	/// #ARTIST:Usao;
	/// </summary>
	public class PropertyToSongPropertyParser : PropertyParser
	{
		private readonly string SongPropertyName;
		private readonly Song Song;

		public PropertyToSongPropertyParser(string smPropertyName, string songPropertyName, Song song)
			: base(smPropertyName)
		{
			SongPropertyName = songPropertyName;
			Song = song;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var songValueStr))
				return false;

			// Record the string value in the extras.
			Song.Extras.AddSourceExtra(PropertyName, songValueStr, true);

			// Only consider this line if the property is valid.
			var prop = Song.GetType().GetProperty(SongPropertyName, BindingFlags.Public | BindingFlags.Instance);
			if (null == prop || !prop.CanWrite)
			{
				Logger?.Error($"{PropertyName}: Could not find public instance writable property for Song property '{SongPropertyName}'.");
				return true;
			}

			// Set the property on the Song.
			object o;
			try
			{
				o = Convert.ChangeType(songValueStr, prop.PropertyType);
			}
			catch (Exception)
			{
				Logger?.Warn($"{PropertyName}: Failed to Convert '{songValueStr}' to type '{prop.PropertyType}' for Song property '{SongPropertyName}'.");
				return true;
			}
			prop.SetValue(Song, o, null);

			// Overwrite the extras value with the correct type.
			Song.Extras.AddSourceExtra(PropertyName, o, true);

			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter directly into the property specified for the Chart.
	/// Example:
	/// #ARTIST:Usao;
	/// </summary>
	public class PropertyToChartPropertyParser : PropertyParser
	{
		private readonly string ChartPropertyName;
		private readonly Chart Chart;

		public PropertyToChartPropertyParser(string smPropertyName, string chartPropertyName, Chart chart)
			: base(smPropertyName)
		{
			ChartPropertyName = chartPropertyName;
			Chart = chart;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var chartValueStr))
				return false;

			// Record the string value in the extras.
			Chart.Extras.AddSourceExtra(PropertyName, chartValueStr, true);

			// Only consider this line if the property is valid.
			var prop = Chart.GetType().GetProperty(ChartPropertyName, BindingFlags.Public | BindingFlags.Instance);
			if (null == prop || !prop.CanWrite)
			{
				Logger?.Error($"{PropertyName}: Could not find public instance writable property for Chart property '{ChartPropertyName}'.");
				return true;
			}

			// Set the property on the Chart.
			object o;
			try
			{
				o = Convert.ChangeType(chartValueStr, prop.PropertyType);
			}
			catch (Exception)
			{
				Logger?.Warn($"{PropertyName}: Failed to Convert '{chartValueStr}' to type '{prop.PropertyType}' for Chart property '{ChartPropertyName}'.");
				return true;
			}
			prop.SetValue(Chart, o, null);

			// Overwrite the extras value with the correct type.
			Chart.Extras.AddSourceExtra(PropertyName, o, true);

			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter into the Extras as the type T.
	/// Examples:
	/// #SELECTABLE:YES;
	/// #VERSION:0.83;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class PropertyToSourceExtrasParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Extras Extras;

		public PropertyToSourceExtrasParser(string smPropertyName, Extras extras)
			: base(smPropertyName)
		{
			Extras = extras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var valueStr))
				return false;

			T tValue = default;
			if (!string.IsNullOrEmpty(valueStr))
			{
				try
				{
					tValue = (T)Convert.ChangeType(valueStr, typeof(T));
				}
				catch (Exception)
				{
					Logger?.Warn($"{PropertyName}: Failed to Convert '{valueStr}' to type '{typeof(T)}'.");
					return true;
				}
			}

			Extras.AddSourceExtra(PropertyName, tValue, true);
			return true;
		}
	}

	/// <summary>
	/// Parses a value with multiple parameters separated by the MSDFile ParamMarker
	/// into a List of values of type T and stores it on the Extras.
	/// It is important to use a list instead of a raw string when parameters are
	/// separated by the MSDFile ParamMarker so we do not escape them when writing
	/// back out. We need to understand that they are separate values.
	/// Example:
	/// #DISPLAYBPM:150.000:170.000;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ListPropertyToSourceExtrasParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Extras Extras;

		public ListPropertyToSourceExtrasParser(string smPropertyName, Extras extras)
			: base(smPropertyName)
		{
			Extras = extras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!DoesValueMatchProperty(value))
				return false;

			List<T> parsedList = new List<T>();
			for (var paramIndex = 1; paramIndex < value.Params.Count; paramIndex++)
			{
				if (!string.IsNullOrEmpty(value.Params[paramIndex]))
				{
					T tValue;
					try
					{
						tValue = (T)Convert.ChangeType(value.Params[paramIndex], typeof(T));
					}
					catch (Exception)
					{
						Logger?.Warn($"{PropertyName}: Failed to Convert '{value.Params[paramIndex]}' to type '{typeof(T)}'.");
						return true;
					}
					parsedList.Add(tValue);
				}
			}

			Extras.AddSourceExtra(PropertyName, parsedList, true);
			return true;
		}
	}

	/// <summary>
	/// Parses a value with multiple parameters separated by the MSDFile ParamMarker
	/// into a List of Fractions.
	/// Parses these into a Dictionary of Fractions.
	/// Will also store the raw value on the Extras if given raw string property name.
	/// Example:
	/// #TIMESIGNATURES:0.000=7=8,3.500=3=4;
	/// </summary>
	public class ListFractionPropertyParser : PropertyParser
	{
		private readonly Dictionary<double, Fraction> Values;
		private readonly Extras Extras;
		private readonly string RawStringPropertyName;

		public ListFractionPropertyParser(
			string smPropertyName,
			Dictionary<double, Fraction> values,
			Extras extras = null,
			string rawStringPropertyName = null)
			: base(smPropertyName)
		{
			Values = values;
			Extras = extras;
			RawStringPropertyName = rawStringPropertyName;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var rawStr))
				return false;

			// Record the raw string to preserve formatting when writing.
			if (!string.IsNullOrEmpty(RawStringPropertyName))
				Extras?.AddSourceExtra(RawStringPropertyName, rawStr);

			if (!string.IsNullOrEmpty(rawStr))
			{
				var fractionEntries = value.Params[1].Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var fractionEntry in fractionEntries)
				{
					var fractionParts = fractionEntry.Split('=');
					if (fractionParts.Length != 3)
					{
						Logger?.Warn($"{PropertyName}: Malformed Fraction entry '{fractionEntry}'. This value will be ignored.");
						continue;
					}

					if (!double.TryParse(fractionParts[0], out var time))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{fractionParts[0]}'. Expected double. This value will be ignored.");
						continue;
					}

					var numeratorString = fractionParts[1];
					var denominatorString = fractionParts[2];
					if (denominatorString.IndexOf(';') >= 0)
					{
						denominatorString = denominatorString.Substring(0, denominatorString.IndexOf(';'));
					}

					if (!int.TryParse(numeratorString, out var numerator) || numerator <= 0)
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{numeratorString}'. Expected positive integer. This value will be ignored.");
						continue;
					}
					if (!int.TryParse(denominatorString, out var denominator) || denominator <= 0)
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{denominatorString}'. Expected positive integer. This value will be ignored.");
						continue;
					}
					
					Values[time] = new Fraction(numerator, denominator);
				}
			}

			Extras?.AddSourceExtra(PropertyName, Values);

			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter that is a string of comma-separated times to values.
	/// Parses these into a Dictionary of doubles to values of type T.
	/// Will also store the raw value on the Extras if given raw string property name.
	/// Example:
	/// #BPMS:0.000=175.000,100.000=125.000;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CSVListAtTimePropertyParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Dictionary<double, T> Values;
		private readonly Extras Extras;
		private readonly string RawStringPropertyName;

		public CSVListAtTimePropertyParser(
			string smPropertyName,
			Dictionary<double, T> values,
			Extras extras = null,
			string rawStringPropertyName = null)
			: base(smPropertyName)
		{
			Values = values;
			Extras = extras;
			RawStringPropertyName = rawStringPropertyName;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var rawStr))
				return false;

			// Record the raw string to preserve formatting when writing.
			if (!string.IsNullOrEmpty(RawStringPropertyName))
				Extras?.AddSourceExtra(RawStringPropertyName, rawStr);

			if (!string.IsNullOrEmpty(rawStr))
			{
				var pairs = value.Params[1].Trim().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var pair in pairs)
				{
					var kvp = pair.Split('=');
					if (kvp.Length != 2)
					{
						Logger?.Warn($"{PropertyName}: Malformed pair '{pair}'. This value will be ignored.");
						continue;
					}

					if (!double.TryParse(kvp[0], out var time))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[0]}'. Expected double. This value will be ignored.");
						continue;
					}

					var valueStr = kvp[1];
					if (valueStr.IndexOf(';') >= 0)
					{
						valueStr = valueStr.Substring(0, valueStr.IndexOf(';'));
					}

					T tValue;
					try
					{
						tValue = (T) Convert.ChangeType(valueStr, typeof(T));
					}
					catch (Exception)
					{
						Logger?.Warn($"{PropertyName}: Failed to Convert '{valueStr}' to type '{typeof(T)}'.");
						continue;
					}

					Values[time] = tValue;
				}
			}

			Extras?.AddSourceExtra(PropertyName, Values);

			return true;
		}
	}

	/// <summary>
	/// Parses an interpolated scroll change (a "speed" event) into a Tuple of values
	/// containing the speed, length, and length mode (0 = beats, 1 = seconds).
	/// Will also store the raw value on the Extras if given raw string property name.
	/// Example:
	/// #SPEEDS:0.000=1.000=0.000=0;
	/// </summary>
	public class ScrollRateInterpolationPropertyParser : PropertyParser
	{
		private readonly Dictionary<double, Tuple<double, double, int>> Values;
		private readonly Extras Extras;
		private readonly string RawStringPropertyName;

		public ScrollRateInterpolationPropertyParser(
			string smPropertyName,
			Dictionary<double, Tuple<double, double, int>> values,
			Extras extras = null,
			string rawStringPropertyName = null)
			: base(smPropertyName)
		{
			Values = values;
			Extras = extras;
			RawStringPropertyName = rawStringPropertyName;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var rawStr))
				return false;

			// Record the raw string to preserve formatting when writing.
			if (!string.IsNullOrEmpty(RawStringPropertyName))
				Extras?.AddSourceExtra(RawStringPropertyName, rawStr);

			if (!string.IsNullOrEmpty(rawStr))
			{
				var interpolationDatas = value.Params[1].Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var interpolationData in interpolationDatas)
				{
					var kvp = interpolationData.Split('=');
					if (kvp.Length != 4)
					{
						Logger?.Warn($"{PropertyName}: Malformed {SMCommon.TagSpeeds} '{interpolationData}'. This value will be ignored.");
						continue;
					}

					if (!double.TryParse(kvp[0], out var beat))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[0]}'. Expected double. This value will be ignored.");
						continue;
					}
					if (!double.TryParse(kvp[1], out var speed))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[1]}'. Expected double. This value will be ignored.");
						continue;
					}
					if (!double.TryParse(kvp[2], out var length))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[2]}'. Expected double. This value will be ignored.");
						continue;
					}
					if (!int.TryParse(kvp[3], out var mode))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[3]}'. Expected int. This value will be ignored.");
						continue;
					}

					Values[beat] = new Tuple<double, double, int>(speed, length, mode);
				}
			}

			Extras?.AddSourceExtra(PropertyName, Values);

			return true;
		}
	}

	/// <summary>
	/// Parses a combo event into a Dictionary of Tuples of values containing
	/// the hit multiplier and miss multiplier.
	/// Will also store the raw value on the Extras if given raw string property name.
	/// Example:
	/// #COMBOS:0.000=1,10.000=7=8,11.000=111=0;
	/// </summary>
	public class ComboPropertyParser : PropertyParser
	{
		private readonly Dictionary<double, Tuple<int, int>> Values;
		private readonly Extras Extras;
		private readonly string RawStringPropertyName;

		public ComboPropertyParser(
			string smPropertyName,
			Dictionary<double, Tuple<int, int>> values,
			Extras extras = null,
			string rawStringPropertyName = null)
			: base(smPropertyName)
		{
			Values = values;
			Extras = extras;
			RawStringPropertyName = rawStringPropertyName;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var rawStr))
				return false;

			// Record the raw string to preserve formatting when writing.
			if (!string.IsNullOrEmpty(RawStringPropertyName))
				Extras?.AddSourceExtra(RawStringPropertyName, rawStr);

			if (!string.IsNullOrEmpty(rawStr))
			{
				var comboDatas = value.Params[1].Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var comboData in comboDatas)
				{
					var kvp = comboData.Split('=');

					if (kvp.Length != 3 && kvp.Length != 2)
					{
						Logger?.Warn($"{PropertyName}: Malformed {SMCommon.TagCombos} '{comboData}'. This value will be ignored.");
						continue;
					}

					if (!double.TryParse(kvp[0], out var beat))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[0]}'. Expected double. This value will be ignored.");
						continue;
					}
					if (!int.TryParse(kvp[1], out var hitMultiplier))
					{
						Logger?.Warn($"{PropertyName}: Malformed value '{kvp[0]}'. Expected int. This value will be ignored.");
						continue;
					}
					var missMultiplier = hitMultiplier;
					
					// The third value is optional.
					if (kvp.Length == 3)
					{
						if (!int.TryParse(kvp[2], out missMultiplier))
						{
							Logger?.Warn($"{PropertyName}: Malformed value '{kvp[0]}'. Expected int. This value will be ignored.");
							continue;
						}
					}

					Values[beat] = new Tuple<int, int>(hitMultiplier, missMultiplier);
				}
			}

			Extras?.AddSourceExtra(PropertyName, Values);

			return true;
		}
	}

	/// <summary>
	/// Abstract PropertyParser with static helper to parse the sm/scc string representation
	/// if notes data into a Chart.
	/// </summary>
	public abstract class NotesPropertyParser : PropertyParser
	{
		public NotesPropertyParser(string smPropertyName) : base(smPropertyName) { }

		/// <summary>
		/// Parses the sm/ssc string representation of measures and notes as
		/// events into the given Chart.
		/// </summary>
		/// <param name="chart">Chart to parse notes into.</param>
		/// <param name="notesStr">String representation of notes from an sm or ssc chart.</param>
		/// <returns>Whether the notes represent a valid chart or not.</returns>
		protected bool ParseNotes(Chart chart, string notesStr)
		{
			if (chart.NumInputs < 1)
			{
				var loggedType = chart.Type;
				if (string.IsNullOrEmpty(loggedType))
					loggedType = "<unknown type>";

				Logger?.Warn(
					$"Cannot parse notes for {loggedType} {chart.DifficultyType} Chart. Unknown number of inputs. This Chart will be ignored.");
				return false;
			}

			var validChart = true;
			var player = 0;
			var measure = 0;
			var currentMeasureEvents = new List<Event>();
			notesStr = notesStr.Trim(SMCommon.SMAllWhiteSpace);
			var notesStrsPerPlayer = notesStr.Split('&');
			foreach (var notesStrForPlayer in notesStrsPerPlayer)
			{
				var holding = new bool[chart.NumInputs];
				var rolling = new bool[chart.NumInputs];

				// RemoveEmptyEntries seems wrong, but matches Stepmania parsing logic.
				var measures = notesStrForPlayer.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var measureStr in measures)
				{
					var lines = measureStr.Trim(SMCommon.SMAllWhiteSpace)
						.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
					var linesInMeasure = lines.Length;
					var lineInMeasure = 0;
					foreach (var line in lines)
					{
						var trimmedLine = line.Trim(SMCommon.SMAllWhiteSpace);

						// Parse this line as note data
						for (int charIndex = 0, laneIndex = 0;
							charIndex < trimmedLine.Length && laneIndex < chart.NumInputs;
							charIndex++, laneIndex++)
						{
							// Get the note type.
							var c = trimmedLine[charIndex];
							var noteType = SMCommon.NoteType.None;
							var noteString = SMCommon.NoteStrings[(int) noteType];
							for (var i = 0; i < SMCommon.NoteChars.Length; i++)
							{
								if (c == SMCommon.NoteChars[i])
								{
									noteType = (SMCommon.NoteType) i;
									noteString = SMCommon.NoteStrings[i];
									break;
								}
							}

							// Validation.
							if (noteType == SMCommon.NoteType.Tap
							    || noteType == SMCommon.NoteType.Mine
							    || noteType == SMCommon.NoteType.Lift
								|| noteType == SMCommon.NoteType.Fake
								|| noteType == SMCommon.NoteType.KeySound
								|| noteType == SMCommon.NoteType.HoldStart
								|| noteType == SMCommon.NoteType.RollStart)
							{
								if(holding[laneIndex])
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. {noteString} during hold on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}

								if (rolling[laneIndex])
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. {noteString} during roll on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}
							}
							else if (noteType == SMCommon.NoteType.HoldEnd)
							{
								if (!holding[laneIndex] && !rolling[laneIndex])
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. {noteString} while neither holding nor rolling on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}
							}
							if (!validChart)
								break;

							// Create a LaneNote based on the note type.
							LaneNote note = null;
							if (noteType == SMCommon.NoteType.Tap
							    || noteType == SMCommon.NoteType.Fake
							    || noteType == SMCommon.NoteType.Lift)
							{
								note = new LaneTapNote { SourceType = c.ToString() };
							}
							else if (noteType == SMCommon.NoteType.Mine
							         || noteType == SMCommon.NoteType.KeySound)
							{
								note = new LaneNote { SourceType = c.ToString() };
							}
							else if (noteType == SMCommon.NoteType.HoldStart)
							{
								holding[laneIndex] = true;
								note = new LaneHoldStartNote { SourceType = c.ToString() };
							}
							else if (noteType == SMCommon.NoteType.RollStart)
							{
								rolling[laneIndex] = true;
								note = new LaneHoldStartNote { SourceType = c.ToString() };
							}
							else if (noteType == SMCommon.NoteType.HoldEnd)
							{
								note = new LaneHoldEndNote {SourceType = c.ToString()};
								holding[laneIndex] = false;
								rolling[laneIndex] = false;
							}

							// Keysound parsing.
							// TODO: Parse keysounds properly. For now, putting them in SourceExtras.
							if (charIndex + 1 < trimmedLine.Length && trimmedLine[charIndex + 1] == '[')
							{
								var startIndex = charIndex + 1;
								while (charIndex < trimmedLine.Length)
								{
									if (trimmedLine[charIndex] == ']')
										break;
									charIndex++;
								}

								var endIndex = charIndex - 1;
								if (endIndex > startIndex && note != null)
								{
									if (int.TryParse(trimmedLine.Substring(startIndex, endIndex - startIndex),
										out var keySoundIndex))
									{
										note.Extras.AddSourceExtra(SMCommon.TagFumenKeySoundIndex, keySoundIndex, true);
									}
								}
							}

							// Deprecated Attack parsing
							if (charIndex + 1 < trimmedLine.Length && trimmedLine[charIndex + 1] == '{')
							{
								while (charIndex < trimmedLine.Length)
								{
									if (trimmedLine[charIndex] == '}')
										break;
									charIndex++;
								}
							}

							// Deprecated Item parsing
							if (charIndex + 1 < trimmedLine.Length && trimmedLine[charIndex + 1] == '<')
							{
								while (charIndex < trimmedLine.Length)
								{
									if (trimmedLine[charIndex] == '>')
										break;
									charIndex++;
								}
							}

							// No note at this position, continue.
							if (null == note)
								continue;

							// Configure common parameters on the note and add it.
							note.Lane = laneIndex;
							note.Player = player;
							note.IntegerPosition = (measure * SMCommon.NumBeatsPerMeasure * SMCommon.MaxValidDenominator)
								+ Convert.ToInt32(((double)(SMCommon.NumBeatsPerMeasure * SMCommon.MaxValidDenominator) / linesInMeasure) * lineInMeasure);
							note.Extras.AddSourceExtra(SMCommon.TagFumenNoteOriginalMeasurePosition, new Fraction(lineInMeasure, linesInMeasure));
							currentMeasureEvents.Add(note);
						}

						if (!validChart)
							break;

						// Advance line marker
						lineInMeasure++;
					}

					if (!validChart)
						break;

					chart.Layers[0].Events.AddRange(currentMeasureEvents);
					currentMeasureEvents.Clear();
					measure++;
				}

				// Validation.
				for (var i = 0; i < chart.NumInputs; i++)
				{
					if (holding[i])
					{
						Logger?.Error(
							$"Invalid {chart.Type} {chart.DifficultyType} Chart. Incomplete hold on lane {i}. This Chart will be ignored.");
						validChart = false;
					}
					if (rolling[i])
					{
						Logger?.Error(
							$"Invalid {chart.Type} {chart.DifficultyType} Chart. Incomplete roll on lane {i}. This Chart will be ignored.");
						validChart = false;
					}
				}

				if (!validChart)
					break;

				player++;
			}

			return validChart;
		}
	}

	/// <summary>
	/// Parses a NOTES property and adds a new Chart to the Song to hold the notes.
	/// Used for SM Charts where all the Chart data is in one value/param set.
	/// </summary>
	public class SongNotesPropertyParser : NotesPropertyParser
	{
		private readonly Song Song;

		public SongNotesPropertyParser(string smPropertyName, Song song)
			: base(smPropertyName)
		{
			Song = song;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!DoesValueMatchProperty(value))
				return false;

			if (value.Params.Count < 7)
			{
				Logger?.Warn($"{PropertyName}: Expected at least 7 parameters. Found {value.Params.Count}. Ignoring all note data.");
				return true;
			}

			var chart = new Chart
			{
				Type = value.Params[1]?.Trim(SMCommon.SMAllWhiteSpace) ?? "",
				Description = value.Params[2]?.Trim(SMCommon.SMAllWhiteSpace) ?? "",
				DifficultyType = value.Params[3]?.Trim(SMCommon.SMAllWhiteSpace) ?? ""
			};
			chart.Layers.Add(new Layer());

			// Record whether this chart was written under NOTES or NOTES2.
			chart.Extras.AddSourceExtra(SMCommon.TagFumenNotesType, PropertyName, true);

			// Parse the chart information before measure data.
			var chartDifficultyRatingStr = value.Params[4]?.Trim(SMCommon.SMAllWhiteSpace) ?? "";
			var chartRadarValuesStr = value.Params[5]?.Trim(SMCommon.SMAllWhiteSpace) ?? "";

			// Parse the difficulty rating as a number.
			if (int.TryParse(chartDifficultyRatingStr, out var difficultyRatingInt))
				chart.DifficultyRating = (double)difficultyRatingInt;

			// Parse the radar values into a list.
			var radarValues = new List<double>();
			var radarValuesStr = chartRadarValuesStr.Split(',');
			foreach (var radarValueStr in radarValuesStr)
			{
				if (double.TryParse(radarValueStr, out var d))
				{
					radarValues.Add(d);
				}
			}
			chart.Extras.AddSourceExtra(SMCommon.TagRadarValues, radarValues, true);

			// Parse chart type and set number of players and inputs.
			if (!SMCommon.TryGetChartType(chart.Type, out var smChartType))
			{
				Logger?.Error($"{PropertyName}: Failed to parse {SMCommon.TagStepsType} value '{chart.Type}'. This chart will be ignored.");
				return true;
			}
			chart.NumPlayers = SMCommon.Properties[(int)smChartType].NumPlayers;
			chart.NumInputs = SMCommon.Properties[(int)smChartType].NumInputs;

			// Parse the notes.
			if (!ParseNotes(chart, value.Params[6] ?? ""))
				return true;

			Song.Charts.Add(chart);

			return true;
		}
	}

	/// <summary>
	/// Parses a NOTES property for the given Chart.
	/// Used for SSC Charts where Charts have many values, one of which is NOTES.
	/// </summary>
	public class ChartNotesPropertyParser : NotesPropertyParser
	{
		private readonly Chart Chart;

		public ChartNotesPropertyParser(string smPropertyName, Chart chart)
			: base(smPropertyName)
		{
			Chart = chart;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var notesStr))
				return false;

			// Record whether this chart was written under NOTES or NOTES2.
			Chart.Extras.AddSourceExtra(SMCommon.TagFumenNotesType, PropertyName, true);

			// Parse the notes.
			if (!ParseNotes(Chart, notesStr))
				Chart.Type = null;

			return true;
		}
	}

	public class ChartTypePropertyParser : PropertyParser
	{
		private readonly Chart Chart;

		public ChartTypePropertyParser(Chart chart)
			: base(SMCommon.TagStepsType)
		{
			Chart = chart;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var type))
				return false;

			Chart.Type = type;

			// Parse chart type and set number of players and inputs.
			if (!SMCommon.TryGetChartType(Chart.Type, out var smChartType))
			{
				Logger?.Error($"{PropertyName}: Failed to parse {SMCommon.TagStepsType} value '{Chart.Type}'. This chart will be ignored.");
				Chart.Type = null;
				return true;
			}
			Chart.NumPlayers = SMCommon.Properties[(int)smChartType].NumPlayers;
			Chart.NumInputs = SMCommon.Properties[(int)smChartType].NumInputs;

			Chart.Extras.AddSourceExtra(SMCommon.TagStepsType, Chart.Type, true);

			return true;
		}
	}

}
