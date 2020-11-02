using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fumen;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	public class ExpressedChartEvent
	{
		public MetricPosition Position;
	}

	public class StepEvent : ExpressedChartEvent
	{
		//public StepType Type;
		public GraphLink Link;
	}

	public class MineEvent : ExpressedChartEvent
	{
		public MineType Type;
		public FootAction Action;
	}

	public class ExpressedChart
	{
		public List<StepEvent> StepEvents;
		public List<MineEvent> MineEvents;
	}
}
