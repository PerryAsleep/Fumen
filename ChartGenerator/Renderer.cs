using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fumen;
using Fumen.Converters;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	public class Renderer
	{
		private const int BannerW = 418;
		private const int BannerH = 164;
		private const int ArrowW = 128;
		private const int HoldCapH = 64;
		private const int TableBorderW = 1;
		private const int ChartColW = 50;
		private const int ChartHeaderH = 20;
		private const int MinesColW = 256;
		private const int CostColW = 64;
		private const int ExpressionColW = 256;

		private const int BeatYSeparation = ArrowW * 2;
		private const int MeasureMarkerH = 10;
		private const int BeatMarkerH = 6;
		private const int ChartTextH = 20;

		private static string SrcDir;

		private enum ChartColumns
		{
			TimeSignature,
			BPM,
			Stop,
			Measure
		}

		private enum ExpressionColumns
		{
			Mines,
			Cost,
			LeftFoot,
			RightFoot
		}

		private class ColumnInfo
		{
			public string Name;
			public int X;
			public int Width;
		}

		private static readonly ColumnInfo[] ChartColumnInfo;
		private static readonly ColumnInfo[] ExpressionColumnInfo;
		private static string[] ArrowNames = { "L", "D", "U", "R" };
		private static string[] ArrowClassStrings = { @" class=""left""", "", @" class=""up""", @" class=""right""" };

		private static string[] StepTypeStrings =
		{
			"Same Arrow",
			"New Arrow",
			"Crossover In Front",
			"Crossover In Back",
			"Invert In Front",
			"Invert In Back",
			"Foot Swap",
			"Br H New T New",
			"Br H New T Same",
			"Br H Same T New",
			"Br H Same T Same",
			"Br H Same T Swap",
			"Br H New T Swap",
			"Br H Swap T Same",
			"Br H Swap T New",
			"Br 1 H Same",
			"Br 1 H New",
			"Br 1 T Same",
			"Br 1 T New",
		};

		private StringBuilder StringBuilder = new StringBuilder();
		private Song Song;
		private Chart OriginalChart;
		private ExpressedChart ExpressedChart;
		private ExpressedChart.ChartSearchNode OriginalExpressedSearchNode;
		private Chart GeneratedChart;
		private PerformedChart GeneratedPerformedChart;
		private string SongPath;
		private string SaveFile;

		private readonly int OriginalChartX = 0;
		private readonly int ExpressedChartX;
		private readonly int GeneratedChartX;

		private double[] LastExpressionPosition = new double[NumFeet];

		static Renderer()
		{
			SrcDir = AppDomain.CurrentDomain.BaseDirectory;
			SrcDir = SrcDir.Replace('\\', '/');
			SrcDir += "/html/src/";

			var x = 0;
			ChartColumnInfo = new ColumnInfo[Enum.GetNames(typeof(ChartColumns)).Length];
			ChartColumnInfo[(int)ChartColumns.TimeSignature] = new ColumnInfo {Name = "Time", Width = ChartColW, X = x };
			x += ChartColumnInfo[(int) ChartColumns.TimeSignature].Width;
			ChartColumnInfo[(int)ChartColumns.BPM] = new ColumnInfo { Name = "BPM", Width = ChartColW, X = x };
			x += ChartColumnInfo[(int)ChartColumns.BPM].Width;
			ChartColumnInfo[(int)ChartColumns.Stop] = new ColumnInfo { Name = "Stop", Width = ChartColW, X = x };
			x += ChartColumnInfo[(int)ChartColumns.Stop].Width;
			ChartColumnInfo[(int)ChartColumns.Measure] = new ColumnInfo { Name = "Meas", Width = ChartColW, X = x };

			x = 0;
			ExpressionColumnInfo = new ColumnInfo[Enum.GetNames(typeof(ExpressionColumns)).Length];
			ExpressionColumnInfo[(int)ExpressionColumns.Mines] = new ColumnInfo { Name = "Mines", Width = MinesColW, X = x };
			x += ExpressionColumnInfo[(int)ExpressionColumns.Mines].Width;
			ExpressionColumnInfo[(int)ExpressionColumns.Cost] = new ColumnInfo { Name = "Cost", Width = CostColW, X = x };
			x += ExpressionColumnInfo[(int)ExpressionColumns.Cost].Width;
			ExpressionColumnInfo[(int)ExpressionColumns.LeftFoot] = new ColumnInfo { Name = "Left Foot", Width = ExpressionColW, X = x };
			x += ExpressionColumnInfo[(int)ExpressionColumns.LeftFoot].Width;
			ExpressionColumnInfo[(int)ExpressionColumns.RightFoot] = new ColumnInfo { Name = "Right Foot", Width = ExpressionColW, X = x };
		}

		private Renderer() { }
		public Renderer(
			string songPath,
			string saveFile,
			Song song,
			Chart originalChart,
			ExpressedChart expressedChart,
			ExpressedChart.ChartSearchNode originalExpressedSearchNode,
			PerformedChart generatedPerformedChart,
			Chart generatedChart)
		{
			SongPath = songPath;
			SaveFile = saveFile;
			Song = song;
			OriginalChart = originalChart;
			ExpressedChart = expressedChart;
			OriginalExpressedSearchNode = originalExpressedSearchNode;
			GeneratedPerformedChart = generatedPerformedChart;
			GeneratedChart = generatedChart;

			ExpressedChartX = 0;
			foreach (var chartColInfo in ChartColumnInfo)
				ExpressedChartX += chartColInfo.Width;
			ExpressedChartX += (originalChart.NumInputs * ArrowW);

			GeneratedChartX = ExpressedChartX;
			foreach (var expressedColInfo in ExpressionColumnInfo)
				GeneratedChartX += expressedColInfo.Width;

			if (!SongPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				SongPath += Path.DirectorySeparatorChar.ToString();
		}

		public void Write()
		{
			StringBuilder.Append("<!DOCTYPE html>\r\n");
			WriteHead();
			StringBuilder.Append("<html>\r\n\t<body>\r\n");
			WriteTitle();
			WriteChartHeaders();
			StringBuilder.Append($"		<div style=\"position:absolute; top:{BannerH + ChartHeaderH * 2}px;\">\r\n");
			WriteChart(OriginalChart, OriginalChartX, true);
			WriteChart(GeneratedChart, GeneratedChartX, false);
			StringBuilder.Append("		</div>\r\n");
			WriteScript();
			StringBuilder.Append("\t</body>\r\n</html>\r\n");

			// Write to file
			// TODO: Async
			File.WriteAllText(SaveFile, StringBuilder.ToString());
		}

		private void WriteHead()
		{
			StringBuilder.Append(
$@"<head>
	<style>
body {{
  font-family: Arial;
  background: #f3f3f3;
  border: none;
  padding: none;
  margin: 0px;
  margin-block-start: 0.0em;
  margin-block-end: 0.0em;
}}
p {{
	margin-block-start: 0.0em;
	margin-block-end: 0.0em;
	border: none;
	text-align: center;
}}
.sticky {{
	position: fixed;
	top: 0;
	width: 100%;
}}
.sticky + .content {{
	padding-top: {BannerH}px;
}}
.left {{
	-webkit-transform:rotate(90deg);
	-moz-transform: rotate(90deg);
	-ms-transform: rotate(90deg);
	-o-transform: rotate(90deg);
	transform: rotate(90deg);
}}
.up {{
	-webkit-transform:rotate(180deg);
	-moz-transform: rotate(180deg);
	-ms-transform: rotate(180deg);
	-o-transform: rotate(180deg);
	transform: rotate(180deg);
}}
.right {{
	-webkit-transform:rotate(270deg);
	-moz-transform: rotate(270deg);
	-ms-transform: rotate(270deg);
	-o-transform: rotate(270deg);
	transform: rotate(270deg);
}}
#holdbody {{
	border: none;
	padding: none;
	background: url(""{SrcDir}hold.png"");
	background-repeat: repeat;
}}
#rollbody {{
	border: none;
	padding: none;
	background: url(""{SrcDir}roll.png"");
	background-repeat: repeat;
}}
	</style>
</head>
");
		}

		private void WriteTitle()
		{
			var title = Song.Title ?? "";
			var img = "";
			if (!string.IsNullOrEmpty(Song.SongSelectImage))
				img = SongPath + Song.SongSelectImage;
			var subtitle = "";
			if (!string.IsNullOrEmpty(Song.SubTitle))
				subtitle = $@"<span style=""color:#6b6b6b""> {Song.SubTitle}</span>";
			var artist = Song.Artist ?? "";

			StringBuilder.Append(
$@"		<div style=""height: 164px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">
			<img style=""float: left; width: {BannerW}x; height: {BannerH}px; margin-block-start: 0.0em; margin-block-end: 0.0em;"" src=""{img}""/>
			<h1 style=""text-align: left; font-size: 50px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">{title}{subtitle}</h1>
			<h3 style=""text-align: left; font-size: 30px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">{artist}</h3>
		</div>
");
		}

		private void WriteChartHeaders()
		{
			var originalChartWidth = 0;
			var originalChartCols = 0;
			var expressedChartWidth = 0;
			var expressedChartCols = 0;
			var generatedChartWidth = 0;
			var generatedChartCols = 0;

			foreach (var chartCol in ChartColumnInfo)
			{
				originalChartWidth += chartCol.Width;
				originalChartCols++;
				generatedChartWidth += chartCol.Width;
				generatedChartCols++;
			}

			originalChartWidth += (OriginalChart.NumInputs * ArrowW);
			originalChartCols += OriginalChart.NumInputs;
			generatedChartWidth += (GeneratedChart.NumInputs * ArrowW);
			generatedChartCols += GeneratedChart.NumInputs;

			foreach (var expressionCol in ExpressionColumnInfo)
			{
				expressedChartWidth += expressionCol.Width;
				expressedChartCols++;
			}

			// PKL - I do not understand why adding one here is necessary for everything to line up.
			var fullWidth = originalChartWidth + expressedChartWidth + generatedChartWidth + 1;

			originalChartWidth -= TableBorderW;
			expressedChartWidth -= TableBorderW;
			generatedChartWidth -= TableBorderW;

			var colH = ChartHeaderH - TableBorderW;

			var originalChartTitle = $"{OriginalChart.Type} {OriginalChart.DifficultyType}: Level {OriginalChart.DifficultyRating}";
			if (!string.IsNullOrEmpty(OriginalChart.Description))
				originalChartTitle += $", {OriginalChart.Description}";

			var generatedChartTitle = $"{GeneratedChart.Type} {GeneratedChart.DifficultyType}: Level {GeneratedChart.DifficultyRating}";
			if (!string.IsNullOrEmpty(GeneratedChart.Description))
				generatedChartTitle += $", {GeneratedChart.Description}";

			StringBuilder.Append(
$@"		<div id=""chartHeaders"" style=""z-index:10000000; border:none; margin-block-start: 0.0em; margin-block-end: 0.0em;"">
			<table style=""border-collapse: collapse; background: #dbdbdb; width: {fullWidth}px;"">
				<tr>
					<th colspan=""{originalChartCols}"" style=""table-layout: fixed; width: {originalChartWidth}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{originalChartTitle}</th>
					<th colspan=""{expressedChartCols}"" style=""table-layout: fixed; width: {expressedChartWidth}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">Expression</th>
					<th colspan=""{generatedChartCols}"" style=""table-layout: fixed; width: {generatedChartWidth}x; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{generatedChartTitle}</th>
				</tr>
				<tr>
");

			foreach (var chartCol in ChartColumnInfo)
			{
				StringBuilder.Append(
$@"					<th style=""table-layout: fixed; width: {chartCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{chartCol.Name}</th>
");
			}

			for (var a = 0; a < OriginalChart.NumInputs; a++)
			{
				StringBuilder.Append(
$@"					<th style=""table-layout: fixed; width: {ArrowW - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{ArrowNames[a % 4]}</th>
");
			}

			foreach (var expressionCol in ExpressionColumnInfo)
			{
				StringBuilder.Append(
$@"					<th style=""table-layout: fixed; width: {expressionCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{expressionCol.Name}</th>
");
			}

			foreach (var chartCol in ChartColumnInfo)
			{
				StringBuilder.Append(
$@"					<th style=""table-layout: fixed; width: {chartCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{chartCol.Name}</th>
");
			}

			for (var a = 0; a < GeneratedChart.NumInputs; a++)
			{
				StringBuilder.Append(
$@"					<th style=""table-layout: fixed; width: {ArrowW - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{ArrowNames[a % 4]}</th>
");
			}

			StringBuilder.Append(
@"				</tr>
			</table>
		</div>
");
		}

		private void WriteChart(Chart chart, int chartXPosition, bool originalChart)
		{
			for (var f = 0; f < NumFeet; f++)
				LastExpressionPosition[f] = -1.0;

			var firstLaneX = chartXPosition;
			foreach (var chartCol in ChartColumnInfo)
				firstLaneX += chartCol.Width;

			var previousTimeSignaturePosition = new MetricPosition();
			var previousTimeSignatureY = ArrowW * 0.5;
			var currentTimeSignature = new Fraction(4,4);
			var yPerBeat = (double)BeatYSeparation;

			var lastHoldStarts = new int[chart.NumInputs];
			var lastHoldWasRoll = new bool[chart.NumInputs];

			var currentExpressedChartSearchNode = OriginalExpressedSearchNode;
			var currentPerformedChartNode = GeneratedPerformedChart.Root;
			var currentExpressedMineIndex = 0;

			foreach (var chartEvent in chart.Layers[0].Events)
			{
				double eventY =
					previousTimeSignatureY
					+ (chartEvent.Position.Measure - previousTimeSignaturePosition.Measure) * currentTimeSignature.Numerator * yPerBeat
					+ chartEvent.Position.Beat * yPerBeat
					+ (chartEvent.Position.SubDivision.Denominator == 0 ? 0 : chartEvent.Position.SubDivision.ToDouble() * yPerBeat);

				while (currentExpressedChartSearchNode != null && currentExpressedChartSearchNode.Position < chartEvent.Position)
					currentExpressedChartSearchNode = currentExpressedChartSearchNode.GetNextNode();
				while (currentPerformedChartNode != null && currentPerformedChartNode.Position < chartEvent.Position)
					currentPerformedChartNode = currentPerformedChartNode.Next;
				while (currentExpressedMineIndex < ExpressedChart.MineEvents.Count
				       && ExpressedChart.MineEvents[currentExpressedMineIndex].Position < chartEvent.Position)
					currentExpressedMineIndex++;

				if (chartEvent is TimeSignature ts)
				{
					// Write measure markers up until this time signature change
					WriteMeasures(
						chartXPosition,
						previousTimeSignatureY,
						yPerBeat,
						currentTimeSignature,
						previousTimeSignaturePosition.Measure,
						chartEvent.Position.Measure - previousTimeSignaturePosition.Measure,
						chart.NumInputs);

					// Update time signature tracking
					previousTimeSignatureY = eventY;
					currentTimeSignature = ts.Signature;
					yPerBeat = BeatYSeparation * (4.0 / currentTimeSignature.Denominator);
					previousTimeSignaturePosition = chartEvent.Position;
				}

				// Chart column values. Excluding measures which are handled in WriteMeasures.
				var colX = chartXPosition;
				var colW = ChartColumnInfo[(int)ChartColumns.TimeSignature].Width;
				var colY = (int)(eventY - ChartTextH * .5);
				var colH = ChartTextH;
				string colVal = null;
				if (chartEvent is TimeSignature tse)
				{
					colVal = $"{tse.Signature.Numerator}/{tse.Signature.Denominator}";
					colX += ChartColumnInfo[(int)ChartColumns.TimeSignature].X;
				}
				else if (chartEvent is Stop stop)
				{
					colVal = $"{stop.LengthMicros / 1000000.0}";
					colX += ChartColumnInfo[(int)ChartColumns.Stop].X;
				}
				else if (chartEvent is TempoChange tc)
				{
					colVal = $"{tc.TempoBPM}";
					colX += ChartColumnInfo[(int) ChartColumns.BPM].X;
				}
				if (colVal != null)
				{
					StringBuilder.Append(
$@"			<p style=""position:absolute; top:{colY}px; left:{colX}px; width:{colW}px; height:{colH}px; z-index:10;"">{colVal}</p>
");
				}

				// Arrows
				if (chartEvent is LaneTapNote ltn)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					var foot = InvalidFoot;
					if (originalChart)
						foot = GetFootForArrow(ltn.Lane, ltn.Position, currentExpressedChartSearchNode);
					else
						foot = GetFootForArrow(ltn.Lane, ltn.Position, currentPerformedChartNode);
					WriteArrow(ltn.Lane, foot, firstLaneX, eventY, ltn.Position);
				}
				else if (chartEvent is LaneHoldStartNote lhsn)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					var foot = InvalidFoot;
					if (originalChart)
						foot = GetFootForArrow(lhsn.Lane, lhsn.Position, currentExpressedChartSearchNode);
					else
						foot = GetFootForArrow(lhsn.Lane, lhsn.Position, currentPerformedChartNode);
					WriteArrow(lhsn.Lane, foot, firstLaneX, eventY, lhsn.Position);

					lastHoldStarts[lhsn.Lane] = (int)eventY;
					lastHoldWasRoll[lhsn.Lane] = lhsn.SourceType == SMCommon.NoteChars[(int)SMCommon.NoteType.RollStart].ToString();
				}
				else if (chartEvent is LaneHoldEndNote lhen)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					WriteHold(lhen.Lane, firstLaneX, lastHoldStarts[lhen.Lane], eventY, lastHoldWasRoll[lhen.Lane]);
				}
				else if (chartEvent is LaneNote ln)
				{
					if (ln.SourceType == SMCommon.NoteChars[(int) SMCommon.NoteType.Mine].ToString())
					{
						// Write any expressed mine events for mines at this position.
						if (originalChart)
						{
							var mineEvents = new List<ExpressedChart.MineEvent>();
							while (currentExpressedMineIndex < ExpressedChart.MineEvents.Count
							       && ExpressedChart.MineEvents[currentExpressedMineIndex].Position <= chartEvent.Position)
							{
								mineEvents.Add(ExpressedChart.MineEvents[currentExpressedMineIndex]);
								currentExpressedMineIndex++;
							}

							if (mineEvents.Count > 0)
							{
								mineEvents = mineEvents.OrderBy(m => m.OriginalArrow).ToList();
							}
							WriteExpressedMines(mineEvents, eventY);
						}

						// Write the mine.
						WriteMine(ln.Lane, firstLaneX, eventY, ln.Position);
					}
				}
			}

			// Write the final measure markers
			var numMeasuresToWrite =
				chart.Layers[0].Events[chart.Layers[0].Events.Count - 1].Position.Measure
				- previousTimeSignaturePosition.Measure
				+ 1;
			WriteMeasures(
				chartXPosition,
					previousTimeSignatureY,
					yPerBeat,
					currentTimeSignature,
					previousTimeSignaturePosition.Measure,
					numMeasuresToWrite,
					chart.NumInputs);
		}

		private void WriteExpressedMines(List<ExpressedChart.MineEvent> mines, double y)
		{
			if (mines == null || mines.Count == 0)
				return;

			var mineSB = new StringBuilder();
			var first = true;
			foreach (var mine in mines)
			{
				if (!first)
					mineSB.Append("<br>");

				mineSB.Append($"{ArrowNames[mine.OriginalArrow]}: ");
				switch (mine.Type)
				{
					case MineType.NoArrow:
						mineSB.Append("No Association");
						break;
					case MineType.AfterArrow:
					{
						mineSB.Append($"After {FormatSequential(mine.ArrowIsNthClosest + 1)} Most Recent ");
						break;
					}
					case MineType.BeforeArrow:
					{
						mineSB.Append($"Before {FormatSequential(mine.ArrowIsNthClosest + 1)} Next ");
						break;
					}
				}
				if (mine.FootAssociatedWithPairedNote == L)
					mineSB.Append("(Foot: L)");
				else if (mine.FootAssociatedWithPairedNote == R)
					mineSB.Append("(Foot: R)");
				first = false;
			}
			var minesStr = mineSB.ToString();

			var x = ExpressedChartX + ExpressionColumnInfo[(int)ExpressionColumns.Mines].X;
			var h = ChartTextH * mines.Count;
			StringBuilder.Append(
$@"			<p style=""position:absolute; top:{(int)(y - h * .5)}px; left:{x}px; width:{MinesColW}px; height:{h}px; z-index:10;"">{minesStr}</p>
");
		}

		private string FormatSequential(int n)
		{
			string suffix;
			switch (n % 100)
			{
				case 11: suffix = "th"; break;
				case 12: suffix = "th"; break;
				case 13: suffix = "th"; break;
				default:
				{
					switch (n % 10)
					{
						case 1: suffix = "st"; break;
						case 2: suffix = "nd"; break;
						case 3: suffix = "rd"; break;
						default: suffix = "th"; break;
					}
					break;
				}
			}
			return n + suffix;
		}

		private void WriteExpression(ExpressedChart.ChartSearchNode node, double y)
		{
			if (node == null)
				return;
			var position = node.Position;
			while (node != null && node.Position == position)
			{
				if (node.PreviousLink != null && !node.PreviousLink.GraphLink.IsRelease())
				{
					var writeCost = false;
					for (var p = 0; p < NumFootPortions; p++)
					{
						// Left Foot
						if (LastExpressionPosition[L] < y && node.PreviousLink.GraphLink.Links[L, p].Valid)
						{
							var leftX = ExpressedChartX + ExpressionColumnInfo[(int)ExpressionColumns.LeftFoot].X;
							var stepStr = StepTypeStrings[(int)node.PreviousLink.GraphLink.Links[L, p].Step];
							if (node.PreviousLink.GraphLink.IsJump())
								stepStr = "[Jump] " + stepStr;
							StringBuilder.Append(
$@"			<p style=""position:absolute; top:{(int)(y - ChartTextH * .5)}px; left:{leftX}px; width:{ExpressionColW}px; height:{ChartTextH}px; z-index:10;"">{stepStr}</p>
");
							writeCost = true;
							LastExpressionPosition[L] = y;
						}

						// Right Foot
						if (LastExpressionPosition[R] < y && node.PreviousLink.GraphLink.Links[R, p].Valid)
						{
							var rightX = ExpressedChartX + ExpressionColumnInfo[(int)ExpressionColumns.RightFoot].X;
							var stepStr = StepTypeStrings[(int)node.PreviousLink.GraphLink.Links[R, p].Step];
							if (node.PreviousLink.GraphLink.IsJump())
								stepStr = "[Jump] " + stepStr;
							StringBuilder.Append(
$@"			<p style=""position:absolute; top:{(int)(y - ChartTextH * .5)}px; left:{rightX}px; width:{ExpressionColW}px; height:{ChartTextH}px; z-index:10;"">{stepStr}</p>
");
							writeCost = true;
							LastExpressionPosition[R] = y;
						}
					}

					// Cost
					if (writeCost)
					{
						var costX = ExpressedChartX + ExpressionColumnInfo[(int) ExpressionColumns.Cost].X;
						StringBuilder.Append(
$@"			<p style=""position:absolute; top:{(int) (y - ChartTextH * .5)}px; left:{costX}px; width:{CostColW}px; height:{ChartTextH}px; z-index:10;"">{node.Cost}</p>
");
					}
				}

				node = node.GetNextNode();
			}
		}

		private void WriteMine(int arrow, int firstLaneX, double y, MetricPosition position)
		{
			var x = firstLaneX + (arrow * ArrowW);
			var img = SrcDir + "mine.png";

			StringBuilder.Append(
$@"			<img src=""{img}"" style=""position:absolute; top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; width:{ArrowW}px; height:{ArrowW}px; z-index:{(int)y}; border:none;""/>
");
		}

		private int GetFootForArrow(int arrow, MetricPosition position, ExpressedChart.ChartSearchNode node)
		{
			while (node != null && node.Position == position)
			{
				if (node.PreviousLink != null && !node.PreviousLink.GraphLink.IsRelease())
				{
					// If this step is a footswap we need to ignore the other foot, which may still be resting on this arrow.
					var previousStepLink = node.GetPreviousStepLink();
					var footSwapFoot = InvalidFoot;
					var footSwapPortion = DefaultFootPortion;
					var footSwap = previousStepLink?.GraphLink.IsFootSwap(out footSwapFoot, out footSwapPortion) ?? false;
					if (footSwap && node.GraphNode.State[footSwapFoot, footSwapPortion].Arrow == arrow)
						return footSwapFoot;

					// No footswap on the given arrow.
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (node.GraphNode.State[f, p].Arrow == arrow)
							{
								return f;
							}
						}
					}
				}
				node = node.GetNextNode();
			}
			return InvalidFoot;
		}

		private int GetFootForArrow(int arrow, MetricPosition position, PerformedChart.PerformanceNode node)
		{
			while (node != null && node.Position == position)
			{
				if (node is PerformedChart.StepPerformanceNode spn)
				{
					var previousStepLink = spn.GraphLinkInstance;
					if (previousStepLink != null && !previousStepLink.GraphLink.IsRelease())
					{
						// If this step is a footswap we need to ignore the other foot, which may still be resting on this arrow.
						var footSwap = previousStepLink.GraphLink.IsFootSwap(out var footSwapFoot, out var footSwapPortion);
						if (footSwap && spn.GraphNodeInstance.Node.State[footSwapFoot, footSwapPortion].Arrow == arrow)
							return footSwapFoot;

						// No footswap on the given arrow.
						for (var f = 0; f < NumFeet; f++)
						{
							for (var p = 0; p < NumFootPortions; p++)
							{
								if (spn.GraphNodeInstance.Node.State[f, p].Arrow == arrow)
								{
									return f;
								}
							}
						}
					}
				}

				node = node.Next;
			}
			return InvalidFoot;
		}

		private void WriteArrow(int arrow, int foot, int firstLaneX, double y, MetricPosition position)
		{
			var classStr = ArrowClassStrings[arrow % 4];
			var x = firstLaneX + arrow * ArrowW;
			var fraction = position.SubDivision.Reduce();

			string img;
			switch (fraction.Denominator)
			{
				case 0:
				case 1: img = "1_4.png"; break;
				case 2: img = "1_8.png"; break;
				case 3: img = "1_12.png"; break;
				case 4: img = "1_16.png"; break;
				case 6: img = "1_24.png"; break;
				case 8: img = "1_32.png"; break;
				case 12: img = "1_48.png"; break;
				default: img = "1_64.png"; break;
			}
			img = SrcDir + img;

			// Arrow
			StringBuilder.Append(
$@"			<img src=""{img}""{classStr} style=""position:absolute; top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; width:{ArrowW}px; height:{ArrowW}px; z-index:{(int)y}; border:none;""/>
");
			// Foot indicator
			if (foot != InvalidFoot)
			{
				img = foot == L ? "l.png" : "r.png";
				img = SrcDir + img;
				StringBuilder.Append(
$@"			<img src=""{img}""{classStr} style=""position:absolute; top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; width:{ArrowW}px; height:{ArrowW}px; z-index:{(int)y}; border:none;""/>
");
			}
		}

		private void WriteHold(int arrow, int firstLaneX, double startY, double endY, bool roll)
		{
			var id = roll ? "rollbody" : "holdbody";
			var cap = roll ? "roll_cap.png" : "hold_cap.png";
			cap = SrcDir + cap;
			var x = firstLaneX + arrow * ArrowW;

			StringBuilder.Append(
$@"			<div id=""{id}"" style=""position:absolute; top:{(int)startY}px; left:{x}px; width:{ArrowW}px; height:{(int)(endY - startY)}px; z-index:{(int)startY - 1}; border:none;""></div>
");

			StringBuilder.Append(
$@"			<img src=""{cap}"" style=""position:absolute; top:{(int)endY}px; left:{x}px; width:{ArrowW}px; height:{HoldCapH}px; z-index:{(int)startY - 1}; border:none;""/>
");
		}

		private void WriteMeasures(int x, double startY, double yPerBeat, Fraction timeSignature, int currentMeasure, int numMeasures, int numArrows)
		{
			var barX = x;
			foreach (var chartCol in ChartColumnInfo)
				barX += chartCol.Width;
			var barW = numArrows * ArrowW;

			var mmX = x + ChartColumnInfo[(int)ChartColumns.Measure].X;
			var mmW = ChartColumnInfo[(int)ChartColumns.Measure].Width;
			var mmH = ChartTextH;

			for (var m = 0; m < numMeasures; m++)
			{
				for (var b = 0; b < timeSignature.Numerator; b++)
				{
					var y = startY + (m * timeSignature.Numerator + b) * yPerBeat;
					int barH;
					if (b == 0)
					{
						// Write measure number
						StringBuilder.Append(
$@"			<p style=""position:absolute; top:{(int)(y - mmH * .5)}px; left:{mmX}px; width:{mmW}px; height:{mmH}px; z-index:10;"">{currentMeasure + m}</p>
");
						barH = MeasureMarkerH;
						y -= MeasureMarkerH * 0.5;
					}
					else
					{
						barH = BeatMarkerH;
						y -= BeatMarkerH * 0.5;
					}

					// Write measure / beat marker
					StringBuilder.Append(
$@"			<div style=""position:absolute; top:{(int)y}px; left:{barX}px; width:{barW}px; height:{barH}px; background: #929292""></div>
");
				}
			}
		}

		private void WriteScript()
		{
			StringBuilder.Append(
$@"		<script>
			window.onscroll = function() {{ updateSticky() }};
			var chartHeaders = document.getElementById(""chartHeaders"");
			function updateSticky()
			{{
				if (window.pageYOffset >= {BannerH})
				{{
					chartHeaders.classList.add(""sticky"");
				}}
				else
				{{
					chartHeaders.classList.remove(""sticky"");
				}}
			}}
		</script>
");
		}
	}
}
