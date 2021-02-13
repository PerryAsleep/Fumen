
namespace Fumen
{
	public static class Path
	{
		public static string Combine(string path1, string path2)
		{
			if (System.IO.Path.IsPathRooted(path2))
			{
				path2 = path2.TrimStart(System.IO.Path.DirectorySeparatorChar);
				path2 = path2.TrimStart(System.IO.Path.AltDirectorySeparatorChar);
			}
			return System.IO.Path.Combine(path1, path2);
		}

		public static string Combine(string[] paths)
		{
			if (paths == null || paths.Length == 0)
				return null;

			var cleanedPaths = new string[paths.Length];
			for (var i = 0; i < paths.Length; i++)
			{
				cleanedPaths[i] = paths[i];
				if (System.IO.Path.IsPathRooted(cleanedPaths[i]))
				{
					cleanedPaths[i] = cleanedPaths[i].TrimStart(System.IO.Path.DirectorySeparatorChar);
					cleanedPaths[i] = cleanedPaths[i].TrimStart(System.IO.Path.AltDirectorySeparatorChar);
				}
			}
			return System.IO.Path.Combine(cleanedPaths);
		}
	}
}
