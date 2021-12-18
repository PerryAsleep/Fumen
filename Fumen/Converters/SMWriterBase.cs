using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fumen.ChartDefinition;

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
			/// Use the SourceExtra FumenNoteOriginalMeasurePosition fraction per note.
			/// This option should be used when loading an sm file and writing it back out
			/// as it will preserve the spacing.
			/// </summary>
			UseSourceExtraOriginalMeasurePosition,
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
			/// property) that those properties will be ignored when exporting. This option should be
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
			/// The Chart to use when writing properties that are defined at the Song level in an sm or ssc
			/// file which need to be derived from a Chart. This includes, for example, stops and tempo changes
			/// in an sm file and time signatures in both sm and ssc files.
			/// </summary>
			public Chart FallbackChart;
			/// <summary>
			/// The path of the file to write to.
			/// </summary>
			public string FilePath;
			/// <summary>
			/// When writing files, the Event IntegerPosition will be used to determine positioning.
			/// Setting this variable to true will update every Event IntegerPosition based off of it's
			/// MetricPosition prior to writing.
			/// Use this if the Rows have not been set but accurate MetricPositions are available.
			/// </summary>
			public bool UpdateEventRowsFromMetricPosition = false;
			/// <summary>
			/// If true, write BPMs from the Song or Chart's Extras.
			/// If false, write from the Chart's TempoChange Events.
			/// </summary>
			public bool WriteBPMsFromExtras = false;
			/// <summary>
			/// If true, write Stops from the Song or Chart's Extras.
			/// If false, write from the Chart's Stop Events.
			/// </summary>
			public bool WriteStopsFromExtras = false;
			/// <summary>
			/// If true, write BPMs from the Song or Chart's Extras.
			/// If false, write from the Chart's TimeSignature Events.
			/// </summary>
			public bool WriteTimeSignaturesFromExtras = false;
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

			if (Config.UpdateEventRowsFromMetricPosition)
			{
				foreach (var chart in Config.Song.Charts)
					SMCommon.SetEventRowsFromMetricPosition(chart);
			}
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

		protected bool MatchesSourceFileFormatType()
		{
			return Config.Song.SourceType == FileFormatType;
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
					if (!string.IsNullOrEmpty(entry))
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

		protected void WriteChartProperty(Chart chart, string smPropertyName, object value, bool stepmaniaOmitted = false)
		{
			var inSource = chart.Extras.TryGetExtra(smPropertyName, out object _, MatchesSourceFileFormatType());
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteChartPropertyFromExtras(Chart chart, string smPropertyName, bool stepmaniaOmitted = false)
		{
			var inSource = chart.Extras.TryGetExtra(smPropertyName, out object value, MatchesSourceFileFormatType());
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}
		protected void WriteSongProperty(string smPropertyName, object value, bool stepmaniaOmitted = false)
		{
			var inSource = Config.Song.Extras.TryGetExtra(smPropertyName, out object _, MatchesSourceFileFormatType());
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteSongPropertyFromExtras(string smPropertyName, bool stepmaniaOmitted = false)
		{
			var inSource = Config.Song.Extras.TryGetExtra(smPropertyName, out object value, MatchesSourceFileFormatType());
			if (ShouldWriteProperty(inSource, stepmaniaOmitted))
				WritePropertyInternal(smPropertyName, value);
		}

		protected void WriteSongPropertyMusic(bool stepmaniaOmitted = false)
		{
			if (!Config.Song.Extras.TryGetExtra(SMCommon.TagMusic, out object value, MatchesSourceFileFormatType())
			    && Config.FallbackChart != null)
				value = Config.FallbackChart.MusicFile;
			WriteSongProperty(SMCommon.TagMusic, value?.ToString() ?? "", stepmaniaOmitted);
		}

		protected void WriteSongPropertyOffset(bool stepmaniaOmitted = false)
		{
			if (!Config.Song.Extras.TryGetExtra(SMCommon.TagOffset, out object value, MatchesSourceFileFormatType())
			    && Config.FallbackChart != null)
				value = Config.FallbackChart.ChartOffsetFromMusic.ToString(SMCommon.SMDoubleFormat);
			WriteSongProperty(SMCommon.TagOffset, value?.ToString() ?? "", stepmaniaOmitted);
		}

		protected void WriteSongPropertyBPMs(bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			// Stepmania files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (MatchesSourceFileFormatType()
				&& Config.WriteBPMsFromExtras
			    && Config.Song.Extras.TryGetSourceExtra(SMCommon.TagFumenRawBpmsStr, out string rawStr))
			{
				WriteSongProperty(SMCommon.TagBPMs, rawStr, stepmaniaOmitted);
				return;
			}

			if (Config.FallbackChart != null)
			{
				WriteChartPropertyBPMs(Config.FallbackChart, stepmaniaOmitted);
				return;
			}

			WriteSongProperty(SMCommon.TagBPMs, "", stepmaniaOmitted);
		}

		protected void WriteChartPropertyBPMs(Chart chart, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			if (Config.WriteBPMsFromExtras 
			    && chart.Extras.TryGetExtra(SMCommon.TagFumenRawBpmsStr, out string rawStr, MatchesSourceFileFormatType()))
			{
				WriteChartProperty(chart, SMCommon.TagBPMs, rawStr, stepmaniaOmitted);
				return;
			}

			WriteChartProperty(chart, SMCommon.TagBPMs, CreateBPMStringFromChartEvents(chart), stepmaniaOmitted);
		}

		private string CreateBPMStringFromChartEvents(Chart chart)
		{
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
				if (!tc.Extras.TryGetExtra(SMCommon.TagFumenDoublePosition, out double timeInBeats,
					    MatchesSourceFileFormatType()))
				{
					timeInBeats = (double)tc.IntegerPosition / SMCommon.MaxValidDenominator;
				}

				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				var tempoStr = tc.TempoBPM.ToString(SMCommon.SMDoubleFormat);
				sb.Append($"{timeInBeatsStr}={tempoStr}");
			}

			return sb.ToString();
		}

		protected void WriteSongPropertyStops(bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			// Stepmania files have changed how they format BPMs and Stops over the years
			// and this is to cut down on unnecessary diffs when exporting files.
			if (MatchesSourceFileFormatType()
			    && Config.WriteStopsFromExtras
				&& Config.Song.Extras.TryGetSourceExtra(SMCommon.TagFumenRawStopsStr, out string rawStr))
			{
				WriteSongProperty(SMCommon.TagStops, rawStr, stepmaniaOmitted);
				return;
			}

			if (Config.FallbackChart != null)
			{
				WriteChartPropertyStops(Config.FallbackChart, stepmaniaOmitted);
				return;
			}

			WriteSongProperty(SMCommon.TagStops, "", stepmaniaOmitted);
		}

		protected void WriteChartPropertyStops(Chart chart, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			if (Config.WriteStopsFromExtras
			    && chart.Extras.TryGetExtra(SMCommon.TagFumenRawStopsStr, out string rawStr, MatchesSourceFileFormatType()))
			{
				WriteChartProperty(chart, SMCommon.TagStops, rawStr, stepmaniaOmitted);
				return;
			}

			WriteChartProperty(chart, SMCommon.TagStops, CreateStopStringFromChartEvents(chart), stepmaniaOmitted);
		}

		private string CreateStopStringFromChartEvents(Chart chart)
		{
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
				if (!stop.Extras.TryGetExtra(SMCommon.TagFumenDoublePosition, out double timeInBeats,
					    MatchesSourceFileFormatType()))
				{
					timeInBeats = (double)stop.IntegerPosition / SMCommon.MaxValidDenominator;
				}
				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				if (!stop.Extras.TryGetExtra(SMCommon.TagFumenDoubleValue, out double length, MatchesSourceFileFormatType()))
					length = stop.LengthMicros / 1000000.0;
				var lengthStr = length.ToString(SMCommon.SMDoubleFormat);

				sb.Append($"{timeInBeatsStr}={lengthStr}");
			}

			return sb.ToString();
		}

		protected void WriteSongPropertyTimeSignatures(bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			// This is to cut down on unnecessary diffs when exporting files.
			if (MatchesSourceFileFormatType()
				&& Config.WriteTimeSignaturesFromExtras
				&& Config.Song.Extras.TryGetSourceExtra(SMCommon.TagFumenRawTimeSignaturesStr, out string rawStr))
			{
				WriteSongProperty(SMCommon.TagTimeSignatures, rawStr, stepmaniaOmitted);
				return;
			}

			if (Config.FallbackChart != null)
			{
				WriteChartPropertyTimeSignatures(Config.FallbackChart, stepmaniaOmitted);
				return;
			}

			// Default to 4/4.
			WriteSongProperty(SMCommon.TagTimeSignatures, "0.000=4=4;", stepmaniaOmitted);
		}

		protected void WriteChartPropertyTimeSignatures(Chart chart, bool stepmaniaOmitted = false)
		{
			// If we have a raw string from the source file, use it.
			if (Config.WriteTimeSignaturesFromExtras
				&& chart.Extras.TryGetExtra(SMCommon.TagFumenRawTimeSignaturesStr, out string rawStr, MatchesSourceFileFormatType()))
			{
				WriteChartProperty(chart, SMCommon.TagTimeSignatures, rawStr, stepmaniaOmitted);
				return;
			}

			WriteChartProperty(chart, SMCommon.TagStops, CreateTimeSignaturesStringFromChartEvents(chart), stepmaniaOmitted);
		}

		private string CreateTimeSignaturesStringFromChartEvents(Chart chart)
		{
			var numTimeSignatureEvents = 0;
			var sb = new StringBuilder();
			foreach (var e in chart.Layers[0].Events)
			{
				if (!(e is TimeSignature ts))
					continue;
				numTimeSignatureEvents++;
				if (numTimeSignatureEvents > 1)
					sb.Append(",\r\n");

				// If present, use the original double values from the source chart so as not to lose
				// or alter the precision.
				if (!ts.Extras.TryGetExtra(SMCommon.TagFumenDoublePosition, out double timeInBeats,
						MatchesSourceFileFormatType()))
				{
					timeInBeats = (double)ts.IntegerPosition / SMCommon.MaxValidDenominator;
				}
				var timeInBeatsStr = timeInBeats.ToString(SMCommon.SMDoubleFormat);

				sb.Append($"{timeInBeatsStr}={ts.Signature.Numerator}={ts.Signature.Denominator}");
			}

			return sb.ToString();
		}

		protected void WriteChartNotesValueStart(Chart chart)
		{
			if (!chart.Extras.TryGetExtra(SMCommon.TagFumenNotesType, out string notesType, MatchesSourceFileFormatType()))
				notesType = SMCommon.TagNotes;
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
				// Each measure here is forced 4/4.
				foreach (var chartEvent in chart.Layers[0].Events)
				{
					var note = chartEvent as LaneNote;
					if (note == null || note.Player != playerIndex)
						continue;

					var measure = note.IntegerPosition / (SMCommon.MaxValidDenominator * SMCommon.NumBeatsPerMeasure);
					while (measures.Count <= measure)
						measures.Add(new MeasureData());

					measures[measure].Notes.Add(note);

					// Store least common multiple of all notes in this measure.
					var subDivisionFraction = new Fraction(
						note.IntegerPosition % SMCommon.MaxValidDenominator,
						SMCommon.MaxValidDenominator);
					var subDivisionDenom = subDivisionFraction.Reduce().Denominator;
					if (subDivisionDenom == 0)
						subDivisionDenom = 1;
					measures[measure].LCM = Fraction.LeastCommonMultiple(
						subDivisionDenom,
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
			// For UseLeastCommonMultiple and UseLeastCommonMultipleFromStepmaniaEditor we will
			// determine the number of lines to write per beat, assuming 4 beats per measure since
			// Stepmania enforces 4/4.
			// For UseSubDivisionDenominatorAsMeasureSpacing we will use the SubDivision denominator
			// as the measure spacing.
			// This helps maintain spacing for charts which were not written in Stepmania and
			// use technically unsupported spacing (like 14 notes per measure).
			var linesPerBeat = 1;
			var measureCharsDY = 0;
			switch (Config.MeasureSpacingBehavior)
			{
				case MeasureSpacingBehavior.UseSourceExtraOriginalMeasurePosition:
				{
					if (measureData.Notes.Count > 0)
					{
						foreach (var measureNote in measureData.Notes)
						{
							if (!measureNote.Extras.TryGetExtra(
								    SMCommon.TagFumenNoteOriginalMeasurePosition,
								    out Fraction f,
								    true))
							{
								Logger.Error($"Notes in measure {measureIndex} are missing Extras for {SMCommon.TagFumenNoteOriginalMeasurePosition}."
								             + $" Fractions must be present in the Extras for {SMCommon.TagFumenNoteOriginalMeasurePosition} when using"
								             + " UseSourceExtraOriginalMeasurePosition MeasureSpacingBehavior.");
								return;
							}

							// Every note must also have the same denominator (number of lines in the measure).
							if (measureCharsDY == 0)
							{
								measureCharsDY = f.Denominator;
							}
							else if (measureCharsDY != f.Denominator)
							{
								Logger.Error($"Notes in measure {measureIndex} have inconsistent SubDivision denominators."
								             + " These must all be equal when using UseSourceExtraOriginalMeasurePosition MeasureSpacingBehavior.");
								return;
							}
						}
					}
					// Still treat blank measures as 4 lines.
					else
					{
						measureCharsDY = SMCommon.NumBeatsPerMeasure;
					}
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMultiple:
				{
					linesPerBeat = measureData.LCM;
					measureCharsDY = SMCommon.NumBeatsPerMeasure * linesPerBeat;
					break;
				}
				case MeasureSpacingBehavior.UseLeastCommonMultipleFromStepmaniaEditor:
				{
					// Make sure the notes can actually be represented by the stepmania editor.
					if (!SMCommon.GetLowestValidSMSubDivision(measureData.LCM, out linesPerBeat))
					{
						Logger.Error($"Unsupported subdivisions {measureData.LCM} for notes in measure index {measureIndex}."
							+ " Consider using UseLeastCommonMultiple MeasureSpacingBehavior.");
						return;
					}
					measureCharsDY = SMCommon.NumBeatsPerMeasure * linesPerBeat;
					break;
				}
			}

			// Set up a grid of characters to write.
			// TODO: Support keysound tagging.
			var measureCharsDX = chart.NumInputs;
			var measureChars = new char[measureCharsDX, measureCharsDY];

			// Populate characters in the grid based on the events of the measure.
			foreach (var measureNote in measureData.Notes)
			{
				// Get the note char to write.
				var c = GetSMCharForNote(measureNote);

				// Determine the position to record the note.
				int measureEventPositionInMeasure;

				// When using UseSourceExtraOriginalMeasurePosition, get the y position directly from the extra data.
				if (Config.MeasureSpacingBehavior == MeasureSpacingBehavior.UseSourceExtraOriginalMeasurePosition)
				{
					measureNote.Extras.TryGetExtra(
						SMCommon.TagFumenNoteOriginalMeasurePosition,
						out Fraction f,
						true);
					measureEventPositionInMeasure = f.Numerator;
				}

				// Otherwise calculate the position based on the lines per beat.
				else
				{
					var totalBeat = measureNote.IntegerPosition / SMCommon.MaxValidDenominator;
					var relativeBeat = totalBeat % SMCommon.NumBeatsPerMeasure;
					var subDivision = new Fraction(measureNote.IntegerPosition % SMCommon.MaxValidDenominator, SMCommon.MaxValidDenominator);
					var reducedSubDivision = subDivision.Reduce();
					measureEventPositionInMeasure = relativeBeat * linesPerBeat
					                                + (linesPerBeat / Math.Max(1, reducedSubDivision.Denominator))
					                                * reducedSubDivision.Numerator;
				}

				// Bounds checks.
				if (measureNote.Lane < 0 || measureNote.Lane >= chart.NumInputs)
				{
					Logger.Error($"Note at {SMCommon.GetPositionForLogging(measureNote.IntegerPosition)} has invalid lane {measureNote.Lane}.");
					return;
				}
				if (measureEventPositionInMeasure < 0 || measureEventPositionInMeasure >= measureCharsDY)
				{
					Logger.Error($"Note has invalid position {SMCommon.GetPositionForLogging(measureNote.IntegerPosition)}.");
					return;
				}

				// Record the note.
				measureChars[measureNote.Lane, measureEventPositionInMeasure] = c;
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

			if (MatchesSourceFileFormatType()
			    && note.SourceType?.Length == 1
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
			if (!chart.Extras.TryGetExtra(SMCommon.TagRadarValues, out List<double> radarValues, MatchesSourceFileFormatType())
			    || radarValues.Count <= 0)
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
