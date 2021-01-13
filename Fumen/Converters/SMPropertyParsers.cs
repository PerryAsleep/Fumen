using System;
using System.Collections.Generic;
using System.Reflection;

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
	/// Parses a value with one parameter directly into the property specified for the Song.
	/// Example:
	/// #ARTIST:Usao;
	/// </summary>
	public class PropertyToSongPropertyParser : PropertyParser
	{
		private readonly string SongPropertyName;
		private readonly Song Song;
		private readonly bool AddToSourceExtras = false;

		public PropertyToSongPropertyParser(string smPropertyName, string songPropertyName, Song song, bool addToSourceExtras = false)
			: base(smPropertyName)
		{
			SongPropertyName = songPropertyName;
			Song = song;
			AddToSourceExtras = addToSourceExtras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var songValueStr))
				return false;

			if (AddToSourceExtras)
				Song.SourceExtras.Add(PropertyName, songValueStr);

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
			if (AddToSourceExtras)
				Song.SourceExtras[PropertyName] = o;

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
		private readonly bool AddToSourceExtras = false;

		public PropertyToChartPropertyParser(string smPropertyName, string chartPropertyName, Chart chart, bool addToSourceExtras = false)
			: base(smPropertyName)
		{
			ChartPropertyName = chartPropertyName;
			Chart = chart;
			AddToSourceExtras = addToSourceExtras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var chartValueStr))
				return false;

			if (AddToSourceExtras)
				Chart.SourceExtras.Add(PropertyName, chartValueStr);

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
			if (AddToSourceExtras)
				Chart.SourceExtras[PropertyName] = o;

			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter into the SourceExtras as the type T.
	/// Examples:
	/// #SELECTABLE:YES;
	/// #VERSION:0.83;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class PropertyToSourceExtrasParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Dictionary<string, object> SourceExtras;

		public PropertyToSourceExtrasParser(string smPropertyName, Dictionary<string, object> sourceExtras)
			: base(smPropertyName)
		{
			SourceExtras = sourceExtras;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!ParseFirstParameter(value, out var valueStr))
				return false;

			T tValue;
			try
			{
				tValue = (T)Convert.ChangeType(valueStr, typeof(T));
			}
			catch (Exception)
			{
				Logger?.Warn($"{PropertyName}: Failed to Convert '{valueStr}' to type '{typeof(T)}'.");
				return true;
			}

			SourceExtras[PropertyName] = tValue;
			return true;
		}
	}

	/// <summary>
	/// Parses a value with multiple parameters separated by the MSDFile ParamMarker
	/// into a List of values of type T and stores it on the SourceExtras.
	/// It is important to use a list instead of a raw string when parameters are
	/// separated by the MSDFile ParamMarker so we do not escape them when writing
	/// back out. We need to understand that they are separate values.
	/// Example:
	/// #DISPLAYBPM:150.000:170.000;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ListPropertyToSourceExtrasParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Dictionary<string, object> SourceExtras;

		public ListPropertyToSourceExtrasParser(string smPropertyName, Dictionary<string, object> sourceExtras)
			: base(smPropertyName)
		{
			SourceExtras = sourceExtras;
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

			SourceExtras[PropertyName] = parsedList;
			return true;
		}
	}

	/// <summary>
	/// Parses a value with one parameter that is a string of comma-separated times to values.
	/// Parses these into a Dictionary of doubles to values of type T.
	/// Will also store the raw value on the SourceExtras if given raw string property name.
	/// Example:
	/// #BPMS:0.000=175.000,100.000=125.000;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CSVListAtTimePropertyParser<T> : PropertyParser where T : IConvertible
	{
		private readonly Dictionary<double, T> Values;
		private readonly Dictionary<string, object> SourceExtras;
		private readonly string RawStringPropertyName;

		public CSVListAtTimePropertyParser(
			string smPropertyName,
			Dictionary<double, T> values,
			Dictionary<string, object> sourceExtras = null,
			string rawStringPropertyName = null)
			: base(smPropertyName)
		{
			Values = values;
			SourceExtras = sourceExtras;
			RawStringPropertyName = rawStringPropertyName;
		}

		public override bool Parse(MSDFile.Value value)
		{
			// Only consider this line if it matches this property name.
			if (!DoesValueMatchProperty(value))
				return false;

			if (value.Params.Count < 2 || string.IsNullOrEmpty(value.Params[1]))
				return true;

			// Record the raw string to preserve formatting when writing.
			if (!string.IsNullOrEmpty(RawStringPropertyName) && SourceExtras != null)
			{
				SourceExtras.Add(RawStringPropertyName, value.Params[1]);
			}

			var pairs = value.Params[1].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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
					tValue = (T)Convert.ChangeType(valueStr, typeof(T));
				}
				catch (Exception)
				{
					Logger?.Warn($"{PropertyName}: Failed to Convert '{valueStr}' to type '{typeof(T)}'.");
					continue;
				}

				Values[time] = tValue;
			}

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
		protected static void ParseNotes(Chart chart, string notesStr)
		{
			// Parse measure data.
			var player = 0;
			var measure = 0;
			var currentMeasureEvents = new List<Event>();
			notesStr = notesStr.Trim(SMCommon.SMAllWhiteSpace);
			var notesStrsPerPlayer = notesStr.Split('&');
			foreach (var notesStrForPlayer in notesStrsPerPlayer)
			{
				// RemoveEmptyEntries seems wrong, but matches Stepmania parsing logic.
				var measures = notesStrForPlayer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var measureStr in measures)
				{
					var lines = measureStr.Trim(SMCommon.SMAllWhiteSpace).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
					var linesInMeasure = lines.Length;
					var lineInMeasure = 0;
					var linesPerBeat = linesInMeasure / SMCommon.NumBeatsPerMeasure;
					foreach (var line in lines)
					{
						var trimmedLine = line.Trim(SMCommon.SMAllWhiteSpace);

						// Parse this line as note data
						for (int charIndex = 0, laneIndex = 0;
							charIndex < trimmedLine.Length && laneIndex < chart.NumInputs;
							charIndex++, laneIndex++)
						{
							// Check the character at this index to see if it corresponds to a note.
							var c = trimmedLine[charIndex];
							LaneNote note = null;
							if (c == SMCommon.SNoteChars[(int)SMCommon.NoteType.Tap])
								note = new LaneTapNote { SourceType = c.ToString() };
							else if (c == SMCommon.SNoteChars[(int)SMCommon.NoteType.Mine]
										|| c == SMCommon.SNoteChars[(int)SMCommon.NoteType.Lift]
										|| c == SMCommon.SNoteChars[(int)SMCommon.NoteType.Fake]
										|| c == SMCommon.SNoteChars[(int)SMCommon.NoteType.KeySound])
								note = new LaneNote { SourceType = c.ToString() };
							else if (c == SMCommon.SNoteChars[(int)SMCommon.NoteType.HoldStart]
										|| c == SMCommon.SNoteChars[(int)SMCommon.NoteType.RollStart])
								note = new LaneHoldStartNote { SourceType = c.ToString() };
							else if (c == SMCommon.SNoteChars[(int)SMCommon.NoteType.HoldEnd])
								note = new LaneHoldEndNote { SourceType = c.ToString() };

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
									if (int.TryParse(trimmedLine.Substring(startIndex, endIndex - startIndex), out var keySoundIndex))
									{
										note.SourceExtras.Add(SMCommon.TagFumenKeySoundIndex, keySoundIndex);
									}
								}
							}

							// Deprecated Attack parsing
							if(charIndex + 1 < trimmedLine.Length && trimmedLine[charIndex + 1] == '{')
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
							var beat = lineInMeasure / linesPerBeat;
							note.Lane = laneIndex;
							note.Player = player;
							note.Position = new MetricPosition
							{
								Measure = measure,
								Beat = beat,
								SubDivision = new Fraction(lineInMeasure - beat * linesPerBeat, linesPerBeat)
							};

							currentMeasureEvents.Add(note);
						}

						// Advance line marker
						lineInMeasure++;
					}

					chart.Layers[0].Events.AddRange(currentMeasureEvents);
					currentMeasureEvents.Clear();
					measure++;
				}

				player++;
			}
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
				Logger?.Warn($"{PropertyName}: Expected at least 7 parameters Found {value.Params.Count}. Ignoring all note data.");
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
			chart.SourceExtras.Add(SMCommon.TagFumenNotesType, PropertyName);

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
			chart.SourceExtras[SMCommon.TagRadarValues] = radarValues;

			// Parse chart type and set number of players and inputs.
			if (!Enum.TryParse(chart.Type.Replace("-", "_"), out SMCommon.ChartType smChartType))
			{
				Logger?.Warn($"{PropertyName}: Failed to parse {SMCommon.TagStepsType} value '{smChartType}'. This chart will be ignored.");
				return true;
			}
			chart.NumPlayers = SMCommon.SChartProperties[(int)smChartType].NumPlayers;
			chart.NumInputs = SMCommon.SChartProperties[(int)smChartType].NumInputs;

			// Add a 4/4 time signature
			chart.Layers[0].Events.Add(new TimeSignature()
			{
				Position = new MetricPosition
				{
					Measure = 0,
					Beat = 0
				},
				Signature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure)
			});

			// Parse the notes.
			ParseNotes(chart, value.Params[6] ?? "");

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
			Chart.SourceExtras.Add(SMCommon.TagFumenNotesType, PropertyName);

			// Parse the notes.
			ParseNotes(Chart, notesStr);
			return true;
		}
	}
}
