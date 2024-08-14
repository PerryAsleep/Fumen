using System.Collections.Generic;

namespace Fumen;

public sealed class DescendingComparer<T> : IComparer<T>
{
	public int Compare(T first, T second)
	{
		if (first == null)
			return -1;
		if (second == null)
			return 1;
		return Comparer<T>.Default.Compare(second, first);
	}
}
