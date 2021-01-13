using System.IO;
using System.Threading.Tasks;

namespace Fumen.Converters
{
	/// <summary>
	/// Reads a file and creates a new Song based on its contents.
	/// </summary>
	public abstract class Reader
	{
		/// <summary>
		/// Constructor requiring path to file.
		/// </summary>
		/// <param name="filePath">Path to file.</param>
		public Reader(string filePath) { }

		/// <summary>
		/// Load the file and return a Song.
		/// </summary>
		/// <returns>Song populated from file.</returns>
		public abstract Task<Song> Load();

		/// <summary>
		/// Factory method to create the appropriate Reader based on the given file.
		/// </summary>
		/// <param name="fileInfo">FileInfo for the file to read.</param>
		/// <returns>
		/// New Reader for reading the given or null if no appropriate Reader exists.
		/// </returns>
		public static Reader CreateReader(FileInfo fileInfo)
		{
			switch (fileInfo.Extension.ToLower())
			{
				case ".sm":
					return new SMReader(fileInfo.FullName);
				case ".ssc":
					return new SSCReader(fileInfo.FullName);
			}
			return null;
		}
	}
}
