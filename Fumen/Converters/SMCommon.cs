using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fumen.Converters
{
	public static class SMCommon
	{
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
		}

		public static string ChartTypeString(ChartType type)
		{
			return type.ToString().Replace("_", "-");
		}

		public enum ChartDifficultyType
		{
			Beginner,
			Easy,
			Medium,
			Hard,
			Challenge,
			Edit
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
			public int NumInputs { get; set; }
			public int NumPlayers { get; set; }
		}

		public const int NumBeatsPerMeasure = 4;

		public static readonly int[] ValidDenominators =
		{
			1,	// Quarter note
			2,	// Eighth note
			3,	// Eighth note triplet (Twelfth note)
			4,	// Sixteenth note
			6,	// Sixteenth note triplet (Twenty-fourth note)
			8,	// Thirty-second note
			12,	// Thirty-second note triplet (Forty-eighth note)
			16,	// Sixty-fourth note
			48  // One-hundred-ninety-second note
		};

		public static readonly char[] SMAllWhiteSpace = { '\r', '\n', ' ', '\t' };

		public static readonly List<Fraction> SubDivisions = new List<Fraction>();
		public static readonly List<double> SubDivisionLengths = new List<double>();
		public static readonly ChartProperties[] Properties;
		public static readonly char[] NoteChars = { '0', '1', '2', '3', '4', 'M', 'L', 'F', 'K' };
		public static readonly string[] NoteStrings =
		{
			"None", "Tap", "Hold Start", "Hold or Roll End", "Roll Start", "Mine", "Lift", "Fake", "KeySound"
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
		public const string TagFumenRawBpmsStr = "FumenRawBpmsStr";
		public const string TagFumenChartUsesOwnTimingData = "FumenChartUsesOwnTimingData";

		public const string SMDoubleFormat = "N6";

		/// <summary>
		/// Static initialization.
		/// </summary>
		static SMCommon()
		{
			// Initialize valid SM SubDivisions.
			SubDivisions.Add(new Fraction(0, 0));
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
			Properties[(int)ChartType.dance_single] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			Properties[(int)ChartType.dance_double] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.dance_couple] = new ChartProperties { NumInputs = 8, NumPlayers = 2 };
			Properties[(int)ChartType.dance_solo] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.dance_threepanel] = new ChartProperties { NumInputs = 3, NumPlayers = 1 };
			Properties[(int)ChartType.dance_routine] = new ChartProperties { NumInputs = 8, NumPlayers = 2 };
			Properties[(int)ChartType.pump_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			Properties[(int)ChartType.pump_halfdouble] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.pump_double] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			Properties[(int)ChartType.pump_couple] = new ChartProperties { NumInputs = 10, NumPlayers = 2 };
			Properties[(int)ChartType.pump_routine] = new ChartProperties { NumInputs = 10, NumPlayers = 2 };
			Properties[(int)ChartType.kb7_single] = new ChartProperties { NumInputs = 7, NumPlayers = 1 };
			Properties[(int)ChartType.ez2_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			Properties[(int)ChartType.ez2_double] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			Properties[(int)ChartType.ez2_real] = new ChartProperties { NumInputs = 7, NumPlayers = 1 };
			Properties[(int)ChartType.para_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			Properties[(int)ChartType.ds3ddx_single] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.bm_single5] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.bm_versus5] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.bm_double5] = new ChartProperties { NumInputs = 12, NumPlayers = 1 };
			Properties[(int)ChartType.bm_single7] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.bm_versus7] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.bm_double7] = new ChartProperties { NumInputs = 16, NumPlayers = 1 };
			Properties[(int)ChartType.maniax_single] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			Properties[(int)ChartType.maniax_double] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.techno_single4] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			Properties[(int)ChartType.techno_single5] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			Properties[(int)ChartType.techno_single8] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.techno_double4] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			Properties[(int)ChartType.techno_double5] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			Properties[(int)ChartType.techno_double8] = new ChartProperties { NumInputs = 16, NumPlayers = 1 };
			Properties[(int)ChartType.pnm_five] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			Properties[(int)ChartType.pnm_nine] = new ChartProperties { NumInputs = 9, NumPlayers = 1 };
			Properties[(int)ChartType.lights_cabinet] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.kickbox_human] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			Properties[(int)ChartType.kickbox_quadarm] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			Properties[(int)ChartType.kickbox_insect] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			Properties[(int)ChartType.kickbox_arachnid] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
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
						return fractionAsDouble - SubDivisionLengths[midIndex - 1] <
							   SubDivisionLengths[midIndex] - fractionAsDouble
							? SubDivisions[midIndex - 1]
							: SubDivisions[midIndex];

					// Advance search
					rightIndex = midIndex;
				}

				// Value is greater than midpoint, search to the right.
				else if (fractionAsDouble > SubDivisionLengths[midIndex])
				{
					// Value is between midpoint and adjacent.
					if (midIndex < length - 1 && fractionAsDouble < SubDivisionLengths[midIndex + 1])
						return fractionAsDouble - SubDivisionLengths[midIndex] <
							   SubDivisionLengths[midIndex + 1] - fractionAsDouble
							? SubDivisions[midIndex]
							: SubDivisions[midIndex + 1];

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
		/// <param name="lowestValidSMSubDivison">
		/// Out parameter to hold the lowest valid sub-division which stepmania supports.
		/// </param>
		/// <returns>
		/// True if a valid sub-division was found and false otherwise.
		/// </returns>
		public static bool GetLowestValidSMSubDivision(int desiredSubDivision, out int lowestValidSMSubDivison)
		{
			lowestValidSMSubDivison = desiredSubDivision;
			var highestDenominator = ValidDenominators[ValidDenominators.Length - 1];
			do
			{
				var found = false;
				foreach (var validDenominator in ValidDenominators)
				{
					if (desiredSubDivision == validDenominator)
					{
						lowestValidSMSubDivison = desiredSubDivision;
						return true;
					}
				}
				if (found)
					break;
				desiredSubDivision <<= 1;
			}
			while (desiredSubDivision <= highestDenominator);
			return false;
		}

		/// <summary>
		/// Adds TempoChange Events to the given Chart from the given Dictionary of
		/// position to tempo values parsed from the Chart or Song.
		/// </summary>
		/// <param name="tempos">
		/// Dictionary of time to value of tempos parsed from the Song or Chart.
		/// </param>
		/// <param name="chart">Chart to add TempoChange Events to.</param>
		public static void AddTempos(Dictionary<double, double> tempos, Chart chart)
		{
			// Insert tempo change events.
			foreach (var tempo in tempos)
			{
				var tempoChangeEvent = new TempoChange()
				{
					Position = new MetricPosition(
						(int)tempo.Key / NumBeatsPerMeasure,
						(int)tempo.Key % NumBeatsPerMeasure,
						FindClosestSMSubDivision(tempo.Key - (int)tempo.Key)),
					TempoBPM = tempo.Value
				};

				// Record the actual doubles.
				tempoChangeEvent.SourceExtras.Add(TagFumenDoublePosition, tempo.Key);
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
		public static void AddStops(Dictionary<double, double> stops, Chart chart)
		{
			foreach (var stop in stops)
			{
				var stopEvent = new Stop()
				{
					Position = new MetricPosition(
						(int)stop.Key / NumBeatsPerMeasure,
						(int)stop.Key % NumBeatsPerMeasure,
						FindClosestSMSubDivision(stop.Key - (int)stop.Key)),
					LengthMicros = (long)(stop.Value * 1000000.0)
				};

				// Record the actual doubles.
				stopEvent.SourceExtras.Add(TagFumenDoublePosition, stop.Key);
				stopEvent.SourceExtras.Add(TagFumenDoubleValue, stop.Value);

				chart.Layers[0].Events.Add(stopEvent);
			}
		}

		/// <summary>
		/// Tries to get a string representing the BPM to set as the Tempo on
		/// a Chart for display purposes. First tries to get the DisplayBPM
		/// from the source extras, and convert that list to a string. Failing that
		/// it tries to look through the provided tempo events from the Song or Chart
		/// and use the one at position 0.0.
		/// </summary>
		/// <param name="sourceExtras">
		/// SourceExtras from Song or Chart to check the TagDisplayBPM value.
		/// </param>
		/// <param name="tempos">
		/// Dictionary of time to value of tempos parsed from the Song or Chart.
		/// </param>
		/// <returns>String representation of display tempo to use.</returns>
		public static string GetDisplayBPMStringFromSourceExtrasList(
			Dictionary<string, object> sourceExtras,
			Dictionary<double, double> tempos)
		{
			var displayTempo = "";
			if (sourceExtras.TryGetValue(TagDisplayBPM, out var chartDisplayTempoObj))
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
			else if (tempos.ContainsKey(0.0))
				displayTempo = tempos[0.0].ToString("N3");
			return displayTempo;
		}

		/// <summary>
		/// Custom Comparer for Events in an SM Chart.
		/// </summary>
		public class SMEventComparer : IComparer<Event>
		{
			private static readonly List<Type> SMEventOrder = new List<Type>
			{
				typeof(TimeSignature),
				typeof(TempoChange),
				typeof(LaneTapNote),
				typeof(LaneHoldStartNote),
				typeof(LaneHoldEndNote),
				typeof(Stop),	// Stops occur after other notes at the same time.
			};

			int IComparer<Event>.Compare(Event e1, Event e2)
			{
				if (null == e1 && null == e2)
					return 0;
				if (null == e1)
					return -1;
				if (null == e2)
					return 1;

				// Order by time / position
				var comparison = e1.CompareTo(e2);
				if (comparison != 0)
					return comparison;

				// Order by lane
				if (e1 is LaneNote note1 && e2 is LaneNote note2)
					comparison = note1.Lane.CompareTo(note2.Lane);

				// Order by type
				var e1Index = SMEventOrder.IndexOf(e1.GetType());
				var e2Index = SMEventOrder.IndexOf(e2.GetType());
				if (e1Index >= 0 && e2Index >= 0)
					comparison = e1Index.CompareTo(e2Index);
				if (comparison != 0)
					return comparison;

				return comparison;
			}
		}
	}
}
