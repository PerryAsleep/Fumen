using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fumen.Converters
{
	public class SSCReader : Reader
	{
		/// <summary>
		/// In stepmania parsing, if one of these values is present on a Chart then the Chart is considered
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
		public override async Task<Song> Load()
		{
			// Load the file as an MSDFile.
			var msdFile = new MSDFile();
			var result = await msdFile.Load(FilePath);
			if (!result)
			{
				Logger.Error("Failed to load MSD File.");
				return null;
			}

			var song = new Song();
			song.SourceType = FileFormatType.SSC;
			var songTempos = new Dictionary<double, double>();
			var songStops = new Dictionary<double, double>();
			var songPropertyParsers = GetSongPropertyParsers(song, songTempos, songStops);

			Dictionary<string, PropertyParser> chartPropertyParsers = null;
			Chart activeChart = null;
			Dictionary<double, double> activeChartTempos = null;
			Dictionary<double, double> activeChartStops = null;

			// Parse all Values from the MSDFile.
			foreach (var value in msdFile.Values)
			{
				var valueStr = value.Params[0]?.ToUpper() ?? "";

				// Starting a new Chart.
				if (valueStr == SMCommon.TagNoteData)
				{
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
							activeChartTempos,
							activeChartStops,
							song,
							songTempos,
							songStops);
					}

					// Set up a new Chart.
					activeChart = new Chart();
					activeChart.Layers.Add(new Layer());
					// Add a 4/4 Time Signature.
					activeChart.Layers[0].Events.Add(new TimeSignature()
					{
						Position = new MetricPosition(),
						Signature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure)
					});

					activeChartTempos = new Dictionary<double, double>();
					activeChartStops = new Dictionary<double, double>();
					chartPropertyParsers = GetChartPropertyParsers(activeChart, activeChartTempos, activeChartStops);
					continue;
				}

				// Parse as a Chart property.
				if (activeChart != null)
				{
					// Matches Stepmania logic. If any timing value is present, assume all timing values must be from the
					// Chart and not the Song.
					if (ChartTimingDataTags.Contains(valueStr))
						activeChart.SourceExtras[SMCommon.TagFumenChartUsesOwnTimingData] = true;

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
					activeChartTempos,
					activeChartStops,
					song,
					songTempos,
					songStops);
			}

			return song;
		}

		private void FinalizeChartAndAddToSong(
			Chart chart,
			Dictionary<double, double> chartTempos,
			Dictionary<double, double> chartStops,
			Song song,
			Dictionary<double, double> songTempos,
			Dictionary<double, double> songStops)
		{
			// Do not add this Chart if we failed to parse the type.
			if (string.IsNullOrEmpty(chart.Type))
			{
				return;
			}

			var useChartTimingData = false;
			if (chart.SourceExtras.TryGetValue(SMCommon.TagFumenChartUsesOwnTimingData, out var useTimingDataObject))
			{
				if (useTimingDataObject is bool b)
					useChartTimingData = b;
			}

			// Insert stop and tempo change events.
			SMCommon.AddStops(useChartTimingData ? chartStops : songStops, chart);
			SMCommon.AddTempos(useChartTimingData ? chartTempos : songTempos, chart);

			// Sort events.
			chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

			// Copy Song information over missing Chart information.
			if (string.IsNullOrEmpty(chart.MusicFile))
			{
				var chartMusicFile = "";
				if (song.SourceExtras.TryGetValue(SMCommon.TagMusic, out var chartMusicFileObj))
					chartMusicFile = (string)chartMusicFileObj;
				if (!string.IsNullOrEmpty(chartMusicFile))
					chart.MusicFile = chartMusicFile;
			}
			if (string.IsNullOrEmpty(chart.Author))
			{
				var chartAuthor = "";
				if (song.SourceExtras.TryGetValue(SMCommon.TagCredit, out var chartAuthorObj))
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

			if (!chart.SourceExtras.ContainsKey(SMCommon.TagOffset) && song.SourceExtras.ContainsKey(SMCommon.TagOffset))
			{
				if (song.SourceExtras.TryGetValue(SMCommon.TagOffset, out var offsetObj))
					chart.ChartOffsetFromMusic = (double)offsetObj;
			}

			if (!chart.SourceExtras.ContainsKey(SMCommon.TagDisplayBPM))
				chart.Tempo = SMCommon.GetDisplayBPMStringFromSourceExtrasList(
					song.SourceExtras,
					useChartTimingData ? chartTempos : songTempos);

			SMCommon.SetEventTimeMicros(chart);

			// Add the Chart.
			song.Charts.Add(chart);
		}

		private Dictionary<string, PropertyParser> GetSongPropertyParsers(
			Song song,
			Dictionary<double, double> songTempos,
			Dictionary<double, double> songStops)
		{
			var parsers = new Dictionary<string, PropertyParser>()
			{
				// Song tags.
				[SMCommon.TagVersion] = new PropertyToSourceExtrasParser<double>(SMCommon.TagVersion, song.SourceExtras),
				[SMCommon.TagTitle] = new PropertyToSongPropertyParser(SMCommon.TagTitle, nameof(Song.Title), song),
				[SMCommon.TagSubtitle] = new PropertyToSongPropertyParser(SMCommon.TagSubtitle, nameof(Song.SubTitle), song),
				[SMCommon.TagArtist] = new PropertyToSongPropertyParser(SMCommon.TagArtist, nameof(Song.Artist), song),
				[SMCommon.TagTitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagTitleTranslit, nameof(Song.TitleTransliteration), song),
				[SMCommon.TagSubtitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
				[SMCommon.TagArtistTranslit] = new PropertyToSongPropertyParser(SMCommon.TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
				[SMCommon.TagGenre] = new PropertyToSongPropertyParser(SMCommon.TagGenre, nameof(Song.Genre), song),
				[SMCommon.TagOrigin] = new PropertyToSourceExtrasParser<string>(SMCommon.TagOrigin, song.SourceExtras),
				[SMCommon.TagCredit] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCredit, song.SourceExtras),
				[SMCommon.TagBanner] = new PropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage), song),
				[SMCommon.TagBackground] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBackground, song.SourceExtras),
				[SMCommon.TagPreviewVid] = new PropertyToSourceExtrasParser<string>(SMCommon.TagPreviewVid, song.SourceExtras),
				[SMCommon.TagJacket] = new PropertyToSourceExtrasParser<string>(SMCommon.TagJacket, song.SourceExtras),
				[SMCommon.TagCDImage] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDImage, song.SourceExtras),
				[SMCommon.TagDiscImage] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDiscImage, song.SourceExtras),
				[SMCommon.TagLyricsPath] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLyricsPath, song.SourceExtras),
				[SMCommon.TagCDTitle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDTitle, song.SourceExtras),
				[SMCommon.TagMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagMusic, song.SourceExtras),
				[SMCommon.TagPreview] = new PropertyToSongPropertyParser(SMCommon.TagPreview, nameof(Song.PreviewMusicFile), song),
				[SMCommon.TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(SMCommon.TagInstrumentTrack, song.SourceExtras),
				[SMCommon.TagMusicLength] = new PropertyToSourceExtrasParser<double>(SMCommon.TagMusicLength, song.SourceExtras),
				[SMCommon.TagLastSecondHint] = new PropertyToSourceExtrasParser<double>(SMCommon.TagLastSecondHint, song.SourceExtras),
				[SMCommon.TagSampleStart] = new PropertyToSongPropertyParser(SMCommon.TagSampleStart, nameof(Song.PreviewSampleStart), song),
				[SMCommon.TagSampleLength] = new PropertyToSongPropertyParser(SMCommon.TagSampleLength, nameof(Song.PreviewSampleLength), song),
				[SMCommon.TagDisplayBPM] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagDisplayBPM, song.SourceExtras),
				[SMCommon.TagSelectable] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSelectable, song.SourceExtras),
				[SMCommon.TagAnimations] = new PropertyToSourceExtrasParser<string>(SMCommon.TagAnimations, song.SourceExtras),
				[SMCommon.TagBGChanges] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges, song.SourceExtras),
				[SMCommon.TagBGChanges1] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges1, song.SourceExtras),
				[SMCommon.TagBGChanges2] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBGChanges2, song.SourceExtras),
				[SMCommon.TagFGChanges] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFGChanges, song.SourceExtras),
				// TODO: Parse Keysounds properly.
				[SMCommon.TagKeySounds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagKeySounds, song.SourceExtras),
				[SMCommon.TagAttacks] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagAttacks, song.SourceExtras),
				[SMCommon.TagOffset] = new PropertyToSourceExtrasParser<double>(SMCommon.TagOffset, song.SourceExtras),

				// These tags are only used if the individual charts do not specify values.
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, songStops, song.SourceExtras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDelays, song.SourceExtras),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, songTempos, song.SourceExtras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagWarps] = new PropertyToSourceExtrasParser<string>(SMCommon.TagWarps, song.SourceExtras),
				[SMCommon.TagLabels] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLabels, song.SourceExtras),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TagTimeSignatures] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTimeSignatures, song.SourceExtras),
				[SMCommon.TagTickCounts] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTickCounts, song.SourceExtras),
				[SMCommon.TagCombos] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCombos, song.SourceExtras),
				[SMCommon.TagSpeeds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSpeeds, song.SourceExtras),
				[SMCommon.TagScrolls] = new PropertyToSourceExtrasParser<string>(SMCommon.TagScrolls, song.SourceExtras),
				[SMCommon.TagFakes] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFakes, song.SourceExtras),
				[SMCommon.TagFirstSecond] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFirstSecond, song.SourceExtras),
				[SMCommon.TagLastSecond] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLastSecond, song.SourceExtras),
				[SMCommon.TagSongFileName] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSongFileName, song.SourceExtras),
				[SMCommon.TagHasMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagHasMusic, song.SourceExtras),
				[SMCommon.TagHasBanner] = new PropertyToSourceExtrasParser<string>(SMCommon.TagHasBanner, song.SourceExtras),
			};
			foreach (var kvp in parsers)
				kvp.Value.SetLogger(Logger);
			return parsers;
		}

		private Dictionary<string, PropertyParser> GetChartPropertyParsers(
			Chart chart,
			Dictionary<double, double> chartTempos,
			Dictionary<double, double> chartStops)
		{
			var parsers = new Dictionary<string, PropertyParser>()
			{
				[SMCommon.TagVersion] = new PropertyToSourceExtrasParser<double>(SMCommon.TagVersion, chart.SourceExtras),
				[SMCommon.TagChartName] = new PropertyToSourceExtrasParser<string>(SMCommon.TagChartName, chart.SourceExtras),
				[SMCommon.TagStepsType] = new ChartTypePropertyParser(chart),
				[SMCommon.TagChartStyle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagChartStyle, chart.SourceExtras),
				[SMCommon.TagDescription] = new PropertyToChartPropertyParser(SMCommon.TagDescription, nameof(Chart.Description), chart),
				[SMCommon.TagDifficulty] = new PropertyToChartPropertyParser(SMCommon.TagDifficulty, nameof(Chart.DifficultyType), chart),
				[SMCommon.TagMeter] = new PropertyToChartPropertyParser(SMCommon.TagMeter, nameof(Chart.DifficultyRating), chart),
				[SMCommon.TagRadarValues] = new PropertyToSourceExtrasParser<string>(SMCommon.TagRadarValues, chart.SourceExtras),
				[SMCommon.TagCredit] = new PropertyToChartPropertyParser(SMCommon.TagCredit, nameof(Chart.Author), chart),
				[SMCommon.TagMusic] = new PropertyToChartPropertyParser(SMCommon.TagMusic, nameof(Chart.MusicFile), chart),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, chartTempos, chart.SourceExtras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, chartStops, chart.SourceExtras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDelays, chart.SourceExtras),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TagTimeSignatures] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTimeSignatures, chart.SourceExtras),
				[SMCommon.TagTickCounts] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTickCounts, chart.SourceExtras),
				[SMCommon.TagCombos] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCombos, chart.SourceExtras),
				[SMCommon.TagWarps] = new PropertyToSourceExtrasParser<string>(SMCommon.TagWarps, chart.SourceExtras),
				[SMCommon.TagSpeeds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSpeeds, chart.SourceExtras),
				[SMCommon.TagScrolls] = new PropertyToSourceExtrasParser<string>(SMCommon.TagScrolls, chart.SourceExtras),
				[SMCommon.TagFakes] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFakes, chart.SourceExtras),
				[SMCommon.TagLabels] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLabels, chart.SourceExtras),
				[SMCommon.TagAttacks] = new ListPropertyToSourceExtrasParser<string>(SMCommon.TagAttacks, chart.SourceExtras),
				[SMCommon.TagOffset] = new PropertyToChartPropertyParser(SMCommon.TagOffset, nameof(Chart.ChartOffsetFromMusic), chart),
				[SMCommon.TagSampleStart] = new PropertyToSourceExtrasParser<double>(SMCommon.TagSampleStart, chart.SourceExtras),
				[SMCommon.TagSampleLength] = new PropertyToSourceExtrasParser<double>(SMCommon.TagSampleLength, chart.SourceExtras),
				[SMCommon.TagSelectable] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSelectable, chart.SourceExtras),
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
