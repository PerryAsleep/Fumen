using System.IO;
using Fumen.ChartDefinition;

namespace Fumen.Converters
{
	/// <summary>
	/// Writer for .ssc files.
	/// </summary>
	public class SSCWriter : SMWriterBase
	{
		private const double Version = 0.83;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="config">SMWriterConfig for configuring how to write the file.</param>
		public SSCWriter(SMWriterBaseConfig config)
			: base(config, new SSCWriterLogger(config.FilePath), FileFormatType.SSC)
		{
		}

		/// <summary>
		/// Save the song using the parameters set in SMWriterConfig.
		/// </summary>
		/// <returns>True if saving was successful and false otherwise.</returns>
		public bool Save()
		{
			using (StreamWriter = new StreamWriter(Config.FilePath))
			{
				// Version
				if (!Config.Song.Extras.TryGetExtra(SMCommon.TagVersion, out object version, MatchesSourceFileFormatType()))
					WriteSongProperty(SMCommon.TagVersion, Version.ToString("N2"));
				else
				{
					if (version is double d)
						WriteSongProperty(SMCommon.TagVersion, d.ToString("N2"));
					else
						WriteSongPropertyFromExtras(SMCommon.TagVersion);
				}

				WriteSongProperty(SMCommon.TagTitle, Config.Song.Title);
				WriteSongProperty(SMCommon.TagSubtitle, Config.Song.SubTitle);
				WriteSongProperty(SMCommon.TagArtist, Config.Song.Artist);
				WriteSongProperty(SMCommon.TagTitleTranslit, Config.Song.TitleTransliteration);
				WriteSongProperty(SMCommon.TagSubtitleTranslit, Config.Song.SubTitleTransliteration);
				WriteSongProperty(SMCommon.TagArtistTranslit, Config.Song.ArtistTransliteration);
				WriteSongProperty(SMCommon.TagGenre, Config.Song.Genre);
				WriteSongPropertyFromExtras(SMCommon.TagOrigin);
				WriteSongPropertyFromExtras(SMCommon.TagCredit);
				WriteSongProperty(SMCommon.TagBanner, Config.Song.SongSelectImage);
				WriteSongPropertyFromExtras(SMCommon.TagBackground);
				WriteSongPropertyFromExtras(SMCommon.TagPreviewVid);
				WriteSongPropertyFromExtras(SMCommon.TagJacket);
				WriteSongPropertyFromExtras(SMCommon.TagCDImage);
				WriteSongPropertyFromExtras(SMCommon.TagDiscImage);
				WriteSongPropertyFromExtras(SMCommon.TagLyricsPath);
				WriteSongPropertyFromExtras(SMCommon.TagCDTitle);
				WriteSongPropertyMusic();
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagPreview, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagPreview);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagInstrumentTrack, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagInstrumentTrack);
				WriteSongPropertyOffset();
				WriteSongProperty(SMCommon.TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMCommon.SMDoubleFormat));
				WriteSongProperty(SMCommon.TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMCommon.SMDoubleFormat));
				WriteSongPropertyFromExtras(SMCommon.TagSelectable);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagDisplayBPM, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagDisplayBPM);

				// Timing data.
				WriteSongPropertyBPMs();
				WriteSongPropertyStops();
				WriteSongPropertyDelays();
				WriteSongPropertyWarps();
				WriteSongPropertyTimeSignatures();
				WriteSongPropertyFromExtras(SMCommon.TagTickCounts);
				WriteSongPropertyFromExtras(SMCommon.TagCombos);
				WriteSongPropertySpeeds();
				WriteSongPropertyScrolls();
				WriteSongPropertyFromExtras(SMCommon.TagFakes);
				WriteSongPropertyFromExtras(SMCommon.TagLabels);

				if (Config.Song.Extras.TryGetExtra(SMCommon.TagLastSecondHint, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagLastSecondHint);
				WriteSongPropertyFromExtras(SMCommon.TagAnimations, true);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagBGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagBGChanges1, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges1);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagBGChanges2, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges2);
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagFGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(SMCommon.TagFGChanges);
				WriteSongPropertyFromExtras(SMCommon.TagKeySounds);		// TODO: Write keysounds properly
				WriteSongPropertyFromExtras(SMCommon.TagAttacks);

				// Cache
				if (Config.Song.Extras.TryGetExtra(SMCommon.TagFirstSecond, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(SMCommon.TagLastSecond, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(SMCommon.TagSongFileName, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(SMCommon.TagHasMusic, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(SMCommon.TagHasBanner, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(SMCommon.TagMusicLength, out object _, MatchesSourceFileFormatType()))
				{
					StreamWriter.WriteLine("// cache tags:");
					WriteSongPropertyFromExtras(SMCommon.TagFirstSecond);
					WriteSongPropertyFromExtras(SMCommon.TagLastSecond);
					WriteSongPropertyFromExtras(SMCommon.TagSongFileName);
					WriteSongPropertyFromExtras(SMCommon.TagHasMusic);
					WriteSongPropertyFromExtras(SMCommon.TagHasBanner);
					WriteSongPropertyFromExtras(SMCommon.TagMusicLength);
					StreamWriter.WriteLine("// end cache tags");
				}

				foreach (var chart in Config.Song.Charts)
				{
					WriteChart(chart);
				}
			}
			StreamWriter = null;

			return true;
		}

		private void WriteChart(Chart chart)
		{
			// We need a valid ChartType. If we don't have one, don't write the Chart.
			// Logging about ChartType errors performed in DetermineChartDifficultyTypes.
			if (!TryGetChartType(chart, out var chartType))
				return;

			var charTypeStr = chartType.ToString().Replace('_', '-');
			var chartDifficultyType = GetChartDifficultyTypeString(chart);

			StreamWriter.WriteLine();
			StreamWriter.WriteLine($"//---------------{charTypeStr} - {MSDFile.Escape(chart.Description ?? "")}----------------");
			StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{SMCommon.TagNoteData}{MSDFile.ParamMarker}{MSDFile.ValueEndMarker}");
			WriteChartPropertyFromExtras(chart, SMCommon.TagChartName);
			WriteChartProperty(chart, SMCommon.TagStepsType, charTypeStr);
			WriteChartProperty(chart, SMCommon.TagDescription, chart.Description);
			WriteChartPropertyFromExtras(chart, SMCommon.TagChartStyle);
			WriteChartProperty(chart, SMCommon.TagDifficulty, chartDifficultyType);
			WriteChartProperty(chart, SMCommon.TagMeter, (int)chart.DifficultyRating);
			if (!string.IsNullOrEmpty(chart.MusicFile))
				WriteChartProperty(chart, SMCommon.TagMusic, chart.MusicFile);
			WriteChartPropertyFromExtras(chart, SMCommon.TagRadarValues);
			WriteChartProperty(chart, SMCommon.TagCredit, chart.Author);

			// Timing
			var writeTimingData = true;
			if (Config.PropertyEmissionBehavior == PropertyEmissionBehavior.MatchSource)
			{
				writeTimingData = false;
				var matchesSource = MatchesSourceFileFormatType();
				if (chart.Extras.TryGetExtra(SMCommon.TagOffset, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagBPMs, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagStops, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagDelays, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagWarps, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagTimeSignatures, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagTickCounts, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagCombos, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagSpeeds, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagScrolls, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagFakes, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(SMCommon.TagLabels, out object _, matchesSource))
					writeTimingData = true;
			}
			if (writeTimingData)
			{
				WriteChartProperty(chart, SMCommon.TagOffset, chart.ChartOffsetFromMusic);
				WriteChartPropertyBPMs(chart);
				WriteChartPropertyStops(chart);
				WriteChartPropertyDelays(chart);
				WriteChartPropertyWarps(chart);
				WriteChartPropertyTimeSignatures(chart);
				WriteChartPropertyFromExtras(chart, SMCommon.TagTickCounts);
				WriteChartPropertyFromExtras(chart, SMCommon.TagCombos);
				WriteChartPropertySpeeds(chart);
				WriteChartPropertyScrolls(chart);
				WriteChartPropertyFromExtras(chart, SMCommon.TagFakes);
				WriteChartPropertyFromExtras(chart, SMCommon.TagLabels);
			}

			WriteChartPropertyFromExtras(chart, SMCommon.TagSampleStart, true);
			WriteChartPropertyFromExtras(chart, SMCommon.TagSampleLength, true);
			WriteChartPropertyFromExtras(chart, SMCommon.TagSelectable, true);
			WriteChartPropertyFromExtras(chart, SMCommon.TagAttacks, true);
			WriteChartProperty(chart, SMCommon.TagDisplayBPM, chart.Tempo);

			// Write all the notes.
			WriteChartNotesValueStart(chart);
			WriteChartNotes(chart);

			// Mark the notes as complete.
			StreamWriter.WriteLine(MSDFile.ValueEndMarker);
			StreamWriter.WriteLine();
		}
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	public class SSCWriterLogger : ILogger
	{
		private readonly string FilePath;
		private const string Tag = "[SSC Writer]";

		public SSCWriterLogger(string filePath)
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
