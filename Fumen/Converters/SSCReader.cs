using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;

namespace Fumen.Converters
{
	/// <summary>
	/// Reader for StepMania SSC files.
	/// </summary>
	public class SSCReader : Reader
	{
		/// <summary>
		/// In StepMania parsing, if one of these values is present on a Chart then the Chart is considered
		/// to use its own set of timing data and should not fall back to Song timing data.
		/// </summary>
		private static readonly HashSet<string> ChartTimingDataTags = new HashSet<string>()
		{
			SMCommon.TagBPMs,
			SMCommon.TagStops,
			SMCommon.TagDelays,
			SMCommon.TagTimeSignatures,
			SMCommon.TagTickCounts,
			SMCommon.TagCombos,
			SMCommon.TagWarps,
			SMCommon.TagSpeeds,
			SMCommon.TagScrolls,
			SMCommon.TagFakes,
			SMCommon.TagLabels,
			SMCommon.TagOffset,
		};

		/// <summary>
		/// SSC properties which affect scroll rate and note timing.
		/// Grouped in a small class for organization.
		/// </summary>
		private class TimingProperties
		{
			public readonly Dictionary<double, double> Tempos = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Stops = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Delays = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Warps = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Scrolls = new Dictionary<double, double>();
			public readonly Dictionary<double, Tuple<double, double, int>> Speeds = new Dictionary<double, Tuple<double, double, int>>();
			public readonly Dictionary<double, Fraction> TimeSignatures = new Dictionary<double, Fraction>();
			public readonly Dictionary<double, int> TickCounts = new Dictionary<double, int>();
			public readonly Dictionary<double, string> Labels = new Dictionary<double, string>();
			public readonly Dictionary<double, Tuple<int, int>> Combos = new Dictionary<double, Tuple<int, int>>();
			public readonly Dictionary<double, double> Fakes = new Dictionary<double, double>();
		}

		/// <summary>
		/// Path to the ssc file to load.
		/// </summary>
		private readonly string FilePath;

		/// <summary>
		/// Logger to help identify the Song in the logs.
		/// </summary>
		private readonly SCCReaderLogger Logger;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filePath">Path to the ssc file to load.</param>
		public SSCReader(string filePath)
			: base(filePath)
		{
			FilePath = filePath;
			Logger = new SCCReaderLogger(FilePath);
		}

		/// <summary>
		/// Load the ssc file specified by the provided file path.
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
				song.SourceType = FileFormatType.SSC;
				var songTimingProperties = new TimingProperties();
				var songPropertyParsers = GetSongPropertyParsers(song, songTimingProperties);

				Dictionary<string, PropertyParser> chartPropertyParsers = null;
				Chart activeChart = null;
				var firstChart = true;
				var activeChartTimingProperties = new TimingProperties();

				// Parse all Values from the MSDFile.
				foreach (var value in msdFile.Values)
				{
					var valueStr = value.Params[0]?.ToUpper() ?? "";

					// Starting a new Chart.
					if (valueStr == SMCommon.TagNoteData)
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
								firstChart);
							firstChart = false;
						}

						// Set up a new Chart.
						activeChart = new Chart();
						activeChart.Layers.Add(new Layer());
						activeChartTimingProperties = new TimingProperties();
						chartPropertyParsers = GetChartPropertyParsers(activeChart, activeChartTimingProperties);
						continue;
					}

					// Parse as a Chart property.
					if (activeChart != null)
					{
						// Matches Stepmania logic. If any timing value is present, assume all timing values must be from the
						// Chart and not the Song.
						if (ChartTimingDataTags.Contains(valueStr))
							activeChart.Extras.AddSourceExtra(SMCommon.TagFumenChartUsesOwnTimingData, true, true);

						if (chartPropertyParsers.TryGetValue(valueStr, out var propertyParser))
							propertyParser.Parse(value);
					}

					// Parse as a Song property.
					else
					{
						if (songPropertyParsers.TryGetValue(valueStr, out var propertyParser))
							propertyParser.Parse(value);
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
						firstChart);
					firstChart = false;
				}
			}, token);

			return song;
		}

		private void FinalizeChartAndAddToSong(
			Chart chart,
			TimingProperties chartTimingProperties,
			Song song,
			TimingProperties songTimingProperties,
			bool firstChart)
		{
			// Do not add this Chart if we failed to parse the type.
			if (string.IsNullOrEmpty(chart.Type))
			{
				return;
			}

			var timingProperties = songTimingProperties;
			if (chart.Extras.TryGetSourceExtra(SMCommon.TagFumenChartUsesOwnTimingData, out object useTimingDataObject))
			{
				if (useTimingDataObject is bool b && b)
					timingProperties = chartTimingProperties;
			}

			// Insert time property events.
			SMCommon.AddStops(timingProperties.Stops, chart);
			SMCommon.AddDelays(timingProperties.Delays, chart);
			SMCommon.AddWarps(timingProperties.Warps, chart);
			SMCommon.AddScrollRateEvents(timingProperties.Scrolls, chart);
			SMCommon.AddScrollRateInterpolationEvents(timingProperties.Speeds, chart);
			SMCommon.AddTempos(timingProperties.Tempos, chart);
			SMCommon.AddTimeSignatures(timingProperties.TimeSignatures, chart, Logger, firstChart);
			SMCommon.AddTickCountEvents(timingProperties.TickCounts, chart);
			SMCommon.AddLabelEvents(timingProperties.Labels, chart);
			SMCommon.AddFakeSegmentEvents(timingProperties.Fakes, chart);
			SMCommon.AddMultipliersEvents(timingProperties.Combos, chart);

			// Sort events.
			chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

			// Copy Song information over missing Chart information.
			if (string.IsNullOrEmpty(chart.MusicFile))
			{
				var chartMusicFile = "";
				if (song.Extras.TryGetSourceExtra(SMCommon.TagMusic, out object chartMusicFileObj))
					chartMusicFile = (string)chartMusicFileObj;
				if (!string.IsNullOrEmpty(chartMusicFile))
					chart.MusicFile = chartMusicFile;
			}
			if (string.IsNullOrEmpty(chart.Author))
			{
				var chartAuthor = "";
				if (song.Extras.TryGetSourceExtra(SMCommon.TagCredit, out object chartAuthorObj))
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

			if (!chart.Extras.TryGetSourceExtra(SMCommon.TagOffset, out object _)
			    && song.Extras.TryGetSourceExtra(SMCommon.TagOffset, out object offsetObj))
			{
				chart.ChartOffsetFromMusic = (double)offsetObj;
			}

			if (!chart.Extras.TryGetSourceExtra(SMCommon.TagDisplayBPM, out object _))
				chart.Tempo = SMCommon.GetDisplayBPMStringFromSourceExtrasList(
					song.Extras,
					timingProperties.Tempos);

			SMCommon.SetEventTimeAndMetricPositionsFromRows(chart);

			// Add the Chart.
			song.Charts.Add(chart);
		}

		private Dictionary<string, PropertyParser> GetSongPropertyParsers(
			Song song,
			TimingProperties songTimingProperties)
		{
			var parsers = new Dictionary<string, PropertyParser>()
			{
				// Song tags.
				[SMCommon.TagVersion] = new PropertyToSourceExtrasParser<double>(SMCommon.TagVersion, song.Extras),
				[SMCommon.TagTitle] = new PropertyToSongPropertyParser(SMCommon.TagTitle, nameof(Song.Title), song),
				[SMCommon.TagSubtitle] = new PropertyToSongPropertyParser(SMCommon.TagSubtitle, nameof(Song.SubTitle), song),
				[SMCommon.TagArtist] = new PropertyToSongPropertyParser(SMCommon.TagArtist, nameof(Song.Artist), song),
				[SMCommon.TagTitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagTitleTranslit, nameof(Song.TitleTransliteration), song),
				[SMCommon.TagSubtitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
				[SMCommon.TagArtistTranslit] = new PropertyToSongPropertyParser(SMCommon.TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
				[SMCommon.TagGenre] = new PropertyToSongPropertyParser(SMCommon.TagGenre, nameof(Song.Genre), song),
				[SMCommon.TagOrigin] = new PropertyToSourceExtrasParser<string>(SMCommon.TagOrigin, song.Extras),
				[SMCommon.TagCredit] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCredit, song.Extras),
				[SMCommon.TagBanner] = new PropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage), song),
				[SMCommon.TagBackground] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBackground, song.Extras),
				[SMCommon.TagPreviewVid] = new PropertyToSourceExtrasParser<string>(SMCommon.TagPreviewVid, song.Extras),
				[SMCommon.TagJacket] = new PropertyToSourceExtrasParser<string>(SMCommon.TagJacket, song.Extras),
				[SMCommon.TagCDImage] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDImage, song.Extras),
				[SMCommon.TagDiscImage] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDiscImage, song.Extras),
				[SMCommon.TagLyricsPath] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLyricsPath, song.Extras),
				[SMCommon.TagCDTitle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDTitle, song.Extras),
				[SMCommon.TagMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagMusic, song.Extras),
				[SMCommon.TagPreview] = new PropertyToSongPropertyParser(SMCommon.TagPreview, nameof(Song.PreviewMusicFile), song),
				[SMCommon.TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(SMCommon.TagInstrumentTrack, song.Extras),
				[SMCommon.TagMusicLength] = new PropertyToSourceExtrasParser<double>(SMCommon.TagMusicLength, song.Extras),
				[SMCommon.TagLastSecondHint] = new PropertyToSourceExtrasParser<double>(SMCommon.TagLastSecondHint, song.Extras),
				[SMCommon.TagSampleStart] = new PropertyToSongPropertyParser(SMCommon.TagSampleStart, nameof(Song.PreviewSampleStart), song),
				[SMCommon.TagSampleLength] = new PropertyToSongPropertyParser(SMCommon.TagSampleLength, nameof(Song.PreviewSampleLength), song),
				[SMCommon.TagDisplayBPM] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagDisplayBPM, song.Extras),
				[SMCommon.TagSelectable] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSelectable, song.Extras),
				[SMCommon.TagAnimations] = new PropertyToSourceExtrasParser<string>(SMCommon.TagAnimations, song.Extras),
				[SMCommon.TagBGChanges] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges, song.Extras),
				[SMCommon.TagBGChanges1] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges1, song.Extras),
				[SMCommon.TagBGChanges2] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges2, song.Extras),
				[SMCommon.TagFGChanges] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFGChanges, song.Extras),
				// TODO: Parse Keysounds properly.
				[SMCommon.TagKeySounds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagKeySounds, song.Extras),
				[SMCommon.TagAttacks] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagAttacks, song.Extras),
				[SMCommon.TagOffset] = new PropertyToSourceExtrasParser<double>(SMCommon.TagOffset, song.Extras),

				// These tags are only used if the individual charts do not specify values.
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, songTimingProperties.Stops, song.Extras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new CSVListAtTimePropertyParser<double>(SMCommon.TagDelays, songTimingProperties.Delays, song.Extras, SMCommon.TagFumenRawDelaysStr),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, songTimingProperties.Tempos, song.Extras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagWarps] = new CSVListAtTimePropertyParser<double>(SMCommon.TagWarps, songTimingProperties.Warps, song.Extras, SMCommon.TagFumenRawWarpsStr),
				[SMCommon.TagLabels] = new CSVListAtTimePropertyParser<string>(SMCommon.TagLabels, songTimingProperties.Labels, song.Extras, SMCommon.TagFumenRawLabelsStr),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				// SSC files are forced 4/4 time signatures. Other time signatures can be provided but they are only
				// suggestions to a renderer for how to draw measure markers.
				[SMCommon.TagTimeSignatures] = new ListFractionPropertyParser(SMCommon.TagTimeSignatures, songTimingProperties.TimeSignatures, song.Extras, SMCommon.TagFumenRawTimeSignaturesStr),
				[SMCommon.TagTickCounts] = new CSVListAtTimePropertyParser<int>(SMCommon.TagTickCounts, songTimingProperties.TickCounts, song.Extras, SMCommon.TagFumenRawTickCountsStr),
				[SMCommon.TagCombos] = new ComboPropertyParser(SMCommon.TagCombos, songTimingProperties.Combos, song.Extras, SMCommon.TagFumenRawCombosStr),
				[SMCommon.TagSpeeds] = new ScrollRateInterpolationPropertyParser(SMCommon.TagSpeeds, songTimingProperties.Speeds, song.Extras, SMCommon.TagFumenRawSpeedsStr),
				[SMCommon.TagScrolls] = new CSVListAtTimePropertyParser<double>(SMCommon.TagScrolls, songTimingProperties.Scrolls, song.Extras, SMCommon.TagFumenRawScrollsStr),
				[SMCommon.TagFakes] = new CSVListAtTimePropertyParser<double>(SMCommon.TagFakes, songTimingProperties.Fakes, song.Extras, SMCommon.TagFumenRawFakesStr),
				[SMCommon.TagFirstSecond] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFirstSecond, song.Extras),
				[SMCommon.TagLastSecond] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLastSecond, song.Extras),
				[SMCommon.TagSongFileName] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSongFileName, song.Extras),
				[SMCommon.TagHasMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagHasMusic, song.Extras),
				[SMCommon.TagHasBanner] = new PropertyToSourceExtrasParser<string>(SMCommon.TagHasBanner, song.Extras),
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
				[SMCommon.TagVersion] = new PropertyToSourceExtrasParser<double>(SMCommon.TagVersion, chart.Extras),
				[SMCommon.TagChartName] = new PropertyToSourceExtrasParser<string>(SMCommon.TagChartName, chart.Extras),
				[SMCommon.TagStepsType] = new ChartTypePropertyParser(chart),
				[SMCommon.TagChartStyle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagChartStyle, chart.Extras),
				[SMCommon.TagDescription] = new PropertyToChartPropertyParser(SMCommon.TagDescription, nameof(Chart.Description), chart),
				[SMCommon.TagDifficulty] = new PropertyToChartPropertyParser(SMCommon.TagDifficulty, nameof(Chart.DifficultyType), chart),
				[SMCommon.TagMeter] = new PropertyToChartPropertyParser(SMCommon.TagMeter, nameof(Chart.DifficultyRating), chart),
				[SMCommon.TagRadarValues] = new PropertyToSourceExtrasParser<string>(SMCommon.TagRadarValues, chart.Extras),
				[SMCommon.TagCredit] = new PropertyToChartPropertyParser(SMCommon.TagCredit, nameof(Chart.Author), chart),
				[SMCommon.TagMusic] = new PropertyToChartPropertyParser(SMCommon.TagMusic, nameof(Chart.MusicFile), chart),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, chartTimingProperties.Tempos, chart.Extras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, chartTimingProperties.Stops, chart.Extras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new CSVListAtTimePropertyParser<double>(SMCommon.TagDelays, chartTimingProperties.Delays, chart.Extras, SMCommon.TagFumenRawDelaysStr),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				// SSC files are forced 4/4 time signatures. Other time signatures can be provided but they are only
				// suggestions to a renderer for how to draw measure markers.
				[SMCommon.TagTimeSignatures] = new ListFractionPropertyParser(SMCommon.TagTimeSignatures, chartTimingProperties.TimeSignatures, chart.Extras, SMCommon.TagFumenRawTimeSignaturesStr),
				[SMCommon.TagTickCounts] = new CSVListAtTimePropertyParser<int>(SMCommon.TagTickCounts, chartTimingProperties.TickCounts, chart.Extras),
				[SMCommon.TagCombos] = new ComboPropertyParser(SMCommon.TagCombos, chartTimingProperties.Combos, chart.Extras),
				[SMCommon.TagWarps] = new CSVListAtTimePropertyParser<double>(SMCommon.TagWarps, chartTimingProperties.Warps, chart.Extras, SMCommon.TagFumenRawWarpsStr),
				[SMCommon.TagSpeeds] = new ScrollRateInterpolationPropertyParser(SMCommon.TagSpeeds, chartTimingProperties.Speeds, chart.Extras, SMCommon.TagFumenRawSpeedsStr),
				[SMCommon.TagScrolls] = new CSVListAtTimePropertyParser<double>(SMCommon.TagScrolls, chartTimingProperties.Scrolls, chart.Extras, SMCommon.TagFumenRawScrollsStr),
				[SMCommon.TagFakes] = new CSVListAtTimePropertyParser<double>(SMCommon.TagFakes, chartTimingProperties.Fakes, chart.Extras),
				[SMCommon.TagLabels] = new CSVListAtTimePropertyParser<string>(SMCommon.TagLabels, chartTimingProperties.Labels, chart.Extras),
				[SMCommon.TagAttacks] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagAttacks, chart.Extras),
				[SMCommon.TagOffset] = new PropertyToChartPropertyParser(SMCommon.TagOffset, nameof(Chart.ChartOffsetFromMusic), chart),
				[SMCommon.TagSampleStart] = new PropertyToSourceExtrasParser<double>(SMCommon.TagSampleStart, chart.Extras),
				[SMCommon.TagSampleLength] = new PropertyToSourceExtrasParser<double>(SMCommon.TagSampleLength, chart.Extras),
				[SMCommon.TagSelectable] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSelectable, chart.Extras),
				[SMCommon.TagDisplayBPM] = new PropertyToChartPropertyParser(SMCommon.TagDisplayBPM, nameof(Chart.Tempo), chart),
				[SMCommon.TagNotes] = new ChartNotesPropertyParser(SMCommon.TagNotes, chart),
				[SMCommon.TagNotes2] = new ChartNotesPropertyParser(SMCommon.TagNotes2, chart),
			};
			foreach (var kvp in parsers)
				kvp.Value.SetLogger(Logger);
			return parsers;
		}
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	public class SCCReaderLogger : ILogger
	{
		private readonly string FilePath;
		private const string Tag = "[SSC Reader]";

		public SCCReaderLogger(string filePath)
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
