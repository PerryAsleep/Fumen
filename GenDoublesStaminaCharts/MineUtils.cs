using System.Collections.Generic;
using Fumen;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	/// <summary>
	/// Utilities to help with creating mine events in ExpressedCharts and PerformedCharts.
	/// This class isn't strictly necessary but it is helpful for a few reasons:
	///  1) Mines are associated with neighboring steps or releases. ExpressedCharts and
	///     PerformedCharts don't have a simple way to report what is a step and what is
	///     a release. This class keeps that logic in one spot so that these classes can
	///     stay in sync. See GetReleasesAndSteps.
	///  2) Similarly, logic for what constitutes the Nth most recent arrow (chiefly, that
	///     arrows at the same position share the same N) was duplicated in ExpressedChart
	///     and now PerformedChart. This class keeps that logic in one spot.
	///  3) Mine logic was causing legibility issues when integrated into ExpressedChart
	///     and PerformedChart. By moving that logic here those classes focus more on just
	///     steps.
	/// </summary>
	public class MineUtils
	{
		/// <summary>
		/// Mines are associated with neighboring arrows.
		/// This class is used to navigate over releases and steps easily to associate mines.
		/// </summary>
		public class FootActionEvent
		{
			public MetricPosition Position;
			public int Arrow;
			public int Foot;
		}

		/// <summary>
		/// To create FootActionEvents from an ExpressedChart or a PerformedChart we need
		/// to loop over the structures of those charts. Those charts both represent data
		/// with GraphNodes and GraphLinks. This interfaces allows for a common method
		/// of generating release and step lists of FootActionEvents for the charts.
		/// </summary>
		public interface IChartNode
		{
			GraphNode GetGraphNode();
			GraphLink GetGraphLinkToNode();
			MetricPosition GetPosition();
		}

		/// <summary>
		/// Creates lists of FootActionEvents for all steps and all releases for the given
		/// List of IChartNodes.
		/// This assumes the first event in the given events is the resting position.
		/// </summary>
		/// <typeparam name="T">IChartNode, for example ExpressedChart.ChartSearchNode or
		/// PerformedChart.StepPerformanceNode.</typeparam>
		/// <param name="events">List of IChartNodes representing the charts nodes.</param>
		/// <returns>List representing releases and List representing steps.</returns>
		public static (List<FootActionEvent>, List<FootActionEvent>) GetReleasesAndSteps<T>(List<T> events) where T : IChartNode
		{
			var releases = new List<FootActionEvent>();
			var steps = new List<FootActionEvent>();
			var numEvents = events.Count;
			var eventIndex = 0;

			if (numEvents == 0)
				return (releases, steps);

			// Skip first event representing the resting position.
			eventIndex++;

			while (eventIndex < numEvents)
			{
				var node = events[eventIndex];
				var graphNode = node.GetGraphNode();
				var linkToNode = node.GetGraphLinkToNode();

				for (var f = 0; f < NumFeet; f++)
				{
					for (var a = 0; a < MaxArrowsPerFoot; a++)
					{
						// This is a release.
						if (graphNode.State[f, a].Arrow != InvalidArrowIndex
						    && graphNode.State[f, a].State == GraphArrowState.Resting
						    && linkToNode.Links[f, a].Valid
						    && (linkToNode.Links[f, a].Action == FootAction.Release ||
						        linkToNode.Links[f, a].Action == FootAction.Tap))
						{
							releases.Add(new FootActionEvent
							{
								Position = node.GetPosition(),
								Foot = f,
								Arrow = graphNode.State[f, a].Arrow
							});
						}

						// This is a step.
						if (graphNode.State[f, a].Arrow != InvalidArrowIndex
						    && (graphNode.State[f, a].State == GraphArrowState.Resting
						        || graphNode.State[f, a].State == GraphArrowState.Held
						        || graphNode.State[f, a].State == GraphArrowState.Rolling)
						    && linkToNode.Links[f, a].Valid
						    && (linkToNode.Links[f, a].Action == FootAction.Tap
						        || linkToNode.Links[f, a].Action == FootAction.Hold
						        || linkToNode.Links[f, a].Action == FootAction.Roll))
						{
							steps.Add(new FootActionEvent
							{
								Position = node.GetPosition(),
								Foot = f,
								Arrow = graphNode.State[f, a].Arrow
							});
						}
					}
				}
				eventIndex++;
			}

			return (releases, steps);
		}

		/// <summary>
		/// Creates an ExpressedChart MineEvent.
		/// If possible, this MineEvent will be an AfterArrow type.
		/// If no arrow precedes this mine then this MineEvent will be a BeforeArrow type.
		/// If no arrow exists in the same lane as the mine then this will be a NoArrow type.
		/// </summary>
		/// <param name="numArrows">
		/// Number of arrows in the chart being used to generate the ExpressedChart.
		/// This is used to ease the searches into neighboring events.
		/// </param>
		/// <param name="releases">
		/// List of all FootActionEvents representing release events for the ExpressedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="releaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="steps">
		/// List of all FootActionEvents representing step events for the ExpressedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="stepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="mine">The LaneNote representing a mine from the original chart.</param>
		/// <returns>New MineEvent.</returns>
		public static ExpressedChart.MineEvent CreateExpressedMineEvent(
			int numArrows,
			List<FootActionEvent> releases,
			int releaseIndex,
			List<FootActionEvent> steps,
			int stepIndex,
			LaneNote mine)
		{
			// Try first to create an AfterArrow type of mine by associating the mine
			// with a preceding arrow.
			var (n, f) = GetHowRecentIsNeighboringArrow(true, releaseIndex, numArrows, releases, mine.Lane);
			if (n >= 0)
			{
				return new ExpressedChart.MineEvent
				{
					Type = MineType.AfterArrow,
					Position = mine.Position,
					ArrowIsNthClosest = n,
					FootAssociatedWithPairedNote = f
				};
			}

			// Next, try to create a BeforeArrow type of mine by associating the mine
			// with a following arrow.
			(n, f) = GetHowRecentIsNeighboringArrow(false, stepIndex, numArrows, steps, mine.Lane);
			if (n >= 0)
			{
				return new ExpressedChart.MineEvent
				{
					Type = MineType.BeforeArrow,
					Position = mine.Position,
					ArrowIsNthClosest = n,
					FootAssociatedWithPairedNote = f
				};
			}

			// The mine could not be associated with an arrow, use the default NoArrow type.
			return new ExpressedChart.MineEvent { Position = mine.Position };
		}

		/// <summary>
		/// For a mine positioned at the given arrow, determines how far away the neighboring
		/// arrow in the same lane is relative to arrows in other lanes. This is a number from
		/// [0, numArrows). Arrows sharing the same position (jumps and brackets) are considered
		/// at the same relative location.
		/// Example:
		/// X--- (This arrow is the 1st most recent relative to the mine.)
		/// -XX- (Both arrows here are the 0th most recent relative to the mine.)
		/// O--X (Mine. Arrow at this position is disqualified.)
		/// </summary>
		/// <param name="searchBackwards">
		/// If true then search backwards through the events.
		/// If false then search forwards through the events.
		/// </param>
		/// <param name="searchIndex">Search index into events to start the searching at.</param>
		/// <param name="numArrows">Number of arrows in the chart.</param>
		/// <param name="events">The List of FootActionEvent to search.</param>
		/// <param name="arrow">Arrow of the mine. Between [0, numArrows).</param>
		/// <returns>
		/// A tuple where the first value is the relative position of the neighboring arrow and
		/// the second value is the foot that was used for this arrow. If no arrow could be found
		/// then (InvalidArrowIndex, InvalidFoot) is returned.
		/// </returns>
		private static (int, int) GetHowRecentIsNeighboringArrow(
			bool searchBackwards,
			int searchIndex,
			int numArrows,
			List<FootActionEvent> events,
			int arrow)
		{
			var currentN = 0;
			var consideredArrows = new bool[numArrows];
			MetricPosition currentNPosition = null;
			while (searchBackwards ? searchIndex >= 0 : searchIndex < events.Count)
			{
				if (!consideredArrows[events[searchIndex].Arrow])
				{
					var newN = !(currentNPosition == null || currentNPosition == events[searchIndex].Position);
					if (newN)
						currentN++;
					currentNPosition = events[searchIndex].Position;

					if (events[searchIndex].Arrow == arrow)
						return (currentN, events[searchIndex].Foot);

					consideredArrows[events[searchIndex].Arrow] = true;
				}
				searchIndex += searchBackwards ? -1 : 1;
			}

			// Could not find an arrow
			return (InvalidArrowIndex, InvalidFoot);
		}

		/// <summary>
		/// Finds the lane of the Nth most recent arrow relative to the given indices
		/// of the given releases and steps. If all lanes at the desired N are occupied,
		/// then the search will continue to the next N until either a free location is
		/// found, or the search can no longer continue.
		/// If no arrow can be found, InvalidArrowIndex is returned.
		/// Used by PerformedChart in order to find which arrow should be used for a mine
		/// when we know the layout of the arrows and the N value from the
		/// ExpressedChart's MineEvent (ArrowIsNthClosest/desiredN).
		/// For determining N, arrows sharing the same position (jumps and brackets)
		/// are considered at the same relative location.
		/// Example:
		/// X--- (This arrow is the 1st most recent relative to the mine.)
		/// -XX- (Both arrows here are the 0th most recent relative to the mine.)
		/// O--X (Mine. Arrow at this position is disqualified.)
		/// </summary>
		/// <param name="searchBackwards">
		/// If true then search backwards through the releases.
		/// If false then search forwards through the steps.
		/// </param>
		/// <param name="desiredN">
		/// The desired N value (Nth most recent).
		/// </param>
		/// <param name="desiredFoot">
		/// The desired foot for the associated arrow.
		/// If multiple arrows are found that share the desired position then preference
		/// will be given to one that uses this foot.
		/// </param>
		/// <param name="numArrows"> Number of arrows in the chart.</param>
		/// <param name="releases">
		/// List of all FootActionEvents representing release events for the PerformedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="releaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="steps">
		/// List of all FootActionEvents representing step events for the PerformedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="stepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="arrowsOccupiedByMines">
		/// Array of booleans representing if the lane at that index is occupied by a mine
		/// at the position of the mine in question. Tracked externally.
		/// </param>
		/// <returns></returns>
		public static int FindBestNthMostRecentArrow(
			bool searchBackwards,
			int desiredN,
			int desiredFoot,
			int numArrows,
			List<FootActionEvent> releases,
			int releaseIndex,
			List<FootActionEvent> steps,
			int stepIndex,
			bool[] arrowsOccupiedByMines)
		{
			var events = searchBackwards ? releases : steps;
			var searchIndex = searchBackwards ? releaseIndex : stepIndex;
			var currentN = 0;
			var bestArrow = InvalidArrowIndex;
			var consideredArrows = new bool[numArrows];
			var numArrowsConsidered = 0;
			MetricPosition currentNPosition = null;

			// Search
			while (searchBackwards ? searchIndex >= 0 : searchIndex < events.Count)
			{
				var arrow = events[searchIndex].Arrow;
				var pos = events[searchIndex].Position;

				// This event is for an arrow we haven't yet considered as the Nth most recent
				if (!consideredArrows[arrow])
				{
					// Check to see if we have already considered an arrow at this depth before.
					var newN = !(currentNPosition == null || currentNPosition == pos);

					// If we have advanced positions but we found a possible arrow a the last position
					// then use that and stop.
					if (newN && bestArrow != InvalidArrowIndex)
						break;

					// If we have advanced positions update the currentN tracker.
					if (newN)
					{
						currentN++;
						if (currentN == numArrows)
							break;
					}

					// Update the currentNPosition tracker.
					currentNPosition = pos;

					// This arrow is at least the Nth most recent.
					// This is a greater than or equal check in case no arrow is free at the desiredN.
					// In that case we continue searching further away.
					if (currentN >= desiredN)
					{
						// If this arrow free then we can use it.
						if (IsArrowFreeAtPosition(arrow, pos, releases, releaseIndex, steps, stepIndex, arrowsOccupiedByMines))
						{
							// Record this arrow as the best arrow.
							bestArrow = arrow;
							// If this arrow uses the desired foot, then we are done. If it doesn't keep
							// searching to see if another arrow exists at this depth that would be better.
							// If searching results in advancing currentN again we will stop since we have
							// recorded a bestArrow.
							if (events[searchIndex].Foot == desiredFoot)
								break;
						}
					}

					// Mark this arrow considered.
					consideredArrows[arrow] = true;
					numArrowsConsidered++;
					// If we have considered every possible arrow then stop searching.
					if (numArrowsConsidered == numArrows)
						break;
				}

				// Advance search
				searchIndex += searchBackwards ? -1 : 1;
			}

			return bestArrow;
		}

		/// <summary>
		/// Helper method for FindBestNthMostRecentArrow to determine if the given
		/// arrow is free at the given position.
		/// </summary>
		/// <param name="arrow">The arrow to check.</param>
		/// <param name="position">The position to check.</param>
		/// <param name="releases">
		/// The List of release FootActionEvents from the PerformedChart.
		/// Needed to check hold/roll overlap.
		/// </param>
		/// <param name="startingReleaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// Used to check hold/roll overlap.
		/// </param>
		/// <param name="steps">
		/// The List of step FootActionEvents from the PerformedChart.
		/// Needed to check hold/roll overlap.
		/// </param>
		/// <param name="startingStepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// Used to check hold/roll overlap.
		/// </param>
		/// <param name="arrowsOccupiedByMines">
		/// Array of booleans representing if the lane at that index is occupied by a mine
		/// at the position of the mine in question. Tracked externally.
		/// </param>
		/// <returns>True if the arrow is free and false otherwise.</returns>
		private static bool IsArrowFreeAtPosition(
			int arrow,
			MetricPosition position,
			List<FootActionEvent> releases,
			int startingReleaseIndex,
			List<FootActionEvent> steps,
			int startingStepIndex,
			bool[] arrowsOccupiedByMines)
		{
			// If there is a mine at this location it is not free.
			if (arrowsOccupiedByMines[arrow])
				return false;

			// Find the step for this arrow at or before this position.
			var precedingStepIndex = System.Math.Min(startingStepIndex, steps.Count - 1);
			MetricPosition precedingStepPosition = null;
			while (steps[precedingStepIndex].Position > position && precedingStepIndex >= 0)
			{
				if (steps[precedingStepIndex].Arrow == arrow)
				{
					precedingStepPosition = steps[precedingStepIndex].Position;
					break;
				}
				precedingStepIndex--;
			}
			// If the preceding step is at the same position then it is not free.
			if (precedingStepPosition != null && precedingStepPosition == position)
				return false;

			// Find the release for this arrow at or after this position.
			var followingReleaseIndex = startingReleaseIndex;
			MetricPosition followingReleasePosition = null;
			while (releases[followingReleaseIndex].Position < position && followingReleaseIndex < releases.Count)
			{
				if (releases[followingReleaseIndex].Arrow == arrow)
				{
					followingReleasePosition = releases[followingReleaseIndex].Position;
					break;
				}
				followingReleaseIndex++;
			}
			// If the following release is at the same position then it is not free.
			if (followingReleasePosition != null && followingReleasePosition == position)
				return false;

			// If a hold or roll is active over this position then it is not free.
			if (precedingStepPosition != null
				&& followingReleasePosition != null
				&& precedingStepPosition < position
				&& followingReleasePosition > position)
				return false;

			// It is free.
			return true;
		}
	}
}
