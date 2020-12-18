using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Fumen.Converters
{
	public class SMReader
	{
		protected abstract class PropertyParser
		{
			protected readonly string PropertyName;

			protected PropertyParser(string smPropertyName)
			{
				PropertyName = smPropertyName;
			}

			public abstract bool Parse(StreamReader reader, string line, Song song);

			protected bool DoesLineMatchProperty(string line, out string trimmedLine)
			{
				trimmedLine = line;

				var key = $"#{PropertyName}:";
				if (!line.StartsWith(key))
					return false;

				trimmedLine = line.Substring(key.Length);
				return true;
			}
		}

		protected abstract class StringPropertyParser : PropertyParser
		{
			protected StringPropertyParser(string smPropertyName)
				: base(smPropertyName)
			{
			}

			protected bool ParseAsString(StreamReader streamReader, string line, out string parsedValue)
			{
				parsedValue = "";

				// Only consider this line if it matches this property name.
				if (!DoesLineMatchProperty(line, out line))
					return false;

				// Loop over lines until the end marker is found.
				var first = true;
				while (null != line)
				{
					var endMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, ';');

					// Clean the line.
					line = TrimComments(line);
					line = UnEscape(line);

					// Append this value as a string.
					if (!first)
						parsedValue += "\n";
					if (endMarkerIndex < 0)
						parsedValue += line;
					else if (endMarkerIndex > 0)
						parsedValue += line.Substring(0, endMarkerIndex);

					// This line contains the end of this property. Stop reading.
					if (endMarkerIndex >= 0)
						break;

					// Advance to the next line.
					line = streamReader.ReadLine();
					first = false;
				}

				return true;
			}
		}

		protected class StringPropertyToSongPropertyParser : StringPropertyParser
		{
			private readonly string _songPropertyName;

			public StringPropertyToSongPropertyParser(string smPropertyName, string songPropertyName)
				: base(smPropertyName)
			{
				_songPropertyName = songPropertyName;
			}

			public override bool Parse(StreamReader streamReader, string line, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!ParseAsString(streamReader, line, out var songValueStr))
					return false;

				// Only consider this line if the property is valid.
				var prop = song.GetType().GetProperty(_songPropertyName, BindingFlags.Public | BindingFlags.Instance);
				if (null == prop || !prop.CanWrite)
					return true;

				// Set the property on the Song.
				object o;
				try
				{
					o = Convert.ChangeType(songValueStr, prop.PropertyType);
				}
				catch (Exception)
				{
					return true;
				}
				prop.SetValue(song, o, null);

				return true;
			}
		}

		protected class StringPropertyToExtrasParser<T> : StringPropertyParser where T : IConvertible
		{
			public StringPropertyToExtrasParser(string smPropertyName)
				: base(smPropertyName)
			{
			}

			public override bool Parse(StreamReader streamReader, string line, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!ParseAsString(streamReader, line, out var songValueStr))
					return false;

				T value;
				try
				{
					value = (T) Convert.ChangeType(songValueStr, typeof(T));
				}
				catch (Exception)
				{
					return true;
				}

				song.SourceExtras[PropertyName] = value;
				return true;
			}
		}

		protected class ListAtTimePropertyParser<T> : PropertyParser where T : IConvertible
		{
			private readonly Dictionary<double, T> _values;

			public ListAtTimePropertyParser(string smPropertyName, Dictionary<double, T> values)
				: base(smPropertyName)
			{
				_values = values;
			}

			public override bool Parse(StreamReader streamReader, string line, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!DoesLineMatchProperty(line, out line))
					return false;

				// Loop over lines until the end marker is found.
				while (null != line)
				{
					var endMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, ';');

					// Parse the line into a list of key value paris
					line = CleanLine(line);
					var pairs = line.Split(',');
					foreach (var pair in pairs)
					{
						var kvp = pair.Split('=');
						if (kvp.Length != 2)
							continue;
						if (!double.TryParse(kvp[0], out var time))
							continue;

						T value;
						try
						{
							value = (T) Convert.ChangeType(kvp[1], typeof(T));
						}
						catch (Exception)
						{
							continue;
						}

						_values[time] = value;
					}

					// This line contains the end of this property. Stop reading.
					if (endMarkerIndex >= 0)
						break;

					// Advance to the next line.
					line = streamReader.ReadLine();
				}

				return true;
			}
		}

		protected class NotesPropertyParser : PropertyParser
		{
			public NotesPropertyParser(string smPropertyName)
				: base(smPropertyName)
			{
			}

			private static string ParseNextNoteData(StreamReader streamReader, ref string line)
			{
				var parsedValue = "";
				while (null != line)
				{
					var endMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, ':');
					if (endMarkerIndex >= 0)
						line = line.Substring(0, endMarkerIndex);
					line = CleanLine(line);

					parsedValue += line;

					line = streamReader.ReadLine();

					if (endMarkerIndex >= 0)
						break;
				}

				return parsedValue;
			}

			public override bool Parse(StreamReader reader, string line, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!DoesLineMatchProperty(line, out line))
					return false;

				var chart = new Chart
				{
					Type = ParseNextNoteData(reader, ref line),
					Description = ParseNextNoteData(reader, ref line),
					DifficultyType = ParseNextNoteData(reader, ref line)
				};
				chart.Layers.Add(new Layer());

				// Parse the chart information before measure data.
				var chartDifficultyRatingStr = ParseNextNoteData(reader, ref line);
				var chartRadarValuesStr = ParseNextNoteData(reader, ref line);

				// Parse the difficulty rating as a number.
				if (int.TryParse(chartDifficultyRatingStr, out var difficultyRatingInt))
					chart.DifficultyRating = (double) difficultyRatingInt;

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
					return true;
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

				// Parse measure data.
				var player = 0;
				var measure = 0;
				var lineInMeasure = 0;
				var currentMeasureEvents = new List<Event>();
				while (null != line)
				{
					var endMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, ';');
					var measureMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, ',');
					var playerMarkerIndex = GetFirstUnEscapedPropertyEndMarker(line, '&');
					line = CleanLine(line);

					// Complete the current measure
					if (endMarkerIndex >= 0 || measureMarkerIndex >= 0 || playerMarkerIndex >= 0)
					{
						// Now that the number of lines in the measure is known,
						// correct the position of each event.
						foreach (var measureEvent in currentMeasureEvents)
						{
							// Time signatures are not supported in sm files. Every measure (even if empty)
							// has a multiple of four notes. If for some reason there is not a multiple of
							// four, then just keep the beat as the position.
							if (lineInMeasure % SMCommon.NumBeatsPerMeasure == 0)
							{
								var measureIndex = measureEvent.Position.Beat;
								var linesPerBeat = lineInMeasure / SMCommon.NumBeatsPerMeasure;
								measureEvent.Position.Beat /= linesPerBeat;
								measureEvent.Position.SubDivision = new Fraction(
									measureIndex - measureEvent.Position.Beat * linesPerBeat,
									linesPerBeat);
							}
						}

						chart.Layers[0].Events.AddRange(currentMeasureEvents);
						currentMeasureEvents.Clear();
						measure++;
						lineInMeasure = 0;
						if (playerMarkerIndex >= 0)
							player++;
					}

					// Parse this line as note data
					else if (line.Length > 0)
					{
						for (var lineIndex = 0; lineIndex < line.Length; lineIndex++)
						{
							// Check the character at this index to see if it corresponds to a note.
							var c = line[lineIndex];
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

							// No note at this position, continue.
							if (null == note)
								continue;

							// Configure common parameters on the note and add it.
							note.Lane = lineIndex;
							note.Player = player;
							note.Position = new MetricPosition
							{
								Measure = measure,
								Beat = lineInMeasure
							};

							currentMeasureEvents.Add(note);
						}

						// Advance line marker
						lineInMeasure++;
					}

					// Stop
					if (endMarkerIndex >= 0)
						break;

					// Advance
					line = reader.ReadLine();
				}

				song.Charts.Add(chart);

				return true;
			}
		}

		public static string TrimComments(string input)
		{
			var index = input.IndexOf("//");
			return index < 0 ? input : input.Substring(0, index);
		}

		public static string UnEscape(string input)
		{
			return input
				.Replace("\\;", ";")
				.Replace("\\:", ":")
				.Replace("\\\\", "\\")
				.Replace("\\/\\/", "//");
		}

		public static string CleanLine(string input)
		{
			input = TrimComments(input);
			input = UnEscape(input);
			input = Regex.Replace(input, @"\s+", "");
			return input;
		}

		public static int GetFirstUnEscapedPropertyEndMarker(string input, char endMarker)
		{
			var numberOfPrecedingSlashes = 0;
			var inputLen = input.Length;
			for (var i = 0; i < inputLen; i++)
			{
				var currentCharIsEscaped = numberOfPrecedingSlashes % 2 == 1;

				// Check if a comment block was reached and if so, return
				if (i < inputLen - 1 && input[i] == '/' && input[i + 1] == '/' && !currentCharIsEscaped)
					return -1;

				// Checked if the endMarker was 
				if (input[i] == endMarker && !currentCharIsEscaped)
					return i;

				if (input[i] == '\\')
					numberOfPrecedingSlashes++;
				else
					numberOfPrecedingSlashes = 0;
			}

			// No end marker was found
			return -1;
		}

		private static Fraction FindClosestSMSubDivision(double fractionAsDouble)
		{
			var length = SMCommon.SSubDivisionLengths.Count;

			// Edge cases
			if (fractionAsDouble <= SMCommon.SSubDivisionLengths[0])
				return SMCommon.SSubDivisions[0];
			if (fractionAsDouble >= SMCommon.SSubDivisionLengths[length - 1])
				return SMCommon.SSubDivisions[length - 1];

			// Search
			int leftIndex = 0, rightIndex = length, midIndex = 0;
			while (leftIndex < rightIndex)
			{
				midIndex = (leftIndex + rightIndex) >> 1;

				// Value is less than midpoint, search to the left.
				if (fractionAsDouble < SMCommon.SSubDivisionLengths[midIndex])
				{
					// Value is between midpoint and adjacent.
					if (midIndex > 0 && fractionAsDouble > SMCommon.SSubDivisionLengths[midIndex - 1])
						return fractionAsDouble - SMCommon.SSubDivisionLengths[midIndex - 1] <
						       SMCommon.SSubDivisionLengths[midIndex] - fractionAsDouble
							? SMCommon.SSubDivisions[midIndex - 1]
							: SMCommon.SSubDivisions[midIndex];

					// Advance search
					rightIndex = midIndex;
				}

				// Value is greater than midpoint, search to the right.
				else if (fractionAsDouble > SMCommon.SSubDivisionLengths[midIndex])
				{
					// Value is between midpoint and adjacent.
					if (midIndex < length - 1 && fractionAsDouble < SMCommon.SSubDivisionLengths[midIndex + 1])
						return fractionAsDouble - SMCommon.SSubDivisionLengths[midIndex] <
						       SMCommon.SSubDivisionLengths[midIndex + 1] - fractionAsDouble
							? SMCommon.SSubDivisions[midIndex]
							: SMCommon.SSubDivisions[midIndex + 1];
					
					// Advance search
					leftIndex = midIndex + 1;
				}

				// Value equals midpoint.
				else
				{
					return SMCommon.SSubDivisions[midIndex];
				}
			}
			return SMCommon.SSubDivisions[midIndex];
		}

		//public static async Task<Song> Load(string filePath)
		public static Song Load(string filePath)
		{
			var tempos = new Dictionary<double, double>();
			var stops = new Dictionary<double, double>();

			var propertyParsers = new List<PropertyParser>()
			{
				new StringPropertyToSongPropertyParser(SMCommon.TagTitle, nameof(Song.Title)),
				new StringPropertyToSongPropertyParser(SMCommon.TagSubtitle, nameof(Song.SubTitle)),
				new StringPropertyToSongPropertyParser(SMCommon.TagArtist, nameof(Song.Artist)),
				new StringPropertyToSongPropertyParser(SMCommon.TagTitleTranslit, nameof(Song.TitleTransliteration)),
				new StringPropertyToSongPropertyParser(SMCommon.TagSubtitleTranslit, nameof(Song.SubTitleTransliteration)),
				new StringPropertyToSongPropertyParser(SMCommon.TagArtistTranslit, nameof(Song.ArtistTransliteration)),
				new StringPropertyToSongPropertyParser(SMCommon.TagGenre, nameof(Song.Genre)),
				new StringPropertyToExtrasParser<string>(SMCommon.TagCredit),
				new StringPropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage)),
				new StringPropertyToExtrasParser<string>(SMCommon.TagBackground),
				new StringPropertyToExtrasParser<string>(SMCommon.TagLyricsPath),
				new StringPropertyToExtrasParser<string>(SMCommon.TagCDTitle),
				new StringPropertyToExtrasParser<string>(SMCommon.TagMusic),
				new StringPropertyToExtrasParser<string>(SMCommon.TagOffset),
				new StringPropertyToSongPropertyParser(SMCommon.TagSampleStart, nameof(Song.PreviewSampleStart)),
				new StringPropertyToSongPropertyParser(SMCommon.TagSampleLength, nameof(Song.PreviewSampleLength)),
				new StringPropertyToExtrasParser<string>(SMCommon.TagSelectable),
				new StringPropertyToExtrasParser<string>(SMCommon.TagDisplayBPM),
				new ListAtTimePropertyParser<double>(SMCommon.TagBPMs, tempos),
				new ListAtTimePropertyParser<double>(SMCommon.TagStops, stops),
				new StringPropertyToExtrasParser<string>(SMCommon.TagTimeSignatures), // Removed, see https://github.com/stepmania/stepmania/issues/9
				new StringPropertyToExtrasParser<string>(SMCommon.TagBGChanges),
				new StringPropertyToExtrasParser<string>(SMCommon.TagFGChanges),
				// TODO: Parse Keysounds properly.
				// Comma separated list where index is tap note index and value is keysound for that note?
				new StringPropertyToExtrasParser<string>(SMCommon.TagKeySounds),
				new StringPropertyToExtrasParser<string>(SMCommon.TagAttacks),
				new StringPropertyToExtrasParser<string>(SMCommon.TagMenuColor),
				new NotesPropertyParser(SMCommon.TagNotes)
			};

			var song = new Song();
			song.SourceType = FileFormatType.SM;

			using (var streamReader = new System.IO.StreamReader(filePath))
			{
				string line;
				//while ((line = await streamReader.ReadLineAsync()) != null)
				while ((line = streamReader.ReadLine()) != null)
				{
					foreach (var propertyParser in propertyParsers)
					{
						if (propertyParser.Parse(streamReader, line, song))
							break;
					}
				}
			}

			// Insert stop events
			foreach (var stop in stops)
			{
				var stopEvent = new Stop()
				{
					Position = new MetricPosition()
					{
						Measure = (int)stop.Key / SMCommon.NumBeatsPerMeasure,
						Beat = (int)stop.Key % SMCommon.NumBeatsPerMeasure,
						SubDivision = FindClosestSMSubDivision(stop.Key - (int)stop.Key)
					},
					LengthMicros = (long)(stop.Value * 1000000.0)
				};

				foreach (var chart in song.Charts)
					chart.Layers[0].Events.Add(stopEvent);
			}

			// Insert tempo change events
			foreach (var tempo in tempos)
			{
				var tempoChangeEvent = new TempoChange()
				{
					Position = new MetricPosition()
					{
						Measure = (int)tempo.Key / SMCommon.NumBeatsPerMeasure,
						Beat = (int)tempo.Key % SMCommon.NumBeatsPerMeasure,
						SubDivision = FindClosestSMSubDivision(tempo.Key - (int)tempo.Key)
					},
					TempoBPM = tempo.Value
				};

				foreach (var chart in song.Charts)
					chart.Layers[0].Events.Add(tempoChangeEvent);
			}

			// Sort events
			foreach (var chart in song.Charts)
				chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

			song.GenreTransliteration = song.Genre;

			var chartOffset = 0.0;
			if (song.SourceExtras.TryGetValue(SMCommon.TagOffset, out var offsetObj))
				double.TryParse((string)offsetObj, out chartOffset);
			
			var chartMusicFile = "";
			if (song.SourceExtras.TryGetValue(SMCommon.TagMusic, out var chartMusicFileObj))
				chartMusicFile = (string) chartMusicFileObj;

			var chartDisplayTempo = "";
			if (song.SourceExtras.TryGetValue(SMCommon.TagDisplayBPM, out var chartDisplayTempoObj))
				chartDisplayTempo = (string)chartDisplayTempoObj;
			else if (tempos.ContainsKey(0.0))
				chartDisplayTempo = tempos[0.0].ToString("N3");

			foreach (var chart in song.Charts)
			{
				chart.MusicFile = chartMusicFile;
				chart.ChartOffsetFromMusic = chartOffset;
				chart.Tempo = chartDisplayTempo;
				chart.Artist = song.Artist;
				chart.ArtistTransliteration = song.ArtistTransliteration;
				chart.Genre = song.Genre;
				chart.GenreTransliteration = song.GenreTransliteration;
			}

			// TODO: Cleanup async / task

			return song;
		}
	}
}
