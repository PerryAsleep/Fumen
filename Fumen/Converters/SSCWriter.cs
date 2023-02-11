using System.Collections.Generic;
using System.IO;
using Fumen.ChartDefinition;
using static Fumen.Converters.SMCommon;

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
				if (!Config.Song.Extras.TryGetExtra(TagVersion, out object version, MatchesSourceFileFormatType()))
					WriteSongProperty(TagVersion, Version.ToString("N2"));
				else
				{
					if (version is double d)
						WriteSongProperty(TagVersion, d.ToString("N2"));
					else
						WriteSongPropertyFromExtras(TagVersion);
				}

				WriteSongProperty(TagTitle, Config.Song.Title);
				WriteSongProperty(TagSubtitle, Config.Song.SubTitle);
				WriteSongProperty(TagArtist, Config.Song.Artist);
				WriteSongProperty(TagTitleTranslit, Config.Song.TitleTransliteration);
				WriteSongProperty(TagSubtitleTranslit, Config.Song.SubTitleTransliteration);
				WriteSongProperty(TagArtistTranslit, Config.Song.ArtistTransliteration);
				WriteSongProperty(TagGenre, Config.Song.Genre);
				WriteSongPropertyFromExtras(TagOrigin);
				WriteSongPropertyFromExtras(TagCredit);
				WriteSongProperty(TagBanner, Config.Song.SongSelectImage);
				WriteSongPropertyFromExtras(TagBackground);
				WriteSongPropertyFromExtras(TagPreviewVid);
				WriteSongPropertyFromExtras(TagJacket);
				WriteSongPropertyFromExtras(TagCDImage);
				WriteSongPropertyFromExtras(TagDiscImage);
				WriteSongPropertyFromExtras(TagLyricsPath);
				WriteSongPropertyFromExtras(TagCDTitle);
				WriteSongPropertyMusic();
				if (Config.Song.Extras.TryGetExtra(TagPreview, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagPreview);
				if (Config.Song.Extras.TryGetExtra(TagInstrumentTrack, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagInstrumentTrack);
				WriteSongPropertyOffset();
				WriteSongProperty(TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMDoubleFormat));
				WriteSongProperty(TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMDoubleFormat));
				WriteSongPropertyFromExtras(TagSelectable);
				if (Config.Song.Extras.TryGetExtra(TagDisplayBPM, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagDisplayBPM, false, false);

				// Custom properties. Always write these if they are present.
				if (Config.CustomProperties?.CustomSongProperties != null)
				{
					foreach (var customProperty in Config.CustomProperties.CustomSongProperties)
						WriteProperty(customProperty.Key, customProperty.Value, true);
				}

				// Timing data.
				WriteSongPropertyBPMs();
				WriteSongPropertyStops();
				WriteSongPropertyDelays();
				WriteSongPropertyWarps();
				WriteSongPropertyTimeSignatures();
				WriteSongPropertyTickCounts();
				WriteSongPropertyCombos();
				WriteSongPropertySpeeds();
				WriteSongPropertyScrolls();
				WriteSongPropertyFakes();
				WriteSongPropertyLabels();

				if (Config.Song.Extras.TryGetExtra(TagLastSecondHint, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagLastSecondHint, false, false);
				WriteSongPropertyFromExtras(TagAnimations, true, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges, false, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges1, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges1, false, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges2, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges2, false, false);
				if (Config.Song.Extras.TryGetExtra(TagFGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagFGChanges, false, false);
				WriteSongPropertyFromExtras(TagKeySounds, false, false);		// TODO: Write keysounds properly
				WriteSongPropertyFromExtras(TagAttacks, false, false);

				// Cache
				if (Config.Song.Extras.TryGetExtra(TagFirstSecond, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(TagLastSecond, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(TagSongFileName, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(TagHasMusic, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(TagHasBanner, out object _, MatchesSourceFileFormatType())
				    || Config.Song.Extras.TryGetExtra(TagMusicLength, out object _, MatchesSourceFileFormatType()))
				{
					StreamWriter.WriteLine("// cache tags:");
					WriteSongPropertyFromExtras(TagFirstSecond);
					WriteSongPropertyFromExtras(TagLastSecond);
					WriteSongPropertyFromExtras(TagSongFileName);
					WriteSongPropertyFromExtras(TagHasMusic);
					WriteSongPropertyFromExtras(TagHasBanner);
					WriteSongPropertyFromExtras(TagMusicLength);
					StreamWriter.WriteLine("// end cache tags");
				}

				var chartIndex = 0;
				foreach (var chart in Config.Song.Charts)
				{
					Dictionary<string, string> customChartProperties = null;
					if (Config.CustomProperties?.CustomChartProperties?.Count > chartIndex)
						customChartProperties = Config.CustomProperties.CustomChartProperties[chartIndex];
					WriteChart(chart, customChartProperties);
				}
			}
			StreamWriter = null;

			return true;
		}

		private void WriteChart(Chart chart, Dictionary<string, string> customProperties)
		{
			// We need a valid ChartType. If we don't have one, don't write the Chart.
			// Logging about ChartType errors performed in DetermineChartDifficultyTypes.
			if (!TryGetChartType(chart, out var chartType))
				return;

			var charTypeStr = chartType.ToString().Replace('_', '-');
			var chartDifficultyType = GetChartDifficultyTypeString(chart);

			StreamWriter.WriteLine();
			StreamWriter.WriteLine($"//---------------{charTypeStr} - {MSDFile.Escape(chart.Description ?? "")}----------------");
			StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{TagNoteData}{MSDFile.ParamMarker}{MSDFile.ValueEndMarker}");
			WriteChartPropertyFromExtras(chart, TagChartName);
			WriteChartProperty(chart, TagStepsType, charTypeStr);
			WriteChartProperty(chart, TagDescription, chart.Description);
			WriteChartPropertyFromExtras(chart, TagChartStyle);
			WriteChartProperty(chart, TagDifficulty, chartDifficultyType);
			WriteChartProperty(chart, TagMeter, (int)chart.DifficultyRating);
			if (!string.IsNullOrEmpty(chart.MusicFile))
				WriteChartProperty(chart, TagMusic, chart.MusicFile);
			WriteChartPropertyFromExtras(chart, TagRadarValues);
			WriteChartProperty(chart, TagCredit, chart.Author);

			// Custom properties. Always write these if they are present.
			if (customProperties != null)
			{
				foreach (var customProperty in customProperties)
					WriteProperty(customProperty.Key, customProperty.Value, true);
			}

			// Timing
			var writeTimingData = true;
			if (Config.PropertyEmissionBehavior == PropertyEmissionBehavior.MatchSource)
			{
				writeTimingData = false;
				var matchesSource = MatchesSourceFileFormatType();
				if (chart.Extras.TryGetExtra(TagOffset, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagBPMs, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagStops, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagDelays, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagWarps, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagTimeSignatures, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagTickCounts, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagCombos, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagSpeeds, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagScrolls, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagFakes, out object _, matchesSource)
				    || chart.Extras.TryGetExtra(TagLabels, out object _, matchesSource))
					writeTimingData = true;
			}
			if (writeTimingData)
			{
				WriteChartProperty(chart, TagOffset, chart.ChartOffsetFromMusic);
				WriteChartPropertyBPMs(chart);
				WriteChartPropertyStops(chart);
				WriteChartPropertyDelays(chart);
				WriteChartPropertyWarps(chart);
				WriteChartPropertyTimeSignatures(chart);
				WriteChartPropertyTickCounts(chart);
				WriteChartPropertyCombos(chart);
				WriteChartPropertySpeeds(chart);
				WriteChartPropertyScrolls(chart);
				WriteChartPropertyFakes(chart);
				WriteChartPropertyLabels(chart);
			}

			WriteChartPropertyFromExtras(chart, TagSampleStart, true, false);
			WriteChartPropertyFromExtras(chart, TagSampleLength, true, false);
			WriteChartPropertyFromExtras(chart, TagSelectable, true, false);
			WriteChartPropertyFromExtras(chart, TagAttacks, true, false);
			WriteChartProperty(chart, TagDisplayBPM, chart.Tempo, false, false);

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
