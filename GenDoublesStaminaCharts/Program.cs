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
		private const int P1L = 0;
		private const int P1D = 1;
		private const int P1U = 2;
		private const int P1R = 3;
		private const int P2L = 4;
		private const int P2D = 5;
		private const int P2U = 6;
		private const int P2R = 7;


		class Position
		{
			private static readonly Random randomStep = new Random();

			public int L;
			public int R;
			public List<Position> NextL = new List<Position>();
			public List<Position> NextR = new List<Position>();

			public Position GetNextL(int nextL)
			{
				foreach (var pos in NextL)
				{
					if (pos.L == nextL && pos.R == R)
						return pos;
				}
				return null;
			}

			public Position GetNextR(int nextR)
			{
				foreach (var pos in NextR)
				{
					if (pos.L == L && pos.R == nextR)
						return pos;
				}
				return null;
			}

			public Position ChooseRandomNextL()
			{
				return NextL[randomStep.Next(0, NextL.Count)];
			}

			public Position ChooseRandomNextR()
			{
				return NextR[randomStep.Next(0, NextR.Count)];
			}
		}

		private static void InitDoublesStepGraph()
		{
			// Valid positions
			var pLP1LRP1D = new Position {L = P1L, R = P1D};
			var pLP1LRP1U = new Position {L = P1L, R = P1U};
			var pLP1LRP1R = new Position {L = P1L, R = P1R};
			var pLP1DRP1U = new Position {L = P1D, R = P1U};
			var pLP1DRP1R = new Position {L = P1D, R = P1R};
			var pLP1DRP2L = new Position {L = P1D, R = P2L};
			var pLP1URP1R = new Position {L = P1U, R = P1R};
			var pLP1URP1D = new Position {L = P1U, R = P1D};
			var pLP1URP2L = new Position {L = P1U, R = P2L};
			var pLP1RRP2L = new Position {L = P1R, R = P2L};
			var pLP1RRP2D = new Position {L = P1R, R = P2D};
			var pLP1RRP2U = new Position {L = P1R, R = P2U};
			var pLP2LRP2D = new Position {L = P2L, R = P2D};
			var pLP2LRP2U = new Position {L = P2L, R = P2U};
			var pLP2LRP2R = new Position {L = P2L, R = P2R};
			var pLP2DRP2U = new Position {L = P2D, R = P2U};
			var pLP2DRP2R = new Position {L = P2D, R = P2R};
			var pLP2URP2R = new Position {L = P2U, R = P2R};
			var pLP2URP2D = new Position {L = P2U, R = P2D};

			// Valid steps
			pLP1LRP1D.NextL = new List<Position> { pLP1LRP1D, pLP1URP1D };
			pLP1LRP1D.NextR = new List<Position> { pLP1LRP1D, pLP1LRP1U, pLP1LRP1R };

			pLP1LRP1U.NextL = new List<Position> { pLP1LRP1U, pLP1DRP1U };
			pLP1LRP1U.NextR = new List<Position> { pLP1LRP1U, pLP1LRP1D, pLP1LRP1R };

			pLP1LRP1R.NextL = new List<Position> { pLP1LRP1R, pLP1DRP1R, pLP1URP1R };
			pLP1LRP1R.NextR = new List<Position> { pLP1LRP1R, pLP1LRP1U, pLP1LRP1D };

			pLP1DRP1U.NextL = new List<Position> { pLP1DRP1U, pLP1LRP1U };
			pLP1DRP1U.NextR = new List<Position> { pLP1DRP1U, pLP1DRP1R };

			pLP1DRP1R.NextL = new List<Position> { pLP1DRP1R, pLP1URP1R, pLP1LRP1R };
			pLP1DRP1R.NextR = new List<Position> { pLP1DRP1R, pLP1DRP1U, pLP1DRP2L };

			pLP1DRP2L.NextL = new List<Position> { pLP1DRP2L, pLP1RRP2L };
			pLP1DRP2L.NextR = new List<Position> { pLP1DRP2L, pLP1DRP1R };

			pLP1URP1R.NextL = new List<Position> { pLP1URP1R, pLP1LRP1R, pLP1DRP1R };
			pLP1URP1R.NextR = new List<Position> { pLP1URP1R, pLP1URP1D, pLP1URP2L };

			pLP1URP1D.NextL = new List<Position> { pLP1URP1D, pLP1LRP1D };
			pLP1URP1D.NextR = new List<Position> { pLP1URP1D, pLP1LRP1R };

			pLP1URP2L.NextL = new List<Position> { pLP1URP2L, pLP1RRP2L };
			pLP1URP2L.NextR = new List<Position> { pLP1URP2L, pLP1URP1R };

			pLP1RRP2L.NextL = new List<Position> { pLP1RRP2L, pLP1URP2L, pLP1DRP2L };
			pLP1RRP2L.NextR = new List<Position> { pLP1RRP2L, pLP1RRP2U, pLP1RRP2D };

			pLP1RRP2D.NextL = new List<Position> { pLP1RRP2D, pLP2LRP2D };
			pLP1RRP2D.NextR = new List<Position> { pLP1RRP2D, pLP1RRP2L };

			pLP1RRP2U.NextL = new List<Position> { pLP1RRP2U, pLP2LRP2U };
			pLP1RRP2U.NextR = new List<Position> { pLP1RRP2U, pLP1RRP2L };

			pLP2LRP2D.NextL = new List<Position> { pLP2LRP2D, pLP1RRP2D, pLP2URP2D };
			pLP2LRP2D.NextR = new List<Position> { pLP2LRP2D, pLP2LRP2R, pLP2LRP2U };

			pLP2LRP2U.NextL = new List<Position> { pLP2LRP2U, pLP1RRP2U, pLP2DRP2U };
			pLP2LRP2U.NextR = new List<Position> { pLP2LRP2U, pLP2LRP2D, pLP2LRP2R };

			pLP2LRP2R.NextL = new List<Position> { pLP2LRP2R, pLP2DRP2R, pLP2URP2R };
			pLP2LRP2R.NextR = new List<Position> { pLP2LRP2R, pLP2LRP2D, pLP2LRP2U };

			pLP2DRP2U.NextL = new List<Position> { pLP2DRP2U, pLP2LRP2U };
			pLP2DRP2U.NextR = new List<Position> { pLP2DRP2U, pLP2DRP2R };

			pLP2URP2R.NextL = new List<Position> { pLP2URP2R, pLP2LRP2R, pLP2DRP2R };
			pLP2URP2R.NextR = new List<Position> { pLP2URP2R, pLP2LRP2D };

			pLP2URP2D.NextL = new List<Position> { pLP2URP2D, pLP2LRP2R };
			pLP2URP2D.NextR = new List<Position> { pLP2URP2D, pLP2URP2R };

			doublesRoot = pLP1RRP2L;
		}

		private static void InitSinglesStepGraph()
		{
			// Valid positions
			var pLLRD = new Position { L = P1L, R = P1D };
			var pLLRU = new Position { L = P1L, R = P1U };
			var pLLRR = new Position { L = P1L, R = P1R };
			var pLDRU = new Position { L = P1D, R = P1U };
			var pLDRR = new Position { L = P1D, R = P1R };
			var pLURR = new Position { L = P1U, R = P1R };
			var pLURD = new Position { L = P1U, R = P1D };

			// Valid steps
			pLLRD.NextL = new List<Position> { pLLRD, pLURD };
			pLLRD.NextR = new List<Position> { pLLRD, pLLRR, pLLRU };

			pLLRU.NextL = new List<Position> { pLLRU, pLDRU };
			pLLRU.NextR = new List<Position> { pLLRU, pLLRR, pLLRD };

			pLLRR.NextL = new List<Position> { pLLRR, pLURR, pLDRR };
			pLLRR.NextL = new List<Position> { pLLRR, pLLRD, pLLRU };

			pLDRU.NextL = new List<Position> { pLDRU, pLLRU };
			pLDRU.NextR = new List<Position> { pLDRU, pLDRR };

			pLDRR.NextL = new List<Position> { pLDRR, pLLRR, pLURR };
			pLDRR.NextR = new List<Position> { pLDRR, pLDRU };

			pLURR.NextL = new List<Position> { pLURR, pLLRR, pLDRR };
			pLURR.NextR = new List<Position> { pLURR, pLURD };

			pLURD.NextL = new List<Position> { pLURD, pLLRD };
			pLURD.NextR = new List<Position> { pLURD, pLURR };

			singlesRoot = pLLRR;
		}

		private static Position doublesRoot;
		private static Position singlesRoot;

		enum StepType
		{
			L,
			R,
			Jump
		}

		private class Step
		{
			public MetricPosition Postion { get; set; }

		}

		private enum SingleStepType
		{
			Step,
			HoldStart,
			HoldEnd,
			RollStart,
			RollEnd,
			Fake,
			Mine,
		}

		private enum JumpIndividualStepType
		{
			Step,
			HoldStart,
			RollStart,
		}

		private enum JumpType
		{
			BothSame,
			OnlyLeftSame,
			OnlyRightSame,
			// TODO: Neither Same? There are some doubles jumps of this type which shouldn't be preserved:
			// e.g. both middles.
		}

		private class SingleStep : Step
		{
			public bool Left { get; set; }
			public SingleStepType Type { get; set; }
			public int LRUIndex { get; set; }
		}

		private class JumpStep : Step
		{
			public JumpType JumpStepType { get; set; }
			public JumpIndividualStepType LeftStepType { get; set; }
			public JumpIndividualStepType RightStepType { get; set; }
		}

		static void Main(string[] args)
		{
			var song = Fumen.Converters.SMReader.Load(
				@"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey.sm");
            AddDoublesStaminaCharts(song);
			Fumen.Converters.SMWriter.Save(song, @"C:\Users\perry\Sync\Temp\Hey Sexy Lady (Skrillex Remix)\hey_2.sm");
		}

		static List<LaneNote> GetNextSteps(ref int index, List<Event> events)
		{
			// ignore mines, keysounds, lifts, etc
			// sort by hold releases first

			var steps = new List<LaneNote>();
			while (true)
			{
				if (index == events.Count)
					break;


				index++;
			}
		}

		static LaneNote AddStep(LaneNote singlesStep, int doublesPosition, Dictionary<int, int> singleHoldLanesToDoublesHoldLanes)
		{
			if (singlesStep is LaneTapNote tapNote)
			{
				var doublesNote = new LaneTapNote(tapNote);
				doublesNote.Lane = doublesPosition;
				return doublesNote;
			}
			else if (singlesStep is LaneHoldStartNote holdNote)
			{
				var doublesNote = new LaneHoldStartNote(holdNote);
				doublesNote.Lane = doublesPosition;
				singleHoldLanesToDoublesHoldLanes[singlesStep.Lane] = doublesPosition;
				return doublesNote;
			}
			else if (singlesStep is LaneHoldEndNote holdEndNote)
			{
				var doublesNote = new LaneHoldEndNote(holdEndNote);
				doublesNote.Lane = doublesPosition;
				singleHoldLanesToDoublesHoldLanes.Remove(singlesStep.Lane);
				return doublesNote;
			}

			return null;
		}

		static void HandleStep(
			LaneNote step,
			ref Position singlesPosition,
			ref Position doublesPosition,
			List<Event> doublesEvents,
			StepType lastStepType,
			Dictionary<int, int> singleHoldLanesToDoublesHoldLanes)
		{
			bool bCanAddSteps = singleHoldLanesToDoublesHoldLanes.Count < 2;

			// Left foot jack
			if (step.Lane == singlesPosition.L)
			{
				if (bCanAddSteps)
					doublesEvents.Add(new LaneTapNote());
			}
			// Right foot jack
			else if (step.Lane == singlesPosition.R)
			{
				if (bCanAddSteps)
					doublesEvents.Add(new LaneTapNote());
			}
			// Normal step
			else
			{
				// Step after a jump where the step is not in either lane from the jump
				// Need to look ahead to see if this is best interpreted as a left foot or right foot
				if (lastStepType == StepType.Jump)
				{

				}

				// Last step was with L, try to interpret this as a step with R
				else if (lastStepType == StepType.L)
				{
					var nextSinglesPos = singlesPosition.GetNextR(step.Lane);
					if (null != nextSinglesPos && bCanAddSteps)
					{
						// This step is a valid R step
						doublesPosition = doublesPosition.ChooseRandomNextR();
						AddStep(step, doublesPosition.R, singleHoldLanesToDoublesHoldLanes);
					}
					else
					{
						// Failed to interpret this as a R step, try L
						// This will never fail for a singles chart.
						nextSinglesPos = singlesPosition.GetNextL(step.Lane);

						// This step is a valid L step
						if (bCanAddSteps)
						{
							doublesPosition = doublesPosition.ChooseRandomNextL();
							AddStep(step, doublesPosition.L, singleHoldLanesToDoublesHoldLanes);
						}
					}

					// This will never be null 
					if (null != nextSinglesPos)
						singlesPosition = nextSinglesPos;
				}

				else if (lastStepType == StepType.R)
				{

				}
			}
		}

		static void AddDoublesStaminaCharts(Song song)
		{
			foreach (var chart in song.Charts)
			{
				if (chart.Layers.Count > 0 && chart.Type == SMCommon.ChartType.dance_single.ToString() && chart.NumPlayers == 1 &&
				    chart.NumInputs == 4)
				{



					Chart doublesChart = new Chart();
					doublesChart.Layers.Add(new Layer());
					var doublesEvents = doublesChart.Layers[0].Events;

					var doublesPosition = doublesRoot;

					var singlesPosition = singlesRoot;
					var singleHoldLanesToDoublesHoldLanes = new Dictionary<int, int>();

					var lastStepType = StepType.Jump;
					var index = 0;

					while (true)
					{
						var steps = GetNextSteps(ref index, chart.Layers[0].Events);

						// make sure steps are sorted by hold releases first

						if (steps.Count == 1)
						{
							var step = steps[0];

							if (step is LaneHoldEndNote holdEnd)
							{
								if (singleHoldLanesToDoublesHoldLanes.ContainsKey(step.Lane))
								{
									var doublesLane = singleHoldLanesToDoublesHoldLanes[step.Lane];
									AddStep(step, doublesLane, singleHoldLanesToDoublesHoldLanes);
								}
							}
							else if (step is LaneHoldStartNote  || step is LaneTapNote)
							{
								HandleStep(step, ref singlesPosition, ref doublesPosition, doublesEvents, lastStepType,
									singleHoldLanesToDoublesHoldLanes);
							}
						}

						else
						{

						}
					}
				}
			}
		}
	}
}
