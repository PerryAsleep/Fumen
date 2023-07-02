using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;
using static Fumen.Converters.SMCommon;

namespace Fumen.Converters;

/// <summary>
/// Reader for StepMania .sm files.
/// </summary>
public class SMReader : Reader
{
	/// <summary>
	/// SM properties which affect scroll rate and note timing.
	/// Grouped in a small class for organization.
	/// </summary>
	private class TimingProperties
	{
		public readonly Dictionary<double, double> Tempos = new();
		public readonly Dictionary<double, double> Stops = new();
		public readonly Dictionary<double, double> Delays = new();
		public readonly Dictionary<double, Fraction> TimeSignatures = new();
		public readonly Dictionary<double, int> TickCounts = new();
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	private readonly SMReaderLogger Logger;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="filePath">Path to the sm file to load.</param>
	public SMReader(string filePath)
		: base(filePath)
	{
		Logger = new SMReaderLogger(FilePath);
	}

	/// <summary>
	/// Load the sm file specified by the provided file path.
	/// </summary>
	/// <param name="token">CancellationToken to cancel task.</param>
	public override async Task<Song> LoadAsync(CancellationToken token)
	{
		// Load the file as an MSDFile.
		var msdFile = new MSDFile();
		var result = await msdFile.LoadAsync(FilePath, token);
		if (!result)
		{
			Logger.Error("Failed to load MSD File.");
			return null;
		}

		token.ThrowIfCancellationRequested();

		var song = new Song();
		await Task.Run(() =>
		{
			var timingProperties = new TimingProperties();
			song.SourceType = FileFormatType.SM;

			var propertyParsers = new Dictionary<string, PropertyParser>()
			{
				[TagTitle] = new PropertyToSongPropertyParser(TagTitle, nameof(Song.Title), song),
				[TagSubtitle] = new PropertyToSongPropertyParser(TagSubtitle, nameof(Song.SubTitle), song),
				[TagArtist] = new PropertyToSongPropertyParser(TagArtist, nameof(Song.Artist), song),
				[TagTitleTranslit] =
					new PropertyToSongPropertyParser(TagTitleTranslit, nameof(Song.TitleTransliteration), song),
				[TagSubtitleTranslit] =
					new PropertyToSongPropertyParser(TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
				[TagArtistTranslit] =
					new PropertyToSongPropertyParser(TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
				[TagGenre] = new PropertyToSongPropertyParser(TagGenre, nameof(Song.Genre), song),
				[TagCredit] = new PropertyToSourceExtrasParser<string>(TagCredit, song.Extras),
				[TagBanner] = new PropertyToSongPropertyParser(TagBanner, nameof(Song.SongSelectImage), song),
				[TagBackground] = new PropertyToSourceExtrasParser<string>(TagBackground, song.Extras),
				[TagLyricsPath] = new PropertyToSourceExtrasParser<string>(TagLyricsPath, song.Extras),
				[TagCDTitle] = new PropertyToSourceExtrasParser<string>(TagCDTitle, song.Extras),
				[TagMusic] = new PropertyToSourceExtrasParser<string>(TagMusic, song.Extras),
				[TagOffset] = new PropertyToSourceExtrasParser<double>(TagOffset, song.Extras),
				[TagBPMs] = new CSVListAtTimePropertyParser<double>(TagBPMs, timingProperties.Tempos, song.Extras,
					TagFumenRawBpmsStr),
				[TagStops] = new CSVListAtTimePropertyParser<double>(TagStops, timingProperties.Stops, song.Extras,
					TagFumenRawStopsStr),
				[TagFreezes] = new CSVListAtTimePropertyParser<double>(TagFreezes, timingProperties.Stops),
				[TagDelays] = new CSVListAtTimePropertyParser<double>(TagDelays, timingProperties.Delays, song.Extras,
					TagFumenRawDelaysStr),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				// SM files are forced 4/4 time signatures. Other time signatures can be provided but they are only
				// suggestions to a renderer for how to draw measure markers.
				[TagTimeSignatures] = new ListFractionPropertyParser(TagTimeSignatures, timingProperties.TimeSignatures,
					song.Extras, TagFumenRawTimeSignaturesStr),
				[TagTickCounts] = new CSVListAtTimePropertyParser<int>(TagTickCounts, timingProperties.TickCounts,
					song.Extras, TagFumenRawTickCountsStr),
				[TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(TagInstrumentTrack, song.Extras),
				[TagSampleStart] = new PropertyToSongPropertyParser(TagSampleStart, nameof(Song.PreviewSampleStart), song),
				[TagSampleLength] = new PropertyToSongPropertyParser(TagSampleLength, nameof(Song.PreviewSampleLength), song),
				[TagDisplayBPM] = new ListPropertyToSourceExtrasParser<string>(TagDisplayBPM, song.Extras),
				[TagSelectable] = new PropertyToSourceExtrasParser<string>(TagSelectable, song.Extras),
				[TagAnimations] = new PropertyToSourceExtrasParser<string>(TagAnimations, song.Extras),
				[TagBGChanges] = new PropertyToSourceExtrasParser<string>(TagBGChanges, song.Extras),
				[TagBGChanges1] = new PropertyToSourceExtrasParser<string>(TagBGChanges1, song.Extras),
				[TagBGChanges2] = new PropertyToSourceExtrasParser<string>(TagBGChanges2, song.Extras),
				[TagFGChanges] = new PropertyToSourceExtrasParser<string>(TagFGChanges, song.Extras),
				// TODO: Parse Keysounds properly.
				[TagKeySounds] = new PropertyToSourceExtrasParser<string>(TagKeySounds, song.Extras),
				[TagAttacks] = new ListPropertyToSourceExtrasParser<string>(TagAttacks, song.Extras),
				[TagNotes] = new SongNotesPropertyParser(TagNotes, song),
				[TagNotes2] = new SongNotesPropertyParser(TagNotes2, song),
				[TagLastBeatHint] = new PropertyToSourceExtrasParser<string>(TagLastBeatHint, song.Extras),
			};
			foreach (var kvp in propertyParsers)
				kvp.Value.SetLogger(Logger);

			// Parse all Values from the MSDFile.
			var chartProperties = new Dictionary<int, Dictionary<string, string>>();
			foreach (var value in msdFile.Values)
			{
				// Normal case.
				if (propertyParsers.TryGetValue(value.Params[0]?.ToUpper() ?? "", out var propertyParser))
				{
					propertyParser.Parse(value);
				}

				// Attempt to parse song and chart specific properties.
				// For sm files, all song and chart custom properties are defined at the song level since
				// sm files do not have per-chart keys. We need to parse the keys and see if they match
				// expected keys that specify if they are for the song or chart.
				// See also SMWriter.
				else if (value.Params.Count == 2)
				{
					var key = value.Params[0];
					var parsed = false;

					// Song custom property.
					if (key.Length > SMCustomPropertySongMarkerLength
					    && key.EndsWith(SMCustomPropertySongMarker))
					{
						// Remove the song marker to get the original key.
						var propertyKey = key.Substring(0, key.Length - SMCustomPropertySongMarkerLength);
						// Add the custom property to the song extras.
						song.Extras.AddSourceExtra(propertyKey, value.Params[1]);
						parsed = true;
					}
					// Check for a chart custom property.
					// The markers for chart custom properties have a marker and an index value.
					// e.g. "CHART0001" in "CUSTOMPROPERTYCHART0001".
					else if (key.Length > SMCustomPropertyChartIndexMarkerLength)
					{
						if (key.LastIndexOf(SMCustomPropertyChartMarker, StringComparison.Ordinal) ==
						    key.Length - SMCustomPropertyChartIndexMarkerLength)
						{
							// Parse the index out so we can associate this property with the correct chart later.
							var indexString = key.Substring(key.Length - SMCustomPropertyChartIndexNumberLength,
								SMCustomPropertyChartIndexNumberLength);
							if (int.TryParse(indexString, out var index))
							{
								// Remove the chart marker to get the original key.
								var propertyKey = key.Substring(0, key.Length - SMCustomPropertyChartIndexMarkerLength);
								if (!chartProperties.ContainsKey(index))
									chartProperties.Add(index, new Dictionary<string, string>());
								// Record the custom property for adding to the chart later.
								chartProperties[index][propertyKey] = value.Params[1];
								parsed = true;
							}
						}
					}

					// If we failed to parse the property with the tags used by SMWriter, still record
					// the property on the song extras so the caller has access to it.
					if (!parsed)
					{
						song.Extras.AddSourceExtra(key, value.Params[1]);
					}
				}
			}

			token.ThrowIfCancellationRequested();

			// Insert stop and tempo change events.
			var firstChart = true;
			foreach (var chart in song.Charts)
			{
				AddStops(timingProperties.Stops, chart);
				AddDelays(timingProperties.Delays, chart);
				AddTempos(timingProperties.Tempos, chart);
				AddTimeSignatures(timingProperties.TimeSignatures, chart, Logger, firstChart);
				AddTickCountEvents(timingProperties.TickCounts, chart);
				firstChart = false;
			}

			// Sort events.
			foreach (var chart in song.Charts)
				chart.Layers[0].Events.Sort(new SMEventComparer());

			song.GenreTransliteration = song.Genre;

			var chartOffset = 0.0;
			if (song.Extras.TryGetSourceExtra(TagOffset, out object offsetObj))
				chartOffset = (double)offsetObj;

			var chartMusicFile = "";
			if (song.Extras.TryGetSourceExtra(TagMusic, out object chartMusicFileObj))
				chartMusicFile = (string)chartMusicFileObj;

			var chartAuthor = "";
			if (song.Extras.TryGetSourceExtra(TagCredit, out object chartAuthorObj))
				chartAuthor = (string)chartAuthorObj;

			var chartDisplayTempo = GetDisplayBPMStringFromSourceExtrasList(song.Extras, timingProperties.Tempos);

			for (var chartIndex = 0; chartIndex < song.Charts.Count; chartIndex++)
			{
				var chart = song.Charts[chartIndex];
				chart.MusicFile = chartMusicFile;
				chart.ChartOffsetFromMusic = chartOffset;
				chart.Tempo = chartDisplayTempo;
				chart.Artist = song.Artist;
				chart.ArtistTransliteration = song.ArtistTransliteration;
				chart.Genre = song.Genre;
				chart.GenreTransliteration = song.GenreTransliteration;
				chart.Author = chartAuthor;

				// Add any custom properties for this chart that were parsed earlier.
				if (chartProperties.TryGetValue(chartIndex, out var chartProperty))
				{
					foreach (var extraKvp in chartProperty)
						chart.Extras.AddSourceExtra(extraKvp.Key, extraKvp.Value);
				}

				SetEventTimeAndMetricPositionsFromRows(chart);
			}
		}, token);

		return song;
	}
}

/// <summary>
/// Logger to help identify the Song in the logs.
/// </summary>
public class SMReaderLogger : ILogger
{
	private readonly string FilePath;
	private const string Tag = "[SM Reader]";

	public SMReaderLogger(string filePath)
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
