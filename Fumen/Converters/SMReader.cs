using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;

namespace Fumen.Converters
{
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
			public readonly Dictionary<double, double> Tempos = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Stops = new Dictionary<double, double>();
			public readonly Dictionary<double, double> Delays = new Dictionary<double, double>();
			public readonly Dictionary<double, Fraction> TimeSignatures = new Dictionary<double, Fraction>();
			public readonly Dictionary<double, int> TickCounts = new Dictionary<double, int>();
		}

		/// <summary>
		/// Path to the sm file to load.
		/// </summary>
		private readonly string FilePath;

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
			FilePath = filePath;
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
					[SMCommon.TagTitle] = new PropertyToSongPropertyParser(SMCommon.TagTitle, nameof(Song.Title), song),
					[SMCommon.TagSubtitle] = new PropertyToSongPropertyParser(SMCommon.TagSubtitle, nameof(Song.SubTitle), song),
					[SMCommon.TagArtist] = new PropertyToSongPropertyParser(SMCommon.TagArtist, nameof(Song.Artist), song),
					[SMCommon.TagTitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagTitleTranslit, nameof(Song.TitleTransliteration), song),
					[SMCommon.TagSubtitleTranslit] = new PropertyToSongPropertyParser(SMCommon.TagSubtitleTranslit, nameof(Song.SubTitleTransliteration), song),
					[SMCommon.TagArtistTranslit] = new PropertyToSongPropertyParser(SMCommon.TagArtistTranslit, nameof(Song.ArtistTransliteration), song),
					[SMCommon.TagGenre] = new PropertyToSongPropertyParser(SMCommon.TagGenre, nameof(Song.Genre), song),
					[SMCommon.TagCredit] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCredit, song.Extras),
					[SMCommon.TagBanner] = new PropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage), song),
					[SMCommon.TagBackground] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBackground, song.Extras),
					[SMCommon.TagLyricsPath] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLyricsPath, song.Extras),
					[SMCommon.TagCDTitle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDTitle, song.Extras),
					[SMCommon.TagMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagMusic, song.Extras),
					[SMCommon.TagOffset] = new PropertyToSourceExtrasParser<double>(SMCommon.TagOffset, song.Extras),
					[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, timingProperties.Tempos, song.Extras, SMCommon.TagFumenRawBpmsStr),
					[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, timingProperties.Stops, song.Extras, SMCommon.TagFumenRawStopsStr),
					[SMCommon.TagFreezes] = new CSVListAtTimePropertyParser<double>(SMCommon.TagFreezes, timingProperties.Stops),
					[SMCommon.TagDelays] = new CSVListAtTimePropertyParser<double>(SMCommon.TagDelays, timingProperties.Delays, song.Extras, SMCommon.TagFumenRawDelaysStr),
					// Removed, see https://github.com/stepmania/stepmania/issues/9
					// SM files are forced 4/4 time signatures. Other time signatures can be provided but they are only
					// suggestions to a renderer for how to draw measure markers.
					[SMCommon.TagTimeSignatures] = new ListFractionPropertyParser(SMCommon.TagTimeSignatures, timingProperties.TimeSignatures, song.Extras, SMCommon.TagFumenRawTimeSignaturesStr),
					[SMCommon.TagTickCounts] = new CSVListAtTimePropertyParser<int>(SMCommon.TagTickCounts, timingProperties.TickCounts, song.Extras, SMCommon.TagFumenRawTickCountsStr),
					[SMCommon.TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(SMCommon.TagInstrumentTrack, song.Extras),
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
					[SMCommon.TagNotes] = new SongNotesPropertyParser(SMCommon.TagNotes, song),
					[SMCommon.TagNotes2] = new SongNotesPropertyParser(SMCommon.TagNotes2, song),
					[SMCommon.TagLastBeatHint] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLastBeatHint, song.Extras),
				};
				foreach (var kvp in propertyParsers)
					kvp.Value.SetLogger(Logger);

				// Parse all Values from the MSDFile.
				foreach (var value in msdFile.Values)
				{
					if (propertyParsers.TryGetValue(value.Params[0]?.ToUpper() ?? "", out var propertyParser))
						propertyParser.Parse(value);
				}

				token.ThrowIfCancellationRequested();

				// Insert stop and tempo change events.
				var firstChart = true;
				foreach (var chart in song.Charts)
				{
					SMCommon.AddStops(timingProperties.Stops, chart);
					SMCommon.AddDelays(timingProperties.Delays, chart);
					SMCommon.AddTempos(timingProperties.Tempos, chart);
					SMCommon.AddTimeSignatures(timingProperties.TimeSignatures, chart, Logger, firstChart);
					SMCommon.AddTickCountEvents(timingProperties.TickCounts, chart);
					firstChart = false;
				}

				// Sort events.
				foreach (var chart in song.Charts)
					chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

				song.GenreTransliteration = song.Genre;

				var chartOffset = 0.0;
				if (song.Extras.TryGetSourceExtra(SMCommon.TagOffset, out object offsetObj))
					chartOffset = (double) offsetObj;

				var chartMusicFile = "";
				if (song.Extras.TryGetSourceExtra(SMCommon.TagMusic, out object chartMusicFileObj))
					chartMusicFile = (string) chartMusicFileObj;

				var chartAuthor = "";
				if (song.Extras.TryGetSourceExtra(SMCommon.TagCredit, out object chartAuthorObj))
					chartAuthor = (string) chartAuthorObj;

				var chartDisplayTempo = SMCommon.GetDisplayBPMStringFromSourceExtrasList(song.Extras, timingProperties.Tempos);

				foreach (var chart in song.Charts)
				{
					chart.MusicFile = chartMusicFile;
					chart.ChartOffsetFromMusic = chartOffset;
					chart.Tempo = chartDisplayTempo;
					chart.Artist = song.Artist;
					chart.ArtistTransliteration = song.ArtistTransliteration;
					chart.Genre = song.Genre;
					chart.GenreTransliteration = song.GenreTransliteration;
					chart.Author = chartAuthor;
					SMCommon.SetEventTimeMicrosAndMetricPositionsFromRows(chart);
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
}
