using System.IO;

namespace Fumen.Converters
{
	/// <summary>
	/// Writer for .sm files.
	/// </summary>
	public class SMWriter : SMWriterBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="config">SMWriterConfig for configuring how to write the file.</param>
		public SMWriter(SMWriterBaseConfig config)
			: base(config, new SMWriterLogger(config.FilePath), FileFormatType.SM)
		{
		}

		/// <summary>
		/// Save the song using the parameters set in SMWriterConfig.
		/// </summary>
		/// <returns>True if saving was successful and false otherwise.</returns>
		public bool Save()
		{
			// TODO: Async
			// TODO: Handle non 4/4 time signatures correctly.

			// If Song data is missing that is normally defined on the Chart level, use the first
			// Chart for writing the values.
			Chart fallbackChartMissingSongProperties = null;
			if (Config.Song.Charts.Count > 0)
				fallbackChartMissingSongProperties = Config.Song.Charts[0];

			using (StreamWriter = new StreamWriter(Config.FilePath))
			{
				WriteSongProperty(SMCommon.TagTitle, Config.Song.Title);
				WriteSongProperty(SMCommon.TagSubtitle, Config.Song.SubTitle);
				WriteSongProperty(SMCommon.TagArtist, Config.Song.Artist);
				WriteSongProperty(SMCommon.TagTitleTranslit, Config.Song.TitleTransliteration);
				WriteSongProperty(SMCommon.TagSubtitleTranslit, Config.Song.SubTitleTransliteration);
				WriteSongProperty(SMCommon.TagArtistTranslit, Config.Song.ArtistTransliteration);
				WriteSongProperty(SMCommon.TagGenre, Config.Song.Genre);
				WriteSongPropertyFromExtras(SMCommon.TagCredit);
				WriteSongProperty(SMCommon.TagBanner, Config.Song.SongSelectImage);
				WriteSongPropertyFromExtras(SMCommon.TagBackground);
				WriteSongPropertyFromExtras(SMCommon.TagLyricsPath);
				WriteSongPropertyFromExtras(SMCommon.TagCDTitle);
				WriteSongPropertyMusic(fallbackChartMissingSongProperties);
				WriteSongPropertyOffset(fallbackChartMissingSongProperties);
				WriteSongProperty(SMCommon.TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMCommon.SMDoubleFormat));
				WriteSongProperty(SMCommon.TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMCommon.SMDoubleFormat));
				if (TryGetSongExtra(SMCommon.TagLastBeatHint, out _))
					WriteSongPropertyFromExtras(SMCommon.TagLastBeatHint);
				WriteSongPropertyFromExtras(SMCommon.TagSelectable);
				if (TryGetSongExtra(SMCommon.TagDisplayBPM, out _))
					WriteSongPropertyFromExtras(SMCommon.TagDisplayBPM);
				WriteSongPropertyBPMs(fallbackChartMissingSongProperties);
				WriteSongPropertyStops(fallbackChartMissingSongProperties);
				WriteSongPropertyFromExtras(SMCommon.TagFreezes, true);
				WriteSongPropertyFromExtras(SMCommon.TagDelays, true);
				WriteSongPropertyFromExtras(SMCommon.TagTimeSignatures, true);
				WriteSongPropertyFromExtras(SMCommon.TagTickCounts, true);
				WriteSongPropertyFromExtras(SMCommon.TagInstrumentTrack, true);
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

				StreamWriter.WriteLine();

				foreach (var chart in Config.Song.Charts)
				{
					WriteChart(chart);
				}
			}
			StreamWriter = null;

			return true;
		}

		/// <summary>
		/// Write the given Chart.
		/// </summary>
		/// <param name="chart">Chart to write.</param>
		private void WriteChart(Chart chart)
		{
			// We need a valid ChartType. If we don't have one, don't write the Chart.
			// Logging about ChartType errors performed in DetermineChartDifficultyTypes.
			if (!TryGetChartType(chart, out var chartType))
				return;

			var charTypeStr = chartType.ToString().Replace('_', '-');
			var chartDifficultyType = GetChartDifficultyTypeString(chart);
			var radarValues = GetRadarValues(chart);

			// Write chart header.
			StreamWriter.WriteLine();
			StreamWriter.WriteLine($"//---------------{charTypeStr} - {MSDFile.Escape(chart.Description ?? "")}----------------");
			WriteChartNotesValueStart(chart);
			StreamWriter.WriteLine($"     {charTypeStr}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {MSDFile.Escape(chart.Description)}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {chartDifficultyType}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {(int)chart.DifficultyRating}{MSDFile.ParamMarker}");
			StreamWriter.WriteLine($"     {radarValues}{MSDFile.ParamMarker}");

			// Write all the notes.
			WriteChartNotes(chart);

			// Mark the chart as complete.
			StreamWriter.WriteLine(MSDFile.ValueEndMarker);
		}
	}

	/// <summary>
	/// Logger to help identify the Song in the logs.
	/// </summary>
	public class SMWriterLogger : ILogger
	{
		private readonly string FilePath;
		private const string Tag = "[SM Writer]";

		public SMWriterLogger(string filePath)
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
