using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;
using static Fumen.Converters.SMCommon;

namespace Fumen.Converters;

/// <summary>
/// Reader for StepMania SSC files.
/// </summary>
public class SSCReader : Reader
{
	/// <summary>
	/// In StepMania parsing, if one of these values is present on a Chart then the Chart is considered
	/// to use its own set of timing data and should not fall back to Song timing data.
	/// </summary>
	private static readonly HashSet<string> ChartTimingDataTags =
	[
		TagBPMs,
		TagStops,
		TagDelays,
		TagTimeSignatures,
		TagTickCounts,
		TagCombos,
		TagWarps,
		TagSpeeds,
		TagScrolls,
		TagFakes,
		TagLabels,
		TagOffset,
	];

	/// <summary>
	/// SSC properties which affect scroll rate and note timing.
	/// Grouped in a small class for organization.
	/// </summary>
	private class TimingProperties
	{
		public readonly Dictionary<double, double> Tempos = new();
		public readonly Dictionary<double, double> Stops = new();
		public readonly Dictionary<double, double> Delays = new();
		public readonly Dictionary<double, double> Warps = new();
		public readonly Dictionary<double, double> Scrolls = new();
		public readonly Dictionary<double, Tuple<double, double, int>> Speeds = new();
		public readonly Dictionary<double, Fraction> TimeSignatures = new();
		public readonly Dictionary<double, int> TickCounts = new();
		public readonly Dictionary<double, string> Labels = new();
		public readonly Dictionary<double, Tuple<int, int>> Combos = new();
		public readonly Dictionary<double, double> Fakes = new();
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	private readonly SSCReaderLogger Logger;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="filePath">Path to the ssc file to load.</param>
	public SSCReader(string filePath)
		: base(filePath)
	{
		Logger = new SSCReaderLogger(FilePath);
	}

	/// <summary>
	/// Load the ssc file and return a Song containing the full set of Charts.
	/// </summary>
	/// <param name="token">CancellationToken to cancel task.</param>
	/// <returns>Song with full Charts.</returns>
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
		var timingProperties = new TimingProperties();
		var propertyParsers = GetSongPropertyParsers(song, timingProperties);
		await LoadAsyncInternal(token, song, msdFile, propertyParsers, timingProperties, false);
		token.ThrowIfCancellationRequested();
		return song;
	}

	/// <summary>
	/// Load the ssc file and return a Song containing only Song and Chart metadata with no step data.
	/// </summary>
	/// <param name="token">CancellationToken to cancel task.</param>
	/// <returns>Song with metadata populated.</returns>
	public override async Task<Song> LoadMetaDataAsync(CancellationToken token)
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
		var timingProperties = new TimingProperties();
		var propertyParsers = GetSongMetaDataPropertyParsers(song);
		await LoadAsyncInternal(token, song, msdFile, propertyParsers, timingProperties, true);
		token.ThrowIfCancellationRequested();
		return song;
	}

	private async Task LoadAsyncInternal(
		CancellationToken token,
		Song song,
		MSDFile msdFile,
		Dictionary<string, PropertyParser> songPropertyParsers,
		TimingProperties songTimingProperties,
		bool metaDataOnly)
	{
		token.ThrowIfCancellationRequested();

		await Task.Run(() =>
		{
			var previousCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

				song.SourceType = FileFormatType.SSC;
				var songExtrasPropertyParser = new ExtrasPropertyParser(song.Extras);
				songExtrasPropertyParser.SetLogger(Logger);

				Dictionary<string, PropertyParser> chartPropertyParsers = null;
				ExtrasPropertyParser chartExtrasPropertyParser = null;
				Chart activeChart = null;
				var firstChart = true;
				var activeChartTimingProperties = new TimingProperties();

				// Parse all Values from the MSDFile.
				foreach (var value in msdFile.Values)
				{
					var valueStr = value.Params[0]?.ToUpper() ?? "";

					// Starting a new Chart.
					if (valueStr == TagNoteData)
					{
						token.ThrowIfCancellationRequested();

						// Final cleanup on the Song
						if (activeChart == null)
						{
							song.GenreTransliteration = song.Genre;
						}

						// Add the previous Chart.
						if (activeChart != null)
						{
							FinalizeChartAndAddToSong(
								activeChart,
								activeChartTimingProperties,
								song,
								songTimingProperties,
								ref firstChart,
								metaDataOnly);
						}

						// Set up a new Chart.
						activeChart = new Chart();
						activeChart.Layers.Add(new Layer());
						activeChartTimingProperties = new TimingProperties();
						chartPropertyParsers = metaDataOnly
							? GetChartPropertyParsers(activeChart, activeChartTimingProperties)
							: GetChartMetaDataPropertyParsers(activeChart);
						chartExtrasPropertyParser = new ExtrasPropertyParser(activeChart.Extras);
						chartExtrasPropertyParser.SetLogger(Logger);
						continue;
					}

					// Parse as a Chart property.
					if (activeChart != null)
					{
						// Matches Stepmania logic. If any timing value is present, assume all timing values must be from the
						// Chart and not the Song.
						if (ChartTimingDataTags.Contains(valueStr))
							activeChart.Extras.AddSourceExtra(TagFumenChartUsesOwnTimingData, true, true);

						if (chartPropertyParsers.TryGetValue(valueStr, out var propertyParser))
							propertyParser.Parse(value);
						else
							chartExtrasPropertyParser.Parse(value);
					}

					// Parse as a Song property.
					else
					{
						if (songPropertyParsers.TryGetValue(valueStr, out var propertyParser))
							propertyParser.Parse(value);
						else
							songExtrasPropertyParser.Parse(value);
					}
				}

				// Add the final Chart.
				if (activeChart != null)
				{
					FinalizeChartAndAddToSong(
						activeChart,
						activeChartTimingProperties,
						song,
						songTimingProperties,
						ref firstChart,
						metaDataOnly);
				}
			}
			catch (OperationCanceledException)
			{
				// Intentionally Ignored.
			}
			finally
			{
				CultureInfo.CurrentCulture = previousCulture;
			}
		}, token);

		token.ThrowIfCancellationRequested();
	}

	private void FinalizeChartAndAddToSong(
		Chart chart,
		TimingProperties chartTimingProperties,
		Song song,
		TimingProperties songTimingProperties,
		ref bool firstChart,
		bool metaDataOnly)
	{
		// Do not add this Chart if we failed to parse the type.
		if (string.IsNullOrEmpty(chart.Type))
		{
			return;
		}

		var logTimeEventErrors = firstChart;
		var timingProperties = songTimingProperties;
		if (chart.Extras.TryGetSourceExtra(TagFumenChartUsesOwnTimingData, out object useTimingDataObject))
		{
			if (useTimingDataObject is true)
			{
				timingProperties = chartTimingProperties;
				logTimeEventErrors = true;
			}
		}

		if (!metaDataOnly)
		{
			// Insert time property events.
			AddStops(timingProperties.Stops, chart, Logger, logTimeEventErrors);
			AddDelays(timingProperties.Delays, chart, Logger, logTimeEventErrors);
			AddWarps(timingProperties.Warps, chart, Logger, logTimeEventErrors);
			AddScrollRateEvents(timingProperties.Scrolls, chart, Logger, logTimeEventErrors);
			AddScrollRateInterpolationEvents(timingProperties.Speeds, chart, Logger, logTimeEventErrors);
			AddTempos(timingProperties.Tempos, chart, Logger, logTimeEventErrors);
			AddTimeSignatures(timingProperties.TimeSignatures, chart, Logger, logTimeEventErrors);
			AddTickCountEvents(timingProperties.TickCounts, chart, Logger, logTimeEventErrors);
			AddLabelEvents(timingProperties.Labels, chart, Logger, logTimeEventErrors);
			AddFakeSegmentEvents(timingProperties.Fakes, chart, Logger, logTimeEventErrors);
			AddMultipliersEvents(timingProperties.Combos, chart, Logger, logTimeEventErrors);

			// Sort events.
			chart.Layers[0].Events.Sort(new SMEventComparer());
		}

		// Copy Song information over missing Chart information.
		if (string.IsNullOrEmpty(chart.MusicFile))
		{
			var chartMusicFile = "";
			if (song.Extras.TryGetSourceExtra(TagMusic, out object chartMusicFileObj))
				chartMusicFile = (string)chartMusicFileObj;
			if (!string.IsNullOrEmpty(chartMusicFile))
				chart.MusicFile = chartMusicFile;
		}

		if (string.IsNullOrEmpty(chart.Author))
		{
			var chartAuthor = "";
			if (song.Extras.TryGetSourceExtra(TagCredit, out object chartAuthorObj))
				chartAuthor = (string)chartAuthorObj;
			if (!string.IsNullOrEmpty(chartAuthor))
				chart.Author = chartAuthor;
		}

		if (string.IsNullOrEmpty(chart.Artist) && !string.IsNullOrEmpty(song.Artist))
			chart.Artist = song.Artist;
		if (string.IsNullOrEmpty(chart.ArtistTransliteration) && !string.IsNullOrEmpty(song.ArtistTransliteration))
			chart.ArtistTransliteration = song.ArtistTransliteration;
		if (string.IsNullOrEmpty(chart.Genre) && !string.IsNullOrEmpty(song.Genre))
			chart.Genre = song.Genre;
		if (string.IsNullOrEmpty(chart.GenreTransliteration) && !string.IsNullOrEmpty(song.GenreTransliteration))
			chart.GenreTransliteration = song.GenreTransliteration;

		if (!chart.Extras.TryGetSourceExtra(TagOffset, out object _)
		    && song.Extras.TryGetSourceExtra(TagOffset, out object offsetObj))
		{
			chart.ChartOffsetFromMusic = (double)offsetObj;
		}

		if (!chart.Extras.TryGetSourceExtra(TagDisplayBPM, out object _))
		{
			chart.Tempo = GetDisplayBPMStringFromSourceExtrasList(
				song.Extras,
				timingProperties.Tempos);
		}

		if (!metaDataOnly)
			SetEventTimeAndMetricPositionsFromRows(chart);

		// Add the Chart.
		song.Charts.Add(chart);

		firstChart = false;
	}

	private Dictionary<string, PropertyParser> GetSongPropertyParsers(
		Song song,
		TimingProperties songTimingProperties)
	{
		var parsers = new Dictionary<string, PropertyParser>()
		{
			// Song tags.
			[TagVersion] = new PropertyToSourceExtrasParser<double>(TagVersion, song.Extras),
			[TagTitle] = new PropertyToSongPropertyParser(TagTitle, nameof(Song.Title), song),
			[TagSubtitle] = new PropertyToSongPropertyParser(TagSubtitle, nameof(Song.SubTitle), song),
			[TagArtist] = new PropertyToSongPropertyParser(TagArtist, nameof(Song.Artist), song),
			[TagTitleTranslit] = new PropertyToSongPropertyParser(TagTitleTranslit, nameof(Song.TitleTransliteration), song),
			[TagSubtitleTranslit] =
				new PropertyToSongPropertyParser(TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
			[TagArtistTranslit] =
				new PropertyToSongPropertyParser(TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
			[TagGenre] = new PropertyToSongPropertyParser(TagGenre, nameof(Song.Genre), song),
			[TagOrigin] = new PropertyToSourceExtrasParser<string>(TagOrigin, song.Extras),
			[TagCredit] = new PropertyToSourceExtrasParser<string>(TagCredit, song.Extras),
			[TagBanner] = new PropertyToSongPropertyParser(TagBanner, nameof(Song.SongSelectImage), song),
			[TagBackground] = new PropertyToSourceExtrasParser<string>(TagBackground, song.Extras),
			[TagPreviewVid] = new PropertyToSourceExtrasParser<string>(TagPreviewVid, song.Extras),
			[TagJacket] = new PropertyToSourceExtrasParser<string>(TagJacket, song.Extras),
			[TagCDImage] = new PropertyToSourceExtrasParser<string>(TagCDImage, song.Extras),
			[TagDiscImage] = new PropertyToSourceExtrasParser<string>(TagDiscImage, song.Extras),
			[TagLyricsPath] = new PropertyToSourceExtrasParser<string>(TagLyricsPath, song.Extras),
			[TagCDTitle] = new PropertyToSourceExtrasParser<string>(TagCDTitle, song.Extras),
			[TagMusic] = new PropertyToSourceExtrasParser<string>(TagMusic, song.Extras),
			[TagPreview] = new PropertyToSongPropertyParser(TagPreview, nameof(Song.PreviewMusicFile), song),
			[TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(TagInstrumentTrack, song.Extras),
			[TagMusicLength] = new PropertyToSourceExtrasParser<double>(TagMusicLength, song.Extras),
			[TagLastSecondHint] = new PropertyToSourceExtrasParser<double>(TagLastSecondHint, song.Extras),
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
			[TagOffset] = new PropertyToSourceExtrasParser<double>(TagOffset, song.Extras),

			// These tags are only used if the individual charts do not specify values.
			[TagStops] = new CSVListAtTimePropertyParser<double>(TagStops, songTimingProperties.Stops, song.Extras,
				TagFumenRawStopsStr),
			[TagDelays] = new CSVListAtTimePropertyParser<double>(TagDelays, songTimingProperties.Delays, song.Extras,
				TagFumenRawDelaysStr),
			[TagBPMs] = new CSVListAtTimePropertyParser<double>(TagBPMs, songTimingProperties.Tempos, song.Extras,
				TagFumenRawBpmsStr),
			[TagWarps] = new CSVListAtTimePropertyParser<double>(TagWarps, songTimingProperties.Warps, song.Extras,
				TagFumenRawWarpsStr),
			[TagLabels] = new CSVListAtTimePropertyParser<string>(TagLabels, songTimingProperties.Labels, song.Extras,
				TagFumenRawLabelsStr),
			// Removed, see https://github.com/stepmania/stepmania/issues/9
			// SSC files are forced 4/4 time signatures. Other time signatures can be provided, but they are only
			// suggestions to a renderer for how to draw measure markers.
			[TagTimeSignatures] = new ListFractionPropertyParser(TagTimeSignatures, songTimingProperties.TimeSignatures,
				song.Extras, TagFumenRawTimeSignaturesStr),
			[TagTickCounts] = new CSVListAtTimePropertyParser<int>(TagTickCounts, songTimingProperties.TickCounts,
				song.Extras, TagFumenRawTickCountsStr),
			[TagCombos] = new ComboPropertyParser(TagCombos, songTimingProperties.Combos, song.Extras, TagFumenRawCombosStr),
			[TagSpeeds] = new ScrollRateInterpolationPropertyParser(TagSpeeds, songTimingProperties.Speeds, song.Extras,
				TagFumenRawSpeedsStr),
			[TagScrolls] = new CSVListAtTimePropertyParser<double>(TagScrolls, songTimingProperties.Scrolls, song.Extras,
				TagFumenRawScrollsStr),
			[TagFakes] = new CSVListAtTimePropertyParser<double>(TagFakes, songTimingProperties.Fakes, song.Extras,
				TagFumenRawFakesStr),
			[TagFirstSecond] = new PropertyToSourceExtrasParser<string>(TagFirstSecond, song.Extras),
			[TagLastSecond] = new PropertyToSourceExtrasParser<string>(TagLastSecond, song.Extras),
			[TagSongFileName] = new PropertyToSourceExtrasParser<string>(TagSongFileName, song.Extras),
			[TagHasMusic] = new PropertyToSourceExtrasParser<string>(TagHasMusic, song.Extras),
			[TagHasBanner] = new PropertyToSourceExtrasParser<string>(TagHasBanner, song.Extras),
		};
		foreach (var kvp in parsers)
			kvp.Value.SetLogger(Logger);
		return parsers;
	}

	private Dictionary<string, PropertyParser> GetSongMetaDataPropertyParsers(Song song)
	{
		var parsers = new Dictionary<string, PropertyParser>()
		{
			// Song tags.
			[TagVersion] = new PropertyToSourceExtrasParser<double>(TagVersion, song.Extras),
			[TagTitle] = new PropertyToSongPropertyParser(TagTitle, nameof(Song.Title), song),
			[TagSubtitle] = new PropertyToSongPropertyParser(TagSubtitle, nameof(Song.SubTitle), song),
			[TagArtist] = new PropertyToSongPropertyParser(TagArtist, nameof(Song.Artist), song),
			[TagTitleTranslit] = new PropertyToSongPropertyParser(TagTitleTranslit, nameof(Song.TitleTransliteration), song),
			[TagSubtitleTranslit] =
				new PropertyToSongPropertyParser(TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
			[TagArtistTranslit] =
				new PropertyToSongPropertyParser(TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
			[TagGenre] = new PropertyToSongPropertyParser(TagGenre, nameof(Song.Genre), song),
			[TagOrigin] = new PropertyToSourceExtrasParser<string>(TagOrigin, song.Extras),
			[TagCredit] = new PropertyToSourceExtrasParser<string>(TagCredit, song.Extras),
			[TagBanner] = new PropertyToSongPropertyParser(TagBanner, nameof(Song.SongSelectImage), song),
			[TagBackground] = new PropertyToSourceExtrasParser<string>(TagBackground, song.Extras),
			[TagPreviewVid] = new PropertyToSourceExtrasParser<string>(TagPreviewVid, song.Extras),
			[TagJacket] = new PropertyToSourceExtrasParser<string>(TagJacket, song.Extras),
			[TagCDImage] = new PropertyToSourceExtrasParser<string>(TagCDImage, song.Extras),
			[TagDiscImage] = new PropertyToSourceExtrasParser<string>(TagDiscImage, song.Extras),
			[TagLyricsPath] = new PropertyToSourceExtrasParser<string>(TagLyricsPath, song.Extras),
			[TagCDTitle] = new PropertyToSourceExtrasParser<string>(TagCDTitle, song.Extras),
			[TagMusic] = new PropertyToSourceExtrasParser<string>(TagMusic, song.Extras),
			[TagPreview] = new PropertyToSongPropertyParser(TagPreview, nameof(Song.PreviewMusicFile), song),
			[TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(TagInstrumentTrack, song.Extras),
			[TagMusicLength] = new PropertyToSourceExtrasParser<double>(TagMusicLength, song.Extras),
			[TagLastSecondHint] = new PropertyToSourceExtrasParser<double>(TagLastSecondHint, song.Extras),
			[TagSampleStart] = new PropertyToSongPropertyParser(TagSampleStart, nameof(Song.PreviewSampleStart), song),
			[TagSampleLength] = new PropertyToSongPropertyParser(TagSampleLength, nameof(Song.PreviewSampleLength), song),
			[TagDisplayBPM] = new ListPropertyToSourceExtrasParser<string>(TagDisplayBPM, song.Extras),
			[TagSelectable] = new PropertyToSourceExtrasParser<string>(TagSelectable, song.Extras),
			[TagAnimations] = new PropertyToSourceExtrasParser<string>(TagAnimations, song.Extras),
			[TagBGChanges] = new PropertyToSourceExtrasParser<string>(TagBGChanges, song.Extras),
			[TagBGChanges1] = new PropertyToSourceExtrasParser<string>(TagBGChanges1, song.Extras),
			[TagBGChanges2] = new PropertyToSourceExtrasParser<string>(TagBGChanges2, song.Extras),
			[TagFGChanges] = new PropertyToSourceExtrasParser<string>(TagFGChanges, song.Extras),
			[TagKeySounds] = new PropertyToSourceExtrasParser<string>(TagKeySounds, song.Extras),
			[TagAttacks] = new ListPropertyToSourceExtrasParser<string>(TagAttacks, song.Extras),
			[TagOffset] = new PropertyToSourceExtrasParser<double>(TagOffset, song.Extras),
		};
		foreach (var kvp in parsers)
			kvp.Value.SetLogger(Logger);
		return parsers;
	}

	private Dictionary<string, PropertyParser> GetChartPropertyParsers(
		Chart chart,
		TimingProperties chartTimingProperties)
	{
		var parsers = new Dictionary<string, PropertyParser>()
		{
			[TagVersion] = new PropertyToSourceExtrasParser<double>(TagVersion, chart.Extras),
			[TagChartName] = new PropertyToSourceExtrasParser<string>(TagChartName, chart.Extras),
			[TagStepsType] = new ChartTypePropertyParser(chart),
			[TagChartStyle] = new PropertyToSourceExtrasParser<string>(TagChartStyle, chart.Extras),
			[TagDescription] = new PropertyToChartPropertyParser(TagDescription, nameof(Chart.Description), chart),
			[TagDifficulty] = new PropertyToChartPropertyParser(TagDifficulty, nameof(Chart.DifficultyType), chart),
			[TagMeter] = new PropertyToChartPropertyParser(TagMeter, nameof(Chart.DifficultyRating), chart),
			[TagRadarValues] = new PropertyToSourceExtrasParser<string>(TagRadarValues, chart.Extras),
			[TagCredit] = new PropertyToChartPropertyParser(TagCredit, nameof(Chart.Author), chart),
			[TagMusic] = new PropertyToChartPropertyParser(TagMusic, nameof(Chart.MusicFile), chart),
			[TagBPMs] = new CSVListAtTimePropertyParser<double>(TagBPMs, chartTimingProperties.Tempos, chart.Extras,
				TagFumenRawBpmsStr),
			[TagStops] = new CSVListAtTimePropertyParser<double>(TagStops, chartTimingProperties.Stops, chart.Extras,
				TagFumenRawStopsStr),
			[TagDelays] = new CSVListAtTimePropertyParser<double>(TagDelays, chartTimingProperties.Delays, chart.Extras,
				TagFumenRawDelaysStr),
			// Removed, see https://github.com/stepmania/stepmania/issues/9
			// SSC files are forced 4/4 time signatures. Other time signatures can be provided, but they are only
			// suggestions to a renderer for how to draw measure markers.
			[TagTimeSignatures] = new ListFractionPropertyParser(TagTimeSignatures, chartTimingProperties.TimeSignatures,
				chart.Extras, TagFumenRawTimeSignaturesStr),
			[TagTickCounts] =
				new CSVListAtTimePropertyParser<int>(TagTickCounts, chartTimingProperties.TickCounts, chart.Extras),
			[TagCombos] = new ComboPropertyParser(TagCombos, chartTimingProperties.Combos, chart.Extras),
			[TagWarps] = new CSVListAtTimePropertyParser<double>(TagWarps, chartTimingProperties.Warps, chart.Extras,
				TagFumenRawWarpsStr),
			[TagSpeeds] = new ScrollRateInterpolationPropertyParser(TagSpeeds, chartTimingProperties.Speeds, chart.Extras,
				TagFumenRawSpeedsStr),
			[TagScrolls] = new CSVListAtTimePropertyParser<double>(TagScrolls, chartTimingProperties.Scrolls, chart.Extras,
				TagFumenRawScrollsStr),
			[TagFakes] = new CSVListAtTimePropertyParser<double>(TagFakes, chartTimingProperties.Fakes, chart.Extras),
			[TagLabels] = new CSVListAtTimePropertyParser<string>(TagLabels, chartTimingProperties.Labels, chart.Extras),
			[TagAttacks] = new ListPropertyToSourceExtrasParser<string>(TagAttacks, chart.Extras),
			[TagOffset] = new PropertyToChartPropertyParser(TagOffset, nameof(Chart.ChartOffsetFromMusic), chart),
			[TagSampleStart] = new PropertyToSourceExtrasParser<double>(TagSampleStart, chart.Extras),
			[TagSampleLength] = new PropertyToSourceExtrasParser<double>(TagSampleLength, chart.Extras),
			[TagSelectable] = new PropertyToSourceExtrasParser<string>(TagSelectable, chart.Extras),
			[TagDisplayBPM] = new PropertyToChartPropertyParser(TagDisplayBPM, nameof(Chart.Tempo), chart),
			[TagNotes] = new ChartNotesPropertyParser(TagNotes, chart),
			[TagNotes2] = new ChartNotesPropertyParser(TagNotes2, chart),
		};
		foreach (var kvp in parsers)
			kvp.Value.SetLogger(Logger);
		return parsers;
	}

	private Dictionary<string, PropertyParser> GetChartMetaDataPropertyParsers(Chart chart)
	{
		var parsers = new Dictionary<string, PropertyParser>()
		{
			[TagVersion] = new PropertyToSourceExtrasParser<double>(TagVersion, chart.Extras),
			[TagChartName] = new PropertyToSourceExtrasParser<string>(TagChartName, chart.Extras),
			[TagStepsType] = new ChartTypePropertyParser(chart),
			[TagChartStyle] = new PropertyToSourceExtrasParser<string>(TagChartStyle, chart.Extras),
			[TagDescription] = new PropertyToChartPropertyParser(TagDescription, nameof(Chart.Description), chart),
			[TagDifficulty] = new PropertyToChartPropertyParser(TagDifficulty, nameof(Chart.DifficultyType), chart),
			[TagMeter] = new PropertyToChartPropertyParser(TagMeter, nameof(Chart.DifficultyRating), chart),
			[TagRadarValues] = new PropertyToSourceExtrasParser<string>(TagRadarValues, chart.Extras),
			[TagCredit] = new PropertyToChartPropertyParser(TagCredit, nameof(Chart.Author), chart),
			[TagMusic] = new PropertyToChartPropertyParser(TagMusic, nameof(Chart.MusicFile), chart),
			[TagOffset] = new PropertyToChartPropertyParser(TagOffset, nameof(Chart.ChartOffsetFromMusic), chart),
			[TagSampleStart] = new PropertyToSourceExtrasParser<double>(TagSampleStart, chart.Extras),
			[TagSampleLength] = new PropertyToSourceExtrasParser<double>(TagSampleLength, chart.Extras),
			[TagSelectable] = new PropertyToSourceExtrasParser<string>(TagSelectable, chart.Extras),
			[TagDisplayBPM] = new PropertyToChartPropertyParser(TagDisplayBPM, nameof(Chart.Tempo), chart),
		};
		foreach (var kvp in parsers)
			kvp.Value.SetLogger(Logger);
		return parsers;
	}
}

/// <summary>
/// Logger to help identify the Song in the logs.
/// </summary>
public class SSCReaderLogger : ILogger
{
	private readonly string FilePath;
	private const string Tag = "[SSC Reader]";

	public SSCReaderLogger(string filePath)
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
