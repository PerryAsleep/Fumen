using System.IO;

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
				if (!TryGetSongExtra(SMCommon.TagVersion, out var version))
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
				if (TryGetSongExtra(SMCommon.TagPreview, out _))
					WriteSongPropertyFromExtras(SMCommon.TagPreview);
				if (TryGetSongExtra(SMCommon.TagInstrumentTrack, out _))
					WriteSongPropertyFromExtras(SMCommon.TagInstrumentTrack);
				WriteSongPropertyOffset();
				WriteSongProperty(SMCommon.TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMCommon.SMDoubleFormat));
				WriteSongProperty(SMCommon.TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMCommon.SMDoubleFormat));
				WriteSongPropertyFromExtras(SMCommon.TagSelectable);
				if (TryGetSongExtra(SMCommon.TagDisplayBPM, out _))
					WriteSongPropertyFromExtras(SMCommon.TagDisplayBPM);

				// Timing data.
				WriteSongPropertyBPMs();
				WriteSongPropertyStops();
				WriteSongPropertyFromExtras(SMCommon.TagDelays);
				WriteSongPropertyFromExtras(SMCommon.TagWarps);
				WriteSongPropertyFromExtras(SMCommon.TagTimeSignatures);
				WriteSongPropertyFromExtras(SMCommon.TagTickCounts);
				WriteSongPropertyFromExtras(SMCommon.TagCombos);
				WriteSongPropertyFromExtras(SMCommon.TagSpeeds);
				WriteSongPropertyFromExtras(SMCommon.TagScrolls);
				WriteSongPropertyFromExtras(SMCommon.TagFakes);
				WriteSongPropertyFromExtras(SMCommon.TagLabels);

				if (TryGetSongExtra(SMCommon.TagLastSecondHint, out _))
					WriteSongPropertyFromExtras(SMCommon.TagLastSecondHint);
				WriteSongPropertyFromExtras(SMCommon.TagAnimations, true);
				if (TryGetSongExtra(SMCommon.TagBGChanges, out _))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges);
				if (TryGetSongExtra(SMCommon.TagBGChanges1, out _))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges1);
				if (TryGetSongExtra(SMCommon.TagBGChanges2, out _))
					WriteSongPropertyFromExtras(SMCommon.TagBGChanges2);
				if (TryGetSongExtra(SMCommon.TagFGChanges, out _))
					WriteSongPropertyFromExtras(SMCommon.TagFGChanges);
				WriteSongPropertyFromExtras(SMCommon.TagKeySounds);		// TODO: Write keysounds properly
				WriteSongPropertyFromExtras(SMCommon.TagAttacks);

				// Cache
				if (TryGetSongExtra(SMCommon.TagFirstSecond, out _)
				    || TryGetSongExtra(SMCommon.TagLastSecond, out _)
				    || TryGetSongExtra(SMCommon.TagSongFileName, out _)
				    || TryGetSongExtra(SMCommon.TagHasMusic, out _)
				    || TryGetSongExtra(SMCommon.TagHasBanner, out _)
				    || TryGetSongExtra(SMCommon.TagMusicLength, out _))
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
			StreamWriter.WriteLine($"//---------------{charTypeStr} - {MSDFile.Escape(chart.Description)}----------------");
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
				if (TryGetChartExtra(chart, SMCommon.TagOffset, out _)
				    || TryGetChartExtra(chart, SMCommon.TagBPMs, out _)
				    || TryGetChartExtra(chart, SMCommon.TagStops, out _)
				    || TryGetChartExtra(chart, SMCommon.TagDelays, out _)
				    || TryGetChartExtra(chart, SMCommon.TagWarps, out _)
				    || TryGetChartExtra(chart, SMCommon.TagTimeSignatures, out _)
				    || TryGetChartExtra(chart, SMCommon.TagTickCounts, out _)
				    || TryGetChartExtra(chart, SMCommon.TagCombos, out _)
				    || TryGetChartExtra(chart, SMCommon.TagSpeeds, out _)
				    || TryGetChartExtra(chart, SMCommon.TagScrolls, out _)
				    || TryGetChartExtra(chart, SMCommon.TagFakes, out _)
				    || TryGetChartExtra(chart, SMCommon.TagLabels, out _))
					writeTimingData = true;
			}
			if (writeTimingData)
			{
				WriteChartProperty(chart, SMCommon.TagOffset, chart.ChartOffsetFromMusic);
				WriteChartPropertyBPMs(chart);
				WriteChartPropertyStops(chart);
				WriteChartPropertyFromExtras(chart, SMCommon.TagDelays);
				WriteChartPropertyFromExtras(chart, SMCommon.TagWarps);
				WriteChartPropertyFromExtras(chart, SMCommon.TagTimeSignatures);
				WriteChartPropertyFromExtras(chart, SMCommon.TagTickCounts);
				WriteChartPropertyFromExtras(chart, SMCommon.TagCombos);
				WriteChartPropertyFromExtras(chart, SMCommon.TagSpeeds);
				WriteChartPropertyFromExtras(chart, SMCommon.TagScrolls);
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
