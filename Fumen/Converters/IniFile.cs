using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Fumen.Logger;

namespace Fumen.Converters;

/// <summary>
/// Ini file.
/// There is no official spec for ini files. This class is based off of Stepmania ini parsing logic.
/// </summary>
public class IniFile
{
	/// <summary>
	/// Individual Group within the file.
	/// </summary>
	public class Group
	{
		/// <summary>
		/// Group name.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// All the values for this Group.
		/// </summary>
		public readonly Dictionary<string, string> Values = [];

		public Group(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// List of all Groups defined in the file.
	/// </summary>
	public List<Group> Groups = [];

	/// <summary>
	/// Asynchronously load the ini file at the specified path.
	/// </summary>
	/// <param name="filePath">Path to the ini file.</param>
	/// <param name="token">CancellationToken to cancel task.</param>
	/// <returns>Whether the file was loaded successfully or not.</returns>
	public static async Task<IniFile> LoadAsync(string filePath, CancellationToken token)
	{
		var iniFile = new IniFile();

		// Load the file into an array of lines to parse.
		string[] lines;
		try
		{
			lines = await File.ReadAllLinesAsync(filePath, token);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			Error($"[INI] Failed to read {filePath}.");
			Error($"[INI] {e}");
			return null;
		}

		token.ThrowIfCancellationRequested();

		// Parse the lines.
		await Task.Run(() =>
		{
			void ParseValue(Group currentGroup, string val)
			{
				if (currentGroup == null)
					return;
				var kvp = val.Split('=', 2);
				if (kvp != null && kvp.Length == 2)
				{
					var key = kvp[0].Trim();
					if (!string.IsNullOrEmpty(key))
					{
						if (!currentGroup.Values.TryAdd(key, kvp[1]))
						{
							Warn($"[INI] [{currentGroup.Name}] has multiple entries for \"{key}\". Ignoring \"{kvp[1]}\"");
						}
					}
				}
				else
				{
					Warn($"[INI] Could not split line on '=': \"{val}\"");
				}
			}

			Group currentGroup = null;

			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line))
					continue;
				switch (line[0])
				{
					case ';':
					case '#':
						// Comment
						continue;
					case '/':
					case '-':
						// "//" or "--" comment.
						if (line.Length > 1 && line[0] == line[1])
							continue;
						ParseValue(currentGroup, line);
						break;
					case '[':
						if (line[^1] == ']')
						{
							var groupName = line.Substring(1, line.Length - 2);
							currentGroup = new Group(groupName);
							iniFile.Groups.Add(currentGroup);
							break;
						}

						ParseValue(currentGroup, line);
						break;
					default:
						ParseValue(currentGroup, line);
						break;
				}
			}
		}, token);

		return iniFile;
	}

	/// <summary>
	/// Asynchronously save the IniFile to the given file path.
	/// </summary>
	/// <param name="filePath">Path to save to.</param>
	/// <returns>True if saving was successful and false otherwise.</returns>
	public async Task<bool> SaveAsync(string filePath)
	{
		return await Task.Run(() => Save(filePath));
	}

	/// <summary>
	/// Synchronously save the IniFile to the given file path.
	/// </summary>
	/// <param name="filePath">Path to save to.</param>
	/// <returns>True if saving was successful and false otherwise.</returns>
	public bool Save(string filePath)
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			using var atomicFile = new AtomicPersistenceFile(filePath);
			using var streamWriter = new StreamWriter(atomicFile.GetFilePathToSaveTo());
			foreach (var group in Groups)
			{
				streamWriter.WriteLine($"[{group.Name}]");
				foreach (var kvp in group.Values)
				{
					streamWriter.WriteLine($"{kvp.Key}={kvp.Value}");
				}
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}

		return true;
	}
}
