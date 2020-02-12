using System;
using System.Collections.Generic;

namespace Fumen
{
	public class Chart
	{

		public string Artist { get; set; }
		public string ArtistTransliteration { get; set; }
		public string Genre { get; set; }
		public string GenreTransliteration { get; set; }

		public string Author { get; set; }
		public string Description { get; set; }

		public string MusicFile { get; set; }
		public double ChartOffsetFromMusic { get; set; }

		/// <summary>
		/// String representation of tempo for display purposes.
		/// </summary>
		/// <remarks>
		/// May differ from actual tempo for songs with tempo changes or gimmicks.
		/// </remarks>
		public string Tempo { get; set; }

		/// <summary>
		/// Numeric difficulty rating for this Chart.
		/// </summary>
		public double DifficultyRating { get; set; }

		/// <summary>
		/// Game-specific Difficulty type string for this Chart.
		/// </summary>
		/// <remarks>
		/// The game-specific identifier for how easy or difficult this Chart type is.
		/// For example Beatmania IIDX would use Beginner, Normal, Hyper, Another, and Leggendaria.
		/// Some games use multiple identifiers for one tier of difficulty, like how
		/// Sound Voltex uses Gravity, Heavenly, etc.
		/// </remarks>
		public string DifficultyType { get; set; }

		/// <summary>
		/// Game-specific Type string for this Chart.
		/// </summary>
		/// <remarks>
		/// In addition to DifficultyType, NumPlayers, and NumInputs some games separate charts by other types.
		/// For example StepMania can have Couples and Routine which are both 2 player, and 8 input.
		/// </remarks>
		public string Type { get; set; }

		/// <summary>
		/// Number of players this Chart is intended for.
		/// </summary>
		/// <remarks>
		/// While most Charts are for one player some charts (like Routine/Couples) are for more than one.
		/// </remarks>
		public int NumPlayers { get; set; }

		/// <summary>
		/// Number of inputs for this Chart.
		/// </summary>
		/// <remarks>
		/// For example, DDR Singles would be 4, Beatmania IIDX Doubles would be 16, etc.
		/// </remarks>
		public int NumInputs { get; set; }

		/// <summary>
		/// Extra Information from the source file for this Chart.
		/// </summary>
		public Dictionary<string, object> SourceExtras { get; set; } = new Dictionary<string, object>();

		public Dictionary<string, object> DestExtras { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Layers of Events, including the Notes of the Chart.
		/// </summary>
		public List<Layer> Layers { get; set; } = new List<Layer>();
	}
}
