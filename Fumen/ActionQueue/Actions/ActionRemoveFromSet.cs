using System.Collections.Generic;

namespace Fumen;

public class ActionRemoveFromSet<T> : UndoableAction
{
	private readonly ISet<T> Set;
	private readonly T Value;
	private bool ActuallyRemoved;

	public ActionRemoveFromSet(ISet<T> set, T value) : base(false, false)
	{
		Set = set;
		Value = value;
	}

	public override string ToString()
	{
		return $"Remove {Value} from set.";
	}

	public override bool AffectsFile()
	{
		return true;
	}

	protected override void DoImplementation()
	{
		ActuallyRemoved = Set.Remove(Value);
	}

	protected override void UndoImplementation()
	{
		if (ActuallyRemoved)
			Set.Add(Value);
	}
}
