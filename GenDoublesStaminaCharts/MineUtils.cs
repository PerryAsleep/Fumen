using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Fumen;
using static GenDoublesStaminaCharts.Constants;

namespace GenDoublesStaminaCharts
{
	class MineUtils
	{
		public class FootActionEvent
		{
			public MetricPosition Position;
			public int Arrow;
			public int Foot;
		}

		public interface INodeLink
		{
			GraphNode GetGraphNode();
			GraphLink GetGraphLink();
			MetricPosition GetPosition();
		}

		public static (List<FootActionEvent>, List<FootActionEvent>) GetReleasesAndSteps<T>(List<T> events) where T : INodeLink
		{
			var releases = new List<FootActionEvent>();
			var steps = new List<FootActionEvent>();
			var numEvents = events.Count;
			var eventIndex = 0;

			if (numEvents == 0)
				return (releases, steps);

			// Skip first event representing the resting position.
			var previousLink = events[eventIndex].GetGraphLink();
			eventIndex++;

			while (eventIndex < numEvents)
			{
				var node = events[eventIndex];
				var graphNode = node.GetGraphNode();

				for (var f = 0; f < NumFeet; f++)
				{
					for (var a = 0; a < MaxArrowsPerFoot; a++)
					{
						// This is a release.
						if (graphNode.State[f, a].Arrow != InvalidArrowIndex
						    && graphNode.State[f, a].State == GraphArrowState.Resting
						    && previousLink.Links[f, a].Valid
						    && (previousLink.Links[f, a].Action == FootAction.Release ||
						        previousLink.Links[f, a].Action == FootAction.Tap))
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
						    && previousLink.Links[f, a].Valid
						    && (previousLink.Links[f, a].Action == FootAction.Tap
						        || previousLink.Links[f, a].Action == FootAction.Hold
						        || previousLink.Links[f, a].Action == FootAction.Roll))
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
				previousLink = node.GetGraphLink();
				eventIndex++;
			}

			return (releases, steps);
		}

		public static MineEvent CreateMineEvent(
			List<FootActionEvent> releases,
			int releaseIndex,
			List<FootActionEvent> steps,
			int stepIndex)
		{
			return new MineEvent();
		}

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
						if (IsArrowFreeAtPosition(arrow, pos, steps, stepIndex, releases, releaseIndex, arrowsOccupiedByMines))
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

		public static bool IsArrowFreeAtPosition(
			int arrow,
			MetricPosition position,
			List<FootActionEvent> steps,
			int startingStepIndex,
			List<FootActionEvent> releases,
			int startingReleaseIndex,
			bool[] arrowsOccupiedByMines)
		{
			if (arrowsOccupiedByMines[arrow])
				return false;

			// Find the step for this arrow at or before this position
			var precedingStepIndex = startingStepIndex;
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
			if (precedingStepPosition != null && precedingStepPosition == position)
				return false;

			// Find the release for this arrow at or after this position
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
			if (followingReleasePosition != null && followingReleasePosition == position)
				return false;

			if (precedingStepPosition != null
				&& followingReleasePosition != null
				&& precedingStepPosition < position
				&& followingReleasePosition > position)
				return false;
			return true;
		}
	}
}
