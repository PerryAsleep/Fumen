using System;

namespace Fumen
{
	/// <summary>
	/// Path static utility methods.
	/// </summary>
	public static class Path
	{
		private const string Win32DeviceNamespace = @"\\?\";

		/// <summary>
		/// Combines two paths and returns the result.
		/// Unlike System.IO.Path.Combine, this does not ignore the first path component
		/// if the second path component begins with the directory separator character.
		/// If the second path component is rooted with a drive root then the first path
		/// component will be ignored.
		/// </summary>
		/// <param name="path1">First path component.</param>
		/// <param name="path2">Second path component.</param>
		/// <returns>Combined path.</returns>
		public static string Combine(string path1, string path2)
		{
			// System.IO.Path.Combine will throw out the first path if the second path
			// appears rooted. It treats a path which starts with the directory separator as rooted.
			// Clean the second path to work around this behavior, and then combine.
			if (System.IO.Path.IsPathRooted(path2))
			{
				path2 = path2.TrimStart(System.IO.Path.DirectorySeparatorChar);
				path2 = path2.TrimStart(System.IO.Path.AltDirectorySeparatorChar);
			}
			return System.IO.Path.Combine(path1, path2);
		}

		/// <summary>
		/// Combines the given paths and returns the result.
		/// Unlike System.IO.Path.Combine, this does not ignore path components which
		/// precede path components that begin with the directory separator character.
		/// If a path component is rooted with a drive root then preceding path components
		/// will be ignored.
		/// </summary>
		/// <param name="paths">Path components.</param>
		/// <returns>Combined path.</returns>
		public static string Combine(string[] paths)
		{
			if (paths == null || paths.Length == 0)
				return null;

			var cleanedPaths = new string[paths.Length];
			for (var i = 0; i < paths.Length; i++)
			{
				cleanedPaths[i] = paths[i];

				// System.IO.Path.Combine will throw out the paths preceding a path that
				// appears rooted. It treats a path which starts with the directory separator as rooted.
				// Clean the paths to work around this behavior, and then combine.
				if (i > 0 && System.IO.Path.IsPathRooted(cleanedPaths[i]))
				{
					cleanedPaths[i] = cleanedPaths[i].TrimStart(System.IO.Path.DirectorySeparatorChar);
					cleanedPaths[i] = cleanedPaths[i].TrimStart(System.IO.Path.AltDirectorySeparatorChar);
				}
			}
			return System.IO.Path.Combine(cleanedPaths);
		}

		/// <summary>
		/// Gets the full path, prepended with the Win32 device namespace to allows
		/// paths longer than 260 characters in some .NetFramework API calls which are
		/// still restricted by MAX_PATH.
		/// </summary>
		/// <remarks>
		/// See https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#win32-file-namespaces
		/// </remarks>
		/// <param name="path">The path to format.</param>
		/// <returns>Full formatted path.</returns>
		public static string GetWin32FileSystemFullPath(string path)
		{
			return Win32DeviceNamespace + System.IO.Path.GetFullPath(path);
		}

		/// <summary>
		/// Gets the relative path between the two given paths.
		/// Accepts paths beginning with the Win32 device namespace.
		/// </summary>
		/// <param name="fromPath">The path to get the relative path from.</param>
		/// <param name="toPath">The path to get the relative path to.</param>
		/// <returns>The relative path between the two given paths.</returns>
		public static string GetRelativePath(string fromPath, string toPath)
		{
			if (fromPath.StartsWith(Win32DeviceNamespace))
				fromPath = fromPath.Substring(Win32DeviceNamespace.Length);
			fromPath = fromPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
			if (!fromPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
				fromPath = $"{fromPath}{System.IO.Path.DirectorySeparatorChar}";

			if (toPath.StartsWith(Win32DeviceNamespace))
				toPath = toPath.Substring(Win32DeviceNamespace.Length);
			toPath = toPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);
			var relativeUri = fromUri.MakeRelativeUri(toUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
			return relativePath;
		}
	}
}
