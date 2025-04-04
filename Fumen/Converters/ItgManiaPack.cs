using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fumen.Converters;

/// <summary>
/// ItgMania Pack file.
/// </summary>
public class ItgManiaPack : ICloneable
{
	public enum SyncOffSetType
	{
		// ReSharper disable InconsistentNaming
		NULL,
		ITG,
		// ReSharper restore InconsistentNaming
	}

	private const int LatestVersion = 1;
	public const string FileName = "Pack.ini";

	private const string TagGroup = "Group";
	private const string TagVersion = "Version";
	private const string TagTitle = "DisplayTitle";
	private const string TagTitleTransliteration = "TranslitTitle";
	private const string TagTitleSort = "SortTitle";
	private const string TagSeries = "Series";
	private const string TagYear = "Year";
	private const string TagBanner = "Banner";
	private const string TagSyncOffset = "SyncOffset";

	public string Title { get; set; }
	public string TitleTransliteration { get; set; }
	public string TitleSort { get; set; }
	public string Series { get; set; }
	public int Year { get; set; }
	public string Banner { get; set; }
	public SyncOffSetType SyncOffset { get; set; }

	/// <summary>
	/// Asynchronously load the pack file from disk.
	/// </summary>
	/// <param name="filePath">Path to the file to load.</param>
	/// <param name="token">CancellationToken.</param>
	/// <returns>Loaded ItgManiaPack or null if loading failed.</returns>
	public static async Task<ItgManiaPack> LoadAsync(string filePath, CancellationToken token)
	{
		var logger = new ItgManiaPackLogger(filePath);
		var pack = new ItgManiaPack();
		try
		{
			var iniFile = await IniFile.LoadAsync(filePath, token);
			var foundGroup = false;
			foreach (var group in iniFile.Groups)
			{
				if (group.Name == TagGroup)
				{
					foundGroup = true;

					if (!group.Values.TryGetValue(TagVersion, out var versionString))
					{
						throw new Exception($"No {TagVersion} specified.");
					}

					if (!int.TryParse(versionString, out var version))
					{
						throw new Exception($"Malformed {TagVersion} specified: {versionString}. Expected integer.");
					}

					if (version > LatestVersion)
					{
						logger.Warn(
							$"{TagVersion} is greater than the latest known version {LatestVersion}. Parsing this file as if it is Version {LatestVersion}.");
					}

					if (version < 1)
					{
						logger.Warn($"{TagVersion} is less than 1. Parsing this file as if it is Version {LatestVersion}.");
					}

					if (!group.Values.TryGetValue(TagTitle, out var title))
						logger.Warn($"No {TagTitle} specified.");
					pack.Title = title;
					if (!group.Values.TryGetValue(TagTitleTransliteration, out var titleTransliteration))
						logger.Warn($"No {TagTitleTransliteration} specified.");
					pack.TitleTransliteration = titleTransliteration;
					if (!group.Values.TryGetValue(TagTitleSort, out var titleSort))
						logger.Warn($"No {TagTitleSort} specified.");
					pack.TitleSort = titleSort;
					if (!group.Values.TryGetValue(TagSeries, out var series))
						logger.Warn($"No {TagSeries} specified.");
					pack.Series = series;
					if (!group.Values.TryGetValue(TagYear, out var yearString))
					{
						logger.Warn($"No {TagYear} specified.");
					}
					else
					{
						if (!int.TryParse(yearString, out var year))
						{
							logger.Warn($"Malformed {TagYear} specified: {yearString}. Expected integer.");
						}
						else
						{
							pack.Year = year;
						}
					}

					if (!group.Values.TryGetValue(TagBanner, out var banner))
						logger.Warn($"No {TagBanner} specified.");
					pack.Banner = banner;
					if (!group.Values.TryGetValue(TagSyncOffset, out var syncOffsetString))
						logger.Warn($"No {TagSyncOffset} specified.");
					if (!Enum.TryParse(syncOffsetString, out SyncOffSetType syncOffset))
					{
						logger.Warn($"Unknown {TagSyncOffset} specified: {syncOffsetString}.");
					}
					else
					{
						pack.SyncOffset = syncOffset;
					}

					break;
				}
			}

			if (!foundGroup)
			{
				throw new Exception("No [Group] specified.");
			}
		}
		catch (Exception e)
		{
			logger.Error($"Failed loading file. {e}");
			return null;
		}

		return pack;
	}

	/// <summary>
	/// Asynchronously save the pack file to disk.
	/// </summary>
	/// <param name="filePath">File path to save to.</param>
	/// <returns>True is saving was successful and false otherwise.</returns>
	public async Task<bool> SaveAsync(string filePath)
	{
		return await Task.Run(() => Save(filePath));
	}

	/// <summary>
	/// Synchronously save the pack file to disk.
	/// </summary>
	/// <param name="filePath">File path to save to.</param>
	/// <returns>True is saving was successful and false otherwise.</returns>
	public bool Save(string filePath)
	{
		var logger = new ItgManiaPackLogger(filePath);
		try
		{
			var iniFile = new IniFile();
			var group = new IniFile.Group(TagGroup);
			group.Values.Add(TagVersion, LatestVersion.ToString());
			group.Values.Add(TagTitle, Title);
			group.Values.Add(TagTitleTransliteration, TitleTransliteration);
			group.Values.Add(TagTitleSort, TitleSort);
			group.Values.Add(TagSeries, Series);
			group.Values.Add(TagYear, Year.ToString());
			group.Values.Add(TagBanner, Banner);
			group.Values.Add(TagSyncOffset, SyncOffset.ToString());
			iniFile.Groups.Add(group);
			iniFile.Save(filePath);
		}
		catch (Exception e)
		{
			logger.Error($"Failed saving. {e}");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Returns whether this pack should be considered the same as the given pack.
	/// This is implemented as a custom method rather than use IEquatable because
	/// this object is expected to be mutable.
	/// </summary>
	/// <param name="other">Other ItgManiaPack to compare to.</param>
	/// <returns>
	/// True if this pack should be considered the same as the given pack and false otherwise.
	/// </returns>
	public bool Matches(ItgManiaPack other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;
		return Title == other.Title
		       && TitleTransliteration == other.TitleTransliteration
		       && TitleSort == other.TitleSort
		       && Series == other.Series
		       && Year == other.Year
		       && Banner == other.Banner
		       && SyncOffset == other.SyncOffset;
	}

	#region ICloneable

	/// <summary>
	/// Clones this ItgManiaPack.
	/// </summary>
	/// <returns>Cloned ItgManiaPack.</returns>
	public object Clone()
	{
		return new ItgManiaPack
		{
			Title = Title,
			TitleTransliteration = TitleTransliteration,
			TitleSort = TitleSort,
			Series = Series,
			Year = Year,
			Banner = Banner,
			SyncOffset = SyncOffset,
		};
	}

	#endregion ICloneable
}

/// <summary>
/// Logger to help identify the Pack in the logs.
/// </summary>
public class ItgManiaPackLogger : ILogger
{
	private readonly string FilePath;
	private const string Tag = "[ITGmania Pack]";

	public ItgManiaPackLogger(string filePath)
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
