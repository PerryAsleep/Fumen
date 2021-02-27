using System.Collections.Generic;
using System.Threading.Tasks;
using Fumen.ChartDefinition;

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
					activeChart.Layers[0].Events.Add(new TimeSignature(new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure))
					{
						Position = new MetricPosition()
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
			if (chart.Extras.TryGetSourceExtra(SMCommon.TagFumenChartUsesOwnTimingData, out object useTimingDataObject))
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
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, songStops, song.Extras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDelays, song.Extras),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, songTempos, song.Extras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagWarps] = new PropertyToSourceExtrasParser<string>(SMCommon.TagWarps, song.Extras),
				[SMCommon.TagLabels] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLabels, song.Extras),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TagTimeSignatures] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTimeSignatures, song.Extras),
				[SMCommon.TagTickCounts] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTickCounts, song.Extras),
				[SMCommon.TagCombos] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCombos, song.Extras),
				[SMCommon.TagSpeeds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSpeeds, song.Extras),
				[SMCommon.TagScrolls] = new PropertyToSourceExtrasParser<string>(SMCommon.TagScrolls, song.Extras),
				[SMCommon.TagFakes] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFakes, song.Extras),
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
			Dictionary<double, double> chartTempos,
			Dictionary<double, double> chartStops)
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
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, chartTempos, chart.Extras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, chartStops, chart.Extras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagDelays] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDelays, chart.Extras),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TagTimeSignatures] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTimeSignatures, chart.Extras),
				[SMCommon.TagTickCounts] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTickCounts, chart.Extras),
				[SMCommon.TagCombos] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCombos, chart.Extras),
				[SMCommon.TagWarps] = new PropertyToSourceExtrasParser<string>(SMCommon.TagWarps, chart.Extras),
				[SMCommon.TagSpeeds] = new PropertyToSourceExtrasParser<string>(SMCommon.TagSpeeds, chart.Extras),
				[SMCommon.TagScrolls] = new PropertyToSourceExtrasParser<string>(SMCommon.TagScrolls, chart.Extras),
				[SMCommon.TagFakes] = new PropertyToSourceExtrasParser<string>(SMCommon.TagFakes, chart.Extras),
				[SMCommon.TagLabels] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLabels, chart.Extras),
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
