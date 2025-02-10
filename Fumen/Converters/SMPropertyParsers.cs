using System;
using System.Collections.Generic;
using System.Reflection;
using Fumen.ChartDefinition;
using static Fumen.Converters.SMCommon;

namespace Fumen.Converters;

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
	/// the Params if it does.
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
		Extras.AddSourceExtra(value.Params[0], value.Params[1], true);
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
			Logger?.Error(
				$"{PropertyName}: Could not find public instance writable property for Song property '{SongPropertyName}'.");
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
			Logger?.Warn(
				$"{PropertyName}: Failed to Convert '{songValueStr}' to type '{prop.PropertyType}' for Song property '{SongPropertyName}'.");
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
			Logger?.Error(
				$"{PropertyName}: Could not find public instance writable property for Chart property '{ChartPropertyName}'.");
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
			Logger?.Warn(
				$"{PropertyName}: Failed to Convert '{chartValueStr}' to type '{prop.PropertyType}' for Chart property '{ChartPropertyName}'.");
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

		var parsedList = new List<T>();
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
			var fractionEntries = value.Params[1].Trim().Split((char[]) [','], StringSplitOptions.RemoveEmptyEntries);
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
					Logger?.Warn(
						$"{PropertyName}: Malformed value '{fractionParts[0]}'. Expected double. This value will be ignored.");
					continue;
				}

				var numeratorString = fractionParts[1];
				var denominatorString = fractionParts[2];
				if (denominatorString.IndexOf(';') >= 0)
					denominatorString = denominatorString.Substring(0, denominatorString.IndexOf(';'));

				if (!int.TryParse(numeratorString, out var numerator) || numerator <= 0)
				{
					Logger?.Warn(
						$"{PropertyName}: Malformed value '{numeratorString}'. Expected positive integer. This value will be ignored.");
					continue;
				}

				if (!int.TryParse(denominatorString, out var denominator) || denominator <= 0)
				{
					Logger?.Warn(
						$"{PropertyName}: Malformed value '{denominatorString}'. Expected positive integer. This value will be ignored.");
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
			var pairs = value.Params[1].Trim().Split((char[]) [','], StringSplitOptions.RemoveEmptyEntries);
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
			var interpolationDatas = value.Params[1].Trim().Split((char[]) [','], StringSplitOptions.RemoveEmptyEntries);
			foreach (var interpolationData in interpolationDatas)
			{
				var kvp = interpolationData.Split('=');
				if (kvp.Length != 4)
				{
					Logger?.Warn(
						$"{PropertyName}: Malformed {TagSpeeds} '{interpolationData}'. This value will be ignored.");
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
			var comboDatas = value.Params[1].Trim().Split((char[]) [','], StringSplitOptions.RemoveEmptyEntries);
			foreach (var comboData in comboDatas)
			{
				var kvp = comboData.Split('=');

				if (kvp.Length != 3 && kvp.Length != 2)
				{
					Logger?.Warn(
						$"{PropertyName}: Malformed {TagCombos} '{comboData}'. This value will be ignored.");
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
						Logger?.Warn(
							$"{PropertyName}: Malformed value '{kvp[0]}'. Expected int. This value will be ignored.");
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
	protected NotesPropertyParser(string smPropertyName) : base(smPropertyName)
	{
	}

	/// <summary>
	/// Parses the sm/ssc string representation of measures and notes as
	/// events into the given Chart.
	/// </summary>
	/// <param name="chart">Chart to parse notes into.</param>
	/// <param name="properties">ChartProperties for the given Chart.</param>
	/// <param name="notesStr">String representation of notes from an sm or ssc chart.</param>
	/// <returns>Whether the notes represent a valid chart or not.</returns>
	protected bool ParseNotes(Chart chart, ChartProperties properties, string notesStr)
	{
		return ParseNotes(chart, properties, notesStr, false);
	}

	/// <summary>
	/// Parses the sm/ssc string representation of measures and notes as
	/// events into the given Chart.
	/// </summary>
	/// <param name="chart">Chart to parse notes into.</param>
	/// <param name="properties">ChartProperties for the given Chart.</param>
	/// <param name="notesStr">String representation of notes from an sm or ssc chart.</param>
	/// <param name="isStepF2CoopChart">
	/// If true then we know for certain this is a StepF2 style co-op chart where some symbols
	/// need to re-interpreted (namely 1 and 2 going from Tap and HoldEnd to P4 Tap and P4 Hold End).
	/// </param>
	/// <returns>Whether the notes represent a valid chart or not.</returns>
	private bool ParseNotes(Chart chart, ChartProperties properties, string notesStr, bool isStepF2CoopChart)
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

		var isPumpChart = TryGetChartType(chart.Type, out var smChartType) && IsPumpType(smChartType);
		var isPumpDoublesChart = isPumpChart && smChartType == ChartType.pump_double;
		var isPumpRoutineChart = isPumpChart && smChartType == ChartType.pump_routine;
		var validChart = true;
		var player = 0;
		var currentMeasureEvents = new List<Event>();
		notesStr = notesStr.Trim(SMAllWhiteSpace);
		var notesStringsPerPlayer = notesStr.Split('&');

		// Validate the number of players.
		if (properties.GetSupportsVariableNumberOfPlayers())
		{
			chart.NumPlayers = Math.Max(chart.NumPlayers, notesStringsPerPlayer.Length);
		}
		else
		{
			if (notesStringsPerPlayer.Length > chart.NumPlayers)
			{
				Logger?.Error(
					$"Invalid {chart.Type} {chart.DifficultyType} Chart. Expected steps for at most {chart.NumPlayers} Players but found steps for {notesStringsPerPlayer.Length} Players. This Chart will be ignored.");
				return false;
			}
		}

		var specialNoteFlags = new Dictionary<string, object>();

		foreach (var notesStrForPlayer in notesStringsPerPlayer)
		{
			var measure = 0;
			var activeHolds = new LaneHoldStartNote[chart.NumInputs];

			// RemoveEmptyEntries seems wrong, but matches Stepmania parsing logic.
			var measures = notesStrForPlayer.Split(',', StringSplitOptions.RemoveEmptyEntries);
			foreach (var measureStr in measures)
			{
				var lines = measureStr.Trim(SMAllWhiteSpace)
					.Split('\n', StringSplitOptions.RemoveEmptyEntries);
				var linesInMeasure = lines.Length;
				var lineInMeasure = 0;
				foreach (var line in lines)
				{
					var trimmedLine = line.Trim(SMAllWhiteSpace);

					// Parse this line as note data
					for (int charIndex = 0, laneIndex = 0;
					     charIndex < trimmedLine.Length && laneIndex < chart.NumInputs;
					     charIndex++, laneIndex++)
					{
						var c = trimmedLine[charIndex];
						specialNoteFlags.Clear();
						int? playerOverride = null;

						// Check for special StepF2 note types.
						if (isPumpChart)
						{
							// Notes in StepF2 can optionally be a compound string instead of a character.
							// Compound notes look like {<note>|<attribute>|<fake>|<reserved>} where:
							// <note> is a character denoting the step type.
							// <attribute> is a character denoting special attributes.
							// <fake> is a character denoting a fake note.
							// <reserved> is a reserved character,
							var compound = c == StepF2CompoundNoteStartMarker;
							if (compound)
							{
								if (charIndex + 1 < trimmedLine.Length)
									charIndex++;
							}

							c = trimmedLine[charIndex];
							if (isStepF2CoopChart)
							{
								if (c == StepF2NoteChars[(int)StepF2NoteType.P1Tap])
								{
									playerOverride = 0;
									c = NoteChars[(int)NoteType.Tap];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P1HoldStart])
								{
									playerOverride = 0;
									c = NoteChars[(int)NoteType.HoldStart];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P2Tap])
								{
									playerOverride = 1;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 2);
									c = NoteChars[(int)NoteType.Tap];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P2HoldStart])
								{
									playerOverride = 1;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 2);
									c = NoteChars[(int)NoteType.HoldStart];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P3Tap])
								{
									playerOverride = 2;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 3);
									c = NoteChars[(int)NoteType.Tap];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P3HoldStart])
								{
									playerOverride = 2;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 3);
									c = NoteChars[(int)NoteType.HoldStart];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P4Tap])
								{
									playerOverride = 3;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 4);
									c = NoteChars[(int)NoteType.Tap];
								}
								else if (c == StepF2NoteChars[(int)StepF2NoteType.P4HoldStart])
								{
									playerOverride = 3;
									chart.NumPlayers = Math.Max(chart.NumPlayers, 4);
									c = NoteChars[(int)NoteType.HoldStart];
								}
							}

							// Handle special note types.
							if (c == StepF2NoteChars[(int)StepF2NoteType.Sudden])
							{
								specialNoteFlags[TagFumenStepF2Sudden] = true;
								c = NoteChars[(int)NoteType.Tap];
							}
							else if (c == StepF2NoteChars[(int)StepF2NoteType.Vanish])
							{
								specialNoteFlags[TagFumenStepF2Vanish] = true;
								c = NoteChars[(int)NoteType.Tap];
							}
							else if (c == StepF2NoteChars[(int)StepF2NoteType.Hidden])
							{
								specialNoteFlags[TagFumenStepF2Hidden] = true;
								c = NoteChars[(int)NoteType.Tap];
							}

							if (compound)
							{
								// Advance past the | mark to the attribute.
								if (charIndex + 2 < trimmedLine.Length)
									charIndex += 2;
								if (trimmedLine[charIndex] == StepF2AttributeChars[(int)StepF2AttributeType.Sudden])
									specialNoteFlags[TagFumenStepF2Sudden] = true;
								if (trimmedLine[charIndex] == StepF2AttributeChars[(int)StepF2AttributeType.Vanish])
									specialNoteFlags[TagFumenStepF2Vanish] = true;
								if (trimmedLine[charIndex] == StepF2AttributeChars[(int)StepF2AttributeType.Hidden])
									specialNoteFlags[TagFumenStepF2Hidden] = true;

								// Advance past the next | mark to the fake flag.
								if (charIndex + 2 < trimmedLine.Length)
									charIndex += 2;
								if (trimmedLine[charIndex] == StepF2CompoundNoteFakeMarker)
								{
									specialNoteFlags[TagFumenStepF2Fake] = true;
									if (c == NoteChars[(int)NoteType.Tap])
										c = NoteChars[(int)NoteType.Fake];
								}

								// Advance past the next | mark and the reserved flag.
								if (charIndex + 3 < trimmedLine.Length)
									charIndex += 3;
								if (trimmedLine[charIndex] != StepF2CompoundNoteEndMarker)
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. Malformed StepF2 compound note on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}
							}

							if (!validChart)
								break;
						}

						// Get the note type.
						var noteType = NoteType.None;
						var noteString = NotePrettyStrings[(int)noteType];
						for (var i = 0; i < NoteChars.Length; i++)
						{
							if (c == NoteChars[i])
							{
								noteType = (NoteType)i;
								noteString = NotePrettyStrings[i];
								break;
							}
						}

						// Check for special StepF2 notes
						if (noteType == NoteType.None && isPumpChart)
						{
							// Check for characters which indicate this is a StepF2 co-op chart.
							if ((isPumpDoublesChart || isPumpRoutineChart) && !isStepF2CoopChart)
							{
								if (IsCharExclusiveToStepF2CoopChart(c))
								{
									// We need to reparse the chart starting from the beginning.
									// StepF2 interprets some characters differently depending on if it is
									// a co-op chart or not, like '1' being a P4 tap in co-op but a P1 tap
									// in non-co-op.
									if (chart.Type != "pump-routine")
									{
										Logger.Warn(
											$"{chart.Type} chart uses StepF2 notation for multiple players but it is not a pump-routine chart." +
											" Changing the chart type to pump-routine.");
										chart.Type = "pump-routine";
									}

									return ParseNotes(chart, properties, notesStr, true);
								}
							}
						}

						// Validation.
						switch (noteType)
						{
							case NoteType.Tap:
							case NoteType.Mine:
							case NoteType.Lift:
							case NoteType.Fake:
							case NoteType.KeySound:
							case NoteType.HoldStart:
							case NoteType.RollStart:
							{
								if (activeHolds[laneIndex] != null)
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. {noteString} during hold or roll on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}

								break;
							}
							case NoteType.HoldEnd:
							{
								if (activeHolds[laneIndex] == null)
								{
									Logger?.Error(
										$"Invalid {chart.Type} {chart.DifficultyType} Chart. {noteString} while neither holding nor rolling on lane {laneIndex} during measure {measure}. This Chart will be ignored.");
									validChart = false;
								}

								break;
							}
						}

						if (!validChart)
							break;

						// Create a LaneNote based on the note type.
						LaneNote note = null;
						switch (noteType)
						{
							case NoteType.Tap:
							case NoteType.Fake:
							case NoteType.Lift:
								note = new LaneTapNote { SourceType = c.ToString() };
								break;
							case NoteType.Mine:
							case NoteType.KeySound:
								note = new LaneNote { SourceType = c.ToString() };
								break;
							case NoteType.HoldStart:
							case NoteType.RollStart:
								activeHolds[laneIndex] = new LaneHoldStartNote { SourceType = c.ToString() };
								note = activeHolds[laneIndex];
								break;
							case NoteType.HoldEnd:
								note = new LaneHoldEndNote { SourceType = c.ToString() };
								playerOverride = activeHolds[laneIndex]?.Player ?? player;
								activeHolds[laneIndex] = null;
								break;
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
									note.Extras.AddSourceExtra(TagFumenKeySoundIndex, keySoundIndex, true);
								}
							}
						}

						// Deprecated Attack parsing
						if (!isPumpChart)
						{
							if (charIndex + 1 < trimmedLine.Length && trimmedLine[charIndex + 1] == '{')
							{
								while (charIndex < trimmedLine.Length)
								{
									if (trimmedLine[charIndex] == '}')
										break;
									charIndex++;
								}
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

						// Add any special flags accumulated above.
						foreach (var kvp in specialNoteFlags)
							note.Extras.AddSourceExtra(kvp.Key, kvp.Value);

						// Configure common parameters on the note and add it.
						note.Lane = laneIndex;
						note.Player = playerOverride ?? player;
						note.IntegerPosition = measure * NumBeatsPerMeasure * MaxValidDenominator
						                       + Convert.ToInt32(
							                       (double)(NumBeatsPerMeasure * MaxValidDenominator) /
							                       linesInMeasure * lineInMeasure);
						note.Extras.AddSourceExtra(TagFumenNoteOriginalMeasurePosition,
							new Fraction(lineInMeasure, linesInMeasure));
						currentMeasureEvents.Add(note);
					}

					if (!validChart)
						break;

					// Advance line marker.
					lineInMeasure++;
				}

				if (!validChart)
					break;

				chart.Layers[0].Events.AddRange(currentMeasureEvents);
				currentMeasureEvents.Clear();
				measure++;
			}

			if (!validChart)
				break;

			// Validation.
			for (var i = 0; i < chart.NumInputs; i++)
			{
				if (activeHolds[i] != null)
				{
					Logger?.Error(
						$"Invalid {chart.Type} {chart.DifficultyType} Chart. Incomplete hold or roll on lane {i}. This Chart will be ignored.");
					validChart = false;
				}
			}

			if (!validChart)
				break;

			player++;
		}

		// Multiplayer chart validation.
		if (notesStringsPerPlayer.Length > 1)
		{
			// Copy all the notes for all players, sort them together, and walk the sorted list looking for conflicts.
			// This could potentially be optimized but multiplayer charts are extremely rare.
			var lastEventPerLane = new LaneNote[chart.NumInputs];

			var sortedEvents = new List<Event>(chart.Layers[0].Events);
			sortedEvents.Sort(new SMEventComparer());

			foreach (var chartEvent in sortedEvents)
			{
				if (chartEvent is LaneNote ln)
				{
					var previousEvent = lastEventPerLane[ln.Lane];
					if (previousEvent != null)
					{
						if (previousEvent.IntegerPosition == ln.IntegerPosition
						    || (previousEvent is LaneHoldStartNote && ln is not LaneHoldEndNote))
						{
							Logger?.Error(
								$"Invalid {chart.Type} {chart.DifficultyType} Chart. Notes for Player {ln.Player} and {previousEvent.Player} overlap in lane {ln.Lane} at row {ln.IntegerPosition}. This Chart will be ignored.");
							validChart = false;
							break;
						}
					}

					lastEventPerLane[ln.Lane] = ln;
				}
			}
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
	private new readonly bool ParseNotes;

	public SongNotesPropertyParser(string smPropertyName, Song song, bool parseNotes)
		: base(smPropertyName)
	{
		Song = song;
		ParseNotes = parseNotes;
	}

	public override bool Parse(MSDFile.Value value)
	{
		// Only consider this line if it matches this property name.
		if (!DoesValueMatchProperty(value))
			return false;

		if (value.Params.Count < 7)
		{
			Logger?.Warn(
				$"{PropertyName}: Expected at least 7 parameters. Found {value.Params.Count}. Ignoring all note data.");
			return true;
		}

		var chart = new Chart
		{
			Type = value.Params[1]?.Trim(SMAllWhiteSpace) ?? "",
			Description = value.Params[2]?.Trim(SMAllWhiteSpace) ?? "",
			DifficultyType = value.Params[3]?.Trim(SMAllWhiteSpace) ?? "",
		};
		chart.Layers.Add(new Layer());

		// Record whether this chart was written under NOTES or NOTES2.
		chart.Extras.AddSourceExtra(TagFumenNotesType, PropertyName, true);

		// Parse the chart information before measure data.
		var chartDifficultyRatingStr = value.Params[4]?.Trim(SMAllWhiteSpace) ?? "";
		var chartRadarValuesStr = value.Params[5]?.Trim(SMAllWhiteSpace) ?? "";

		// Parse the difficulty rating as a number.
		if (int.TryParse(chartDifficultyRatingStr, out var difficultyRatingInt))
			chart.DifficultyRating = difficultyRatingInt;

		// Parse the radar values into a list.
		var radarValues = new List<double>();
		var radarValuesStr = chartRadarValuesStr.Split(',');
		foreach (var radarValueStr in radarValuesStr)
		{
			if (double.TryParse(radarValueStr, out var d))
				radarValues.Add(d);
		}

		chart.Extras.AddSourceExtra(TagRadarValues, radarValues, true);

		// Parse chart type and set number of players and inputs.
		if (!TryGetChartType(chart.Type, out var smChartType))
		{
			Logger?.Error(
				$"{PropertyName}: Failed to parse {TagStepsType} value '{chart.Type}'. This chart will be ignored.");
			return true;
		}

		var chartProperties = GetChartProperties(smChartType);
		chart.NumPlayers = chartProperties.GetNumPlayers();
		chart.NumInputs = chartProperties.GetNumInputs();

		// Parse the notes.
		if (ParseNotes)
		{
			if (!ParseNotes(chart, chartProperties, value.Params[6] ?? ""))
				return true;
		}

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
		Chart.Extras.AddSourceExtra(TagFumenNotesType, PropertyName, true);

		var properties = GetChartProperties(Chart.Type);
		if (properties == null)
		{
			Logger?.Error(
				$"{PropertyName}: Failed to parse {TagFumenNotesType}. {TagStepsType} must be set to a type with a known number of players. Could not determine player count for '{Chart.Type}'. This chart will be ignored.");
			return true;
		}

		if (!ParseNotes(Chart, properties, notesStr))
			Chart.Type = null;

		return true;
	}
}

public class ChartTypePropertyParser : PropertyParser
{
	private readonly Chart Chart;

	public ChartTypePropertyParser(Chart chart)
		: base(TagStepsType)
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
		if (!TryGetChartType(Chart.Type, out var smChartType))
		{
			Logger?.Error(
				$"{PropertyName}: Failed to parse {TagStepsType} value '{Chart.Type}'. This chart will be ignored.");
			Chart.Type = null;
			return true;
		}

		var chartProperties = GetChartProperties(smChartType);
		Chart.NumPlayers = chartProperties.GetNumPlayers();
		Chart.NumInputs = chartProperties.GetNumInputs();

		Chart.Extras.AddSourceExtra(TagStepsType, Chart.Type, true);

		return true;
	}
}
