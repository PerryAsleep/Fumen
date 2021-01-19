using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fumen.Converters
{
	public class SMReader : Reader
	{
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

			var tempos = new Dictionary<double, double>();
			var stops = new Dictionary<double, double>();

			var song = new Song();
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
				[SMCommon.TagCredit] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCredit, song.SourceExtras),
				[SMCommon.TagBanner] = new PropertyToSongPropertyParser(SMCommon.TagBanner, nameof(Song.SongSelectImage), song),
				[SMCommon.TagBackground] = new PropertyToSourceExtrasParser<string>(SMCommon.TagBackground, song.SourceExtras),
				[SMCommon.TagLyricsPath] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLyricsPath, song.SourceExtras),
				[SMCommon.TagCDTitle] = new PropertyToSourceExtrasParser<string>(SMCommon.TagCDTitle, song.SourceExtras),
				[SMCommon.TagMusic] = new PropertyToSourceExtrasParser<string>(SMCommon.TagMusic, song.SourceExtras),
				[SMCommon.TagOffset] = new PropertyToSourceExtrasParser<double>(SMCommon.TagOffset, song.SourceExtras),
				[SMCommon.TagBPMs] = new CSVListAtTimePropertyParser<double>(SMCommon.TagBPMs, tempos, song.SourceExtras, SMCommon.TagFumenRawBpmsStr),
				[SMCommon.TagStops] = new CSVListAtTimePropertyParser<double>(SMCommon.TagStops, stops, song.SourceExtras, SMCommon.TagFumenRawStopsStr),
				[SMCommon.TagFreezes] = new CSVListAtTimePropertyParser<double>(SMCommon.TagFreezes, stops),
				[SMCommon.TagDelays] = new PropertyToSourceExtrasParser<string>(SMCommon.TagDelays, song.SourceExtras),
				// Removed, see https://github.com/stepmania/stepmania/issues/9
				[SMCommon.TagTimeSignatures] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTimeSignatures, song.SourceExtras),
				[SMCommon.TagTickCounts] = new PropertyToSourceExtrasParser<string>(SMCommon.TagTickCounts, song.SourceExtras),
				[SMCommon.TagInstrumentTrack] = new PropertyToSourceExtrasParser<string>(SMCommon.TagInstrumentTrack, song.SourceExtras),
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
				[SMCommon.TagNotes] = new SongNotesPropertyParser(SMCommon.TagNotes, song),
				[SMCommon.TagNotes2] = new SongNotesPropertyParser(SMCommon.TagNotes2, song),
				[SMCommon.TagLastBeatHint] = new PropertyToSourceExtrasParser<string>(SMCommon.TagLastBeatHint, song.SourceExtras),
			};
			foreach (var kvp in propertyParsers)
				kvp.Value.SetLogger(Logger);

			// Parse all Values from the MSDFile.
			foreach (var value in msdFile.Values)
			{
				if (propertyParsers.TryGetValue(value.Params[0]?.ToUpper() ?? "", out var propertyParser))
					propertyParser.Parse(value);
			}

			// Insert stop and tempo change events.
			foreach (var chart in song.Charts)
			{
				SMCommon.AddStops(stops, chart);
				SMCommon.AddTempos(tempos, chart);
			}

			// Sort events.
			foreach (var chart in song.Charts)
				chart.Layers[0].Events.Sort(new SMCommon.SMEventComparer());

			song.GenreTransliteration = song.Genre;

			var chartOffset = 0.0;
			if (song.SourceExtras.TryGetValue(SMCommon.TagOffset, out var offsetObj))
				chartOffset = (double)offsetObj;
			
			var chartMusicFile = "";
			if (song.SourceExtras.TryGetValue(SMCommon.TagMusic, out var chartMusicFileObj))
				chartMusicFile = (string) chartMusicFileObj;

			var chartAuthor = "";
			if(song.SourceExtras.TryGetValue(SMCommon.TagCredit, out var chartAuthorObj))
				chartAuthor = (string)chartAuthorObj;

			var chartDisplayTempo = SMCommon.GetDisplayBPMStringFromSourceExtrasList(song.SourceExtras, tempos);

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
			}

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
