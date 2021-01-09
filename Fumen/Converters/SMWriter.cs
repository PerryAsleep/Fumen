using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fumen.Converters
{
	public class SMWriter
	{
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

		private static string Escape(string input)
		{
			return input
				.Replace(@";", @"\;")
				.Replace(@":", @"\:")
				.Replace(@"\", @"\\")
				.Replace(@"/", @"\/");
		}

		private static bool TryGetSongExtra(string sKey, Song song, out object value)
		{
			value = null;
			if (song.DestExtras.TryGetValue(sKey, out value))
				return true;
			if (song.SourceType == FileFormatType.SM && song.SourceExtras.TryGetValue(sKey, out value))
				return true;
			return false;
		}

		private static void WritePropertyFromExtras(string smPropertyName, Song song, StreamWriter streamWriter)
		{
			TryGetSongExtra(smPropertyName, song, out var value);
			WriteProperty(smPropertyName, value == null ? "" : value.ToString(), streamWriter);
		}

		private static void WriteProperty(string smPropertyName, string value, StreamWriter streamWriter)
		{
			if (string.IsNullOrEmpty(value))
				streamWriter.WriteLine($"#{smPropertyName}:;");
			else
				streamWriter.WriteLine($"#{smPropertyName}:{Escape(value)};");
		}

		private static void WritePropertyMusic(Song song, StreamWriter streamWriter)
		{
			if (!TryGetSongExtra(SMCommon.TagMusic, song, out var value) && song.Charts.Count > 0)
				value = song.Charts[0].MusicFile;
			WriteProperty(SMCommon.TagMusic, value.ToString(), streamWriter);
		}

		private static void WritePropertyOffset(Song song, StreamWriter streamWriter)
		{
			if (!TryGetSongExtra(SMCommon.TagOffset, song, out var value) && song.Charts.Count > 0)
				value = song.Charts[0].ChartOffsetFromMusic.ToString(SMCommon.SMDoubleFormat);
			WriteProperty(SMCommon.TagOffset, value.ToString(), streamWriter);
		}

		private static void WritePropertyBPMs(Song song, Dictionary<TempoChange, double> tempoChanges, StreamWriter streamWriter)
		{
			if (song.Charts.Count == 0 || song.Charts[0].Layers.Count == 0)
			{
				WriteProperty(SMCommon.TagBPMs, "", streamWriter);
				return;
			}

			var numTempoChangeEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in song.Charts[0].Layers[0].Events)
			{
				if (!(e is TempoChange tc))
					continue;
				numTempoChangeEvents++;
				if (numTempoChangeEvents > 1)
					sb.Append(",\n");
				var tempoString = tc.TempoBPM.ToString(SMCommon.SMDoubleFormat);
				var timeInBeats = tempoChanges[tc].ToString(SMCommon.SMDoubleFormat);
				sb.Append($"{timeInBeats}={tempoString}");
			}
			WriteProperty(SMCommon.TagBPMs, sb.ToString(), streamWriter);
		}

		private static void WritePropertyStops(Song song, Dictionary<Stop, double> stops, StreamWriter streamWriter)
		{
			if (song.Charts.Count == 0 || song.Charts[0].Layers.Count == 0)
			{
				WriteProperty(SMCommon.TagStops, "", streamWriter);
				return;
			}

			var numStopEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in song.Charts[0].Layers[0].Events)
			{
				if (!(e is Stop stop))
					continue;
				numStopEvents++;
				if (numStopEvents > 1)
					sb.Append(",\n");
				var lengthString = (stop.LengthMicros / 1000000.0).ToString(SMCommon.SMDoubleFormat);
				var timeInBeats = stops[stop].ToString(SMCommon.SMDoubleFormat);
				sb.Append($"{timeInBeats}={lengthString}");
			}
			WriteProperty(SMCommon.TagStops, sb.ToString(), streamWriter);
		}

		private static bool GetChartType(Song song, Chart chart, out SMCommon.ChartType chartType)
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

		private static string GetChartDifficultyType(Song song, Chart chart, SMCommon.ChartDifficultyType suggestedDifficultyType)
		{
			if (Enum.IsDefined(typeof(SMCommon.ChartDifficultyType), chart.DifficultyType))
				return chart.DifficultyType;
			return suggestedDifficultyType.ToString();
		}

		private static char GetSMCharForNote(Song song, LaneNote note)
		{
			if (note.DestType?.Length == 1
				 && SMCommon.SNoteChars.Contains(note.DestType[0]))
				return note.DestType[0];

			if (song.SourceType == FileFormatType.SM
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

		private static string GetRadarValues(Song song, Chart chart)
		{
			var radarValues = new List<double>();
			if (chart.DestExtras.ContainsKey(SMCommon.TagRadarValues)
			    && chart.DestExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>) chart.DestExtras[SMCommon.TagRadarValues];
			}
			else if (song.SourceType == FileFormatType.SM
			         && chart.SourceExtras.ContainsKey(SMCommon.TagRadarValues)
			         && chart.SourceExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>) chart.SourceExtras[SMCommon.TagRadarValues];
			}

			if (radarValues.Count <= 0)
				return "";
			
			var sb = new StringBuilder();
			var bFirst = true;
			foreach (var val in (List<double>) chart.SourceExtras[SMCommon.TagRadarValues])
			{
				if (!bFirst)
					sb.Append(",");
				sb.Append(val.ToString(SMCommon.SMDoubleFormat));
				bFirst = false;
			}

			return sb.ToString();
		}

		private static void WriteChart(Song song, Chart chart, SMCommon.ChartDifficultyType suggestedDifficultyType, StreamWriter streamWriter)
		{
			if (!GetChartType(song, chart, out var chartType))
				return;

			var charTypeStr = chartType.ToString().Replace('_', '-');
			var chartDifficultyType = GetChartDifficultyType(song, chart, suggestedDifficultyType);
			var radarValues = GetRadarValues(song, chart);

			// Write chart header
			streamWriter.WriteLine($"//---------------{charTypeStr} - {chart.Description}----------------");
			streamWriter.WriteLine($"#{SMCommon.TagNotes}:");
			streamWriter.WriteLine($"     {charTypeStr}:");
			streamWriter.WriteLine($"     {chart.Description}:");
			streamWriter.WriteLine($"     {chartDifficultyType}:");
			streamWriter.WriteLine($"     {(int)chart.DifficultyRating}:");
			streamWriter.WriteLine($"     {radarValues}:");

			var currentMeasure = 0;
			var currentMeasureStartIndex = 0;
			var currentMeasureGCD = 1;
			var lastWrittenMeasureIndex = -1;
			var currentNoteMeasure = 0;
			var index = 0;
			var breakOutOfLoop = false;
			var blankLine = new char[chart.NumInputs];
			for (var i = 0; i < chart.NumInputs; i++)
				blankLine[i] = '0';

			// Write one chart per player
			for (var playerIndex = 0; playerIndex < chart.NumPlayers; playerIndex++)
			{
				// Marker to separate players' charts
				if (playerIndex > 0)
					streamWriter.Write("&\n\n");

				var bFirstMeasure = true;

				// TODO: Handle no layers / events - write empty measure

				// Loop over all notes.
				while (true)
				{
					// Skip this event if it is not a note for this player
					LaneNote note = null;
					if (chart.Layers.Count > 0 && index < chart.Layers[0].Events.Count)
					{
						note = chart.Layers[0].Events[index] as LaneNote;
						if (note == null || note.Player != playerIndex)
						{
							index++;
							continue;
						}
						currentNoteMeasure = note.Position.Measure;
					}

					// If we've gone through every note set a flag to break out of the loop
					// after writing any remaining notes.
					if (chart.Layers.Count == 0 || index >= chart.Layers[0].Events.Count)
					{
						// Set the flag
						breakOutOfLoop = true;
						// Advance the current measure if there are unwritten notes so we write them below.
						if (currentMeasure > lastWrittenMeasureIndex)
							currentNoteMeasure = currentMeasure + 1;
					}

					// THe measure has advanced, write out the previous measures now that we know how to divide it.
					if (currentNoteMeasure > currentMeasure)
					{
						// Starting blank measures
						if (currentMeasure == 0 && currentNoteMeasure > 1)
						{
							for (var blankMeasureIndex = 0; blankMeasureIndex < currentNoteMeasure - 1; blankMeasureIndex++)
							{
								if (!bFirstMeasure)
									streamWriter.WriteLine(",");
								for (var beatIndex = 0; beatIndex < SMCommon.NumBeatsPerMeasure; beatIndex++)
									streamWriter.WriteLine(blankLine);
								bFirstMeasure = false;
							}
						}

						// Set up a grid of characters to write
						var measureCharsDX = chart.NumInputs;
						var measureCharsDY = SMCommon.NumBeatsPerMeasure * currentMeasureGCD;
						var measureChars = new char[measureCharsDX, measureCharsDY];

						// Populate characters in the grid based on the events of the measure
						for (var measureIndex = currentMeasureStartIndex; measureIndex < index; measureIndex++)
						{
							// Skip writing this note if it is not a note for this player
							var measureNote = chart.Layers[0].Events[measureIndex] as LaneNote;
							if (measureNote == null || measureNote.Player != playerIndex)
								continue;

							// Get the note char to write
							var c = GetSMCharForNote(song, measureNote);

							// Determine the position to record the note
							var measureEventPositionInMeasure =
								measureNote.Position.Beat * currentMeasureGCD
							+ (currentMeasureGCD /
							   Math.Max(1, measureNote.Position.SubDivision.Denominator)) *
								measureNote.Position.SubDivision.Numerator;

							// Record the note.
							measureChars[measureNote.Lane, measureEventPositionInMeasure] = c;
						}

						// Write the measure of accumulated characters
						if (!bFirstMeasure)
							streamWriter.WriteLine(",");
						for (var y = 0; y < measureCharsDY; y++)
						{
							for (var x = 0; x < measureCharsDX; x++)
							{
								streamWriter.Write(measureChars[x, y] == '\0' ?
									SMCommon.SNoteChars[(int)SMCommon.NoteType.None]
									: measureChars[x, y]);
							}
							streamWriter.Write("\n");
						}

						bFirstMeasure = false;

						// If the current event is more than 1 measure ahead of the last measure,
						// write blank measures to fill the gap.
						for (var extraMeasureIndex = 0;
							extraMeasureIndex > (currentNoteMeasure - currentMeasure - 1);
							extraMeasureIndex++)
						{
							streamWriter.WriteLine(",");
							for (var beatIndex = 0; beatIndex < SMCommon.NumBeatsPerMeasure; beatIndex++)
								streamWriter.WriteLine(blankLine);
						}

						// Update loop parameters
						lastWrittenMeasureIndex = currentNoteMeasure - 1;
						currentMeasureStartIndex = index;
						currentMeasure = currentNoteMeasure;
						currentMeasureGCD = 1;
					}

					// Break out of the loop now that the last measure is written
					if (breakOutOfLoop)
						break;

					// Update the GCD for writing this note's measure later.
					if (note != null)
						currentMeasureGCD = Math.Max(currentMeasureGCD, note.Position.SubDivision.Denominator);

					// Advance
					index++;
				}
			}
			// Mark the chart as complete.
			streamWriter.Write(";\n\n");
		}

		public static bool Save(Song song, string filePath)
		{
			var chartIndex = 0;
			var rankedCharts = new Dictionary<SMCommon.ChartType, List<Chart>>();
			var suggestedDifficultyTypes = new Dictionary<Chart, SMCommon.ChartDifficultyType>();
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count > 1)
				{
					SMCommon.LogWarn($"Chart [{chartIndex}]. Chart has {chart.Layers.Count} Layers."
						+ " Only the first Layer will be used.");
				}

				if (!GetChartType(song, chart, out var smChartType))
				{
					SMCommon.LogError($"Chart [{chartIndex}]. Could not parse type."
						+ " Type should match value from SMCommon.ChartType, or the Chart should have 1"
						+ " Player and 3, 4, 6, or 8 Inputs, or the Chart should have 2 Players and 8"
						+ $" Inputs. This chart has a Type of '{chart.Type}', {chart.NumPlayers} Players"
						+ $" and {chart.NumInputs} Inputs.");
				}
				else
				{
					if(!rankedCharts.ContainsKey(smChartType))
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
							(SMCommon.ChartDifficultyType) Math.Min(i, (int) SMCommon.ChartDifficultyType.Edit);
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
			if (song.Charts.Count > 0 && song.Charts[0].Layers.Count > 0)
			{
				foreach (var chartEvent in song.Charts[0].Layers[0].Events)
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

			using (var streamWriter = new StreamWriter(filePath))
			{
				WriteProperty(SMCommon.TagTitle, song.Title, streamWriter);
				WriteProperty(SMCommon.TagSubtitle, song.SubTitle, streamWriter);
				WriteProperty(SMCommon.TagArtist, song.Artist, streamWriter);
				WriteProperty(SMCommon.TagTitleTranslit, song.TitleTransliteration, streamWriter);
				WriteProperty(SMCommon.TagSubtitleTranslit, song.SubTitleTransliteration, streamWriter);
				WriteProperty(SMCommon.TagArtistTranslit, song.ArtistTransliteration, streamWriter);
				WriteProperty(SMCommon.TagGenre, song.Genre, streamWriter);
				WritePropertyFromExtras(SMCommon.TagCredit, song, streamWriter);
				WriteProperty(SMCommon.TagBanner, song.SongSelectImage, streamWriter);
				WritePropertyFromExtras(SMCommon.TagBackground, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagLyricsPath, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagCDTitle, song, streamWriter);
				WritePropertyMusic(song, streamWriter);
				WritePropertyOffset(song, streamWriter);
				WriteProperty(SMCommon.TagSampleStart, song.PreviewSampleStart.ToString(SMCommon.SMDoubleFormat), streamWriter);
				WriteProperty(SMCommon.TagSampleLength, song.PreviewSampleLength.ToString(SMCommon.SMDoubleFormat), streamWriter);
				WritePropertyFromExtras(SMCommon.TagSelectable, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagDisplayBPM, song, streamWriter);
				WritePropertyBPMs(song, tempoChangeBeats, streamWriter);
				WritePropertyStops(song, stopBeats, streamWriter);
				WritePropertyFromExtras(SMCommon.TagTimeSignatures, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagBGChanges, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagFGChanges, song, streamWriter);
				// TODO: Write keysounds properly
				WritePropertyFromExtras(SMCommon.TagKeySounds, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagAttacks, song, streamWriter);
				WritePropertyFromExtras(SMCommon.TagMenuColor, song, streamWriter);

				streamWriter.WriteLine();
				foreach (var chart in song.Charts)
				{
					WriteChart(song, chart, suggestedDifficultyTypes[chart], streamWriter);
				}
			}

			return true;
		}
	}
}
