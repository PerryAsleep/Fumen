using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
		public const string TagSampleStart = "SAMPLESTART";
		public const string TagSampleLength = "SAMPLELENGTH";
		public const string TagSelectable = "SELECTABLE";
		public const string TagDisplayBPM = "DISPLAYBPM";
		// ReSharper disable once InconsistentNaming
		public const string TagBPMs = "BPMS";
		public const string TagStops = "STOPS";
		public const string TagTimeSignatures = "TIMESIGNATURES";
		public const string TagBGChanges = "BGCHANGES";
		public const string TagFGChanges = "FGCHANGES";
		public const string TagKeySounds = "KEYSOUNDS";
		public const string TagAttacks = "ATTACKS";
		public const string TagMenuColor = "MENUCOLOR";
		public const string TagNotes = "NOTES";
		public const string TagRadarValues = "RADARVALUES";

		public const string SMDoubleFormat = "N6";

		/// <summary>
		/// Static initialization.
		/// </summary>
		static SMCommon()
		{
			// Initialize valid SM SubDivisions.
			var validDenominators = new[] {
				2,	// Eighth note
				3,	// Eighth note triplet (Twelfth note)
				4,	// Sixteenth note
				6,	// Sixteenth note triplet (Twenty-fourth note)
				8,	// Thirty-second note
				12,	// Thirty-second note triplet (Forty-eighth note)
				16,	// Sixty-fourth note
				48};// One-hundred-ninety-second note
			SSubDivisions.Add(new Fraction(0, 0));
			foreach (var denominator in validDenominators)
			{
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
