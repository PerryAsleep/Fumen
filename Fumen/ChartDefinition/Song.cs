using System.Collections.Generic;

namespace Fumen.ChartDefinition
{
	/// <summary>
	/// Song data.
	/// A Song has multiple Charts.
	/// </summary>
	public class Song
	{
		public FileFormatType SourceType { get; set; }

		public string Title { get; set; }
		public string TitleTransliteration { get; set; }
		public string SubTitle { get; set; }
		public string SubTitleTransliteration { get; set; }
		public string Artist { get; set; }
		public string ArtistTransliteration { get; set; }
		public string Genre { get; set; }
		public string GenreTransliteration { get; set; }

		public string Description { get; set; }

		public string SongSelectImage { get; set; }

		public string PreviewMusicFile { get; set; }
		public double PreviewSampleStart { get; set; }
		public double PreviewSampleLength { get; set; }

		/// <summary>
		/// The Charts making up this Song.
		/// </summary>
		public List<Chart> Charts { get; set; } = new List<Chart>();

		/// <summary>
		/// Miscellaneous extra information associated with this Song.
		/// </summary>
		public Extras Extras { get; set; } = new Extras();
	}
}
