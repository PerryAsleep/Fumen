using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;

namespace Fumen.Converters;

/// <summary>
/// Reads a file and creates a new Song based on its contents.
/// </summary>
public abstract class Reader
{
	/// <summary>
	/// Path to the ssc file to load.
	/// </summary>
	protected readonly string FilePath;

	/// <summary>
	/// Constructor requiring path to file.
	/// </summary>
	/// <param name="filePath">Path to file.</param>
	protected Reader(string filePath)
	{
		FilePath = filePath;
	}

	/// <summary>
	/// Load the file and return a Song containing the full set of Charts.
	/// </summary>
	/// <param name="token">CancellationToken to cancel task.</param>
	/// <returns>Song with full Charts populated from file.</returns>
	public abstract /*async*/ Task<Song> LoadAsync(CancellationToken token);

	/// <summary>
	/// Load the file and return a Song containing only the Song and Chart metadata with no step data.
	/// </summary>
	/// <param name="token">CancellationToken to cancel task.</param>
	/// <returns>Song with metadata populated from file.</returns>
	public abstract /*async*/ Task<Song> LoadMetaDataAsync(CancellationToken token);

	/// <summary>
	/// Factory method to create the appropriate Reader based on the given file.
	/// </summary>
	/// <param name="fileInfo">FileInfo for the file to read.</param>
	/// <returns>
	/// New Reader for reading the given or null if no appropriate Reader exists.
	/// </returns>
	public static Reader CreateReader(FileInfo fileInfo)
	{
		var fileFormat = FileFormat.GetFileFormatByExtension(fileInfo.Extension.ToLower());
		if (fileFormat == null)
			return null;
		var fullName = Path.GetWin32FileSystemFullPath(fileInfo.FullName);
		switch (fileFormat.Type)
		{
			case FileFormatType.SM:
				return new SMReader(fullName);
			case FileFormatType.SSC:
				return new SSCReader(fullName);
		}

		return null;
	}

	/// <summary>
	/// Factory method to create the appropriate Reader based on the given file name.
	/// </summary>
	/// <param name="fileName">File name for the file to read.</param>
	/// <returns>
	/// New Reader for reading the given or null if no appropriate Reader exists.
	/// </returns>
	public static Reader CreateReader(string fileName)
	{
		var dotIndex = fileName.LastIndexOf('.');
		if (dotIndex < 0 || dotIndex == fileName.Length - 1)
			return null;
		var extension = fileName.Substring(dotIndex + 1);
		var fileFormat = FileFormat.GetFileFormatByExtension(extension.ToLower());
		if (fileFormat == null)
			return null;
		var fullName = Path.GetWin32FileSystemFullPath(fileName);
		switch (fileFormat.Type)
		{
			case FileFormatType.SM:
				return new SMReader(fullName);
			case FileFormatType.SSC:
				return new SSCReader(fullName);
		}

		return null;
	}
}
