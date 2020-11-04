using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fumen;
using Fumen.Converters;

namespace GenDoublesStaminaCharts
{
	// WIP
	class Program
	{
		

		static void Main(string[] args)
		{
			var stepGraph = new StepGraph();
			//var spGraph = stepGraph.CreateSPStepGraph();
			var dpGraph = stepGraph.CreateDPStepGraph();

			var song = Fumen.Converters.SMReader.Load(
				@"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey.sm");
			AddDoublesCharts(song);
			//Fumen.Converters.SMWriter.Save(song, @"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey_2.sm");
		}


		static void AddDoublesCharts(Song song)
		{
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count == 1
				    && chart.Type == SMCommon.ChartType.dance_single.ToString()
				    && chart.NumPlayers == 1
				    && chart.NumInputs == 4)
				{
					//chart.Layers[0].Events
				}
			}
		}
	}
}
