using System.Collections.Generic;

namespace Fumen
{
	public class Song
	{
		public FileFormatType SourceType { get; set; }

		public string Title { get; set; }
		public string TitleTransliteration{ get; set; }
		public string SubTitle{ get; set; }
		public string SubTitleTransliteration{ get; set; }
		public string Artist{ get; set; }
		public string ArtistTransliteration{ get; set; }
		public string Genre{ get; set; }
		public string GenreTransliteration{ get; set; }

		public string Description{ get; set; }

		public string SongSelectImage { get; set; }

		public string PreviewMusicFile { get; set; }
		public double PreviewSampleStart { get; set; }
		public double PreviewSampleLength { get; set; }

		public Dictionary<string, object> SourceExtras { get; set; } = new Dictionary<string, object>();
		public Dictionary<string, object> DestExtras { get; set; } = new Dictionary<string, object>();

		public List<Chart> Charts { get; set; } = new List<Chart>();
	}
}
