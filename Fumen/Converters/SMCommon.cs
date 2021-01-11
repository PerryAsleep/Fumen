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

		public static readonly int[] ValidDenominators = new int[]
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

		public static readonly char[] SMAllWhiteSpace = new[] { '\r', '\n', ' ', '\t' };

		public static readonly List<Fraction> SSubDivisions = new List<Fraction>();
		public static readonly List<double> SSubDivisionLengths = new List<double>();
		public static readonly ChartProperties[] SChartProperties;
		public static readonly char[] SNoteChars = { '0', '1', '2', '3', '4', 'M', 'L', 'F', 'K' };

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
		public const string TickCounts = "TICKCOUNTS";
		public const string InstrumentTrack = "INSTRUMENTTRACK";
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

		public const string TagFumenKeySoundIndex = "FumenKSI";
		public const string TagFumenDoublePosition = "FumenDoublePos";
		public const string TagFumenDoubleValue = "FumenDoubleVal";
		public const string TagFumenNotesType = "FumenNotesType";
		public const string TagFumenRawStopsStr = "FumenRawStopsStr";
		public const string TagFumenRawBpmsStr = "FumenRawBpmsStr";

		public const string SMDoubleFormat = "N6";

		/// <summary>
		/// Static initialization.
		/// </summary>
		static SMCommon()
		{
			// Initialize valid SM SubDivisions.
			SSubDivisions.Add(new Fraction(0, 0));
			foreach (var denominator in ValidDenominators)
			{
				if (denominator <= 1)
					continue;
				for (var numerator = 0; numerator < denominator; numerator++)
				{
					var fraction = new Fraction(numerator, denominator);
					if (!SSubDivisions.Contains(fraction))
						SSubDivisions.Add(fraction);
				}
			}

			SSubDivisions.Sort();
			foreach (var subDivision in SSubDivisions)
				SSubDivisionLengths.Add(subDivision.ToDouble());

			// Initialize ChartProperties.
			SChartProperties = new ChartProperties[Enum.GetNames(typeof(ChartType)).Length];
			SChartProperties[(int)ChartType.dance_single] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			SChartProperties[(int)ChartType.dance_double] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.dance_couple] = new ChartProperties { NumInputs = 8, NumPlayers = 2 };
			SChartProperties[(int)ChartType.dance_solo] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.dance_threepanel] = new ChartProperties { NumInputs = 3, NumPlayers = 1 };
			SChartProperties[(int)ChartType.dance_routine] = new ChartProperties { NumInputs = 8, NumPlayers = 2 };
			SChartProperties[(int)ChartType.pump_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			SChartProperties[(int)ChartType.pump_halfdouble] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.pump_double] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			SChartProperties[(int)ChartType.pump_couple] = new ChartProperties { NumInputs = 10, NumPlayers = 2 };
			SChartProperties[(int)ChartType.pump_routine] = new ChartProperties { NumInputs = 10, NumPlayers = 2 };
			SChartProperties[(int)ChartType.kb7_single] = new ChartProperties { NumInputs = 7, NumPlayers = 1 };
			SChartProperties[(int)ChartType.ez2_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			SChartProperties[(int)ChartType.ez2_double] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			SChartProperties[(int)ChartType.ez2_real] = new ChartProperties { NumInputs = 7, NumPlayers = 1 };
			SChartProperties[(int)ChartType.para_single] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			SChartProperties[(int)ChartType.ds3ddx_single] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_single5] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_versus5] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_double5] = new ChartProperties { NumInputs = 12, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_single7] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_versus7] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.bm_double7] = new ChartProperties { NumInputs = 16, NumPlayers = 1 };
			SChartProperties[(int)ChartType.maniax_single] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			SChartProperties[(int)ChartType.maniax_double] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_single4] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_single5] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_single8] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_double4] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_double5] = new ChartProperties { NumInputs = 10, NumPlayers = 1 };
			SChartProperties[(int)ChartType.techno_double8] = new ChartProperties { NumInputs = 16, NumPlayers = 1 };
			SChartProperties[(int)ChartType.pnm_five] = new ChartProperties { NumInputs = 5, NumPlayers = 1 };
			SChartProperties[(int)ChartType.pnm_nine] = new ChartProperties { NumInputs = 9, NumPlayers = 1 };
			SChartProperties[(int)ChartType.lights_cabinet] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.kickbox_human] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			SChartProperties[(int)ChartType.kickbox_quadarm] = new ChartProperties { NumInputs = 4, NumPlayers = 1 };
			SChartProperties[(int)ChartType.kickbox_insect] = new ChartProperties { NumInputs = 6, NumPlayers = 1 };
			SChartProperties[(int)ChartType.kickbox_arachnid] = new ChartProperties { NumInputs = 8, NumPlayers = 1 };
		}

		public static void LogInfo(string message)
		{
			Logger.Info($"[SM] {message}");
		}
		public static void LogWarn(string message)
		{
			Logger.Warn($"[SM] {message}");
		}
		public static void LogError(string message)
		{
			Logger.Error($"[SM] {message}");
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
			var length = SSubDivisionLengths.Count;

			// Edge cases
			if (fractionAsDouble <= SSubDivisionLengths[0])
				return SSubDivisions[0];
			if (fractionAsDouble >= SSubDivisionLengths[length - 1])
				return SSubDivisions[length - 1];

			// Search
			int leftIndex = 0, rightIndex = length, midIndex = 0;
			while (leftIndex < rightIndex)
			{
				midIndex = (leftIndex + rightIndex) >> 1;

				// Value is less than midpoint, search to the left.
				if (fractionAsDouble < SSubDivisionLengths[midIndex])
				{
					// Value is between midpoint and adjacent.
					if (midIndex > 0 && fractionAsDouble > SSubDivisionLengths[midIndex - 1])
						return fractionAsDouble - SSubDivisionLengths[midIndex - 1] <
							   SSubDivisionLengths[midIndex] - fractionAsDouble
							? SSubDivisions[midIndex - 1]
							: SSubDivisions[midIndex];

					// Advance search
					rightIndex = midIndex;
				}

				// Value is greater than midpoint, search to the right.
				else if (fractionAsDouble > SMCommon.SSubDivisionLengths[midIndex])
				{
					// Value is between midpoint and adjacent.
					if (midIndex < length - 1 && fractionAsDouble < SSubDivisionLengths[midIndex + 1])
						return fractionAsDouble - SSubDivisionLengths[midIndex] <
							   SSubDivisionLengths[midIndex + 1] - fractionAsDouble
							? SSubDivisions[midIndex]
							: SSubDivisions[midIndex + 1];

					// Advance search
					leftIndex = midIndex + 1;
				}

				// Value equals midpoint.
				else
				{
					return SSubDivisions[midIndex];
				}
			}
			return SSubDivisions[midIndex];
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
				var timeComparison = e1.CompareTo(e2);
				if (timeComparison != 0)
					return timeComparison;

				// Order by type
				var e1Index = SMEventOrder.IndexOf(e1.GetType());
				var e2Index = SMEventOrder.IndexOf(e2.GetType());
				if (e1Index >= 0 && e2Index >= 0)
					return e1Index.CompareTo(e2Index);

				// Order by lane
				if (e1 is LaneNote note1 && e2 is LaneNote note2)
					return note1.Lane.CompareTo(note2.Lane);

				return 0;
			}
		}
	}
}
