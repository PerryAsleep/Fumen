using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fumen.Converters
{
	/// <summary>
	/// Abstract base class for writing stepmania-based SM and SSC files.
	/// These file types are both "MSD" files and are extremely similar.
	/// In stepmania the SSC classes are subclasses of SM classes.
	/// This class exists to capture the common functionality when writing charts in these formats.
	/// </summary>
	public abstract class SMWriterBase
	{
		/// <summary>
		/// Enumeration of methods to space out notes in a measure in an sm or ssc file.
		/// </summary>
		public enum MeasureSpacingBehavior
		{
			/// <summary>
			/// Use the greatest unmodified sub-division from any note in the measure.
			/// This option should be used when loading an sm file and writing it back out
			/// as it will preserve the spacing.
			/// </summary>
			UseUnmodifiedChartSubDivisions,
			/// <summary>
			/// Use the least common multiple of the sub-divisions. This will write the
			/// least number of lines for the notes in the measure. This may write sub-divisions
			/// that the stepmania editor does not support (like 24).
			/// </summary>
			UseLeastCommonMultiple,
			/// <summary>
			/// Use the least common multiple of the sub-divisions but do not write anything
			/// that the stepmania editor does not support. This will increase the sub-division
			/// if it is not supported the next supported sub-division. For example this will
			/// bump 24 to 48.
			/// </summary>
			UseLeastCommonMultipleFromStepmaniaEditor,
		}

		/// <summary>
		/// Enumeration of methods to emit properties in an sm or ssc file.
		/// </summary>
		public enum PropertyEmissionBehavior
		{
			/// <summary>
			/// When writing properties, follow stepmania's rules to generate a chart that roughly
			/// matches what stepmania would export. This means even if the source chart has a property
			/// that stepmania understands but stepmania does not save (like the deprecated ANIMATIONS
			/// property) that hose properties will be ignored when exporting. This option should be
			/// used when creating a Song manually or from a non-stepmania source.
			/// </summary>
			Stepmania,

			/// <summary>
			/// When writing properties, write them only if they are present in the DestExtras or
			/// SourceExtras. This option should be used when loading an sm or ssc chart, and then saving
			/// it back out. This will minimize diffs to the file by skipping properties that were not
			/// present in the original file, and writing deprecated properties that were present.
			/// </summary>
			MatchSource,
		}

		/// <summary>
		/// Configuration for SMWriterBase when writing a file.
		/// </summary>
		public class SMWriterBaseConfig
		{
			/// <summary>
			/// How to space the notes in each measure.
			/// </summary>
			public MeasureSpacingBehavior MeasureSpacingBehavior = MeasureSpacingBehavior.UseLeastCommonMultiple;
			/// <summary>
			/// How to emit properties.
			/// </summary>
			public PropertyEmissionBehavior PropertyEmissionBehavior = PropertyEmissionBehavior.Stepmania;
			/// <summary>
			/// The song to write.
			/// </summary>
			public Song Song;
			/// <summary>
			/// The path of the file to write to.
			/// </summary>
			public string FilePath;
		}

		/// <summary>
		/// Comparer to sort Charts by their note counts.
		/// </summary>
		private class EventCountComparer : IComparer<Chart>
		{
			int IComparer<Chart>.Compare(Chart c1, Chart c2)
			{
				if ((null == c1 || c1.Layers.Count == 0) && (null == c2 || c2.Layers.Count == 0))
					return 0;
				if (null == c1 || c1.Layers.Count == 0)
					return -1;
				if (null == c2 || c2.Layers.Count == 0)
					return 1;

				return c1.Layers[0].Events.Count.CompareTo(c2.Layers[0].Events.Count);
			}
		}

		/// <summary>
		/// Helper class to accumulate measure data when writing measures.
		/// </summary>
		private class MeasureData
		{
			/// <summary>
			/// Least common multiple of note beat sub-divisions for all notes in this measure.
			/// Used for writing the correct number of blank lines in the file.
			/// </summary>
			public int LCM = 1;
			/// <summary>
			/// Greatest unmodified denominator of note beat sub-divisions for all notes in this
			/// measure. Used for writing the correct number of blank lines in the file.
			/// </summary>
			public int GreatestUnmodifiedDenominator = 1;
			/// <summary>
			/// Whether or not this is the first measure for the chart.
			/// </summary>
			public bool FirstMeasure;
			/// <summary>
			/// All LaneNotes in this measure.
			/// </summary>
			public List<LaneNote> Notes = new List<LaneNote>();
		}

		/// <summary>
		/// StreamWriter for writing the Song.
		/// </summary>
		protected StreamWriter StreamWriter;
		/// <summary>
		/// Logger to help identify the Song in the logs.
		/// </summary>
		protected ILogger Logger;
		/// <summary>
		/// Config for how to write the Song.
		/// </summary>
		protected SMWriterBaseConfig Config;
		/// <summary>
		/// The FileFormatType being written. Used to check SourceExtras.
		/// </summary>
		protected FileFormatType FileFormatType;
		/// <summary>
		/// The ChartDifficultyType to use for each Chart when writing.
		/// </summary>
		private Dictionary<Chart, SMCommon.ChartDifficultyType> ChartDifficultyTypes;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="config">SMWriterBaseConfig to configure how to write the Chart.</param>
		/// <param name="logger">ILogger for logging.</param>
		/// <param name="fileFormatType">FileFormatType of file being written.</param>
		protected SMWriterBase(SMWriterBaseConfig config, ILogger logger, FileFormatType fileFormatType)
		{
			Config = config;
			Logger = logger;
			FileFormatType = fileFormatType;

			PerformStartupChecks();
			DetermineChartDifficultyTypes();
		}

		/// <summary>
		/// Perform any startup checks and log as appropriate.
		/// </summary>
		private void PerformStartupChecks()
		{
			var chartIndex = 0;
			foreach (var chart in Config.Song.Charts)
			{
				if (chart.Layers.Count > 1)
					Logger.Warn($"Chart [{chartIndex}]. Chart has {chart.Layers.Count} Layers. Only the first Layer will be used.");
				chartIndex++;
			}
		}

		/// <summary>
		/// Determine what ChartDifficultyTypes should be used for the Charts.
		/// Stores results in ChartDifficultyTypes.
		/// Will use the Chart's DifficultyType if it matches a ChartDifficultyType.
		/// Will sort charts per ChartType by note count for determining ChartDifficultyType
		/// when the Charts' DifficultyTypes do not match ChartDifficultyTypes.
		/// </summary>
		private void DetermineChartDifficultyTypes()
		{
			ChartDifficultyTypes = new Dictionary<Chart, SMCommon.ChartDifficultyType>();

			// Group charts by their types (e.g. singles and doubles).
			var chartIndex = 0;
			var chartsByType = new Dictionary<SMCommon.ChartType, List<Chart>>();
			foreach (var chart in Config.Song.Charts)
			{
				if (!TryGetChartType(chart, out var smChartType))
				{
					Logger.Error($"Could not parse type Chart at index {chartIndex}."
					             + " Type should match a value from SMCommon.ChartType, or the Chart should have 1"
					             + " Player and 3, 4, 6, or 8 Inputs, or the Chart should have 2 Players and 8"
					             + $" Inputs. This chart has a Type of '{chart.Type}', {chart.NumPlayers} Players"
					             + $" and {chart.NumInputs} Inputs. This Chart will be skipped.");
				}
				else
				{
					if (!chartsByType.ContainsKey(smChartType))
						chartsByType[smChartType] = new List<Chart>();
					chartsByType[smChartType].Add(chart);
				}
				chartIndex++;
			}

			// Loop over each type, and determine each Chart's fallback difficulty.
			foreach (var entry in chartsByType)
			{
				// Sort the charts for this type by their note counts.
				var sortedChartsOfType = entry.Value;
				sortedChartsOfType.Sort(new EventCountComparer());

				// If there are few enough charts then rank them by Challenge down to Beginner
				// starting at Challenge for the Chart with the most notes.
				if (sortedChartsOfType.Count <= (int)SMCommon.ChartDifficultyType.Challenge)
				{
					var currentDifficulty = (int)SMCommon.ChartDifficultyType.Challenge;
					for (var i = sortedChartsOfType.Count - 1; i >= 0; i--)
					{
						ChartDifficultyTypes[sortedChartsOfType[i]] = (SMCommon.ChartDifficultyType)currentDifficulty;
						currentDifficulty--;
					}
				}

				// Otherwise start at Beginner and work up and treat anything beyond the bounds
				// of the known ChartDifficultyTypes as Edit.
				else
				{
					for (var i = sortedChartsOfType.Count - 1; i >= 0; i--)
					{
						ChartDifficultyTypes[sortedChartsOfType[i]] =
							(SMCommon.ChartDifficultyType)Math.Min(i, (int)SMCommon.ChartDifficultyType.Edit);
					}
				}
			}

			// If any chart actually has a stepmania ChartDifficultyType, use that.
			foreach (var chart in Config.Song.Charts)
			{
				if (Enum.TryParse(chart.DifficultyType, out SMCommon.ChartDifficultyType explicitType))
					ChartDifficultyTypes[chart] = explicitType;
			}
		}

		protected string GetChartDifficultyTypeString(Chart chart)
		{
			if (ChartDifficultyTypes.TryGetValue(chart, out var difficultyType))
				return difficultyType.ToString();
			return "";
		}

		private void WritePropertyInternal(string smPropertyName, string value)
		{
			if (string.IsNullOrEmpty(value))
				StreamWriter.WriteLine(
					$"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}{MSDFile.ValueEndMarker}");
			else
				StreamWriter.WriteLine(
					$"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}{MSDFile.Escape(value)}{MSDFile.ValueEndMarker}");
		}

		private void WritePropertyInternal(string smPropertyName, object value)
		{
			if (value == null)
			{
				WritePropertyInternal(smPropertyName, "");
			}
			else if (value is List<string> valAsList)
			{
				StreamWriter.Write($"{MSDFile.ValueStartMarker}{smPropertyName}{MSDFile.ParamMarker}");
				var first = true;
				foreach (var entry in valAsList)
				{
					if (!first)
					{
						StreamWriter.Write(MSDFile.ParamMarker);
					}

					StreamWriter.Write(MSDFile.Escape(entry));
					first = false;
				}

				StreamWriter.WriteLine(MSDFile.ValueEndMarker);
			}
			else if (value is double d)
			{
				WritePropertyInternal(smPropertyName, d.ToString(SMCommon.SMDoubleFormat));
			}
			else
			{
				WritePropertyInternal(smPropertyName, value.ToString());
			}
		}

		private bool ShouldWriteProperty(bool inSource, bool stepmaniaOmitted)
		{
			switch (Config.PropertyEmissionBehavior)
			{
				case PropertyEmissionBehavior.MatchSource:
					if (inSource)
						return true;
					break;
				case PropertyEmissionBehavior.Stepmania:
					if (!stepmaniaOmitted)
						return true;
					break;
			}
			return false;
		}

		protected bool TryGetChartExtra(Chart chart, string sKey, out object value)
		{
			value = null;
			if (chart.DestExtras.TryGetValue(sKey, out value))
				return true;
			if (Config.Song.SourceType == FileFormatType && chart.SourceExtras.TryGetValue(sKey, out value))
				return true;
			return false;
		}

		protected void WriteChartProperty(Chart chart, string smPropertyName, object value, bool stepmaniaOmitted = false)
		{
			var inSource = TryGetChartExtra(chart, smPropertyName, out _);
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteChartPropertyFromExtras(Chart chart, string smPropertyName, bool stepmaniaOmitted = false)
		{
			var inSource = TryGetChartExtra(chart, smPropertyName, out var value);
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected bool TryGetSongExtra(string sKey, out object value)
		{
			value = null;
			if (Config.Song.DestExtras.TryGetValue(sKey, out value))
				return true;
			if (Config.Song.SourceType == FileFormatType && Config.Song.SourceExtras.TryGetValue(sKey, out value))
				return true;
			return false;
		}

		protected void WriteSongProperty(string smPropertyName, object value, bool stepmaniaOmitted = false)
		{
			var inSource = TryGetSongExtra(smPropertyName, out _);
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteSongPropertyFromExtras(string smPropertyName, bool stepmaniaOmitted = false)
		{
			var inSource = TryGetSongExtra(smPropertyName, out var value);
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteSongPropertyMusic(Chart fallbackChart = null, bool stepmaniaOmitted = false)
		{
			if (!TryGetSongExtra(SMCommon.TagMusic, out var value) && fallbackChart != null)
				value = fallbackChart.MusicFile;
			WriteSongProperty(SMCommon.TagMusic, value?.ToString() ?? "", stepmaniaOmitted);
		}

		protected void WriteSongPropertyOffset(Chart fallbackChart = null, bool stepmaniaOmitted = false)
		{
			if (!TryGetSongExtra(SMCommon.TagOffset, out var value) && fallbackChart != null)
				value = fallbackChart.ChartOffsetFromMusic.ToString(SMCommon.SMDoubleFormat);
			WriteSongProperty(SMCommon.TagOffset, value?.ToString() ?? "", stepmaniaOmitted);
		}

		protected void WriteSongPropertyBPMs(Chart fallbackChart = null, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			// Stepmania files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (Config.Song.SourceType == FileFormatType
			    && Config.Song.SourceExtras.ContainsKey(SMCommon.TagFumenRawBpmsStr)
			    && Config.Song.SourceExtras[SMCommon.TagFumenRawBpmsStr] is string rawStr)
			{
				WriteSongProperty(SMCommon.TagBPMs, rawStr, stepmaniaOmitted);
				return;
			}

			if (fallbackChart != null)
			{
				WriteChartPropertyBPMs(fallbackChart, stepmaniaOmitted);
				return;
			}

			WriteSongProperty(SMCommon.TagBPMs, "", stepmaniaOmitted);
		}

		protected void WriteChartPropertyBPMs(Chart chart, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			if (Config.Song.SourceType == FileFormatType
			    && chart.SourceExtras.ContainsKey(SMCommon.TagFumenRawBpmsStr)
			    && chart.SourceExtras[SMCommon.TagFumenRawBpmsStr] is string rawStr)
			{
				WriteChartProperty(chart, SMCommon.TagBPMs, rawStr, stepmaniaOmitted);
				return;
			}

			WriteChartProperty(chart, SMCommon.TagBPMs, CreateBPMStringFromChartEvents(chart), stepmaniaOmitted);
		}

		private string CreateBPMStringFromChartEvents(Chart chart)
		{
			var tempoChangeBeats = GetTempoChangeBeatsForChart(chart);

			var numTempoChangeEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in chart.Layers[0].Events)
			{
				if (!(e is TempoChange tc))
					continue;
				numTempoChangeEvents++;
				if (numTempoChangeEvents > 1)
					sb.Append(",\r\n");

				// If present, use the original double value from the source chart so as not to lose
				// or alter the precision.
				var timeInBeats = tempoChangeBeats[tc];
				if (Config.Song.SourceType == FileFormatType
				    && tc.SourceExtras.ContainsKey(SMCommon.TagFumenDoublePosition)
				    && tc.SourceExtras[SMCommon.TagFumenDoublePosition] is double)
				{
					timeInBeats = (double) tc.SourceExtras[SMCommon.TagFumenDoublePosition];
				}

				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				var tempoStr = tc.TempoBPM.ToString(SMCommon.SMDoubleFormat);
				sb.Append($"{timeInBeatsStr}={tempoStr}");
			}

			return sb.ToString();
		}

		private Dictionary<TempoChange, double> GetTempoChangeBeatsForChart(Chart chart)
		{
			var tempoChangeBeats = new Dictionary<TempoChange, double>();
			var beatAtCurrentMeasureStart = 0;
			var currentMeasure = 0;
			var currentTimeSignature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure);
			if (chart.Layers.Count > 0)
			{
				foreach (var chartEvent in chart.Layers[0].Events)
				{
					if (chartEvent.Position.Measure > currentMeasure)
					{
						beatAtCurrentMeasureStart +=
							currentTimeSignature.Numerator * (chartEvent.Position.Measure - currentMeasure);
						currentMeasure = chartEvent.Position.Measure;
					}

					switch (chartEvent)
					{
						case TempoChange tempoChange:
							tempoChangeBeats[tempoChange] =
								beatAtCurrentMeasureStart + tempoChange.Position.Beat +
								tempoChange.Position.SubDivision.ToDouble();
							break;
						case TimeSignature timeSignature:
							currentTimeSignature = timeSignature.Signature;
							break;
					}
				}
			}

			return tempoChangeBeats;
		}

		protected void WriteSongPropertyStops(Chart fallbackChart = null, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			// Stepmania files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (Config.Song.SourceType == FileFormatType
			    && Config.Song.SourceExtras.ContainsKey(SMCommon.TagFumenRawStopsStr)
			    && Config.Song.SourceExtras[SMCommon.TagFumenRawStopsStr] is string rawStr)
			{
				WriteSongProperty(SMCommon.TagStops, rawStr, stepmaniaOmitted);
				return;
			}

			if (fallbackChart != null)
			{
				WriteChartPropertyStops(fallbackChart, stepmaniaOmitted);
				return;
			}

			WriteSongProperty(SMCommon.TagStops, "", stepmaniaOmitted);
		}

		protected void WriteChartPropertyStops(Chart chart, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			if (Config.Song.SourceType == FileFormatType
			    && chart.SourceExtras.ContainsKey(SMCommon.TagFumenRawStopsStr)
			    && chart.SourceExtras[SMCommon.TagFumenRawStopsStr] is string rawStr)
			{
				WriteChartProperty(chart, SMCommon.TagStops, rawStr, stepmaniaOmitted);
				return;
			}

			WriteChartProperty(chart, SMCommon.TagStops, CreateStopStringFromChartEvents(chart), stepmaniaOmitted);
		}

		private string CreateStopStringFromChartEvents(Chart chart)
		{
			var stopBeats = GetStopBeatsForChart(chart);

			var numStopEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in chart.Layers[0].Events)
			{
				if (!(e is Stop stop))
					continue;
				numStopEvents++;
				if (numStopEvents > 1)
					sb.Append(",\r\n");

				// If present, use the original double values from the source chart so as not to lose
				// or alter the precision.
				var timeInBeats = stopBeats[stop];
				if (Config.Song.SourceType == FileFormatType
				    && stop.SourceExtras.ContainsKey(SMCommon.TagFumenDoublePosition)
				    && stop.SourceExtras[SMCommon.TagFumenDoublePosition] is double)
				{
					timeInBeats = (double)stop.SourceExtras[SMCommon.TagFumenDoublePosition];
				}
				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				var length = stop.LengthMicros / 1000000.0;
				if (Config.Song.SourceType == FileFormatType
				    && stop.SourceExtras.ContainsKey(SMCommon.TagFumenDoubleValue)
				    && stop.SourceExtras[SMCommon.TagFumenDoubleValue] is double)
				{
					length = (double)stop.SourceExtras[SMCommon.TagFumenDoubleValue];
				}
				var lengthStr = length.ToString(SMCommon.SMDoubleFormat);

				sb.Append($"{timeInBeatsStr}={lengthStr}");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Stops need to be written using number of beats as position instead of a full metric position.
		/// Compute the beat values.
		/// </summary>
		/// <param name="chart"></param>
		/// <returns></returns>
		private Dictionary<Stop, double> GetStopBeatsForChart(Chart chart)
		{
			var stopBeats = new Dictionary<Stop, double>();
			var beatAtCurrentMeasureStart = 0;
			var currentMeasure = 0;
			var currentTimeSignature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure);
			if (chart.Layers.Count > 0)
			{
				foreach (var chartEvent in chart.Layers[0].Events)
				{
					if (chartEvent.Position.Measure > currentMeasure)
					{
						beatAtCurrentMeasureStart +=
							currentTimeSignature.Numerator * (chartEvent.Position.Measure - currentMeasure);
						currentMeasure = chartEvent.Position.Measure;
					}

					switch (chartEvent)
					{
						case Stop stop:
							stopBeats[stop] =
								beatAtCurrentMeasureStart + stop.Position.Beat + stop.Position.SubDivision.ToDouble();
							break;
						case TimeSignature timeSignature:
							currentTimeSignature = timeSignature.Signature;
							break;
					}
				}
			}

			return stopBeats;
		}

		protected void WriteChartNotesValueStart(Chart chart)
		{
			var notesType = SMCommon.TagNotes;
			if (TryGetChartExtra(chart, SMCommon.TagFumenNotesType, out var notesTypeObj)
			    && notesTypeObj is string notesTypeObjStr)
				notesType = notesTypeObjStr;
			StreamWriter.WriteLine($"{MSDFile.ValueStartMarker}{notesType}{MSDFile.ParamMarker}");
		}

		protected void WriteChartNotes(Chart chart)
		{
			// Write one chart per player.
			for (var playerIndex = 0; playerIndex < chart.NumPlayers; playerIndex++)
			{
				// Marker to separate players' charts.
				if (playerIndex > 0)
					StreamWriter.Write("&\r\n\r\n");

				var measures = new List<MeasureData>();
				// Add one measure so even blank charts write one empty measure.
				measures.Add(new MeasureData { FirstMeasure = true });

				// Accumulate data about each measure.
				foreach (var chartEvent in chart.Layers[0].Events)
				{
					var note = chartEvent as LaneNote;
					if (note == null || note.Player != playerIndex)
						continue;

					var measure = note.Position.Measure;
					while (measures.Count <= measure)
						measures.Add(new MeasureData());

					measures[measure].Notes.Add(note);
					measures[measure].GreatestUnmodifiedDenominator = Math.Max(
						measures[measure].GreatestUnmodifiedDenominator, note.Position.SubDivision.Denominator);
					measures[measure].LCM = Fraction.LeastCommonMultiple(
						note.Position.SubDivision.Reduce().Denominator,
						measures[measure].LCM);
				}

				// Write each measure.
				var index = 0;
				foreach (var measureData in measures)
					WriteMeasure(chart, measureData, index++);
			}
		}

		private void WriteMeasure(Chart chart, MeasureData measureData, int measureIndex)
		{
			var linesPerBeat = 1;
			switch (Config.MeasureSpacingBehavior)
			{
				case MeasureSpacingBehavior.UseUnmodifiedChartSubDivisions:
				{
					linesPerBeat = measureData.GreatestUnmodifiedDenominator;
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMultiple:
				{
					linesPerBeat = measureData.LCM;
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMultipleFromStepmaniaEditor:
				{
					// Make sure the notes can actually be represented by the stepmania editor.
					if (!SMCommon.GetLowestValidSMSubDivision(measureData.LCM, out linesPerBeat))
						Logger.Error($"Unsupported subdivisions {measureData.LCM} for notes in measure index {measureIndex}.");
					break;
				}
			}

			// Set up a grid of characters to write.
			// TODO: Support keysound tagging.
			var measureCharsDX = chart.NumInputs;
			var measureCharsDY = SMCommon.NumBeatsPerMeasure * linesPerBeat;
			var measureChars = new char[measureCharsDX, measureCharsDY];

			// Populate characters in the grid based on the events of the measure.
			foreach (var measureNote in measureData.Notes)
			{
				// Get the note char to write.
				var c = GetSMCharForNote(measureNote);

				// Determine the position to record the note.
				var reducedSubDivision = measureNote.Position.SubDivision.Reduce();
				var measureEventPositionInMeasure =
					measureNote.Position.Beat * linesPerBeat
					+ (linesPerBeat / Math.Max(1, reducedSubDivision.Denominator))
					* reducedSubDivision.Numerator;

				// Record the note.
				if (measureNote.Lane >= 0 && measureNote.Lane < measureCharsDX
					&& measureEventPositionInMeasure >= 0 && measureEventPositionInMeasure < measureCharsDY)
				{
					measureChars[measureNote.Lane, measureEventPositionInMeasure] = c;
				}
				else
				{
					Logger.Error($"Error writing note at position {measureNote.Position} lane {measureNote.Lane}.");
				}
			}

			// Write the measure of accumulated characters.
			if (!measureData.FirstMeasure)
				StreamWriter.WriteLine(",");
			for (var y = 0; y < measureCharsDY; y++)
			{
				for (var x = 0; x < measureCharsDX; x++)
				{
					StreamWriter.Write(measureChars[x, y] == '\0' ?
						SMCommon.NoteChars[(int)SMCommon.NoteType.None]
						: measureChars[x, y]);
				}
				StreamWriter.WriteLine();
			}
		}

		private char GetSMCharForNote(LaneNote note)
		{
			if (note.DestType?.Length == 1
			    && SMCommon.NoteChars.Contains(note.DestType[0]))
				return note.DestType[0];

			if (Config.Song.SourceType == FileFormatType
			    && note.SourceType.Length == 1
			    && SMCommon.NoteChars.Contains(note.SourceType[0]))
				return note.SourceType[0];

			if (note is LaneTapNote)
				return SMCommon.NoteChars[(int)SMCommon.NoteType.Tap];
			if (note is LaneHoldEndNote)
				return SMCommon.NoteChars[(int)SMCommon.NoteType.HoldEnd];
			if (note is LaneHoldStartNote)
				return SMCommon.NoteChars[(int)SMCommon.NoteType.HoldStart];
			return SMCommon.NoteChars[(int)SMCommon.NoteType.None];
		}

		protected string GetRadarValues(Chart chart)
		{
			var radarValues = new List<double>();
			if (chart.DestExtras.ContainsKey(SMCommon.TagRadarValues)
			    && chart.DestExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>)chart.DestExtras[SMCommon.TagRadarValues];
			}
			else if (Config.Song.SourceType == FileFormatType
			         && chart.SourceExtras.ContainsKey(SMCommon.TagRadarValues)
			         && chart.SourceExtras[SMCommon.TagRadarValues] is List<double>)
			{
				radarValues = (List<double>)chart.SourceExtras[SMCommon.TagRadarValues];
			}

			if (radarValues.Count <= 0)
				return "";

			var sb = new StringBuilder();
			var bFirst = true;
			foreach (var val in radarValues)
			{
				if (!bFirst)
					sb.Append(",");
				sb.Append(val.ToString(SMCommon.SMDoubleFormat));
				bFirst = false;
			}

			return sb.ToString();
		}

		protected static bool TryGetChartType(Chart chart, out SMCommon.ChartType chartType)
		{
			// If the Chart's Type is already a supported ChartType, use that.
			if (SMCommon.TryGetChartType(chart.Type, out chartType))
				return true;

			// Otherwise infer the ChartType from the number of inputs and players
			chartType = SMCommon.ChartType.dance_single;
			if (chart.NumPlayers == 1)
			{
				if (chart.NumInputs <= 3)
				{
					chartType = SMCommon.ChartType.dance_threepanel;
					return true;
				}
				if (chart.NumInputs == 4)
				{
					chartType = SMCommon.ChartType.dance_single;
					return true;
				}
				if (chart.NumInputs <= 6)
				{
					chartType = SMCommon.ChartType.dance_solo;
					return true;
				}
				if (chart.NumInputs <= 8)
				{
					chartType = SMCommon.ChartType.dance_double;
					return true;
				}
			}
			if (chart.NumPlayers == 2)
			{
				if (chart.NumInputs <= 8)
				{
					chartType = SMCommon.ChartType.dance_routine;
					return true;
				}
			}

			return false;
		}
	}
}
