using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChartGenerator;
using Fumen.Converters;
using static ChartGenerator.Constants;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Tests for ExpressedChart.
	/// </summary>
	[TestClass]
	public class TestExpressedChart
	{
		/// <summary>
		/// SP StepGraph for running tests.
		/// </summary>
		private static readonly StepGraph SPGraph;

		/// <summary>
		/// Static initializer for creating the SP StepGraph.
		/// </summary>
		static TestExpressedChart()
		{
			SPGraph = StepGraph.CreateStepGraph(ArrowData.SPArrowData, P1L, P1R);
		}

		private string GetTestChartFullPath(string songFolder)
		{
			return $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\TestData\\{songFolder}\\test.sm";
		}

		private ExpressedChart Load(string smFile)
		{
			var song = SMReader.Load(smFile);
			var (expressedChart, _) = ExpressedChart.CreateFromSMEvents(song.Charts[0].Layers[0].Events, SPGraph);
			return expressedChart;
		}

		#region Helpers

		/// <summary>
		/// Helper method to assert that the FootArrowStates of a GraphLink match the expected
		/// single step information.
		/// </summary>
		/// <param name="links">FootArrowStates of the GraphLink.</param>
		/// <param name="foot">The foot which is expected to step.</param>
		/// <param name="step">The StepType that the foot is expected to perform.</param>
		/// <param name="action">The FootAction that fhe foot is expected to perform.</param>
		private void AssertLinksMatchStep(
			GraphLink.FootArrowState[,] links,
			int foot,
			StepType step,
			FootAction action)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					Assert.IsTrue(links[foot, a].Valid);
					Assert.AreEqual(step, links[foot, a].Step);
					Assert.AreEqual(action, links[foot, a].Action);
				}
				else
				{
					Assert.IsFalse(links[foot, a].Valid);
				}

				Assert.IsFalse(links[OtherFoot(foot), a].Valid);
			}
		}

		/// <summary>
		/// Helper method to assert that the FootArrowStates of a GraphLink match the expected
		/// jump information for both feet.
		/// </summary>
		/// <param name="links">FootArrowStates of the GraphLink.</param>
		/// <param name="leftStep">The left foot expected StepType.</param>
		/// <param name="leftAction">The left foot expected FootAction.</param>
		/// <param name="rightStep">The right foot expected StepType.</param>
		/// <param name="rightAction">The right foot expected FootAction.</param>
		private void AssertLinksMatchJump(
			GraphLink.FootArrowState[,] links,
			StepType leftStep,
			FootAction leftAction,
			StepType rightStep,
			FootAction rightAction)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					Assert.IsTrue(links[L, a].Valid);
					Assert.AreEqual(leftStep, links[L, a].Step);
					Assert.AreEqual(leftAction, links[L, a].Action);
					Assert.IsTrue(links[R, a].Valid);
					Assert.AreEqual(rightStep, links[R, a].Step);
					Assert.AreEqual(rightAction, links[R, a].Action);
				}
				else
				{
					Assert.IsFalse(links[L, a].Valid);
					Assert.IsFalse(links[R, a].Valid);
				}
			}
		}

		/// <summary>
		/// Helper method to assert that the FootArrowStates of a GraphLink match the expected
		/// bracket information for one foot.
		/// </summary>
		/// <param name="links">FootArrowStates of the GraphLink.</param>
		/// <param name="foot">The foot expected to perform the bracket.</param>
		/// <param name="step1">One of the expected StepTypes for the bracket.</param>
		/// <param name="action1">One of the expected FootActions for the bracket.</param>
		/// <param name="step2">The other expected StepType for the bracket.</param>
		/// <param name="action2">The other expected FootAction for the bracket.</param>
		private void AssertLinksMatchBracket(
			GraphLink.FootArrowState[,] links,
			int foot,
			StepType step1,
			FootAction action1,
			StepType step2,
			FootAction action2)
		{
			var matches = 0;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				Assert.IsTrue(links[foot, a].Valid);
				Assert.IsFalse(links[OtherFoot(foot), a].Valid);

				if ((step1 == links[foot, a].Step && action1 == links[foot, a].Action)
				    || (step2 == links[foot, a].Step && action2 == links[foot, a].Action))
					matches++;
			}
			Assert.AreEqual(2, matches);
		}

		/// <summary>
		/// Helper method to assert that the given MineEvent matches the expected configuration.
		/// </summary>
		/// <param name="mineEvent">MineEvent to check.</param>
		/// <param name="type">Expected MineType.</param>
		/// <param name="n">Expected value for the MineEvent's ArrowIsNthClosest value.</param>
		/// <param name="f">Expected value for the MineEvent's FootAssociatedWithPairedNote.</param>
		private void AssertMineEventMatches(ExpressedChart.MineEvent mineEvent, MineType type, int n, int f)
		{
			Assert.AreEqual(mineEvent.Type, type);
			Assert.AreEqual(mineEvent.ArrowIsNthClosest, n);
			Assert.AreEqual(mineEvent.FootAssociatedWithPairedNote, f);
		}

		/// <summary>
		/// Helper method to determine if the FootArrowStates of a GraphLink represent a single step
		/// with a given foot.
		/// </summary>
		/// <param name="links">FootArrowStates of the GraphLink.</param>
		/// <param name="foot">The foot to check.</param>
		/// <param name="step">The StepType to check.</param>
		/// <param name="action">The FootAction to check.</param>
		/// <returns>
		/// Whether or not this set of FootArrowStates represent a single step with the given foot
		/// </returns>
		private bool IsSingleStepWithFoot(GraphLink.FootArrowState[,] links, int foot, StepType step, FootAction action)
		{
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (a == 0)
				{
					if (!(links[foot, a].Valid && links[foot, a].Step == step && links[foot, a].Action == action))
						return false;
				}
				else
				{
					if (links[foot, a].Valid)
						return false;
				}

				if (links[OtherFoot(foot), a].Valid)
					return false;
			}

			return true;
		}

		#endregion Helpers

		/// <summary>
		/// Test that an empty chart results in an ExpressedChart with no events.
		/// </summary>
		[TestMethod]
		public void TestEmpty()
		{
			var ec = Load(GetTestChartFullPath("TestEmpty"));
			Assert.AreEqual(0, ec.StepEvents.Count);
			Assert.AreEqual(0, ec.MineEvents.Count);
		}

		#region Mines

		/// <summary>
		/// Mine which could be associated with arrows that occur both before or after
		/// the mine should prefer being associated with the arrow that occurs before.
		/// MineType.AfterArrow should be preferred to MineType.BeforeArrow.
		/// </summary>
		[TestMethod]
		public void TestMinesPreferAfterArrow()
		{
			var ec = Load(GetTestChartFullPath("TestMinesPreferAfterArrow"));
			Assert.AreEqual(8, ec.StepEvents.Count);
			Assert.AreEqual(12, ec.MineEvents.Count);
			var i = 0;

			// Mines which come before the first arrow in their lanes must be BeforeArrow.
			AssertMineEventMatches(ec.MineEvents[i++], MineType.BeforeArrow, 0, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.BeforeArrow, 1, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.BeforeArrow, 2, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.BeforeArrow, 3, R);

			// Mines which are both before and after other arrows in their lane should be AfterArrow.
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 0, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 1, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 2, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 3, L);

			// Mines which come after the last arrow in their lanes must be AfterArrow.
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 0, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 1, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 2, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 3, L);
		}

		/// <summary>
		/// Test that when no arrow is present in the lane of a mine, that mine is
		/// MineType.NoArrow.
		/// </summary>
		[TestMethod]
		public void TestMinesNoArrow()
		{
			var ec = Load(GetTestChartFullPath("TestMinesNoArrow"));
			Assert.AreEqual(0, ec.StepEvents.Count);
			Assert.AreEqual(4, ec.MineEvents.Count);
			var i = 0;

			// Mines which occur in lanes with no arrows should be NoArrow.
			AssertMineEventMatches(ec.MineEvents[i++], MineType.NoArrow, InvalidArrowIndex, InvalidFoot);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.NoArrow, InvalidArrowIndex, InvalidFoot);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.NoArrow, InvalidArrowIndex, InvalidFoot);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.NoArrow, InvalidArrowIndex, InvalidFoot);
		}

		/// <summary>
		/// Test that N values used to associate mines with the Nth most recent arrow
		/// treat arrows that are of equal distance with the same N value.
		/// </summary>
		[TestMethod]
		public void TestMinesNTies()
		{
			var ec = Load(GetTestChartFullPath("TestMinesNTies"));
			Assert.AreEqual(3, ec.StepEvents.Count);
			Assert.AreEqual(4, ec.MineEvents.Count);
			var i = 0;

			// Mines which occur in lanes with no arrows should be NoArrow.
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 2, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 1, L);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 1, R);
			AssertMineEventMatches(ec.MineEvents[i++], MineType.AfterArrow, 0, R);
		}

		#endregion Mines

		#region Simple Patterns

		/// <summary>
		/// Simple alternating roll patten.
		/// </summary>
		[TestMethod]
		public void TestSameArrowAlternating()
		{
			var ec = Load(GetTestChartFullPath("TestSameArrowAlternating"));
			Assert.AreEqual(8, ec.StepEvents.Count);
			var i = 0;

			// Simple alternating pattern
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
		}

		/// <summary>
		/// Simple jack pattern.
		/// </summary>
		[TestMethod]
		public void TestSameArrowJacks()
		{
			var ec = Load(GetTestChartFullPath("TestSameArrowJacks"));
			Assert.AreEqual(12, ec.StepEvents.Count);
			var i = 0;

			// Simple jack patterns
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
		}

		/// <summary>
		/// Simple stream pattern.
		/// </summary>
		[TestMethod]
		public void TestNewArrowStream()
		{
			var ec = Load(GetTestChartFullPath("TestNewArrowStream"));
			Assert.AreEqual(9, ec.StepEvents.Count);
			var i = 0;

			// Simple stream pattern
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Simple Patterns

		#region Crossovers

		/// <summary>
		/// Test crossovers with the left foot crossing over behind.
		/// </summary>
		[TestMethod]
		public void TestCrossoverLBehind()
		{
			var ec = Load(GetTestChartFullPath("TestCrossoverLBehind"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the left foot crossing over in front.
		/// </summary>
		[TestMethod]
		public void TestCrossoverLFront()
		{
			var ec = Load(GetTestChartFullPath("TestCrossoverLFront"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the right foot crossing over behind.
		/// </summary>
		[TestMethod]
		public void TestCrossoverRBehind()
		{
			var ec = Load(GetTestChartFullPath("TestCrossoverRBehind"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the right foot crossing over in front.
		/// </summary>
		[TestMethod]
		public void TestCrossoverRFront()
		{
			var ec = Load(GetTestChartFullPath("TestCrossoverRFront"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Crossovers

		#region Inverted Steps

		/// <summary>
		/// Test simple inverted patterns.
		/// </summary>
		[TestMethod]
		public void TestInversion()
		{
			var ec = Load(GetTestChartFullPath("TestInversion"));
			Assert.AreEqual(28, ec.StepEvents.Count);
			var i = 0;

			// Afronova walk, R over L, R leads
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.InvertBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, L over R, L leads
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.InvertBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, L over R, R leads
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.InvertFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, R over L, L leads
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.InvertFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Inverted Steps

		#region DoubleStep

		/// <summary>
		/// Test that double steps are preferred when nothing else will work.
		/// </summary>
		[TestMethod]
		public void TestDoubleStep()
		{
			var testFiles = new[]
			{
				"TestDoubleStep01",
				"TestDoubleStep02",
				"TestDoubleStep03",
				"TestDoubleStep04"
			};
			foreach (var file in testFiles)
			{
				var ec = Load(GetTestChartFullPath(file));
				Assert.AreEqual(9, ec.StepEvents.Count);

				// There are many equally valid ways to double step. It does not matter which foot is
				// chosen to perform the double step, or when, only that the minimum required number
				// are chosen. In all the TestDoubleStep charts there should only be 2.
				var numDoubleSteps = 0;
				var numTripleSteps = 0;
				for (var i = 1; i < ec.StepEvents.Count; i++)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						if ((IsSingleStepWithFoot(ec.StepEvents[i - 1].Link.Links, f, StepType.NewArrow, FootAction.Tap)
						     || IsSingleStepWithFoot(ec.StepEvents[i - 1].Link.Links, f, StepType.SameArrow, FootAction.Tap))
						    && IsSingleStepWithFoot(ec.StepEvents[i].Link.Links, f, StepType.NewArrow, FootAction.Tap))
						{
							numDoubleSteps++;
						}

						if (i >= 2
						    && (IsSingleStepWithFoot(ec.StepEvents[i - 2].Link.Links, f, StepType.NewArrow, FootAction.Tap)
						        || IsSingleStepWithFoot(ec.StepEvents[i - 2].Link.Links, f, StepType.SameArrow, FootAction.Tap))
						    && IsSingleStepWithFoot(ec.StepEvents[i - 1].Link.Links, f, StepType.NewArrow, FootAction.Tap)
						    && IsSingleStepWithFoot(ec.StepEvents[i].Link.Links, f, StepType.NewArrow, FootAction.Tap))
						{
							numTripleSteps++;
						}
					}
				}

				Assert.AreEqual(2, numDoubleSteps);
				Assert.AreEqual(0, numTripleSteps);
			}
		}

		/// <summary>
		/// Test that when a pattern involves two feed alternating on one arrow due to holds and
		/// a bracket is possible, we prefer the double step.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepHoldAlternateSameArrow()
		{
			var ec = Load(GetTestChartFullPath("TestDoubleStepHoldAlternateSameArrow"));
			Assert.AreEqual(7, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test that when holding with one foot the other foot will double step the other arrows,
		/// even if that means crossing over.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepLHold()
		{
			var ec = Load(GetTestChartFullPath("TestDoubleStepLHold"));
			Assert.AreEqual(34, ec.StepEvents.Count);
			var i = 0;

			// Double step with no crossovers
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Double step after jump
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Double step while holding even if that results in a crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Hold);

			// Double step while holding even if that results in a crossover.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
		}

		/// <summary>
		/// Test to ensure that double stepping is preferred over bracketing when one foot is held
		/// and the other alternates between two arrows.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepLHoldRepeatingPattern()
		{
			var ec = Load(GetTestChartFullPath("TestDoubleStepLHoldRepeatingPattern"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Hold and do a simple back and forth with the other foot. Could be bracketed but
			// prefer the double step.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test that when holding with one foot the other foot will double step the other arrows,
		/// even if that means crossing over.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepRHold()
		{
			var ec = Load(GetTestChartFullPath("TestDoubleStepRHold"));
			Assert.AreEqual(34, ec.StepEvents.Count);
			var i = 0;

			// Double step with no crossovers
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Double step after jump
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Double step while holding even if that results in a crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Hold);

			// Double step while holding even if that results in a crossover.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
		}

		/// <summary>
		/// Test to ensure that double stepping is preferred over bracketing when one foot is held
		/// and the other alternates between two arrows.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepRHoldRepeatingPattern()
		{
			var ec = Load(GetTestChartFullPath("TestDoubleStepRHoldRepeatingPattern"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Hold and do a simple back and forth with the other foot. Could be bracketed but
			// prefer the double step.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		#endregion DoubleStep

		#region FootSwap

		/// <summary>
		/// Test that patterns which could be performed with either a foot swap or a jack
		/// prefer the jack.
		/// </summary>
		[TestMethod]
		public void TestFootSwapPreferJack()
		{
			var ec = Load(GetTestChartFullPath("TestFootSwapPreferJack"));
			Assert.AreEqual(83, ec.StepEvents.Count);
			var i = 0;

			// Jack with R on up with a crossover. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jack with L on up with a crossover. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jack with R on down with a crossover. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jack with L on down with a crossover. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// L Jack into jump. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// R Jack into jump. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// Triple jack. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);

			// Pattern of triple jacks. Could be swapped, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);

			// Long pattern with a jack which could be a swap in the middle. Ambiguous, but prefer jack.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// Same pattern but mirrored.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);
		}

		/// <summary>
		/// Series of footswap tests.
		/// </summary>
		[TestMethod]
		public void TestFootSwap()
		{
			var ec = Load(GetTestChartFullPath("TestFootSwap"));
			Assert.AreEqual(75, ec.StepEvents.Count);
			var i = 0;

			// Swap on up and down starting on L. Jacks would results in inverted orientation.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on R. Jacks would results in inverted orientation.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on L. Jacks would results in double step.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on R. Jacks would results in double step.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap from a right foot crossover position.
			// This also checks for favoring a swap on bracketable arrows rather than further away arrows.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap from a left foot crossover position.
			// This also checks for favoring a swap on bracketable arrows rather than further away arrows.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap on a non-bracketable arrow with left foot.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap on a non-bracketable arrow with right foot.
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);
		}

		#endregion FootSwap

		#region Step After Jump

		/// <summary>
		/// Series of tests for stepping after a jump to a new arrow where both feet can bracket to the new
		/// arrow. The foot to use to hit the arrow depend on mine and hold indication.
		/// </summary>
		[TestMethod]
		public void TestJumpStepBothBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepBothBracketable"));
			Assert.AreEqual(31, ec.StepEvents.Count);
			var i = 0;

			// Normal Jump into ambiguous step with holds to help indicate footing.

			// Jump into ambiguous step with one foot held until the next note should prefer the foot not held (R).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot held until the next note should prefer the foot not held (L).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot released later should prefer the foot released sooner (R).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot released later should prefer the foot released sooner (L).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Normal Jump into ambiguous step with mines to help indicate footing.

			// Jump into ambiguous step with a mine following one foot at the time of the next step. Prefer that foot (R).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot at the time of the next step. Prefer that foot (L).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot before the next step. Prefer that foot (R).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot before the next step. Prefer that foot (L).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step mines after both. Prefer foot with closer mine (R).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step mines after both. Prefer foot with closer mine (L).
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Jump with both mine and hold indication. Mine is more important.

			// Left foot hold but mine indicated.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);

			// Right foot hold but mine indicated.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalL()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalL"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if the bracketable foot is held. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLHoldBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if the non-bracketable foot is held. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLHoldNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if it is followed by a mine, even if it is released later.
		/// Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldAndMineBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLHoldAndMineBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if it is followed by a mine, even if it is released later.
		/// Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldAndMineNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLHoldAndMineNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if it is followed by a mine.
		/// Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLMineBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLMineBracketable"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if it is followed by a mine.
		/// Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLMineNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalLMineNormal"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalR()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalR"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if the bracketable foot is held. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRHoldBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if the non-bracketable foot is held. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRHoldNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if it is followed by a mine, even if it is released later.
		/// Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldAndMineBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRHoldAndMineBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if it is followed by a mine, even if it is released later.
		/// Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldAndMineNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRHoldAndMineNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if it is followed by a mine.
		/// Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRMineBracketable()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRMineBracketable"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if it is followed by a mine.
		/// Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRMineNormal()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneNormalRMineNormal"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Series of tests for stepping after a jump to a new arrow where one foot can bracket to the new
		/// arrow and the other foot would need to crossover to reach the new arrow. Avoid the crossover
		/// unless the bracketable foot is held.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneCrossover()
		{
			var ec = Load(GetTestChartFullPath("TestJumpStepOneBracketableOneCrossover"));
			Assert.AreEqual(45, ec.StepEvents.Count);
			var i = 0;

			// If holding until the step, prefer the crossover.

			// L step.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// L crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.CrossoverBehind, FootAction.Tap);
			// R crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.CrossoverFront, FootAction.Tap);
			// R step.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// If holding but released before the step, prefer the step.

			// L step after R hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// R step after R hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			// L step after L hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// R step after L hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// For mines, it is more natural to ignore the mine to avoid the crossover.
			// This is subjective.

			// Mine at position of next step. L alternate normally.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. R alternate instead of L behind crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. L alternate instead of R in front crossover.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. R alternate normally.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);

			// Holds and mines on the same foot. In all cases, avoid the crossover

			// L step after L hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// R step after L hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
			// L step after R hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			// R step after R hold.
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Release);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Step After Jump

		#region Jumps

		/// <summary>
		/// Test that jumps involving one foot landing where the other foot start are performed
		/// as expected without brackets.
		/// </summary>
		[TestMethod]
		public void TestJumpFootSwap()
		{
			var ec = Load(GetTestChartFullPath("TestJumpFootSwap"));
			Assert.AreEqual(17, ec.StepEvents.Count);
			var i = 0;

			// Circular jump pattern around all arrows
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);

			// Back and forth pattern
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchJump(ec.StepEvents[i++].Link.Links, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Jumps

		#region Miscellaneous

		/// <summary>
		/// Test that if there is a choice between a jump that starts crossed over and ends on two new arrows
		/// that aren't crossed over, that we instead prefer footswapping on a previous step and bracketing.
		/// </summary>
		[TestMethod]
		public void TestFootSwapToAvoidCrossoverAndNewArrowNewArrowJump()
		{
			var ec = Load(GetTestChartFullPath("TestFootSwapToAvoidCrossoverAndNewArrowNewArrowJump"));
			Assert.AreEqual(7, ec.StepEvents.Count);
			var i = 0;

			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.SameArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, R, StepType.FootSwap, FootAction.Tap);
			AssertLinksMatchStep(ec.StepEvents[i++].Link.Links, L, StepType.NewArrow, FootAction.Tap);
			AssertLinksMatchBracket(ec.StepEvents[i++].Link.Links, R, StepType.BracketBothNew, FootAction.Hold, StepType.BracketBothNew, FootAction.Hold);
			AssertLinksMatchBracket(ec.StepEvents[i++].Link.Links, R, StepType.BracketBothSame, FootAction.Release, StepType.BracketBothSame, FootAction.Release);
		}

		#endregion Miscellaneous
	}
}
