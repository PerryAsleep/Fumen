using System.Collections.Generic;
using static System.Diagnostics.Debug;

namespace Fumen;

/// <summary>
/// UndoableAction to perform multiple other UndoableActions together as a single action.
/// </summary>
public sealed class ActionMultiple : UndoableAction
{
	private readonly List<UndoableAction> Actions;

	public ActionMultiple() : base(false, false)
	{
		Actions = [];
	}

	public ActionMultiple(List<UndoableAction> actions) : base(false, false)
	{
		foreach (var action in actions)
		{
			Assert(!action.IsDoAsync() && !action.IsUndoAsync());
		}

		Actions = actions;
	}

	public void EnqueueAndDo(UndoableAction action)
	{
		Assert(!action.IsDoAsync() && !action.IsUndoAsync());

		action.Do();
		Actions.Add(action);
	}

	public void EnqueueWithoutDoing(UndoableAction action)
	{
		Assert(!action.IsDoAsync() && !action.IsUndoAsync());

		Actions.Add(action);
	}

	public List<UndoableAction> GetActions()
	{
		return Actions;
	}

	public override bool AffectsFile()
	{
		foreach (var action in Actions)
		{
			if (action.AffectsFile())
				return true;
		}

		return false;
	}

	public override string ToString()
	{
		return string.Join(' ', Actions);
	}

	protected override void DoImplementation()
	{
		foreach (var action in Actions)
		{
			action.Do();
		}
	}

	protected override void UndoImplementation()
	{
		var i = Actions.Count - 1;
		while (i >= 0)
		{
			Actions[i--].Undo();
		}
	}

	public void Clear()
	{
		Actions.Clear();
	}
}
