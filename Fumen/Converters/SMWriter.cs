using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fumen.Converters
{
	public class SMWriter
	{
		/// <summary>
		/// Ways to space out notes in a measure in an sm file.
		/// </summary>
		public enum MeasureSpacingBehavior
		{
			/// <summary>
			/// Use the greatest unmodified sub-division from any note in the measure.
			/// This option should be used when loading an sm file and writing it back out
			/// as it will preserve the spacing.
			/// </summary>
			UseUnmodifiedChartSubDivisions,
			/// <summary>
			/// Use the least common multiple of the sub-divisions. This will write the
			/// least number of lines for the notes in the measure. This may write sub-divisions
			/// that the stepmania editor does not support (like 24).
			/// </summary>
			UseLeastCommonMultiple,
			/// <summary>
			/// Use the least common multiple of the sub-divisions but do not write anything
			/// that the stepmania editor does not support. This will increase the sub-division
			/// if it is not supported the next supported sub-division. For example this will
			/// bump 24 to 48.
			/// </summary>
			UseLeastCommonMulitpleFromStepmaniaEditor,
		}

		/// <summary>
		/// Configuration for SMWriter when writing an sm file.
		/// </summary>
		public class SMWriterConfig
		{
			/// <summary>
			/// How to space the notes in each measure.
			/// </summary>
			public MeasureSpacingBehavior MeasureSpacingBehavior = MeasureSpacingBehavior.UseLeastCommonMultiple;
			/// <summary>
			/// The song to write.
			/// </summary>
			public Song Song;
			/// <summary>
			/// The path of the file to write to.
			/// </summary>
			public string FilePath;
		}

		/// <summary>
		/// Comparer to sort Charts by their note counts.
		/// </summary>
		private class EventCountComparer : IComparer<Chart>
		{
			int IComparer<Chart>.Compare(Chart c1, Chart c2)
			{
				if ((null == c1 || c1.Layers.Count == 0) && (null == c2 || c2.Layers.Count == 0))
					return 0;
				if (null == c1 || c1.Layers.Count == 0)
					return -1;
				if (null == c2 || c2.Layers.Count == 0)
					return 1;

				return c1.Layers[0].Events.Count.CompareTo(c2.Layers[0].Events.Count);
			}
		}

		/// <summary>
		/// Helper class to accumulate measure data when writing measures.
		/// </summary>
		private class MeasureData
		{
			/// <summary>
			/// Least common multiple of note beat sub-divisions for all notes in this measure.
			/// Used for writing the correct number of blank lines in the file.
			/// </summary>
			public int LCM = 1;
			/// <summary>
			/// Greatest unmodified denominotr of note beat sub-divisions for all notes in this
			/// measure. Used for writing the correct number of blank lines in the file.
			/// </summary>
			public int GreatestUnmodifiedDenominator = 1;
			/// <summary>
			/// Whether or not this is the first measure for the chart.
			/// </summary>
			public bool FirstMeasure = false;
			/// <summary>
			/// All LaneNotes in this measure.
			/// </summary>
			public List<LaneNote> Notes = new List<LaneNote>();
		}

		/// <summary>
		/// SMWriterConfig for controlling save behavior.
		/// </summary>
		private readonly SMWriterConfig Config;
		/// <summary>
		/// StreamWriter for writing the Song.
		/// </summary>
		private StreamWriter StreamWriter;
		/// <summary>
		/// Logger to help identify the Song in the logs.
		/// </summary>
		private readonly SMWriterLogger Logger;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="config">SMWriterConfig for configuring how to write the file.</param>
		public SMWriter(SMWriterConfig config)
		{
			Config = config;
			Logger = new SMWriterLogger(Config.FilePath);
		}

		/// <summary>
		/// Save the song using the parameters set in SMWriterConfig.
		/// </summary>
		/// <returns>True if saving was successful and false otherwise.</returns>
		public bool Save()
		{
			// TODO: Async
			// TODO: Handle non 4/4 time signatures correctly.

			var chartIndex = 0;
			var rankedCharts = new Dictionary<SMCommon.ChartType, List<Chart>>();
			var suggestedDifficultyTypes = new Dictionary<Chart, SMCommon.ChartDifficultyType>();
			foreach (var chart in Config.Song.Charts)
			{
				if (chart.Layers.Count > 1)
				{
					Logger.Warn($"Chart [{chartIndex}]. Chart has {chart.Layers.Count} Layers."
						+ " Only the first Layer will be used.");
				}

				if (!GetChartType(chart, out var smChartType))
				{
					Logger.Error($"Chart [{chartIndex}]. Could not parse type."
						+ " Type should match value from SMCommon.ChartType, or the Chart should have 1"
						+ " Player and 3, 4, 6, or 8 Inputs, or the Chart should have 2 Players and 8"
						+ $" Inputs. This chart has a Type of '{chart.Type}', {chart.NumPlayers} Players"
						+ $" and {chart.NumInputs} Inputs.");
				}
				else
				{
					if (!rankedCharts.ContainsKey(smChartType))
						rankedCharts[smChartType] = new List<Chart>();
					rankedCharts[smChartType].Add(chart);
				}
				chartIndex++;
			}

			foreach (var entry in rankedCharts)
			{
				entry.Value.Sort(new EventCountComparer());
				if (entry.Value.Count <= (int)SMCommon.ChartDifficultyType.Challenge)
				{
					var currentDifficulty = (int)SMCommon.ChartDifficultyType.Challenge;
					for (var i = entry.Value.Count - 1; i >= 0; i--)
					{
						suggestedDifficultyTypes[entry.Value[i]] = (SMCommon.ChartDifficultyType)currentDifficulty;
						currentDifficulty--;
					}
				}
				else
				{
					for (var i = entry.Value.Count - 1; i >= 0; i--)
					{
						suggestedDifficultyTypes[entry.Value[i]] =
							(SMCommon.ChartDifficultyType)Math.Min(i, (int)SMCommon.ChartDifficultyType.Edit);
					}
				}
			}

			// Stops and Tempo Changes are written using number of beats as position,
			// not a full metric position. Compute the beat values.
			var stopBeats = new Dictionary<Stop, double>();
			var tempoChangeBeats = new Dictionary<TempoChange, double>();
			var beatAtCurrentMeasureStart = 0;
			var currentMeasure = 0;
			var currentTimeSignature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure);
			if (Config.Song.Charts.Count > 0 && Config.Song.Charts[0].Layers.Count > 0)
			{
				foreach (var chartEvent in Config.Song.Charts[0].Layers[0].Events)
				{
					if (chartEvent.Position.Measure > currentMeasure)
					{
						beatAtCurrentMeasureStart +=
							currentTimeSignature.Numerator * (chartEvent.Position.Measure - currentMeasure);
						currentMeasure = chartEvent.Position.Measure;
					}

					switch (chartEvent)
					{
						case Stop stop:
							stopBeats[stop] =
								beatAtCurrentMeasureStart + stop.Position.Beat + stop.Position.SubDivision.ToDouble();
							break;
						case TempoChange tempoChange:
							tempoChangeBeats[tempoChange] =
								beatAtCurrentMeasureStart + tempoChange.Position.Beat +
								tempoChange.Position.SubDivision.ToDouble();
							break;
						case TimeSignature timeSignature:
							currentTimeSignature = timeSignature.Signature;
							break;
					}
				}
			}

			using (StreamWriter = new StreamWriter(Config.FilePath))
			{
				WriteProperty(SMCommon.TagTitle, Config.Song.Title);
				WriteProperty(SMCommon.TagSubtitle, Config.Song.SubTitle);
				WriteProperty(SMCommon.TagArtist, Config.Song.Artist);
				WriteProperty(SMCommon.TagTitleTranslit, Config.Song.TitleTransliteration);
				WriteProperty(SMCommon.TagSubtitleTranslit, Config.Song.SubTitleTransliteration);
				WriteProperty(SMCommon.TagArtistTranslit, Config.Song.ArtistTransliteration);
				WriteProperty(SMCommon.TagGenre, Config.Song.Genre);
				WritePropertyFromExtras(SMCommon.TagCredit);
				WriteProperty(SMCommon.TagBanner, Config.Song.SongSelectImage);
				WritePropertyFromExtras(SMCommon.TagBackground);
				WritePropertyFromExtras(SMCommon.TagLyricsPath);
				WritePropertyFromExtras(SMCommon.TagCDTitle);
				WritePropertyMusic();
				WritePropertyOffset();
				WriteProperty(SMCommon.TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMCommon.SMDoubleFormat));
				WriteProperty(SMCommon.TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMCommon.SMDoubleFormat));
				WritePropertyFromExtras(SMCommon.TagLastBeatHint, true);
				WritePropertyFromExtras(SMCommon.TagSelectable);
				WritePropertyFromExtras(SMCommon.TagDisplayBPM, true);
				WritePropertyBPMs(tempoChangeBeats);
				WritePropertyStops(stopBeats);
				WritePropertyFromExtras(SMCommon.TagTimeSignatures, true);
				WritePropertyFromExtras(SMCommon.TagBGChanges, true);
				WritePropertyFromExtras(SMCommon.TagBGChanges1, true);
				WritePropertyFromExtras(SMCommon.TagBGChanges2, true);
				WritePropertyFromExtras(SMCommon.TagFGChanges, true);
				// TODO: Write keysounds properly
				WritePropertyFromExtras(SMCommon.TagKeySounds);
				WritePropertyFromExtras(SMCommon.TagAttacks);

				StreamWriter.WriteLine();

				foreach (var chart in Config.Song.Charts)
				{
					WriteChart(chart, suggestedDifficultyTypes[chart]);
				}
			}
			StreamWriter = null;

			return true;
		}

		private bool TryGetSongExtra(string sKey, out object value)
		{
			value = null;
			if (Config.Song.DestExtras.TryGetValue(sKey, out value))
				return true;
			if (Config.Song.SourceType == FileFormatType.SM && Config.Song.SourceExtras.TryGetValue(sKey, out value))
				return true;
			return false;
		}

		private void WritePropertyFromExtras(string smPropertyName, bool skipIfNotInExtras = false)
		{
			if (!TryGetSongExtra(smPropertyName, out var value) && skipIfNotInExtras)
				return;

			if (value == null)
			{
				WriteProperty(smPropertyName, "");
			}
			else if (value is List<string> valAsList)
			{
				StreamWriter.Write($"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}");
				var first = true;
				foreach (var entry in valAsList)
				{
					if (!first)
					{
						StreamWriter.Write(MSDFile.ParamMarker);
					}
					StreamWriter.Write(MSDFile.Escape(entry));
					first = false;
				}
				StreamWriter.WriteLine(MSDFile.ValueEndMarker);
			}
			else if (value is double d)
			{
				WriteProperty(smPropertyName, d.ToString(SMCommon.SMDoubleFormat));
			}
			else
			{
				WriteProperty(smPropertyName, value.ToString());
			}
		}

		private void WriteProperty(string smPropertyName, string value)
		{
			if (string.IsNullOrEmpty(value))
				StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}{MSDFile.ValueEndMarker}");
			else
				StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}{MSDFile.Escape(value)}{MSDFile.ValueEndMarker}");
		}

		private void WritePropertyMusic()
		{
			if (!TryGetSongExtra(SMCommon.TagMusic, out var value) && Config.Song.Charts.Count > 0)
				value = Config.Song.Charts[0].MusicFile;
			WriteProperty(SMCommon.TagMusic, value.ToString());
		}

		private void WritePropertyOffset()
		{
			if (!TryGetSongExtra(SMCommon.TagOffset, out var value) && Config.Song.Charts.Count > 0)
				value = Config.Song.Charts[0].ChartOffsetFromMusic.ToString(SMCommon.SMDoubleFormat);
			WriteProperty(SMCommon.TagOffset, value.ToString());
		}

		private void WritePropertyBPMs(Dictionary<TempoChange, double> tempoChanges)
		{
			if (Config.Song.Charts.Count == 0 || Config.Song.Charts[0].Layers.Count == 0)
			{
				WriteProperty(SMCommon.TagBPMs, "");
				return;
			}

			// If we have a raw string from the source file, use it.
			// Stepmania sm files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (Config.Song.SourceType == FileFormatType.SM
				&& Config.Song.SourceExtras.ContainsKey(SMCommon.TagFumenRawBpmsStr)
				&& Config.Song.SourceExtras[SMCommon.TagFumenRawBpmsStr] is string)
			{
				WriteProperty(SMCommon.TagBPMs, (string)Config.Song.SourceExtras[SMCommon.TagFumenRawBpmsStr]);
				return;
			}

			var numTempoChangeEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in Config.Song.Charts[0].Layers[0].Events)
			{
				if (!(e is TempoChange tc))
					continue;
				numTempoChangeEvents++;
				if (numTempoChangeEvents > 1)
					sb.Append(",\r\n");

				// If present, use the original double value from the source chart so as not to lose
				// or alter the precision.
				var timeInBeats = tempoChanges[tc];
				if (Config.Song.SourceType == FileFormatType.SM
					&& tc.SourceExtras.ContainsKey(SMCommon.TagFumenDoublePosition)
					&& tc.SourceExtras[SMCommon.TagFumenDoublePosition] is double)
				{
					timeInBeats = (double)tc.SourceExtras[SMCommon.TagFumenDoublePosition];
				}
				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				var tempoStr = tc.TempoBPM.ToString(SMCommon.SMDoubleFormat);
				sb.Append($"{timeInBeatsStr}={tempoStr}");
			}
			WriteProperty(SMCommon.TagBPMs, sb.ToString());
		}

		private void WritePropertyStops(Dictionary<Stop, double> stops)
		{
			if (Config.Song.Charts.Count == 0 || Config.Song.Charts[0].Layers.Count == 0)
			{
				WriteProperty(SMCommon.TagStops, "");
				return;
			}

			// If we have a raw string from the source file, use it.
			// Stepmania sm files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (Config.Song.SourceType == FileFormatType.SM
				&& Config.Song.SourceExtras.ContainsKey(SMCommon.TagFumenRawStopsStr)
				&& Config.Song.SourceExtras[SMCommon.TagFumenRawStopsStr] is string)
			{
				WriteProperty(SMCommon.TagStops, (string)Config.Song.SourceExtras[SMCommon.TagFumenRawStopsStr]);
				return;
			}

			var numStopEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in Config.Song.Charts[0].Layers[0].Events)
			{
				if (!(e is Stop stop))
					continue;
				numStopEvents++;
				if (numStopEvents > 1)
					sb.Append(",\r\n");

				// If present, use the original double values from the source chart so as not to lose
				// or alter the precision.
				var timeInBeats = stops[stop];
				if (Config.Song.SourceType == FileFormatType.SM
					&& stop.SourceExtras.ContainsKey(SMCommon.TagFumenDoublePosition)
					&& stop.SourceExtras[SMCommon.TagFumenDoublePosition] is double)
				{
					timeInBeats = (double)stop.SourceExtras[SMCommon.TagFumenDoublePosition];
				}
				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				var length = stop.LengthMicros / 1000000.0;
				if (Config.Song.SourceType == FileFormatType.SM
					&& stop.SourceExtras.ContainsKey(SMCommon.TagFumenDoubleValue)
					&& stop.SourceExtras[SMCommon.TagFumenDoubleValue] is double)
				{
					length = (double)stop.SourceExtras[SMCommon.TagFumenDoubleValue];
				}
				var lengthStr = length.ToString(SMCommon.SMDoubleFormat);

				sb.Append($"{timeInBeatsStr}={lengthStr}");
			}
			WriteProperty(SMCommon.TagStops, sb.ToString());
		}

		private static bool GetChartType(Chart chart, out SMCommon.ChartType chartType)
		{
			if (Enum.TryParse(chart.Type, out chartType))
				return true;

			chartType = SMCommon.ChartType.dance_single;
			if (chart.NumPlayers == 1)
			{
				if (chart.NumInputs <= 3)
				{
					chartType = SMCommon.ChartType.dance_threepanel;
					return true;
				}
				if (chart.NumInputs == 4)
				{
					chartType = SMCommon.ChartType.dance_single;
					return true;
				}
				if (chart.NumInputs <= 6)
				{
					chartType = SMCommon.ChartType.dance_solo;
					return true;
				}
				if (chart.NumInputs <= 8)
				{
					chartType = SMCommon.ChartType.dance_double;
					return true;
				}
			}
			if (chart.NumPlayers == 2)
			{
				if (chart.NumInputs <= 8)
				{
					chartType = SMCommon.ChartType.dance_routine;
					return true;
				}
			}

			return false;
		}

		private static string GetChartDifficultyType(Chart chart, SMCommon.ChartDifficultyType suggestedDifficultyType)
		{
			if (Enum.IsDefined(typeof(SMCommon.ChartDifficultyType), chart.DifficultyType))
				return chart.DifficultyType;
			return suggestedDifficultyType.ToString();
		}

		private char GetSMCharForNote(LaneNote note)
		{
			if (note.DestType?.Length == 1
				 && SMCommon.SNoteChars.Contains(note.DestType[0]))
				return note.DestType[0];

			if (Config.Song.SourceType == FileFormatType.SM
				&& note.SourceType.Length == 1
				&& SMCommon.SNoteChars.Contains(note.SourceType[0]))
				return note.SourceType[0];

			if (note is LaneTapNote)
				return SMCommon.SNoteChars[(int)SMCommon.NoteType.Tap];
			if (note is LaneHoldEndNote)
				return SMCommon.SNoteChars[(int)SMCommon.NoteType.HoldEnd];
			if (note is LaneHoldStartNote)
				return SMCommon.SNoteChars[(int)SMCommon.NoteType.HoldStart];
			return SMCommon.SNoteChars[(int)SMCommon.NoteType.None];
		}

		private string GetRadarValues(Chart chart)
		{
			var radarValues = new List<double>();
			if (chart.DestExtras.ContainsKey(SMCommon.TagRadarValues)
			    && chart.DestExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>) chart.DestExtras[SMCommon.TagRadarValues];
			}
			else if (Config.Song.SourceType == FileFormatType.SM
			         && chart.SourceExtras.ContainsKey(SMCommon.TagRadarValues)
			         && chart.SourceExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>) chart.SourceExtras[SMCommon.TagRadarValues];
			}

			if (radarValues.Count <= 0)
				return "";
			
			var sb = new StringBuilder();
			var bFirst = true;
			foreach (var val in radarValues)
			{
				if (!bFirst)
					sb.Append(",");
				sb.Append(val.ToString(SMCommon.SMDoubleFormat));
				bFirst = false;
			}

			return sb.ToString();
		}

		private void WriteChart(Chart chart, SMCommon.ChartDifficultyType suggestedDifficultyType)
		{
			if (!GetChartType(chart, out var chartType))
				return;

			var charTypeStr = chartType.ToString().Replace('_', '-');
			var chartDifficultyType = GetChartDifficultyType(chart, suggestedDifficultyType);
			var radarValues = GetRadarValues(chart);

			// Check for writing this chart using the NOTES2 tag.
			var notesType = SMCommon.TagNotes;
			if (chart.DestExtras.ContainsKey(SMCommon.TagFumenNotesType)
				&& chart.DestExtras[SMCommon.TagFumenNotesType] is string)
			{
				notesType = (string)chart.DestExtras[SMCommon.TagFumenNotesType];
			}
			else if (Config.Song.SourceType == FileFormatType.SM
					 && chart.SourceExtras.ContainsKey(SMCommon.TagFumenNotesType)
					 && chart.SourceExtras[SMCommon.TagFumenNotesType] is string)
			{
				notesType = (string)chart.SourceExtras[SMCommon.TagFumenNotesType];
			}

			// Write chart header.
			StreamWriter.WriteLine($"//---------------{charTypeStr} - {MSDFile.Escape(chart.Description)}----------------");
			StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{notesType}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {charTypeStr}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {MSDFile.Escape(chart.Description)}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {chartDifficultyType}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {(int)chart.DifficultyRating}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {radarValues}{MSDFile.ParamMarker}");

			// Write one chart per player.
			for (var playerIndex = 0; playerIndex < chart.NumPlayers; playerIndex++)
			{
				// Marker to separate players' charts.
				if (playerIndex > 0)
					StreamWriter.Write("&\r\n\r\n");

				var measures = new List<MeasureData>();
				// Add one measure so even blank charts write one empty measure.
				measures.Add(new MeasureData { FirstMeasure = true });

				// Accumulate data about each measure.
				foreach (var chartEvent in chart.Layers[0].Events)
				{
					var note = chartEvent as LaneNote;
					if (note == null || note.Player != playerIndex)
						continue;

					var measure = note.Position.Measure;
					while (measures.Count <= measure)
						measures.Add(new MeasureData());

					measures[measure].Notes.Add(note);
					measures[measure].GreatestUnmodifiedDenominator = Math.Max(
						measures[measure].GreatestUnmodifiedDenominator, note.Position.SubDivision.Denominator);
					measures[measure].LCM = Fraction.LeastCommonMultiple(
						note.Position.SubDivision.Reduce().Denominator,
						measures[measure].LCM);
				}

				// Write each measure.
				foreach (var measureData in measures)
					WriteMeasure(chart, measureData);
			}
			// Mark the chart as complete.
			StreamWriter.Write($"{MSDFile.ValueEndMarker}\r\n\r\n");
		}

		private void WriteMeasure(Chart chart, MeasureData measureData)
		{
			int linesPerBeat = 1;
			switch (Config.MeasureSpacingBehavior)
			{
				case MeasureSpacingBehavior.UseUnmodifiedChartSubDivisions:
				{
					linesPerBeat = measureData.GreatestUnmodifiedDenominator;
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMultiple:
				{
					linesPerBeat = measureData.LCM;
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMulitpleFromStepmaniaEditor:
				{
					// Make sure the notes can actually be represented by the stepmania editor.
					if (!SMCommon.GetLowestValidSMSubDivision(measureData.LCM, out linesPerBeat))
					{
						// TODO: Better error logging.
						Logger.Error($"Unsupported subdivisions {measureData.LCM}.");
					}
					break;
				}
			}

			// Set up a grid of characters to write.
			// TODO: Support keysound tagging.
			var measureCharsDX = chart.NumInputs;
			var measureCharsDY = SMCommon.NumBeatsPerMeasure * linesPerBeat;
			var measureChars = new char[measureCharsDX, measureCharsDY];

			// Populate characters in the grid based on the events of the measure.
			foreach (var measureNote in measureData.Notes)
			{
				// Get the note char to write.
				var c = GetSMCharForNote(measureNote);

				// Determine the position to record the note.
				var reducedSubDivision = measureNote.Position.SubDivision.Reduce();
				var measureEventPositionInMeasure =
					measureNote.Position.Beat * linesPerBeat
					+ (linesPerBeat / Math.Max(1, reducedSubDivision.Denominator))
					* reducedSubDivision.Numerator;

				// Record the note.
				measureChars[measureNote.Lane, measureEventPositionInMeasure] = c;
			}

			// Write the measure of accumulated characters.
			if (!measureData.FirstMeasure)
				StreamWriter.WriteLine(",");
			for (var y = 0; y < measureCharsDY; y++)
			{
				for (var x = 0; x < measureCharsDX; x++)
				{
					StreamWriter.Write(measureChars[x, y] == '\0' ?
						SMCommon.SNoteChars[(int)SMCommon.NoteType.None]
						: measureChars[x, y]);
				}
				StreamWriter.Write("\r\n");
			}
		}
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	public class SMWriterLogger : ILogger
	{
		private readonly string FilePath;
		private const string Tag = "[SM Writer]";

		public SMWriterLogger(string filePath)
		{
			FilePath = filePath;
		}

		public void Info(string message)
		{
			if (!string.IsNullOrEmpty(FilePath))
				Logger.Info($"{Tag} [{FilePath}] {message}");
			else
				Logger.Info($"{Tag} {message}");
		}

		public void Warn(string message)
		{
			if (!string.IsNullOrEmpty(FilePath))
				Logger.Warn($"{Tag} [{FilePath}] {message}");
			else
				Logger.Warn($"{Tag} {message}");
		}

		public void Error(string message)
		{
			if (!string.IsNullOrEmpty(FilePath))
				Logger.Error($"{Tag} [{FilePath}] {message}");
			else
				Logger.Error($"{Tag} {message}");
		}
	}
}
