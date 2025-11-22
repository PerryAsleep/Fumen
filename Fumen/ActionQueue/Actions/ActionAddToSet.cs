using System.Collections.Generic;

namespace Fumen;

public class ActionAddToSet<T> : UndoableAction
{
	private readonly ISet<T> Set;
	private readonly T Value;
	private bool ActuallyAdded;

	public ActionAddToSet(ISet<T> set, T value) : base(false, false)
	{
		Set = set;
		Value = value;
	}

	public override string ToString()
	{
		return $"Add {Value} to set.";
	}

	public override bool AffectsFile()
	{
		return true;
	}

	protected override void DoImplementation()
	{
		ActuallyAdded = Set.Add(Value);
	}

	protected override void UndoImplementation()
	{
		if (ActuallyAdded)
			Set.Remove(Value);
	}
}
