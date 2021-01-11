using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChartGenerator;
using Fumen;
using Fumen.Converters;
using static ChartGenerator.Constants;
using System.Threading.Tasks;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Tests for ExpressedChart.
	/// See also ExpressedChartTestGenerator, which generates code that references this class.
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

		/// <summary>
		/// Gets the path with extension to a test sm file in the given folder.
		/// </summary>
		/// <param name="songFolder">Name of the folder containing the test sm file.</param>
		/// <param name="smFile">
		/// Optional sm file name without extension.
		/// Defaults to "test".
		/// </param>
		/// <returns>String representation of path to sm file with extension.</returns>
		public static string GetTestChartPath(string songFolder, string smFile = "test")
		{
			return $@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\ChartGeneratorTests\TestData\{songFolder}\{smFile}.sm";
		}

		/// <summary>
		/// Load an sm file and return the ExpressedChart representation.
		/// </summary>
		/// <param name="smFile">SM file with path and extension.</param>
		/// <param name="chartDifficultyType">
		/// Optional difficulty type string of chart in SM file to load.
		/// If omitted, the first chart found will be used.
		/// </param>
		/// <returns></returns>
		public static ExpressedChart Load(string smFile, string chartDifficultyType = null)
		{
			Song song = null;
			Task.Run(async () => { song = await new SMReader(smFile).Load(); }).Wait();

			// Default to the first chart.
			List<Event> events = song.Charts[0].Layers[0].Events;
			
			// Use the specified chart, if present.
			if (chartDifficultyType != null)
			{
				foreach (var chart in song.Charts)
				{
					if (chart.DifficultyType == chartDifficultyType)
					{
						events = chart.Layers[0].Events;
						break;
					}
				}
			}

			// Create the expressed chart.
			var (expressedChart, _) = ExpressedChart.CreateFromSMEvents(events, SPGraph);
			return expressedChart;
		}

		#region Helpers
		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches the expected
		/// single step information.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="foot">The foot which is expected to step.</param>
		/// <param name="step">The StepType that the foot is expected to perform.</param>
		/// <param name="action">The FootAction that fhe foot is expected to perform.</param>
		/// <param name="roll">Whether or not the action is additionally a roll.</param>
		public static void AssertLinkMatchesStep(
			GraphLinkInstance link,
			int foot,
			StepType step,
			FootAction action,
			bool roll = false)
		{
			var links = link.GraphLink.Links;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					Assert.IsTrue(links[foot, p].Valid);
					Assert.AreEqual(step, links[foot, p].Step);
					Assert.AreEqual(action, links[foot, p].Action);
					Assert.AreEqual(roll, link.Rolls[foot, p]);
				}
				else
				{
					Assert.IsFalse(links[foot, p].Valid);
				}

				Assert.IsFalse(links[OtherFoot(foot), p].Valid);
			}
		}

		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches the expected
		/// jump information for both feet.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="leftStep">The left foot expected StepType.</param>
		/// <param name="leftAction">The left foot expected FootAction.</param>
		/// <param name="rightStep">The right foot expected StepType.</param>
		/// <param name="rightAction">The right foot expected FootAction.</param>
		/// <param name="leftRoll">Whether or not the left foot action is additionally a roll.</param>
		/// <param name="rightRoll">Whether or not the right foot action is additionally a roll.</param>
		public static void AssertLinkMatchesJump(
			GraphLinkInstance link,
			StepType leftStep,
			FootAction leftAction,
			StepType rightStep,
			FootAction rightAction,
			bool leftRoll = false,
			bool rightRoll = false)
		{
			var links = link.GraphLink.Links;

			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					Assert.IsTrue(links[L, p].Valid);
					Assert.AreEqual(leftStep, links[L, p].Step);
					Assert.AreEqual(leftAction, links[L, p].Action);
					Assert.AreEqual(leftRoll, link.Rolls[L, p]);
					Assert.IsTrue(links[R, p].Valid);
					Assert.AreEqual(rightStep, links[R, p].Step);
					Assert.AreEqual(rightAction, links[R, p].Action);
					Assert.AreEqual(rightRoll, link.Rolls[R, p]);
				}
				else
				{
					Assert.IsFalse(links[L, p].Valid);
					Assert.IsFalse(links[R, p].Valid);
				}
			}
		}

		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches the expected
		/// bracket information for one foot.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="foot">The foot expected to perform the bracket.</param>
		/// <param name="heelStep">Expected StepType for the Heel.</param>
		/// <param name="heelAction">Expected FootAction for the Heel.</param>
		/// <param name="toeStep">Expected StepType for the Toe.</param>
		/// <param name="toeAction">Expected FootAction for the Toe.</param>
		/// <param name="heelRoll">Whether or not the Heel foot action is additionally a roll.</param>
		/// <param name="toeRoll">Whether or not the Toe foot action is additionally a roll.</param>
		public static void AssertLinkMatchesBracket(
			GraphLinkInstance link,
			int foot,
			StepType heelStep,
			FootAction heelAction,
			StepType toeStep,
			FootAction toeAction,
			bool heelRoll = false,
			bool toeRoll = false)
		{
			var links = link.GraphLink.Links;

			for (var p = 0; p < NumFootPortions; p++)
			{
				Assert.IsTrue(links[foot, p].Valid);
				Assert.IsFalse(links[OtherFoot(foot), p].Valid);
			}

			Assert.AreEqual(heelStep, links[foot, Heel].Step);
			Assert.AreEqual(heelAction, links[foot, Heel].Action);
			Assert.AreEqual(heelRoll, link.Rolls[foot, Heel]);
			Assert.AreEqual(toeStep, links[foot, Toe].Step);
			Assert.AreEqual(toeAction, links[foot, Toe].Action);
			Assert.AreEqual(toeRoll, link.Rolls[foot, Toe]);
		}

		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches the expected
		/// quad information.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="leftHeelStep">Expected StepType for the Left Heel.</param>
		/// <param name="leftHeelAction">Expected FootAction for the Left Heel.</param>
		/// <param name="leftToeStep">Expected StepType for the Left Toe.</param>
		/// <param name="leftToeAction">Expected FootAction for the Left Toe.</param>
		/// <param name="rightHeelStep">Expected StepType for the Right Heel.</param>
		/// <param name="rightHeelAction">Expected FootAction for the Right Heel.</param>
		/// <param name="rightToeStep">Expected StepType for the Right Toe.</param>
		/// <param name="rightToeAction">Expected FootAction for the Right Toe.</param>
		/// <param name="leftHeelRoll">Whether or not the Left Heel foot action is additionally a roll.</param>
		/// <param name="leftToeRoll">Whether or not the Left Toe foot action is additionally a roll.</param>
		/// <param name="rightHeelRoll">Whether or not the Right Heel foot action is additionally a roll.</param>
		/// <param name="rightToeRoll">Whether or not the Right Toe foot action is additionally a roll.</param>
		public static void AssertLinkMatchesQuad(
			GraphLinkInstance link,
			StepType leftHeelStep,
			FootAction leftHeelAction,
			StepType leftToeStep,
			FootAction leftToeAction,
			StepType rightHeelStep,
			FootAction rightHeelAction,
			StepType rightToeStep,
			FootAction rightToeAction,
			bool leftHeelRoll = false,
			bool leftToeRoll = false,
			bool rightHeelRoll = false,
			bool rightToeRoll = false)
		{
			var links = link.GraphLink.Links;

			Assert.IsTrue(links[L, Heel].Valid);
			Assert.IsTrue(links[L, Toe].Valid);
			Assert.IsTrue(links[R, Heel].Valid);
			Assert.IsTrue(links[R, Toe].Valid);

			Assert.AreEqual(leftHeelStep, links[L, Heel].Step);
			Assert.AreEqual(leftHeelAction, links[L, Heel].Action);
			Assert.AreEqual(leftHeelRoll, link.Rolls[L, Heel]);
			Assert.AreEqual(leftToeStep, links[L, Toe].Step);
			Assert.AreEqual(leftToeAction, links[L, Toe].Action);
			Assert.AreEqual(leftToeRoll, link.Rolls[L, Toe]);
			Assert.AreEqual(rightHeelStep, links[R, Heel].Step);
			Assert.AreEqual(rightHeelAction, links[R, Heel].Action);
			Assert.AreEqual(rightHeelRoll, link.Rolls[R, Heel]);
			Assert.AreEqual(rightToeStep, links[R, Toe].Step);
			Assert.AreEqual(rightToeAction, links[R, Toe].Action);
			Assert.AreEqual(rightToeRoll, link.Rolls[R, Toe]);
		}

		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches the expected
		/// single step information with explicit foot portion. Can be used for
		/// e.g. a bracket release on a toe.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="foot">The foot expected to perform the step.</param>
		/// <param name="footPortion">The portion of the foot expected to perform the step.</param>
		/// <param name="step">The StepType that the foot is expected to perform.</param>
		/// <param name="action">The FootAction that fhe foot is expected to perform.</param>
		/// <param name="roll">Whether or not the action is additionally a roll.</param>
		public static void AssertLinkMatchesOneStep(
			GraphLinkInstance link,
			int foot,
			int footPortion,
			StepType step,
			FootAction action,
			bool roll)
		{
			var links = link.GraphLink.Links;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (f == foot && p == footPortion)
					{
						Assert.IsTrue(links[f, p].Valid);
						Assert.AreEqual(step, links[f, p].Step);
						Assert.AreEqual(action, links[f, p].Action);
						Assert.AreEqual(roll, link.Rolls[f, p]);
					}
					else
					{
						Assert.IsFalse(links[f, p].Valid);
					}
				}
			}
		}

		/// <summary>
		/// Helper method to assert that a GraphLinkInstance matches two expected
		/// steps with explicit feet and foot portion. Can be used for
		/// e.g. bracket releases on a toes.
		/// </summary>
		/// <param name="foot1">Step 1. The foot expected to perform the step.</param>
		/// <param name="footPortion1">Step 1. The portion of the foot expected to perform the step.</param>
		/// <param name="step1">Step 1. The StepType that the foot is expected to perform.</param>
		/// <param name="action1">Step 1. The FootAction that fhe foot is expected to perform.</param>
		/// <param name="roll1">Step 1. Whether or not the action is additionally a roll.</param>
		/// <param name="foot2">Step 2. The foot expected to perform the step.</param>
		/// <param name="footPortion2">Step 2. The portion of the foot expected to perform the step.</param>
		/// <param name="step2">Step 2. The StepType that the foot is expected to perform.</param>
		/// <param name="action2">Step 2. The FootAction that fhe foot is expected to perform.</param>
		/// <param name="roll2">Step 2. Whether or not the action is additionally a roll.</param>
		public static void AssertLinkMatchesTwoSteps(
			GraphLinkInstance link,
			int foot1,
			int footPortion1,
			StepType step1,
			FootAction action1,
			bool roll1,
			int foot2,
			int footPortion2,
			StepType step2,
			FootAction action2,
			bool roll2)
		{
			var links = link.GraphLink.Links;

			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (f == foot1 && p == footPortion1)
					{
						Assert.IsTrue(links[f, p].Valid);
						Assert.AreEqual(step1, links[f, p].Step);
						Assert.AreEqual(action1, links[f, p].Action);
						Assert.AreEqual(roll1, link.Rolls[f, p]);
					}
					else if (f == foot2 && p == footPortion2)
					{
						Assert.IsTrue(links[f, p].Valid);
						Assert.AreEqual(step2, links[f, p].Step);
						Assert.AreEqual(action2, links[f, p].Action);
						Assert.AreEqual(roll2, link.Rolls[f, p]);
					}
					else
					{
						Assert.IsFalse(links[f, p].Valid);
					}
				}
			}
		}

		/// <summary>
		/// Assert that every foot and every portion from a GraphLinkInstance
		/// match expectations.
		/// Used by automatically generated tests.
		/// </summary>
		public static void AssertLinkMatchesFullInformation(
			GraphLinkInstance link,
			bool validLH,
			StepType stepLH,
			FootAction actionLH,
			bool rollLH,
			bool validLT,
			StepType stepLT,
			FootAction actionLT,
			bool rollLT,
			bool validRH,
			StepType stepRH,
			FootAction actionRH,
			bool rollRH,
			bool validRT,
			StepType stepRT,
			FootAction actionRT,
			bool rollRT)
		{
			var links = link.GraphLink.Links;

			Assert.AreEqual(validLH, links[L, Heel].Valid);
			Assert.AreEqual(stepLH, links[L, Heel].Step);
			Assert.AreEqual(actionLH, links[L, Heel].Action);
			Assert.AreEqual(rollLH, link.Rolls[L, Heel]);

			Assert.AreEqual(validLT, links[L, Toe].Valid);
			Assert.AreEqual(stepLT, links[L, Toe].Step);
			Assert.AreEqual(actionLT, links[L, Toe].Action);
			Assert.AreEqual(rollLT, link.Rolls[L, Toe]);

			Assert.AreEqual(validRH, links[R, Heel].Valid);
			Assert.AreEqual(stepRH, links[R, Heel].Step);
			Assert.AreEqual(actionRH, links[R, Heel].Action);
			Assert.AreEqual(rollRH, link.Rolls[R, Heel]);

			Assert.AreEqual(validRT, links[R, Toe].Valid);
			Assert.AreEqual(stepRT, links[R, Toe].Step);
			Assert.AreEqual(actionRT, links[R, Toe].Action);
			Assert.AreEqual(rollRT, link.Rolls[R, Toe]);
		}

		/// <summary>
		/// Helper method to assert that the given MineEvent matches the expected configuration.
		/// </summary>
		/// <param name="mineEvent">MineEvent to check.</param>
		/// <param name="type">Expected MineType.</param>
		/// <param name="n">Expected value for the MineEvent's ArrowIsNthClosest value.</param>
		/// <param name="f">Expected value for the MineEvent's FootAssociatedWithPairedNote.</param>
		public static void AssertMineEventMatches(ExpressedChart.MineEvent mineEvent, MineType type, int n, int f)
		{
			Assert.AreEqual(mineEvent.Type, type);
			Assert.AreEqual(mineEvent.ArrowIsNthClosest, n);
			Assert.AreEqual(mineEvent.FootAssociatedWithPairedNote, f);
		}

		/// <summary>
		/// Helper method to determine if a GraphLinkInstance represents a single step with a given foot.
		/// </summary>
		/// <param name="link">GraphLinkInstance to check.</param>
		/// <param name="foot">The foot to check.</param>
		/// <param name="step">The StepType to check.</param>
		/// <param name="action">The FootAction to check.</param>
		/// <param name="roll">Whether or not the foot action is additionally a roll.</param>
		/// <returns>
		/// Whether or not this set of FootArrowStates represent a single step with the given foot
		/// </returns>
		public static bool IsSingleStepWithFoot(
			GraphLinkInstance link,
			int foot,
			StepType step,
			FootAction action,
			bool roll = false)
		{
			var links = link.GraphLink.Links;

			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					if (!(links[foot, p].Valid && links[foot, p].Step == step && links[foot, p].Action == action))
						return false;
				}
				else
				{
					if (links[foot, p].Valid)
						return false;
				}

				if (links[OtherFoot(foot), p].Valid)
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
			var ec = Load(GetTestChartPath("TestEmpty"));
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
			var ec = Load(GetTestChartPath("TestMinesPreferAfterArrow"));
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
			var ec = Load(GetTestChartPath("TestMinesNoArrow"));
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
			var ec = Load(GetTestChartPath("TestMinesNTies"));
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
			var ec = Load(GetTestChartPath("TestSameArrowAlternating"));
			Assert.AreEqual(8, ec.StepEvents.Count);
			var i = 0;

			// Simple alternating pattern
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
		}

		/// <summary>
		/// Simple jack pattern.
		/// </summary>
		[TestMethod]
		public void TestSameArrowJacks()
		{
			var ec = Load(GetTestChartPath("TestSameArrowJacks"));
			Assert.AreEqual(12, ec.StepEvents.Count);
			var i = 0;

			// Simple jack patterns
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
		}

		/// <summary>
		/// Simple stream pattern.
		/// </summary>
		[TestMethod]
		public void TestNewArrowStream()
		{
			var ec = Load(GetTestChartPath("TestNewArrowStream"));
			Assert.AreEqual(9, ec.StepEvents.Count);
			var i = 0;

			// Simple stream pattern
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Simple Patterns

		#region Crossovers

		/// <summary>
		/// Test crossovers with the left foot crossing over behind.
		/// </summary>
		[TestMethod]
		public void TestCrossoverLBehind()
		{
			var ec = Load(GetTestChartPath("TestCrossoverLBehind"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the left foot crossing over in front.
		/// </summary>
		[TestMethod]
		public void TestCrossoverLFront()
		{
			var ec = Load(GetTestChartPath("TestCrossoverLFront"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the right foot crossing over behind.
		/// </summary>
		[TestMethod]
		public void TestCrossoverRBehind()
		{
			var ec = Load(GetTestChartPath("TestCrossoverRBehind"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test crossovers with the right foot crossing over in front.
		/// </summary>
		[TestMethod]
		public void TestCrossoverRFront()
		{
			var ec = Load(GetTestChartPath("TestCrossoverRFront"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Standard crossover
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with jack
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Crossover with alternating pattern in crossover position
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		#endregion Crossovers

		#region Inverted Steps

		/// <summary>
		/// Test simple inverted patterns.
		/// </summary>
		[TestMethod]
		public void TestInversion()
		{
			var ec = Load(GetTestChartPath("TestInversion"));
			Assert.AreEqual(28, ec.StepEvents.Count);
			var i = 0;

			// Afronova walk, R over L, R leads
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.InvertBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, L over R, L leads
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.InvertBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, L over R, R leads
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.InvertFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Afronova walk, R over L, L leads
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.InvertFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
				var ec = Load(GetTestChartPath(file));
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
						if ((IsSingleStepWithFoot(ec.StepEvents[i - 1].LinkInstance, f, StepType.NewArrow, FootAction.Tap)
						     || IsSingleStepWithFoot(ec.StepEvents[i - 1].LinkInstance, f, StepType.SameArrow, FootAction.Tap))
						    && IsSingleStepWithFoot(ec.StepEvents[i].LinkInstance, f, StepType.NewArrow, FootAction.Tap))
						{
							numDoubleSteps++;
						}

						if (i >= 2
						    && (IsSingleStepWithFoot(ec.StepEvents[i - 2].LinkInstance, f, StepType.NewArrow, FootAction.Tap)
						        || IsSingleStepWithFoot(ec.StepEvents[i - 2].LinkInstance, f, StepType.SameArrow, FootAction.Tap))
						    && IsSingleStepWithFoot(ec.StepEvents[i - 1].LinkInstance, f, StepType.NewArrow, FootAction.Tap)
						    && IsSingleStepWithFoot(ec.StepEvents[i].LinkInstance, f, StepType.NewArrow, FootAction.Tap))
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
			var ec = Load(GetTestChartPath("TestDoubleStepHoldAlternateSameArrow"));
			Assert.AreEqual(7, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test that when holding with one foot the other foot will double step the other arrows,
		/// even if that means crossing over.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepLHold()
		{
			var ec = Load(GetTestChartPath("TestDoubleStepLHold"));
			Assert.AreEqual(34, ec.StepEvents.Count);
			var i = 0;

			// Double step with no crossovers
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Double step after jump
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Double step while holding even if that results in a crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Hold);

			// Double step while holding even if that results in a crossover.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
		}

		/// <summary>
		/// Test to ensure that double stepping is preferred over bracketing when one foot is held
		/// and the other alternates between two arrows.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepLHoldRepeatingPattern()
		{
			var ec = Load(GetTestChartPath("TestDoubleStepLHoldRepeatingPattern"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Hold and do a simple back and forth with the other foot. Could be bracketed but
			// prefer the double step.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test that when holding with one foot the other foot will double step the other arrows,
		/// even if that means crossing over.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepRHold()
		{
			var ec = Load(GetTestChartPath("TestDoubleStepRHold"));
			Assert.AreEqual(34, ec.StepEvents.Count);
			var i = 0;

			// Double step with no crossovers
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Double step after jump
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Double step while holding even if that results in a crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Hold);

			// Double step while holding even if that results in a crossover.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
		}

		/// <summary>
		/// Test to ensure that double stepping is preferred over bracketing when one foot is held
		/// and the other alternates between two arrows.
		/// </summary>
		[TestMethod]
		public void TestDoubleStepRHoldRepeatingPattern()
		{
			var ec = Load(GetTestChartPath("TestDoubleStepRHoldRepeatingPattern"));
			Assert.AreEqual(18, ec.StepEvents.Count);
			var i = 0;

			// Hold and do a simple back and forth with the other foot. Could be bracketed but
			// prefer the double step.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestFootSwapPreferJack"));
			Assert.AreEqual(83, ec.StepEvents.Count);
			var i = 0;

			// Jack with R on up with a crossover. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jack with L on up with a crossover. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jack with R on down with a crossover. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jack with L on down with a crossover. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// L Jack into jump. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// R Jack into jump. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// Triple jack. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);

			// Pattern of triple jacks. Could be swapped, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);

			// Long pattern with a jack which could be a swap in the middle. Ambiguous, but prefer jack.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);

			// Same pattern but mirrored.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow,
				FootAction.Tap);
		}

		/// <summary>
		/// Series of footswap tests.
		/// </summary>
		[TestMethod]
		public void TestFootSwap()
		{
			var ec = Load(GetTestChartPath("TestFootSwap"));
			Assert.AreEqual(75, ec.StepEvents.Count);
			var i = 0;

			// Swap on up and down starting on L. Jacks would results in inverted orientation.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on R. Jacks would results in inverted orientation.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on L. Jacks would results in double step.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Swap on up and down starting on R. Jacks would results in double step.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap from a right foot crossover position.
			// This also checks for favoring a swap on bracketable arrows rather than further away arrows.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap from a left foot crossover position.
			// This also checks for favoring a swap on bracketable arrows rather than further away arrows.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap on a non-bracketable arrow with left foot.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverBehind, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow,
				FootAction.Tap);

			// Swap on a non-bracketable arrow with right foot.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverFront, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow,
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
			var ec = Load(GetTestChartPath("TestJumpStepBothBracketable"));
			Assert.AreEqual(31, ec.StepEvents.Count);
			var i = 0;

			// Normal Jump into ambiguous step with holds to help indicate footing.

			// Jump into ambiguous step with one foot held until the next note should prefer the foot not held (R).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot held until the next note should prefer the foot not held (L).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot released later should prefer the foot released sooner (R).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with one foot released later should prefer the foot released sooner (L).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Normal Jump into ambiguous step with mines to help indicate footing.

			// Jump into ambiguous step with a mine following one foot at the time of the next step. Prefer that foot (R).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot at the time of the next step. Prefer that foot (L).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot before the next step. Prefer that foot (R).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step with a mine following one foot before the next step. Prefer that foot (L).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step mines after both. Prefer foot with closer mine (R).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Jump into ambiguous step mines after both. Prefer foot with closer mine (L).
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Jump with both mine and hold indication. Mine is more important.

			// Left foot hold but mine indicated.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Right foot hold but mine indicated.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Reorient
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalL()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalL"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if the bracketable foot is held. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldBracketable()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLHoldBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if the non-bracketable foot is held. Test with left foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalLHoldNormal()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLHoldNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLHoldAndMineBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLHoldAndMineNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLMineBracketable"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalLMineNormal"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalR()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalR"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// non-bracketable foot if the bracketable foot is held. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldBracketable()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRHoldBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// When performing a step to a new arrow from a jump and the arrow is bracketable with one foot and
		/// not bracketable (though still reachable without crossing over) with the other arrow, prefer the
		/// bracketable foot if the non-bracketable foot is held. Test with right foot as the bracketable foot.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneNormalRHoldNormal()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRHoldNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRHoldAndMineBracketable"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRHoldAndMineNormal"));
			Assert.AreEqual(5, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRMineBracketable"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneNormalRMineNormal"));
			Assert.AreEqual(4, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
		}

		/// <summary>
		/// Series of tests for stepping after a jump to a new arrow where one foot can bracket to the new
		/// arrow and the other foot would need to crossover to reach the new arrow. Avoid the crossover
		/// unless the bracketable foot is held.
		/// </summary>
		[TestMethod]
		public void TestJumpStepOneBracketableOneCrossover()
		{
			var ec = Load(GetTestChartPath("TestJumpStepOneBracketableOneCrossover"));
			Assert.AreEqual(45, ec.StepEvents.Count);
			var i = 0;

			// If holding until the step, prefer the crossover.

			// L step.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// L crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.CrossoverBehind, FootAction.Tap);
			// R crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.CrossoverFront, FootAction.Tap);
			// R step.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// If holding but released before the step, prefer the step.

			// L step after R hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// R step after R hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			// L step after L hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// R step after L hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// For mines, it is more natural to ignore the mine to avoid the crossover.
			// This is subjective.

			// Mine at position of next step. L alternate normally.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. R alternate instead of L behind crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. L alternate instead of R in front crossover.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// Mine at position of next step. R alternate normally.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);

			// Holds and mines on the same foot. In all cases, avoid the crossover

			// L step after L hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Hold, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// R step after L hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			// L step after R hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			// R step after R hold.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestJumpFootSwap"));
			Assert.AreEqual(17, ec.StepEvents.Count);
			var i = 0;

			// Circular jump pattern around all arrows
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);

			// Back and forth pattern
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
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
			var ec = Load(GetTestChartPath("TestFootSwapToAvoidCrossoverAndNewArrowNewArrowJump"));
			Assert.AreEqual(7, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.FootSwap, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelNewToeNew, FootAction.Hold, StepType.BracketHeelNewToeNew, FootAction.Hold);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelSameToeSame, FootAction.Release, StepType.BracketHeelSameToeSame, FootAction.Release);
		}

		/// <summary>
		/// Test that brackets are on a hold and a roll of different lengths that expressed chart
		/// accurately captures which is which. This is important since a short roll and long hold
		/// is much different than a short hold and long roll.
		/// </summary>
		[TestMethod]
		public void TestBracketHoldRoll()
		{
			var ec = Load(GetTestChartPath("TestBracketHoldRoll"));
			Assert.AreEqual(28, ec.StepEvents.Count);
			var i = 0;

			// Orient to force a consistent quad choice (LLRR instead of LRLR)
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);

			// Quad with long holds on toes and short rolls on heels.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeNew, FootAction.Hold, StepType.BracketHeelSameToeNew, FootAction.Hold,
				StepType.BracketHeelNewToeSame, FootAction.Hold, StepType.BracketHeelNewToeSame, FootAction.Hold,
				true, false, true, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);

			// Quad with long holds on heels and short rolls on toes.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				false, true, false, true);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);

			// Quad with long holds on the outer arrows and short rolls on the inner arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				true, false, false, true);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);

			// Quad with long holds on the inner arrows and short rolls on the outer arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				false, true, true, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);

			// Quad with long holds on the inner arrows and short holds on the outer arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				false, false, false, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);

			// Quad with long holds on the outer arrows and short holds on the inner arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				false, false, false, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);

			// Quad with long rolls on the inner arrows and short rolls on the outer arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				true, true, true, true);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);

			// Quad with long rolls on the outer arrows and short rolls on the inner arrows.
			AssertLinkMatchesQuad(ec.StepEvents[i++].LinkInstance,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				StepType.BracketHeelSameToeSame, FootAction.Hold, StepType.BracketHeelSameToeSame, FootAction.Hold,
				true, true, true, true);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false,
				R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesTwoSteps(ec.StepEvents[i++].LinkInstance,
				L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false,
				R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
		}

		/// <summary>
		/// Tests to ensure that when holding an arrow and then tapping one more arrow as a bracket
		/// that the step events are as expected. In these tests the foot holding the arrow will be
		/// using DefaultFootPortion and then when performing the bracket that will update to Heel
		/// or Toe as appropriate.
		/// </summary>
		[TestMethod]
		public void TestBracketOneArrowUpdatesFootPortion()
		{
			var ec = Load(GetTestChartPath("TestBracketOneArrowUpdatesFootPortion"));
			Assert.AreEqual(93, ec.StepEvents.Count);
			var i = 0;

			// Right foot DR bracket one heel hold, release toe first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot DR bracket one heel tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot DR bracket one toe hold, release heel first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot DR bracket one toe tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot UR bracket one toe hold, release heel first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot UR bracket one toe tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot UR bracket one heel hold, release toe first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Right foot UR bracket one heel tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);

			// Left foot LD bracket one heel hold, release toe first
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LD bracket one heel tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LD bracket one toe hold, release heel first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LD bracket one toe tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LU bracket one toe hold, release heel first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LU bracket one toe tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LU bracket one heel hold, release toe first.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelNew, FootAction.Hold, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);

			// Left foot LU bracket one heel tap.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Hold);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelNew, FootAction.Tap, false);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Release);
		}

		/// <summary>
		/// Test a pattern from Giga Violate in the Technical Showcase 4 pack.
		/// This pattern involves a bracket where the heel is held and the other foot then brackets
		/// with a foot swap such that the other foot toe swaps with the original foot toe.
		/// </summary>
		[TestMethod]
		public void TestBracketSwapGigaViolate()
		{
			var ec = Load(GetTestChartPath("TestBracketSwapGigaViolate"));
			Assert.AreEqual(35, ec.StepEvents.Count);
			var i = 0;

			// Original pattern.
			// L bracket on LU, U is swapped with a R bracket on UR.
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelSameToeNew, FootAction.Hold, StepType.BracketHeelSameToeNew, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelSameToeSwap, FootAction.Tap, StepType.BracketHeelSameToeSwap, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelNewToeSame, FootAction.Tap, StepType.BracketHeelNewToeSame, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);

			// Variation.
			// R bracket on UR, U is swapped with a L bracket on LU.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelSameToeNew, FootAction.Hold, StepType.BracketHeelSameToeNew, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Heel, StepType.BracketOneArrowHeelSame, FootAction.Release, false);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelSameToeSwap, FootAction.Tap, StepType.BracketHeelSameToeSwap, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelNewToeSame, FootAction.Tap, StepType.BracketHeelNewToeSame, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);

			// Variation.
			// L bracket on LD, D is swapped with a R bracket on DR.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelNewToeSame, FootAction.Tap, StepType.BracketHeelNewToeSame, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, L, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelSwapToeSame, FootAction.Tap, StepType.BracketHeelSwapToeSame, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelSameToeNew, FootAction.Tap, StepType.BracketHeelSameToeNew, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);

			// Variation.
			// R bracket on DR, D is swapped with a L bracket on LD.
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelNewToeSame, FootAction.Tap, StepType.BracketHeelNewToeSame, FootAction.Hold);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance,L, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesOneStep(ec.StepEvents[i++].LinkInstance, R, Toe, StepType.BracketOneArrowToeSame, FootAction.Release, false);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelSwapToeSame, FootAction.Tap, StepType.BracketHeelSwapToeSame, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, R, StepType.BracketHeelSameToeNew, FootAction.Tap, StepType.BracketHeelSameToeNew, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
		}

		/// <summary>
		/// Test a pattern from Giga Violate in the Technical Showcase 4 pack.
		/// This pattern involves a bracket into a jump that could technically be performed as
		/// another bracket involving a swap, but we should prefer the jump
		/// </summary>
		[TestMethod]
		public void TestBracketIntoJumpsGigaViolate()
		{
			var ec = Load(GetTestChartPath("TestBracketIntoJumpsGigaViolate"));
			Assert.AreEqual(8, ec.StepEvents.Count);
			var i = 0;

			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesBracket(ec.StepEvents[i++].LinkInstance, L, StepType.BracketHeelNewToeSame, FootAction.Tap, StepType.BracketHeelNewToeSame, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.SameArrow, FootAction.Tap, StepType.NewArrow, FootAction.Tap);
			AssertLinkMatchesJump(ec.StepEvents[i++].LinkInstance, StepType.NewArrow, FootAction.Hold, StepType.SameArrow, FootAction.Tap);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, L, StepType.SameArrow, FootAction.Release);
			AssertLinkMatchesStep(ec.StepEvents[i++].LinkInstance, R, StepType.NewArrow, FootAction.Tap);
		}
		#endregion Miscellaneous
	}
}
