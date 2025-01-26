namespace Fumen;

public enum FileFormatType
{
	SM,
	SSC,
}

public class FileFormat
{
	private static readonly FileFormat[] Data;

	public readonly FileFormatType Type;
	public readonly string Extension;
	public readonly string ExtensionWithSeparator;

	private FileFormat(FileFormatType type, string extension)
	{
		Type = type;
		Extension = extension;
		ExtensionWithSeparator = "." + extension;
	}

	static FileFormat()
	{
		Data =
		[
			new FileFormat(FileFormatType.SM, "sm"),
			new FileFormat(FileFormatType.SSC, "ssc"),
		];
	}

	public static FileFormat GetFileFormatByExtension(string extension)
	{
		if (string.IsNullOrEmpty(extension))
			return null;
		extension = extension.ToLower();
		foreach (var data in Data)
		{
			if (data.Extension == extension || data.ExtensionWithSeparator == extension)
				return data;
		}

		return null;
	}
}
