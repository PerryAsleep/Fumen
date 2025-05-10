using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Fumen.ChartDefinition;
using static Fumen.Converters.SMCommon;

namespace Fumen.Converters;

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
	public async Task<bool> SaveAsync()
	{
		return await Task.Run(Save);
	}

	/// <summary>
	/// Save the song using the parameters set in SMWriterConfig.
	/// </summary>
	/// <returns>True if saving was successful and false otherwise.</returns>
	public bool Save()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			using var atomicFile = new AtomicPersistenceFile(Config.FilePath);
			using (StreamWriter = new StreamWriter(atomicFile.GetFilePathToSaveTo()))
			{
				WriteSongProperty(TagTitle, Config.Song.Title);
				WriteSongProperty(TagSubtitle, Config.Song.SubTitle);
				WriteSongProperty(TagArtist, Config.Song.Artist);
				WriteSongProperty(TagTitleTranslit, Config.Song.TitleTransliteration);
				WriteSongProperty(TagSubtitleTranslit, Config.Song.SubTitleTransliteration);
				WriteSongProperty(TagArtistTranslit, Config.Song.ArtistTransliteration);
				WriteSongProperty(TagGenre, Config.Song.Genre);
				WriteSongPropertyFromExtras(TagCredit);
				WriteSongProperty(TagBanner, Config.Song.SongSelectImage);
				WriteSongPropertyFromExtras(TagBackground);
				WriteSongPropertyFromExtras(TagLyricsPath);
				WriteSongPropertyFromExtras(TagCDTitle);
				WriteSongPropertyMusic();
				WriteSongPropertyOffset();
				WriteSongProperty(TagSampleStart, Config.Song.PreviewSampleStart.ToString(SMDoubleFormat));
				WriteSongProperty(TagSampleLength, Config.Song.PreviewSampleLength.ToString(SMDoubleFormat));
				if (Config.Song.Extras.TryGetExtra(TagLastBeatHint, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagLastBeatHint);
				WriteSongPropertyFromExtras(TagSelectable);
				if (Config.Song.Extras.TryGetExtra(TagDisplayBPM, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagDisplayBPM, false, false);

				// Custom properties. Always write these if they are present.
				if (Config.CustomProperties != null)
				{
					// Since sm files do not support MSD keys per chart, we need to include
					// both song and chart properties at the top of the file in the song's section.
					// In order to differentiate song and chart properties, we need to modify the
					// keys to identify what they correspond to.
					// See also SMReader.
					if (Config.CustomProperties.CustomSongProperties != null)
					{
						foreach (var customProperty in Config.CustomProperties.CustomSongProperties)
							WriteProperty($"{customProperty.Key}{SMCustomPropertySongMarker}", customProperty.Value);
					}

					if (Config.CustomProperties.CustomChartProperties != null)
					{
						var chartIndex = 0;
						foreach (var chartPropertySet in Config.CustomProperties.CustomChartProperties)
						{
							foreach (var chartProperty in chartPropertySet)
							{
								WriteProperty(
									$"{chartProperty.Key}{SMCustomPropertyChartMarker}{chartIndex.ToString(SMCustomPropertyChartIndexFormat)}",
									chartProperty.Value);
							}

							chartIndex++;
						}
					}
				}

				// Timing data.
				WriteSongPropertyBPMs();
				WriteSongPropertyStops();
				// Skipping writing of Freezes as they are read as Stops and will be written back out as Stops.
				// WriteSongPropertyFromExtras(TagFreezes, true);
				WriteSongPropertyDelays();
				WriteSongPropertyTimeSignatures();
				WriteSongPropertyTickCounts();

				WriteSongPropertyFromExtras(TagInstrumentTrack, true, false);
				WriteSongPropertyFromExtras(TagAnimations, true, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges, false, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges1, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges1, false, false);
				if (Config.Song.Extras.TryGetExtra(TagBGChanges2, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagBGChanges2, false, false);
				if (Config.Song.Extras.TryGetExtra(TagFGChanges, out object _, MatchesSourceFileFormatType()))
					WriteSongPropertyFromExtras(TagFGChanges, false, false);
				WriteSongPropertyFromExtras(TagKeySounds, false, false); // TODO: Write keysounds properly
				WriteSongPropertyAttacks();

				StreamWriter.WriteLine();

				foreach (var chart in Config.Song.Charts)
				{
					WriteChart(chart);
				}
			}

			StreamWriter = null;
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}

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
		WriteChartNotes(chart, Config.UseStepF2ForPumpMultiplayerCharts);

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
