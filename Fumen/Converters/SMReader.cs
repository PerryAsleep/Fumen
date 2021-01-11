using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

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

			public abstract bool Parse(MSDFile.Value value, Song song);

			protected bool DoesValueMatchProperty(MSDFile.Value value)
			{
				return value.Params.Count > 0
					&& !string.IsNullOrEmpty(value.Params[0])
					&& value.Params[0].ToUpper() == PropertyName;
			}
		}

		protected abstract class StringPropertyParser : PropertyParser
		{
			protected StringPropertyParser(string smPropertyName)
				: base(smPropertyName)
			{
			}

			protected bool ParseAsString(MSDFile.Value value, out string parsedValue)
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

		protected class StringPropertyToSongPropertyParser : StringPropertyParser
		{
			private readonly string SongPropertyName;

			public StringPropertyToSongPropertyParser(string smPropertyName, string songPropertyName)
				: base(smPropertyName)
			{
				SongPropertyName = songPropertyName;
			}

			public override bool Parse(MSDFile.Value value, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!ParseAsString(value, out var songValueStr))
					return false;

				// Only consider this line if the property is valid.
				var prop = song.GetType().GetProperty(SongPropertyName, BindingFlags.Public | BindingFlags.Instance);
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

			public override bool Parse(MSDFile.Value value, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!ParseAsString(value, out var songValueStr))
					return false;

				T tValue;
				try
				{
					tValue = (T) Convert.ChangeType(songValueStr, typeof(T));
				}
				catch (Exception)
				{
					return true;
				}

				song.SourceExtras[PropertyName] = tValue;
				return true;
			}
		}

		protected class StringListPropertyToExtrasParser : StringPropertyParser
		{
			public StringListPropertyToExtrasParser(string smPropertyName)
				: base(smPropertyName)
			{
			}

			public override bool Parse(MSDFile.Value value, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!ParseAsString(value, out var _))
					return false;

				List<string> parsedList = new List<string>();
				for (var paramIndex = 1; paramIndex < value.Params.Count; paramIndex++)
				{
					if (!string.IsNullOrEmpty(value.Params[paramIndex]))
						parsedList.Add(value.Params[paramIndex]);
				}

				song.SourceExtras[PropertyName] = parsedList;
				return true;
			}
		}

		protected class ListAtTimePropertyParser<T> : PropertyParser where T : IConvertible
		{
			private readonly Dictionary<double, T> Values;
			private readonly string RawStringPropertyName;

			public ListAtTimePropertyParser(string smPropertyName, Dictionary<double, T> values, string rawStringPropertyName = null)
				: base(smPropertyName)
			{
				Values = values;
				RawStringPropertyName = rawStringPropertyName;
			}

			public override bool Parse(MSDFile.Value value, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!DoesValueMatchProperty(value))
					return false;

				if (value.Params.Count < 2 || string.IsNullOrEmpty(value.Params[1]))
					return true;

				// Record the raw string to preserve formatting when writing.
				if (!string.IsNullOrEmpty(RawStringPropertyName))
				{
					song.SourceExtras.Add(RawStringPropertyName, value.Params[1]);
				}

				var pairs = value.Params[1].Split(',');
				foreach (var pair in pairs)
				{
					var kvp = pair.Split('=');
					if (kvp.Length != 2)
						continue;
					if (!double.TryParse(kvp[0], out var time))
						continue;

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
						continue;
					}

					Values[time] = tValue;
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

			public override bool Parse(MSDFile.Value value, Song song)
			{
				// Only consider this line if it matches this property name.
				if (!DoesValueMatchProperty(value))
					return false;

				if (value.Params.Count < 7)
					return true;

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
				var currentMeasureEvents = new List<Event>();
				var notesStr = value.Params[6] ?? "";
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
										if (trimmedLine[charIndex] == ']')
											break;
									var endIndex = charIndex - 1;
									if (endIndex > startIndex)
									{
										if (int.TryParse(trimmedLine.Substring(startIndex, endIndex - startIndex), out var keySoundIndex))
										{
											note.SourceExtras.Add(SMCommon.TagFumenKeySoundIndex, keySoundIndex);
										}
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

				song.Charts.Add(chart);

				return true;
			}
		}

		/// <summary>
		/// Path to the sm file to load.
		/// </summary>
		private string FilePath;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filePath">Path to the sm file to load.</param>
		public SMReader(string filePath)
		{
			FilePath = filePath;
		}

		/// <summary>
		/// Load the sm file specified by the provided file path.
		/// </summary>
		public async Task<Song> Load()
		{
			// Load the file as an MSDFile.
			var msdFile = new MSDFile();
			var result = await msdFile.Load(FilePath);
			if (!result)
				return null;

			var tempos = new Dictionary<double, double>();
			var stops = new Dictionary<double, double>();

			var propertyParsers = new Dictionary<string, PropertyParser>()
			{
				[SMCommon.TagTitle] = new StringPropertyToSongPropertyParser(SMCommon.TagTitle, nameof(Song.Title)),
				[SMCommon.TagSubtitle] = new StringPropertyToSongPropertyParser(SMCommon.TagSubtitle, nameof(Song.SubTitle)),
				[SMCommon.TagArtist] = new StringPropertyToSongPropertyParser(SMCommon.TagArtist, nameof(Song.Artist)),
				[SMCommon.TagTitleTranslit] = new StringPropertyToSongPropertyParser(SMCommon.TagTitleTranslit, nameof(Song.TitleTransliteration)),
				[SMCommon.TagSubtitleTranslit] = new StringPropertyToSongPropertyParser(SMCommon.TagSubtitleTranslit, nameof(Song.SubTitleTransliteration)),
				[SMCommon.TagArtistTranslit] = new StringPropertyToSongPropertyParser(SMCommon.TagArtistTranslit, nameof(Song.ArtistTransliteration)),
				[SMCommon.TagGenre] = new StringPropertyToSongPropertyParser(SMCommon.TagGenre, nameof(Song.Genre)),
				[SMCommon.TagCredit] = new StringPropertyToExtrasParser<string>(SMCommon.TagCredit),
				[SMCommon.TagBanner] = new StringPropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage)),
				[SMCommon.TagBackground] = new StringPropertyToExtrasParser<string>(SMCommon.TagBackground),
				[SMCommon.TagLyricsPath] = new StringPropertyToExtrasParser<string>(SMCommon.TagLyricsPath),
				[SMCommon.TagCDTitle] = new StringPropertyToExtrasParser<string>(SMCommon.TagCDTitle),
				[SMCommon.TagMusic] = new StringPropertyToExtrasParser<string>(SMCommon.TagMusic),
				[SMCommon.TagOffset] = new StringPropertyToExtrasParser<string>(SMCommon.TagOffset),
				[SMCommon.TagBPMs] = new ListAtTimePropertyParser<double>(SMCommon.TagBPMs, tempos, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagStops] = new ListAtTimePropertyParser<double>(SMCommon.TagStops, stops, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagFreezes] = new ListAtTimePropertyParser<double>(SMCommon.TagFreezes, stops),
				[SMCommon.TagDelays] = new StringListPropertyToExtrasParser(SMCommon.TagDelays),
				[SMCommon.TagTimeSignatures] = new StringListPropertyToExtrasParser(SMCommon.TagTimeSignatures), // Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TickCounts] = new StringListPropertyToExtrasParser(SMCommon.TickCounts),
				[SMCommon.InstrumentTrack] = new StringPropertyToExtrasParser<string>(SMCommon.InstrumentTrack),
				[SMCommon.TagSampleStart] = new StringPropertyToSongPropertyParser(SMCommon.TagSampleStart, nameof(Song.PreviewSampleStart)),
				[SMCommon.TagSampleLength] = new StringPropertyToSongPropertyParser(SMCommon.TagSampleLength, nameof(Song.PreviewSampleLength)),
				[SMCommon.TagDisplayBPM] = new StringListPropertyToExtrasParser(SMCommon.TagDisplayBPM),
				[SMCommon.TagSelectable] = new StringPropertyToExtrasParser<string>(SMCommon.TagSelectable),
				[SMCommon.TagAnimations] = new StringListPropertyToExtrasParser(SMCommon.TagAnimations),
				[SMCommon.TagBGChanges] = new StringListPropertyToExtrasParser(SMCommon.TagBGChanges),
				[SMCommon.TagBGChanges1] = new StringListPropertyToExtrasParser(SMCommon.TagBGChanges1),
				[SMCommon.TagBGChanges2] = new StringListPropertyToExtrasParser(SMCommon.TagBGChanges2),
				[SMCommon.TagFGChanges] = new StringListPropertyToExtrasParser(SMCommon.TagFGChanges),
				// TODO: Parse Keysounds properly.
				// Comma separated list where index is tap note index and value is keysound for that note?
				[SMCommon.TagKeySounds] = new StringListPropertyToExtrasParser(SMCommon.TagKeySounds),
				[SMCommon.TagAttacks] = new StringListPropertyToExtrasParser(SMCommon.TagAttacks),
				[SMCommon.TagNotes] = new NotesPropertyParser(SMCommon.TagNotes),
				[SMCommon.TagNotes2] = new NotesPropertyParser(SMCommon.TagNotes2),

				// Stepmania does not read LASTBEATHINT, but it writes it if present.
				// In order to not modify charts unintentionally by reading and writing them, read the value
				// in so we can write it back out.
				[SMCommon.TagLastBeatHint] = new StringPropertyToExtrasParser<string>(SMCommon.TagLastBeatHint),
			};

			var song = new Song();
			song.SourceType = FileFormatType.SM;

			// Parse all Values from the MSDFile.
			foreach(var value in msdFile.Values)
			{
				if (propertyParsers.TryGetValue(value.Params[0]?.ToUpper() ?? "", out var propertyParser))
					propertyParser.Parse(value, song);
			}

			// Insert stop events.
			foreach (var stop in stops)
			{
				var stopEvent = new Stop()
				{
					Position = new MetricPosition()
					{
						Measure = (int)stop.Key / SMCommon.NumBeatsPerMeasure,
						Beat = (int)stop.Key % SMCommon.NumBeatsPerMeasure,
						SubDivision = SMCommon.FindClosestSMSubDivision(stop.Key - (int)stop.Key)
					},
					LengthMicros = (long)(stop.Value * 1000000.0)
				};

				// Record the actual doubles.
				stopEvent.SourceExtras.Add(SMCommon.TagFumenDoublePosition, stop.Key);
				stopEvent.SourceExtras.Add(SMCommon.TagFumenDoubleValue, stop.Value);

				foreach (var chart in song.Charts)
					chart.Layers[0].Events.Add(stopEvent);
			}

			// Insert tempo change events.
			foreach (var tempo in tempos)
			{
				var tempoChangeEvent = new TempoChange()
				{
					Position = new MetricPosition()
					{
						Measure = (int)tempo.Key / SMCommon.NumBeatsPerMeasure,
						Beat = (int)tempo.Key % SMCommon.NumBeatsPerMeasure,
						SubDivision = SMCommon.FindClosestSMSubDivision(tempo.Key - (int)tempo.Key)
					},
					TempoBPM = tempo.Value
				};

				// Record the actual doubles.
				tempoChangeEvent.SourceExtras.Add(SMCommon.TagFumenDoublePosition, tempo.Key);

				foreach (var chart in song.Charts)
					chart.Layers[0].Events.Add(tempoChangeEvent);
			}

			// Sort events.
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
			{
				if (chartDisplayTempoObj is List<string> tempoList)
				{
					var first = true;
					foreach(var tempo in tempoList)
					{
						if (!first)
							chartDisplayTempo += MSDFile.ParamMarker;
						chartDisplayTempo += tempo;
					}
				}
				else
				{
					chartDisplayTempo = chartDisplayTempoObj.ToString();
				}
			}
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

			return song;
		}
	}
}
