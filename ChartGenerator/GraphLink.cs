﻿using System;
using static ChartGenerator.Constants;

namespace ChartGenerator
{
	/// <summary>
	/// Link between nodes in a StepGraph.
	/// Represents what each foot does to move from one GraphNode to a set of other GraphNodes.
	/// </summary>
	public class GraphLink : IEquatable<GraphLink>
	{
		/// <summary>
		/// The state of a foot and an arrow within a GraphLink.
		/// </summary>
		public struct FootArrowState
		{
			public StepType Step { get; }
			public FootAction Action { get; }
			public bool Valid { get; }

			public FootArrowState(StepType step, FootAction action)
			{
				Step = step;
				Action = action;
				Valid = true;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Step == f.Step && Action == f.Action && Valid == f.Valid;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + (int)Step);
				hash = unchecked(hash * 31 + (int)Action);
				hash = unchecked(hash * 31 + (Valid ? 1 : 0));
				return hash;
			}
			#endregion
		}

		/// <summary>
		/// The actions of both feet.
		/// First index is the foot.
		/// Second index is the foot portion ([0-MaxArrowsPerFoot)).
		/// For brackets, Heel (index 0) and Toe (index 1) are used for the second index.
		/// For normal steps, DefaultFootPortion (index 0) is used for the second index.
		/// </summary>
		public readonly FootArrowState[,] Links = new FootArrowState[NumFeet, MaxArrowsPerFoot];

		/// <summary>
		/// Whether or not this link represents a jump with both feet.
		/// Includes bracket jumps
		/// </summary>
		/// <returns>True if this link is a jump and false otherwise.</returns>
		public bool IsJump()
		{
			for (var f = 0; f < NumFeet; f++)
			{
				var footHasStep = false;
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (Links[f, a].Valid && Links[f, a].Action != FootAction.Release)
					{
						footHasStep = true;
						break;
					}
				}
				if (!footHasStep)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Whether or not this link represents a release with any foot.
		/// </summary>
		/// <returns>True if this link is a release and false otherwise.</returns>
		public bool IsRelease()
		{
			for (var f = 0; f < NumFeet; f++)
			{
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (Links[f, a].Valid && Links[f, a].Action == FootAction.Release)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Whether or not this link represents a step with one foot, regardless of if
		/// that is a bracket step.
		/// </summary>
		/// <param name="foot">The foot in question.</param>
		/// <returns>True if this link is a step with the given foot and false otherwise.</returns>
		public bool IsStepWithFoot(int foot)
		{
			// If this foot does not step then this is not a step with this foot.
			var thisFootSteps = false;
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (Links[foot, a].Valid && Links[foot, a].Action != FootAction.Release)
				{
					thisFootSteps = true;
					break;
				}
			}
			if (!thisFootSteps)
				return false;

			// If the other foot performs an action this is not a step with this foot.
			var otherFoot = OtherFoot(foot);
			for (var a = 0; a < MaxArrowsPerFoot; a++)
			{
				if (Links[otherFoot, a].Valid)
				{
					return false;
				}
			}

			// This foot steps and the other foot does perform an action.
			return true;
		}

		/// <summary>
		/// Whether or not this link represents a footswap.
		/// </summary>
		/// <param name="foot">
		/// Out param to store the foot which performed the swap, if this is a foot swap.
		/// </param>
		/// <returns>True if this link is a footswap and false otherwise.</returns>
		public bool IsFootSwap(out int foot)
		{
			foot = InvalidFoot;
			for (var f = 0; f < NumFeet; f++)
			{
				for (var a = 0; a < MaxArrowsPerFoot; a++)
				{
					if (Links[f, a].Valid && Links[f, a].Step == StepType.FootSwap)
					{
						foot = f;
						return true;
					}
				}
			}
			return false;
		}

		#region IEquatable Implementation
		public bool Equals(GraphLink other)
		{
			if (other == null)
				return false;
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					if (!Links[f, a].Equals(other.Links[f, a]))
						return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphLink g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var f = 0; f < NumFeet; f++)
				for (var a = 0; a < MaxArrowsPerFoot; a++)
					hash = unchecked(hash * 31 + Links[f, a].GetHashCode());
			return hash;
		}
		#endregion
	}

	/// <summary>
	/// Class for attaching extra data to a GraphLink when it is used for ExpressedCharts
	/// and PerformedCharts. This allows GraphLink to remain slim, while supporting extra
	/// information at runtime, like what type of hold (hold or roll) is used.
	/// </summary>
	public class GraphLinkInstance
	{
		/// <summary>
		/// Underlying GraphLink.
		/// </summary>
		public GraphLink Link;

		/// <summary>
		/// Per foot and portion, whether or not the step type should be treated as
		/// a roll in the underlying GraphLink.
		/// </summary>
		public bool[,] Rolls = new bool[NumFeet, MaxArrowsPerFoot];
	}
}
