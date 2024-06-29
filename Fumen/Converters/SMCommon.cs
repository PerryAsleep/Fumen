﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fumen.ChartDefinition;

namespace Fumen.Converters;

public static class SMCommon
{
	public const string DelayString = "Delay";
	public const string NegativeStopString = "NegativeStop";
	public const string NegativeDelayString = "NegativeDelay";

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	public enum ChartType
	{
		dance_single,
		dance_double,
		dance_couple,
		dance_solo,
		dance_threepanel,
		dance_routine,
		pump_single,
		pump_halfdouble,
		pump_double,
		pump_couple,
		pump_routine,
		kb7_single,
		ez2_single,
		ez2_double,
		ez2_real,
		para_single,
		ds3ddx_single,
		bm_single5,
		bm_versus5,
		bm_double5,
		bm_single7,
		bm_versus7,
		bm_double7,
		maniax_single,
		maniax_double,
		techno_single4,
		techno_single5,
		techno_single8,
		techno_double4,
		techno_double5,
		techno_double8,
		pnm_five,
		pnm_nine,
		lights_cabinet,
		kickbox_human,
		kickbox_quadarm,
		kickbox_insect,
		kickbox_arachnid,

		// These types are not officially supported in Stepmania.
		// If Stepmania or a popular fork ever adds support, these names may need to be adjusted.
		smx_beginner,
		smx_single,
		smx_dual,
		smx_full,
		smx_team,
	}

	private static readonly Dictionary<ChartType, string> ChartTypeStings;

	public static string ChartTypeString(ChartType type)
	{
		if (ChartTypeStings.TryGetValue(type, out var chartTypeString))
			return chartTypeString;
		return null;
	}

	public static bool TryGetChartType(string chartTypeString, out ChartType smChartType)
	{
		return Enum.TryParse(chartTypeString.Replace("-", "_"), out smChartType);
	}

	public static bool IsPumpType(ChartType chartType)
	{
		return chartType == ChartType.pump_single
		       || chartType == ChartType.pump_halfdouble
		       || chartType == ChartType.pump_double
		       || chartType == ChartType.pump_couple
		       || chartType == ChartType.pump_routine;
	}

	public static bool IsSmxType(ChartType chartType)
	{
		return chartType == ChartType.smx_beginner
		       || chartType == ChartType.smx_single
		       || chartType == ChartType.smx_dual
		       || chartType == ChartType.smx_full
		       || chartType == ChartType.smx_team;
	}

	public static bool IsDanceType(ChartType chartType)
	{
		return chartType == ChartType.dance_single
		       || chartType == ChartType.dance_double
		       || chartType == ChartType.dance_couple
		       || chartType == ChartType.dance_solo
		       || chartType == ChartType.dance_threepanel
		       || chartType == ChartType.dance_routine;
	}

	public enum ChartDifficultyType
	{
		Beginner,
		Easy,
		Medium,
		Hard,
		Challenge,
		Edit,
	}

	public enum NoteType
	{
		None,
		Tap,
		HoldStart,
		HoldEnd,
		RollStart,
		Mine,
		Lift,
		Fake,
		KeySound,
	}

	public class ChartProperties
	{
		private readonly int NumInputs;
		private readonly int NumPlayers;

		public ChartProperties(int numInputs, int numPlayers)
		{
			NumInputs = numInputs;
			NumPlayers = numPlayers;
		}

		public int GetNumInputs()
		{
			return NumInputs;
		}

		public int GetNumPlayers()
		{
			return NumPlayers;
		}
	}

	/// <summary>
	/// StepMania always uses 4/4 for representing notes in measures in sm and ssc files.
	/// Even if the time signature changes, notes are still represented in 4/4. Any time
	/// signature is just used for rendering.
	/// </summary>
	public const int NumBeatsPerMeasure = 4;

	/// <summary>
	/// StepMania uses a default tempo of 60 beats per minute.
	/// If a song has invalid tempos and one needs to be set automatically this value will be used.
	/// </summary>
	public const double DefaultTempo = 60.0;

	/// <summary>
	/// In StepMania's representation there are a maximum of 48 rows per beat.
	/// </summary>
	public const int MaxValidDenominator = 48;

	/// <summary>
	/// The number of rows in a StepMania 4/4 measure.
	/// </summary>
	public const int RowsPerMeasure = NumBeatsPerMeasure * MaxValidDenominator;

	/// <summary>
	/// In sm or ssc files notes can only subdivide beats by fractions with these
	/// denominators. For example, StepMania cannot handle a beat subdivided 24 times,
	/// though it can handle 16 and 48 times.
	/// </summary>
	public static readonly int[] ValidDenominators =
	{
		1, // Quarter note
		2, // Eighth note
		3, // Eighth note triplet (Twelfth note)
		4, // Sixteenth note
		6, // Sixteenth note triplet (Twenty-fourth note)
		8, // Thirty-second note
		12, // Thirty-second note triplet (Forty-eighth note)
		16, // Sixty-fourth note
		48, // One-hundred-ninety-second note
	};

	public static readonly char[] SMAllWhiteSpace = { '\r', '\n', ' ', '\t' };

	public static readonly List<Fraction> SubDivisions = new();
	public static readonly List<double> SubDivisionLengths = new();
	private static readonly ChartProperties[] Properties;
	public static readonly char[] NoteChars = { '0', '1', '2', '3', '4', 'M', 'L', 'F', 'K' };
	public static readonly string[] NoteStrings = { "0", "1", "2", "3", "4", "M", "L", "F", "K" };

	public static readonly string[] NotePrettyStrings =
	{
		"None", "Tap", "Hold Start", "Hold or Roll End", "Roll Start", "Mine", "Lift", "Fake", "KeySound",
	};

	public const string TagTitle = "TITLE";
	public const string TagSubtitle = "SUBTITLE";
	public const string TagArtist = "ARTIST";
	public const string TagTitleTranslit = "TITLETRANSLIT";
	public const string TagSubtitleTranslit = "SUBTITLETRANSLIT";
	public const string TagArtistTranslit = "ARTISTTRANSLIT";
	public const string TagGenre = "GENRE";
	public const string TagCredit = "CREDIT";
	public const string TagBanner = "BANNER";
	public const string TagBackground = "BACKGROUND";
	public const string TagLyricsPath = "LYRICSPATH";
	public const string TagCDTitle = "CDTITLE";
	public const string TagMusic = "MUSIC";
	public const string TagOffset = "OFFSET";

	// ReSharper disable once InconsistentNaming
	public const string TagBPMs = "BPMS";
	public const string TagStops = "STOPS";
	public const string TagFreezes = "FREEZES";
	public const string TagDelays = "DELAYS";
	public const string TagTimeSignatures = "TIMESIGNATURES";
	public const string TagTickCounts = "TICKCOUNTS";
	public const string TagInstrumentTrack = "INSTRUMENTTRACK";
	public const string TagSampleStart = "SAMPLESTART";
	public const string TagSampleLength = "SAMPLELENGTH";
	public const string TagDisplayBPM = "DISPLAYBPM";
	public const string TagSelectable = "SELECTABLE";
	public const string TagAnimations = "ANIMATIONS";
	public const string TagBGChanges = "BGCHANGES";
	public const string TagBGChanges1 = "BGCHANGES1";
	public const string TagBGChanges2 = "BGCHANGES2";
	public const string TagFGChanges = "FGCHANGES";
	public const string TagKeySounds = "KEYSOUNDS";
	public const string TagAttacks = "ATTACKS";
	public const string TagNotes = "NOTES";
	public const string TagNotes2 = "NOTES2";
	public const string TagRadarValues = "RADARVALUES";
	public const string TagLastBeatHint = "LASTBEATHINT";

	public const string TagVersion = "VERSION";
	public const string TagOrigin = "ORIGIN";
	public const string TagPreviewVid = "PREVIEWVID";
	public const string TagJacket = "JACKET";
	public const string TagCDImage = "CDIMAGE";
	public const string TagDiscImage = "DISCIMAGE";
	public const string TagPreview = "PREVIEW";
	public const string TagMusicLength = "MUSICLENGTH";
	public const string TagLastSecondHint = "LASTSECONDHINT";
	public const string TagWarps = "WARPS";
	public const string TagLabels = "LABELS";
	public const string TagCombos = "COMBOS";
	public const string TagSpeeds = "SPEEDS";
	public const string TagScrolls = "SCROLLS";
	public const string TagFakes = "FAKES";

	public const string TagFirstSecond = "FIRSTSECOND";
	public const string TagLastSecond = "LASTSECOND";
	public const string TagSongFileName = "SONGFILENAME";
	public const string TagHasMusic = "HASMUSIC";
	public const string TagHasBanner = "HASBANNER";

	public const string TagNoteData = "NOTEDATA";
	public const string TagChartName = "CHARTNAME";
	public const string TagStepsType = "STEPSTYPE";
	public const string TagChartStyle = "CHARTSTYLE";
	public const string TagDescription = "DESCRIPTION";
	public const string TagDifficulty = "DIFFICULTY";
	public const string TagMeter = "METER";

	public const string TagFumenKeySoundIndex = "FumenKSI";
	public const string TagFumenDoublePosition = "FumenDoublePos";
	public const string TagFumenDoubleValue = "FumenDoubleVal";
	public const string TagFumenNotesType = "FumenNotesType";
	public const string TagFumenRawStopsStr = "FumenRawStopsStr";
	public const string TagFumenRawDelaysStr = "FumenRawDelaysStr";
	public const string TagFumenRawWarpsStr = "FumenRawWarpsStr";
	public const string TagFumenRawScrollsStr = "FumenRawScrollsStr";
	public const string TagFumenRawSpeedsStr = "FumenRawSpeedsStr";
	public const string TagFumenRawBpmsStr = "FumenRawBpmsStr";
	public const string TagFumenRawTickCountsStr = "FumenRawTickCountsStr";
	public const string TagFumenRawLabelsStr = "FumenRawLabelsStr";
	public const string TagFumenRawCombosStr = "FumenRawCombosStr";
	public const string TagFumenRawFakesStr = "FumenRawFakesStr";
	public const string TagFumenRawTimeSignaturesStr = "FumenRawTimeSignaturesStr";
	public const string TagFumenChartUsesOwnTimingData = "FumenChartUsesOwnTimingData";
	public const string TagFumenNoteOriginalMeasurePosition = "FumenNoteOriginalMeasurePosition";

	public const string SMDoubleFormat = "F6";

	public const string SMCustomPropertySongMarker = "SONG";
	public const int SMCustomPropertySongMarkerLength = 4;
	public const string SMCustomPropertyChartMarker = "CHART";
	public const string SMCustomPropertyChartIndexFormat = "D4";
	public const int SMCustomPropertyChartIndexMarkerLength = 9;
	public const int SMCustomPropertyChartIndexNumberLength = 4;

	/// <summary>
	/// Static initialization.
	/// </summary>
	static SMCommon()
	{
		// Initialize ChartType strings.
		ChartTypeStings = new Dictionary<ChartType, string>();
		foreach (var chartType in Enum.GetValues(typeof(ChartType)).Cast<ChartType>())
		{
			ChartTypeStings[chartType] = chartType.ToString().Replace("_", "-");
		}

		// Initialize valid SM SubDivisions.
		SubDivisions.Add(new Fraction(0, 1));
		foreach (var denominator in ValidDenominators)
		{
			if (denominator <= 1)
				continue;
			for (var numerator = 0; numerator < denominator; numerator++)
			{
				var fraction = new Fraction(numerator, denominator);
				if (!SubDivisions.Contains(fraction))
					SubDivisions.Add(fraction);
			}
		}

		SubDivisions.Sort();
		foreach (var subDivision in SubDivisions)
			SubDivisionLengths.Add(subDivision.ToDouble());

		// Initialize ChartProperties.
		Properties = new ChartProperties[Enum.GetNames(typeof(ChartType)).Length];
		Properties[(int)ChartType.dance_single] = new ChartProperties(4, 1);
		Properties[(int)ChartType.dance_double] = new ChartProperties(8, 1);
		Properties[(int)ChartType.dance_couple] = new ChartProperties(8, 2);
		Properties[(int)ChartType.dance_solo] = new ChartProperties(6, 1);
		Properties[(int)ChartType.dance_threepanel] = new ChartProperties(3, 1);
		Properties[(int)ChartType.dance_routine] = new ChartProperties(8, 2);
		Properties[(int)ChartType.pump_single] = new ChartProperties(5, 1);
		Properties[(int)ChartType.pump_halfdouble] = new ChartProperties(6, 1);
		Properties[(int)ChartType.pump_double] = new ChartProperties(10, 1);
		Properties[(int)ChartType.pump_couple] = new ChartProperties(10, 2);
		Properties[(int)ChartType.pump_routine] = new ChartProperties(10, 2);
		Properties[(int)ChartType.kb7_single] = new ChartProperties(7, 1);
		Properties[(int)ChartType.ez2_single] = new ChartProperties(5, 1);
		Properties[(int)ChartType.ez2_double] = new ChartProperties(10, 1);
		Properties[(int)ChartType.ez2_real] = new ChartProperties(7, 1);
		Properties[(int)ChartType.para_single] = new ChartProperties(5, 1);
		Properties[(int)ChartType.ds3ddx_single] = new ChartProperties(8, 1);
		Properties[(int)ChartType.bm_single5] = new ChartProperties(6, 1);
		Properties[(int)ChartType.bm_versus5] = new ChartProperties(6, 1);
		Properties[(int)ChartType.bm_double5] = new ChartProperties(12, 1);
		Properties[(int)ChartType.bm_single7] = new ChartProperties(8, 1);
		Properties[(int)ChartType.bm_versus7] = new ChartProperties(8, 1);
		Properties[(int)ChartType.bm_double7] = new ChartProperties(16, 1);
		Properties[(int)ChartType.maniax_single] = new ChartProperties(4, 1);
		Properties[(int)ChartType.maniax_double] = new ChartProperties(8, 1);
		Properties[(int)ChartType.techno_single4] = new ChartProperties(4, 1);
		Properties[(int)ChartType.techno_single5] = new ChartProperties(5, 1);
		Properties[(int)ChartType.techno_single8] = new ChartProperties(8, 1);
		Properties[(int)ChartType.techno_double4] = new ChartProperties(8, 1);
		Properties[(int)ChartType.techno_double5] = new ChartProperties(10, 1);
		Properties[(int)ChartType.techno_double8] = new ChartProperties(16, 1);
		Properties[(int)ChartType.pnm_five] = new ChartProperties(5, 1);
		Properties[(int)ChartType.pnm_nine] = new ChartProperties(9, 1);
		Properties[(int)ChartType.lights_cabinet] = new ChartProperties(6, 1);
		Properties[(int)ChartType.kickbox_human] = new ChartProperties(4, 1);
		Properties[(int)ChartType.kickbox_quadarm] = new ChartProperties(4, 1);
		Properties[(int)ChartType.kickbox_insect] = new ChartProperties(6, 1);
		Properties[(int)ChartType.kickbox_arachnid] = new ChartProperties(8, 1);
		Properties[(int)ChartType.smx_beginner] = new ChartProperties(3, 1);
		Properties[(int)ChartType.smx_single] = new ChartProperties(5, 1);
		Properties[(int)ChartType.smx_dual] = new ChartProperties(6, 1);
		Properties[(int)ChartType.smx_full] = new ChartProperties(10, 1);
		Properties[(int)ChartType.smx_team] = new ChartProperties(10, 2);
	}

	public static ChartProperties GetChartProperties(ChartType chartType)
	{
		return Properties[(int)chartType];
	}

	/// <summary>
	/// Given a TimeSignature and a row in a chart that occurs during that time signature,
	/// returns the row relative to the start of the measure containing the row.
	/// </summary>
	/// <param name="ts">TimeSignature in question.</param>
	/// <param name="row">Row in question.</param>
	/// <returns>Row relative to its measure start.</returns>
	public static int GetRowRelativeToMeasureStart(TimeSignature ts, int row)
	{
		var rowsPerWholeNote = NumBeatsPerMeasure * MaxValidDenominator;
		var rowsPerMeasure = rowsPerWholeNote * ts.Signature.Numerator / ts.Signature.Denominator;
		return (row - ts.IntegerPosition) % rowsPerMeasure;
	}

	/// <summary>
	/// Given a double representation of an arbitrary fraction return the closest
	/// matching Fraction that stepmania supports as a beat subdivision.
	/// </summary>
	/// <param name="fractionAsDouble">Fraction as a double.</param>
	/// <returns>
	/// Closest matching Fraction supported by stepmania as a beat subdivision.
	/// </returns>
	public static Fraction FindClosestSMSubDivision(double fractionAsDouble)
	{
		var length = SubDivisionLengths.Count;

		// Edge cases
		if (fractionAsDouble <= SubDivisionLengths[0])
			return SubDivisions[0];
		if (fractionAsDouble >= SubDivisionLengths[length - 1])
			return SubDivisions[length - 1];

		// Search
		int leftIndex = 0, rightIndex = length, midIndex = 0;
		while (leftIndex < rightIndex)
		{
			midIndex = (leftIndex + rightIndex) >> 1;

			// Value is less than midpoint, search to the left.
			if (fractionAsDouble < SubDivisionLengths[midIndex])
			{
				// Value is between midpoint and adjacent.
				if (midIndex > 0 && fractionAsDouble > SubDivisionLengths[midIndex - 1])
				{
					return fractionAsDouble - SubDivisionLengths[midIndex - 1] <
					       SubDivisionLengths[midIndex] - fractionAsDouble
						? SubDivisions[midIndex - 1]
						: SubDivisions[midIndex];
				}

				// Advance search
				rightIndex = midIndex;
			}

			// Value is greater than midpoint, search to the right.
			else if (fractionAsDouble > SubDivisionLengths[midIndex])
			{
				// Value is between midpoint and adjacent.
				if (midIndex < length - 1 && fractionAsDouble < SubDivisionLengths[midIndex + 1])
				{
					return fractionAsDouble - SubDivisionLengths[midIndex] <
					       SubDivisionLengths[midIndex + 1] - fractionAsDouble
						? SubDivisions[midIndex]
						: SubDivisions[midIndex + 1];
				}

				// Advance search
				leftIndex = midIndex + 1;
			}

			// Value equals midpoint.
			else
			{
				return SubDivisions[midIndex];
			}
		}

		return SubDivisions[midIndex];
	}

	/// <summary>
	/// Given a desired sub-division for a position within a beat, return the lowest possible
	/// sub-division that should be used. This is necessary to convert some valid sub-divisions
	/// which are not supported like 64th note triplets (sub-division 24) into a higher
	/// sub-division which is supported, like 192nd notes (sub-division 48).
	/// If no possible valid sub-division exists, return false.
	/// </summary>
	/// <param name="desiredSubDivision">
	/// The desired sub-division to use. In practice, the least common multiple of the reduced
	/// sub-divisions for all notes in a particular measure.
	/// </param>
	/// <param name="lowestValidSMSubDivision">
	/// Out parameter to hold the lowest valid sub-division which stepmania supports.
	/// </param>
	/// <returns>
	/// True if a valid sub-division was found and false otherwise.
	/// </returns>
	public static bool GetLowestValidSMSubDivision(int desiredSubDivision, out int lowestValidSMSubDivision)
	{
		lowestValidSMSubDivision = desiredSubDivision;
		var highestDenominator = ValidDenominators[^1];
		do
		{
			foreach (var validDenominator in ValidDenominators)
			{
				if (desiredSubDivision == validDenominator)
				{
					lowestValidSMSubDivision = desiredSubDivision;
					return true;
				}
			}

			desiredSubDivision <<= 1;
		} while (desiredSubDivision <= highestDenominator);

		return false;
	}


	/// <summary>
	/// Converts a double value representing absolute beats into the closest IntegerPosition
	/// that matches valid StepMania beat subdivisions.
	/// </summary>
	/// <param name="absoluteBeat">Absolute beat as a double</param>
	/// <returns>Closest IntegerPosition to use.</returns>
	public static int ConvertAbsoluteBeatToIntegerPosition(double absoluteBeat)
	{
		var beatInt = (int)absoluteBeat;
		var lowEstimateSubDivision = FindClosestSMSubDivision(absoluteBeat - beatInt);
		var highEstimateSubDivision = lowEstimateSubDivision + new Fraction(1, MaxValidDenominator);

		var lowerEstimatedPosition = beatInt + lowEstimateSubDivision.ToDouble();
		var higherEstimatedPosition = beatInt + highEstimateSubDivision.ToDouble();

		var subDivisionToUse =
			Math.Abs(absoluteBeat - lowerEstimatedPosition) <= Math.Abs(absoluteBeat - higherEstimatedPosition)
				? lowEstimateSubDivision
				: highEstimateSubDivision;

		var subDivisionAsRow = subDivisionToUse.Numerator * (MaxValidDenominator / subDivisionToUse.Denominator);
		return beatInt * MaxValidDenominator + subDivisionAsRow;
	}

	/// <summary>
	/// Converts an integer position (rows) to a beat value as a double.
	/// </summary>
	/// <param name="integerPosition">Integer position of an Event.</param>
	/// <returns>Beat value as double to use.</returns>
	public static double ConvertIntegerPositionToBeat(int integerPosition)
	{
		return (double)integerPosition / MaxValidDenominator;
	}

	/// <summary>
	/// Helper function for timing events to convert a dictionary of events by their double beat position
	/// into a list that removes the earliest conflicting values that occur on the same row when that row
	/// is converted to an integer.
	/// </summary>
	/// <typeparam name="T">Type of event data.</typeparam>
	/// <param name="events">Dictionary of events by double row position.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	/// <param name="eventTypeString">String representation of the type of event for logging.</param>
	/// <returns>
	/// List of events with duplicates removed. The list values are Tuples.
	/// The Tuples contain the following items:
	///  Item1: Row integer position of the event.
	///  Item2: Original double beat position of the event.
	///  Item3: Original event value.
	/// </returns>
	private static List<Tuple<int, double, T>> ConvertValueAtTimeDictionaryToListWithNoConflicts<T>(
		Dictionary<double, T> events,
		ILogger logger,
		bool logOnErrors,
		string eventTypeString)
	{
		var results = new List<Tuple<int, double, T>>();

		// We need to ensure that there is only at most one event of the given type per rounded integer position.
		// Create a list in descending order by time to loop over so we can ignore earlier events that occur
		// during the same row. This matches Stepmania behavior.
		var orderedEvents = new List<Tuple<double, T>>();
		foreach (var chartEvent in events)
		{
			orderedEvents.Add(new Tuple<double, T>(chartEvent.Key, chartEvent.Value));
		}

		orderedEvents = orderedEvents.OrderByDescending(t => t.Item1).ToList();

		var previousIntegerPosition = int.MinValue;
		foreach (var chartEvent in orderedEvents)
		{
			var integerPosition = ConvertAbsoluteBeatToIntegerPosition(chartEvent.Item1);

			// Ignore this event if it is an earlier event at the same row.
			if (previousIntegerPosition != int.MinValue && previousIntegerPosition == integerPosition)
			{
				if (logOnErrors)
				{
					logger.Warn(
						$"{eventTypeString} with value {chartEvent.Item2} at beat {chartEvent.Item1} occurs at row {integerPosition} which conflicts with a later {eventTypeString} on the same row."
						+ $" This {eventTypeString} will be ignored.");
				}

				continue;
			}

			previousIntegerPosition = integerPosition;

			// Otherwise, add the result with the integer position.
			results.Add(new Tuple<int, double, T>(integerPosition, chartEvent.Item1, chartEvent.Item2));
		}

		// Reverse the list so it is back in ascending order.
		results.Reverse();

		return results;
	}

	/// <summary>
	/// Adds Tempo Events to the given Chart from the given Dictionary of
	/// position to tempo values parsed from the Chart or Song.
	/// </summary>
	/// <param name="tempos">
	/// Dictionary of time to value of tempos parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Tempo Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddTempos(
		Dictionary<double, double> tempos,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		// Insert tempo change events.
		var temposList = ConvertValueAtTimeDictionaryToListWithNoConflicts(tempos, logger, logOnErrors, nameof(Tempo));
		for (var i = 0; i < temposList.Count; i++)
		{
			var tempo = temposList[i];
			var bpm = tempo.Item3;

			// Tempos must be positive.
			if (bpm <= 0.0)
			{
				// On the first tempo, scan forward to replace it with a valid tempo.
				if (i == 0)
				{
					var foundValidTempo = false;
					for (var j = 1; j < temposList.Count; j++)
					{
						if (temposList[j].Item3 > 0.0)
						{
							logger.Warn(
								$"Fist tempo {bpm} is invalid."
								+ $" Defaulting first tempo to first valid tempo: {temposList[j].Item3}bpm.");
							foundValidTempo = true;
							bpm = temposList[j].Item3;
							break;
						}
					}

					if (!foundValidTempo)
					{
						logger.Warn(
							$"Fist tempo {bpm} is invalid and there are no valid tempos to use."
							+ $" Defaulting entire Song to {DefaultTempo}bpm.");
						bpm = DefaultTempo;
					}
				}
				// For subsequent tempos, ignore them.
				else
				{
					if (logOnErrors)
					{
						logger.Warn($"Tempo {bpm} is invalid. Skipping this tempo.");
					}

					continue;
				}
			}

			var tempoChangeEvent = new Tempo(bpm)
			{
				IntegerPosition = tempo.Item1,
			};

			// Record the actual doubles.
			tempoChangeEvent.Extras.AddSourceExtra(TagFumenDoublePosition, tempo.Item2);

			chart.Layers[0].Events.Add(tempoChangeEvent);
		}
	}

	/// <summary>
	/// Adds Stop Events to the given Chart from the given Dictionary of
	/// position to stop values parsed from the Chart or Song.
	/// </summary>
	/// <param name="stops">
	/// Dictionary of time to value of stop lengths parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Stop Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddStops(
		Dictionary<double, double> stops,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var stop in ConvertValueAtTimeDictionaryToListWithNoConflicts(stops, logger, logOnErrors, nameof(Stop)))
		{
			var stopEvent = new Stop(stop.Item3)
			{
				IntegerPosition = stop.Item1,
			};

			// Record the actual doubles.
			stopEvent.Extras.AddSourceExtra(TagFumenDoublePosition, stop.Item2);
			stopEvent.Extras.AddSourceExtra(TagFumenDoubleValue, stop.Item3);

			chart.Layers[0].Events.Add(stopEvent);
		}
	}

	/// <summary>
	/// Adds Delay Stop Events to the given Chart from the given Dictionary of
	/// position to delay values parsed from the Chart or Song.
	/// </summary>
	/// <param name="delays">
	/// Dictionary of time to value of delay lengths parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Stop Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddDelays(
		Dictionary<double, double> delays,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var delay in ConvertValueAtTimeDictionaryToListWithNoConflicts(delays, logger, logOnErrors, DelayString))
		{
			var stopEvent = new Stop(delay.Item3, true)
			{
				IntegerPosition = delay.Item1,
			};

			// Record the actual doubles.
			stopEvent.Extras.AddSourceExtra(TagFumenDoublePosition, delay.Item2);
			stopEvent.Extras.AddSourceExtra(TagFumenDoubleValue, delay.Item3);

			chart.Layers[0].Events.Add(stopEvent);
		}
	}

	/// <summary>
	/// Adds Warp Events to the given Chart from the given Dictionary of
	/// position to warp length values parsed from the Chart or Song.
	/// </summary>
	/// <param name="warps">
	/// Dictionary of time to value of warp lengths parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Warp Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddWarps(
		Dictionary<double, double> warps,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var warp in ConvertValueAtTimeDictionaryToListWithNoConflicts(warps, logger, logOnErrors, nameof(Warp)))
		{
			// Convert warp beats to number of rows
			var warpEvent = new Warp(ConvertAbsoluteBeatToIntegerPosition(warp.Item3))
			{
				IntegerPosition = warp.Item1,
			};

			// Record the actual doubles.
			warpEvent.Extras.AddSourceExtra(TagFumenDoublePosition, warp.Item2);
			warpEvent.Extras.AddSourceExtra(TagFumenDoubleValue, warp.Item3);

			chart.Layers[0].Events.Add(warpEvent);
		}
	}

	/// <summary>
	/// Adds ScrollRate Events to the given Chart from the given Dictionary of
	/// position to rate values parsed from the Chart or Song.
	/// </summary>
	/// <param name="scrollRateEvents">
	/// Dictionary of time to value of scroll rates parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add ScrollRate Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddScrollRateEvents(
		Dictionary<double, double> scrollRateEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var scrollRate in ConvertValueAtTimeDictionaryToListWithNoConflicts(scrollRateEvents, logger, logOnErrors,
			         "Scroll"))
		{
			var scrollRateEvent = new ScrollRate(scrollRate.Item3)
			{
				IntegerPosition = scrollRate.Item1,
			};

			// Record the actual doubles.
			scrollRateEvent.Extras.AddSourceExtra(TagFumenDoublePosition, scrollRate.Item2);
			scrollRateEvent.Extras.AddSourceExtra(TagFumenDoubleValue, scrollRate.Item3);

			chart.Layers[0].Events.Add(scrollRateEvent);
		}
	}

	/// <summary>
	/// Adds ScrollRateInterpolation Events to the given Chart from the given Dictionary of
	/// position to rate values parsed from the Chart or Song.
	/// </summary>
	/// <param name="scrollRateEvents">
	/// Dictionary of time to value of scroll rates parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add ScrollRateInterpolation Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddScrollRateInterpolationEvents(
		Dictionary<double, Tuple<double, double, int>> scrollRateEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var scrollRate in ConvertValueAtTimeDictionaryToListWithNoConflicts(scrollRateEvents, logger, logOnErrors,
			         "Speed"))
		{
			var (speed, length, secondsFlag) = scrollRate.Item3;

			var lengthIsTimeInSeconds = secondsFlag != 0;
			var periodAsTime = 0.0;
			var periodAsIntegerPosition = 0;
			if (lengthIsTimeInSeconds)
				periodAsTime = length;
			else
				periodAsIntegerPosition = ConvertAbsoluteBeatToIntegerPosition(length);

			var scrollRateEvent =
				new ScrollRateInterpolation(speed, periodAsIntegerPosition, periodAsTime, lengthIsTimeInSeconds)
				{
					IntegerPosition = scrollRate.Item1,
				};

			// Record the actual doubles.
			scrollRateEvent.Extras.AddSourceExtra(TagFumenDoublePosition, scrollRate.Item2);
			//scrollRateEvent.Extras.AddSourceExtra(TagFumenDoubleValue, scrollRate.Item3);

			chart.Layers[0].Events.Add(scrollRateEvent);
		}
	}

	/// <summary>
	/// Adds TickCount Events to the given Chart from the given Dictionary of
	/// position to tick values parsed from the Chart or Song.
	/// </summary>
	/// <param name="tickCountEvents">
	/// Dictionary of time to value of ticks parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add TickCount Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddTickCountEvents(
		Dictionary<double, int> tickCountEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var tickCount in ConvertValueAtTimeDictionaryToListWithNoConflicts(tickCountEvents, logger, logOnErrors,
			         nameof(TickCount)))
		{
			var tickCountEvent = new TickCount(tickCount.Item3)
			{
				IntegerPosition = tickCount.Item1,
			};

			// Record the actual doubles.
			tickCountEvent.Extras.AddSourceExtra(TagFumenDoublePosition, tickCount.Item2);

			chart.Layers[0].Events.Add(tickCountEvent);
		}
	}

	/// <summary>
	/// Adds Label Events to the given Chart from the given Dictionary of
	/// position to string values parsed from the Chart or Song.
	/// </summary>
	/// <param name="labelEvents">
	/// Dictionary of time to value of strings parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Label Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddLabelEvents(
		Dictionary<double, string> labelEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var label in ConvertValueAtTimeDictionaryToListWithNoConflicts(labelEvents, logger, logOnErrors, nameof(Label)))
		{
			var labelEvent = new Label(label.Item3)
			{
				IntegerPosition = label.Item1,
			};

			// Record the actual doubles.
			labelEvent.Extras.AddSourceExtra(TagFumenDoublePosition, label.Item2);

			chart.Layers[0].Events.Add(labelEvent);
		}
	}

	/// <summary>
	/// Adds FakeSegment Events to the given Chart from the given Dictionary of
	/// position to fake segment length values parsed from the Chart or Song.
	/// </summary>
	/// <param name="fakeEvents">
	/// Dictionary of time to value of fake segment lengths parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add FakeSegment Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddFakeSegmentEvents(
		Dictionary<double, double> fakeEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var fake in ConvertValueAtTimeDictionaryToListWithNoConflicts(fakeEvents, logger, logOnErrors,
			         nameof(FakeSegment)))
		{
			var fakeSegmentEvent = new FakeSegment(fake.Item3)
			{
				IntegerPosition = fake.Item1,
			};

			// Record the actual doubles.
			fakeSegmentEvent.Extras.AddSourceExtra(TagFumenDoublePosition, fake.Item2);
			fakeSegmentEvent.Extras.AddSourceExtra(TagFumenDoubleValue, fake.Item3);

			chart.Layers[0].Events.Add(fakeSegmentEvent);
		}
	}

	/// <summary>
	/// Adds Multipliers Events to the given Chart from the given Dictionary of
	/// position to hit and miss multiplier values parsed from the Chart or Song.
	/// </summary>
	/// <param name="comboEvents">
	/// Dictionary of time to hit and miss multipliers parsed from the Song or Chart.
	/// </param>
	/// <param name="chart">Chart to add Multipliers Events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddMultipliersEvents(
		Dictionary<double, Tuple<int, int>> comboEvents,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		foreach (var combo in ConvertValueAtTimeDictionaryToListWithNoConflicts(comboEvents, logger, logOnErrors, "Combo"))
		{
			var multipliersEvent = new Multipliers(combo.Item3.Item1, combo.Item3.Item2)
			{
				IntegerPosition = combo.Item1,
			};

			// Record the actual doubles.
			multipliersEvent.Extras.AddSourceExtra(TagFumenDoublePosition, combo.Item2);

			chart.Layers[0].Events.Add(multipliersEvent);
		}
	}

	/// <summary>
	/// Adds Time Signature Events to the given Chart from the given Dictionary of
	/// beat position to time signatures as Fractions parsed from the Song.
	/// </summary>
	/// <param name="timeSignatures">
	/// Dictionary of time in beats to time signatures as Fractions parsed from the Song.
	/// </param>
	/// <param name="chart">Chart to add Time Signature events to.</param>
	/// <param name="logger">Logger for logging errors or warnings.</param>
	/// <param name="logOnErrors">Whether or not to log on warnings on errors.</param>
	public static void AddTimeSignatures(Dictionary<double, Fraction> timeSignatures,
		Chart chart,
		ILogger logger,
		bool logOnErrors)
	{
		var useDefaultTimeSignature = false;

		// If there are no time signatures defined, use a default 4/4 signature.
		if (timeSignatures.Count == 0)
		{
			useDefaultTimeSignature = true;
		}

		// Otherwise, try to parse the time signatures and ensure they are valid.
		else
		{
			var first = true;
			var lastTimeSignatureIntegerPosition = 0;
			var lastTimeSignatureRowsPerMeasure = 0;
			var tsEvents = new List<TimeSignature>();
			foreach (var timeSignatureEvent in ConvertValueAtTimeDictionaryToListWithNoConflicts(timeSignatures, logger,
				         logOnErrors, nameof(TimeSignature)))
			{
				var integerPosition = timeSignatureEvent.Item1;
				var beatDouble = timeSignatureEvent.Item2;
				var ts = timeSignatureEvent.Item3;

				// The first time signature must be at the start of the song.
				if (first && beatDouble != 0.0)
				{
					if (logOnErrors)
					{
						var beatStr = beatDouble.ToString(SMDoubleFormat);
						var expectedBeatStr = 0.0.ToString(SMDoubleFormat);
						logger.Warn(
							$"First time signature occurs at {beatStr}."
							+ $" If explicit time signatures are defined the first must be at {expectedBeatStr}."
							+ $" Defaulting entire Song to {NumBeatsPerMeasure}/{NumBeatsPerMeasure}.");
					}

					useDefaultTimeSignature = true;
					break;
				}

				if (ts.Numerator < 1 || ts.Denominator < 1)
				{
					if (logOnErrors)
					{
						var beatStr = beatDouble.ToString(SMDoubleFormat);
						logger.Warn(
							$"Time signature at {beatStr} ({ts.Numerator}/{ts.Denominator}) is invalid."
							+ " Both values must be greater than 0."
							+ $" Defaulting entire Song to {NumBeatsPerMeasure}/{NumBeatsPerMeasure}.");
					}

					useDefaultTimeSignature = true;
					break;
				}

				// Make sure this new time signature actually falls at the start of a measure.
				var relativePosition = integerPosition - lastTimeSignatureIntegerPosition;
				if (lastTimeSignatureRowsPerMeasure != 0 && relativePosition % lastTimeSignatureRowsPerMeasure != 0)
				{
					if (logOnErrors)
					{
						var beatStr = beatDouble.ToString(SMDoubleFormat);
						logger.Warn(
							$"Time signature at {beatStr} ({ts.Numerator}/{ts.Denominator}) does not fall on a measure boundary."
							+ " Time signatures can only change at the start of a measure."
							+ $" Defaulting entire Song to {NumBeatsPerMeasure}/{NumBeatsPerMeasure}.");
					}

					useDefaultTimeSignature = true;
					break;
				}

				// Make sure this time signature can be represented by StepMania's integer positions.
				if (MaxValidDenominator * NumBeatsPerMeasure % ts.Denominator != 0)
				{
					if (logOnErrors)
					{
						var beatStr = beatDouble.ToString(SMDoubleFormat);
						logger.Warn(
							$"Time signature at {beatStr} ({ts.Numerator}/{ts.Denominator}) cannot be represented by StepMania."
							+ $" The beat ({ts.Denominator}) must evenly divide {MaxValidDenominator * NumBeatsPerMeasure}."
							+ $" Defaulting entire Song to {NumBeatsPerMeasure}/{NumBeatsPerMeasure}.");
					}

					useDefaultTimeSignature = true;
					break;
				}

				// Record measure boundaries so we can check the next time signature.
				var rowsPerBeat = MaxValidDenominator * NumBeatsPerMeasure / ts.Denominator;
				var rowsPerMeasure = rowsPerBeat * ts.Numerator;
				lastTimeSignatureRowsPerMeasure = rowsPerMeasure;
				lastTimeSignatureIntegerPosition = integerPosition;

				tsEvents.Add(new TimeSignature(ts)
				{
					IntegerPosition = integerPosition,
				});

				first = false;
			}

			// All time signatures are valid, record them.
			if (!useDefaultTimeSignature)
			{
				foreach (var tsEvent in tsEvents)
					chart.Layers[0].Events.Add(tsEvent);
			}
		}

		// Add a 4/4 time signature by default.
		if (useDefaultTimeSignature)
		{
			chart.Layers[0].Events.Add(new TimeSignature(new Fraction(NumBeatsPerMeasure, NumBeatsPerMeasure))
			{
				IntegerPosition = 0,
			});
		}
	}

	/// <summary>
	/// Tries to get a string representing the BPM to set as the Tempo on
	/// a Chart for display purposes. First tries to get the DisplayBPM
	/// from the source extras, and convert that list to a string. Failing that
	/// it tries to look through the provided tempo events from the Song or Chart
	/// and use the one at position 0.0.
	/// </summary>
	/// <param name="extras">
	/// Extras from Song or Chart to check the TagDisplayBPM value.
	/// </param>
	/// <param name="tempos">
	/// Dictionary of time to value of tempos parsed from the Song or Chart.
	/// </param>
	/// <returns>String representation of display tempo to use.</returns>
	public static string GetDisplayBPMStringFromSourceExtrasList(
		Extras extras,
		Dictionary<double, double> tempos)
	{
		var displayTempo = "";
		if (extras.TryGetSourceExtra(TagDisplayBPM, out object chartDisplayTempoObj))
		{
			if (chartDisplayTempoObj is List<string> tempoList)
			{
				var first = true;
				foreach (var tempo in tempoList)
				{
					if (!first)
						displayTempo += MSDFile.ParamMarker;
					displayTempo += tempo;
					first = false;
				}
			}
			else
			{
				displayTempo = chartDisplayTempoObj.ToString();
			}
		}
		else if (tempos?.ContainsKey(0.0) ?? false)
		{
			displayTempo = tempos[0.0].ToString("N3");
		}

		return displayTempo;
	}

	/// <summary>
	/// Copies the non-performance events from one List of Events to another.
	/// </summary>
	/// <param name="source">Event List to copy from.</param>
	/// <param name="dest">Event List to copy to.</param>
	public static void CopyNonPerformanceEvents(List<Event> source, List<Event> dest)
	{
		foreach (var e in source)
		{
			if (e is TimeSignature
			    || e is Tempo
			    || e is Stop
			    || e is Warp
			    || e is ScrollRate
			    || e is ScrollRateInterpolation
			    || e is TickCount
			    || e is Label
			    || e is FakeSegment
			    || e is Multipliers)
				dest.Add(e.Clone());
		}
	}

	/// <summary>
	/// Returns whether or not the given Event is an Event which affects other Events' TimeSeconds
	/// or MetricPosition values. 
	/// </summary>
	/// <param name="chartEvent">Event in question.</param>
	/// <returns>
	/// True if this Event affects other Events' TimeSeconds or MetricPosition values and false otherwise.
	/// </returns>
	public static bool DoesEventAffectTiming(Event chartEvent)
	{
		return chartEvent is TimeSignature or Tempo or Stop or Warp;
	}

	/// <summary>
	/// Sets the TimeSeconds and MetricPositions on the given Chart Events based
	/// on the rate altering Events in the Chart.
	/// </summary>
	/// <param name="chart">Chart to set TimeSeconds and MetricPosition on the Events.</param>
	public static void SetEventTimeAndMetricPositionsFromRows(Chart chart)
	{
		SetEventTimeAndMetricPositionsFromRows(chart.Layers[0].Events);
	}

	/// <summary>
	/// Sets the TimeSeconds and MetricPositions on the given Events based on rate altering Events.
	/// </summary>
	/// <param name="events">Enumerable set of Events to update.</param>
	public static void SetEventTimeAndMetricPositionsFromRows(IEnumerable<Event> events)
	{
		var lastTempoChangeRow = 0;
		var lastTempoChangeTime = 0.0;
		Tempo lastTempo = null;
		var lastTimeSigChangeMeasure = 0;
		var lastTimeSigChangeRow = 0;
		var rowsPerBeat = MaxValidDenominator;
		var beatsPerMeasure = NumBeatsPerMeasure;
		var totalStopTimeSeconds = 0.0;
		var previousEventTimeSeconds = 0.0;

		// Warps are unfortunately complicated.
		// Overlapping warps do not stack.
		// Warps are represented as rows / IntegerPosition, unlike Stops which use time.
		// We need to figure out how much time warps account for to update Event TimeSeconds.
		// But we cannot just do a pass to compute the time for all Warps and then sum them up
		// in a second pass since overlapping warps do not stack. We also can't just sum the time
		// between each event during a warp per loop since that would accrue rounding error.
		// So we need to use the logic of determining the time that has elapsed since the last
		// event which has altered the rate of beats that occurred during the warp. This time
		// is tracked in currentWarpTime below. When the rate changes, we commit currentWarpTime
		// to totalWarpTime.
		var warpingEndPosition = -1;
		var totalWarpTime = 0.0;
		var lastWarpBeatTimeChangeRow = -1;

		// Note that overlapping negative Stops DO stack, even though Warps do not.
		// This means that a Chart with overlapping Warps when saved by StepMania will produce
		// a ssc and sm file that are not the same. The ssc file will have a shorter skipped range
		// and the two charts will be out of sync.

		foreach (var chartEvent in events)
		{
			if (chartEvent == null)
				continue;

			var beatRelativeToLastTimeSigChange = (chartEvent.IntegerPosition - lastTimeSigChangeRow) / rowsPerBeat;
			var measureRelativeToLastTimeSigChange = beatRelativeToLastTimeSigChange / beatsPerMeasure;

			var absoluteMeasure = lastTimeSigChangeMeasure + measureRelativeToLastTimeSigChange;
			var beatInMeasure = beatRelativeToLastTimeSigChange - measureRelativeToLastTimeSigChange * beatsPerMeasure;
			var beatSubDivision = new Fraction((chartEvent.IntegerPosition - lastTimeSigChangeRow) % rowsPerBeat, rowsPerBeat)
				.Reduce();

			var timeRelativeToLastTempoChange = lastTempo == null
				? 0.0
				: (chartEvent.IntegerPosition - lastTempoChangeRow) * lastTempo.GetSecondsPerRow(MaxValidDenominator);
			var absoluteTime = lastTempoChangeTime + timeRelativeToLastTempoChange;

			// Handle a currently running warp.
			var currentWarpTime = 0.0;
			if (warpingEndPosition != -1)
			{
				// Figure out the amount of time elapsed during the current warp since the last event
				// which altered the rate of time during this warp.
				var endPosition = Math.Min(chartEvent.IntegerPosition, warpingEndPosition);
				currentWarpTime = lastTempo == null
					? 0.0
					: (endPosition - lastWarpBeatTimeChangeRow) * lastTempo.GetSecondsPerRow(MaxValidDenominator);

				// Warp section is complete.
				if (chartEvent.IntegerPosition >= warpingEndPosition)
				{
					// Clear variables used to track warp time.
					warpingEndPosition = -1;
					lastWarpBeatTimeChangeRow = -1;

					// Commit the current running warp time to the total warp time.
					totalWarpTime += currentWarpTime;
					currentWarpTime = 0.0;
				}
			}

			chartEvent.MetricPosition = new MetricPosition(absoluteMeasure, beatInMeasure, beatSubDivision);
			chartEvent.TimeSeconds = absoluteTime - currentWarpTime - totalWarpTime + totalStopTimeSeconds;

			// In the case of negative stop warps, we need to clamp the time of an event so it does not precede events which
			// have lower IntegerPositions
			if (chartEvent.TimeSeconds < previousEventTimeSeconds)
				chartEvent.TimeSeconds = previousEventTimeSeconds;
			previousEventTimeSeconds = chartEvent.TimeSeconds;

			switch (chartEvent)
			{
				// Stop handling. Just accrue more stop time.
				case Stop stop:
				{
					// Accrue Stop time whether it is positive or negative.
					// Do not worry about overlapping negative stops as they stack in StepMania.
					totalStopTimeSeconds += stop.LengthSeconds;
					break;
				}
				// Warp handling. Update warp start and stop rows so we can compute the warp time.
				case Warp warp:
				{
					// If there is a currently running warp, just extend the Warp.
					warpingEndPosition = Math.Max(warpingEndPosition, warp.IntegerPosition + warp.LengthIntegerPosition);
					if (lastWarpBeatTimeChangeRow == -1)
						lastWarpBeatTimeChangeRow = chartEvent.IntegerPosition;
					break;
				}
				// Time Signature change. Update time signature and beat time tracking.
				case TimeSignature ts:
				{
					var timeSignature = ts.Signature;

					lastTimeSigChangeRow = chartEvent.IntegerPosition;
					lastTimeSigChangeMeasure = absoluteMeasure;
					beatsPerMeasure = timeSignature.Numerator;
					rowsPerBeat = MaxValidDenominator * NumBeatsPerMeasure / timeSignature.Denominator;

					// If this alteration in beat time occurs during a warp, update our warp tracking variables.
					if (warpingEndPosition != -1)
					{
						totalWarpTime += currentWarpTime;
						lastWarpBeatTimeChangeRow = chartEvent.IntegerPosition;
					}

					break;
				}
				// Tempo change. Update beat time tracking.
				case Tempo tc:
				{
					lastTempo = tc;
					lastTempoChangeRow = chartEvent.IntegerPosition;
					lastTempoChangeTime = absoluteTime;

					// If this alteration in beat time occurs during a warp, update our warp tracking variables.
					if (warpingEndPosition != -1)
					{
						totalWarpTime += currentWarpTime;
						lastWarpBeatTimeChangeRow = chartEvent.IntegerPosition;
					}

					break;
				}
			}
		}
	}

	/// <summary>
	/// Sets the Event IntegerPosition values based on their MetricPosition values
	/// for all Events in the given Chart.
	/// </summary>
	/// <param name="chart">Chart to update Events.</param>
	public static void SetEventRowsFromMetricPosition(Chart chart)
	{
		var lastBeatTimeChangeRow = 0;
		var lastBeatTimeChangeMeasure = 0;
		var rowsPerBeat = MaxValidDenominator;
		var beatsPerMeasure = NumBeatsPerMeasure;

		foreach (var chartEvent in chart.Layers[0].Events)
		{
			var pos = chartEvent.MetricPosition;
			var relativeMeasure = pos.Measure - lastBeatTimeChangeMeasure;
			var relativeBeat = relativeMeasure * beatsPerMeasure + pos.Beat;
			var subDivision = pos.SubDivision.Reduce();
			var subDivisionRows = subDivision.Numerator * rowsPerBeat / subDivision.Denominator;
			var relativeRow = relativeBeat * rowsPerBeat + subDivisionRows;
			var absoluteRow = lastBeatTimeChangeRow + relativeRow;
			chartEvent.IntegerPosition = absoluteRow;

			if (chartEvent is TimeSignature ts)
			{
				var timeSignature = ts.Signature;
				lastBeatTimeChangeRow = chartEvent.IntegerPosition;
				lastBeatTimeChangeMeasure = pos.Measure;
				beatsPerMeasure = timeSignature.Numerator;
				rowsPerBeat = MaxValidDenominator * NumBeatsPerMeasure / timeSignature.Denominator;
			}
		}
	}

	/// <summary>
	/// Helper method for logging position information for a row.
	/// Assumes the StepMania 4/4 time signature and determines a MetricPosition from the row.
	/// </summary>
	/// <param name="row">Row / IntegerPosition value.</param>
	/// <returns>Formatting string for logging.</returns>
	public static string GetPositionForLogging(int row)
	{
		var measure = row / (MaxValidDenominator * NumBeatsPerMeasure);
		var beat = row / MaxValidDenominator - measure * NumBeatsPerMeasure;
		var subDivisionRow = row - beat * MaxValidDenominator;
		var subDivision = new Fraction(subDivisionRow, MaxValidDenominator).Reduce();
		return $"Row {row} (File Measure {measure} Beat {beat} SubDivision {subDivision})";
	}

	/// <summary>
	/// Custom Comparer for Events in an SM Chart.
	/// </summary>
	public class SMEventComparer : IComparer<Event>
	{
		public static readonly List<string> SMEventOrderList = new()
		{
			// Time signatures and tempos must occur first because they are needed
			// for timing and position determination.
			nameof(TimeSignature),
			nameof(Tempo),

			// Miscellaneous events.
			nameof(TickCount),
			nameof(FakeSegment),
			nameof(Multipliers),
			nameof(Label),

			// Delays occur before steps by definition.
			DelayString,

			// All step notes.
			nameof(LaneTapNote),
			nameof(LaneHoldStartNote),
			nameof(LaneHoldEndNote),
			nameof(LaneNote),

			// Scroll events.
			nameof(ScrollRate),
			nameof(ScrollRateInterpolation),

			// Stops must occur after steps by definition.
			// Stops must occur after scroll rate events. When a stop and scroll change
			// occur simultaneously the scroll change must happen first.
			// Gimmick charts like NULCTRL exploit this.
			nameof(Stop),

			// Negative stops are effectively warps.
			// See warp comment below.
			NegativeDelayString,
			NegativeStopString,

			// Warps must occur after stops.
			// Some songs have stops and warps at the same time and the chart must
			// stop before warping.
			// Gimmick charts like NULCTRL exploit this.
			nameof(Warp),
		};

		private readonly Dictionary<string, int> SMEventOrderDict = new();

		/// <summary>
		/// Default constructor.
		/// The SMEventComparer will use the default Event ordering.
		/// </summary>
		public SMEventComparer()
		{
			var index = 0;
			foreach (var eventString in SMEventOrderList)
			{
				SMEventOrderDict.Add(eventString, index);
				index++;
			}
		}

		/// <summary>
		/// Constructor with custom Event sort order.
		/// The SMEventComparer will use Event ordering as defined by the given customEventOrder.
		/// Strings in customEventOrder are expected to be the Type names of events in the structure
		/// to be sorted. It is expected that every Type of event in the structure being sorted is
		/// present in the given List. This method is intended to be used when dealing with custom
		/// Event types that are unknown to the Fumen library.
		/// <param name="customEventOrder">
		/// List of names of types of Events in the order they should be sorted.
		/// </param>
		/// </summary>
		public SMEventComparer(List<string> customEventOrder)
		{
			var index = 0;
			foreach (var eventString in customEventOrder)
			{
				SMEventOrderDict.Add(eventString, index);
				index++;
			}
		}

		public int Compare(Event e1, Event e2)
		{
			if (null == e1 && null == e2)
				return 0;
			if (null == e1)
				return -1;
			if (null == e2)
				return 1;

			// Order by position.
			// The order by position or by time is the same.
			// There may be events at different times that share the same row (due to e.g. Stops).
			// There may be events at different rows that share the same time (due to e.g. Warps).
			// Events at a greater time than other events must be at the same row or greater.
			// Events at a greater row than other events must be at the same time or greater.
			var comparison = e1.IntegerPosition.CompareTo(e2.IntegerPosition);
			if (comparison != 0)
				return comparison;

			// Order by lane
			if (e1 is LaneNote note1 && e2 is LaneNote note2)
			{
				comparison = note1.Lane.CompareTo(note2.Lane);
				if (comparison != 0)
					return comparison;
			}

			// Order by type
			var typeStr1 = e1.GetType().Name;
			if (e1 is Stop s1)
			{
				if (s1.IsDelay)
				{
					if (s1.LengthSeconds < 0.0)
						typeStr1 = NegativeDelayString;
					else
						typeStr1 = DelayString;
				}
				else if (s1.LengthSeconds < 0.0)
				{
					typeStr1 = NegativeStopString;
				}
			}

			var typeStr2 = e2.GetType().Name;
			if (e2 is Stop s2)
			{
				if (s2.IsDelay)
				{
					if (s2.LengthSeconds < 0.0)
						typeStr2 = NegativeDelayString;
					else
						typeStr2 = DelayString;
				}
				else if (s2.LengthSeconds < 0.0)
				{
					typeStr2 = NegativeStopString;
				}
			}

			var e1Index = SMEventOrderDict[typeStr1];
			var e2Index = SMEventOrderDict[typeStr2];
			if (e1Index >= 0 && e2Index >= 0)
				comparison = e1Index.CompareTo(e2Index);
			if (comparison != 0)
				return comparison;

			return comparison;
		}

		int IComparer<Event>.Compare(Event e1, Event e2)
		{
			return Compare(e1, e2);
		}
	}
}
