using System;
using System.Collections;
using System.Collections.Generic;

namespace Fumen;

/// <summary>
/// Read-only IntervalTree interface.
/// </summary>
/// <typeparam name="TKey">Type of key to use in tree.</typeparam>
/// <typeparam name="TValue">Type of value to use in tree.</typeparam>
public interface IReadOnlyIntervalTree<TKey, TValue> : IEnumerable<TValue> where TKey : IComparable<TKey>
{
	/// <summary>
	/// Read-only enumerator interface.
	/// </summary>
	public interface IReadOnlyIntervalTreeEnumerator : IEnumerator<TValue>
	{
		public IReadOnlyIntervalTreeEnumerator Clone();
		public bool MovePrev();
		public bool IsCurrentValid();
		public void Unset();
	}

	public int GetCount();
	public IReadOnlyIntervalTreeEnumerator Find(TKey low, TKey high);
	public List<TValue> FindAllOverlapping(TKey key, bool lowInclusive = true, bool highInclusive = true);
	public IReadOnlyIntervalTreeEnumerator FindGreatestPreceding(TKey low, bool orEqualTo = false);
	public IReadOnlyIntervalTreeEnumerator FindLeastFollowing(TKey low, bool orEqualTo = false);
	public IReadOnlyIntervalTreeEnumerator First();
	public IReadOnlyIntervalTreeEnumerator Last();
}

/// <summary>
/// Interval tree.
/// Self-balancing binary search tree of intervals represented by low and high keys.
/// Implemented as an augmented Red Black Tree.
/// O(log(N)) time complexity inserts.
/// O(log(N)) time complexity deletes.
/// O(K + log(N)) time complexity finds where K is the number of overlapping intervals.
/// Amortized O(1) time complexity enumeration.
/// O(N) memory usage.
/// Not thread safe.
/// Duplicate low keys are not supported.
/// </summary>
/// <typeparam name="TKey">Type of key to use in tree.</typeparam>
/// <typeparam name="TValue">Type of value to use in tree.</typeparam>
public class IntervalTree<TKey, TValue> : IReadOnlyIntervalTree<TKey, TValue> where TKey : IComparable<TKey>
{
	/// <summary>
	/// IntervalTree Node.
	/// </summary>
	private class Node
	{
		public TKey Low;
		public TKey High;
		public TKey SubTreeHighest;
		public TValue Value;
		public Node Parent;
		public Node L;
		public Node R;
		public bool Red;
	}

	/// <summary>
	/// Enumerator interface.
	/// </summary>
	public interface IIntervalTreeEnumerator : IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator
	{
		public new IIntervalTreeEnumerator Clone();
		public void Delete();
	}

	/// <summary>
	/// Tree root node.
	/// </summary>
	private Node Root;

	/// <summary>
	/// Simple Red Black Tree algorithms rely on a sentinel null node. This node is modified when deleting.
	/// </summary>
	/// <remarks>
	/// I personally do not like this approach as it feels extremely hacky and is not thread safe.
	/// </remarks>
	private readonly Node Nil;

	/// <summary>
	/// Number of elements in the IntervalTree.
	/// </summary>
	private int Count;

	/// <summary>
	/// Constructor.
	/// </summary>
	public IntervalTree()
	{
		Nil = new Node();
		Root = Nil;
		Count = 0;
	}

	/// <summary>
	/// Gets the number of elements in the IntervalTree.
	/// </summary>
	/// <returns>Number of elements in the IntervalTree.</returns>
	public int GetCount()
	{
		return Count;
	}

	/// <summary>
	/// Root Accessor.
	/// </summary>
	/// <returns>Root node.</returns>
	private Node GetRoot()
	{
		return Root;
	}

	/// <summary>
	/// Returns whether the node should be considered null.
	/// The Nil sentinel Node will be considered null.
	/// </summary>
	/// <param name="n">Node to check.</param>
	/// <returns>Whether or not the Node should be considered null or not.</returns>
	private bool IsNull(Node n)
	{
		return n == Nil;
	}

	/// <summary>
	/// Perform a left rotation around the given Node.
	/// </summary>
	/// <param name="x">Node to perform a left rotation around.</param>
	private void RotateLeft(Node x)
	{
		// Perform left rotation.
		var y = x.R;
		x.R = y.L;
		if (y.L != Nil)
			y.L.Parent = x;
		y.Parent = x.Parent;
		if (x.Parent == Nil)
		{
			Root = y;
		}
		else
		{
			if (x == x.Parent.L)
				x.Parent.L = y;
			else
				x.Parent.R = y;
		}

		y.L = x;
		x.Parent = y;

		// Update the SubTreeHighest values starting with the lower node.
		UpdateSubTreeHighest(x);
		UpdateSubTreeHighest(y);
	}

	/// <summary>
	/// Perform a right rotation around the given Node.
	/// </summary>
	/// <param name="x">Node to perform a right rotation around.</param>
	private void RotateRight(Node x)
	{
		var y = x.L;
		x.L = y.R;
		if (y.R != Nil)
			y.R.Parent = x;
		y.Parent = x.Parent;
		if (x.Parent == Nil)
		{
			Root = y;
		}
		else
		{
			if (x == x.Parent.L)
				x.Parent.L = y;
			else
				x.Parent.R = y;
		}

		y.R = x;
		x.Parent = y;

		// Update the SubTreeHighest values starting with the lower node.
		UpdateSubTreeHighest(x);
		UpdateSubTreeHighest(y);
	}

	/// <summary>
	/// Gets the Node that is next sequentially from the given Node.
	/// Returns Nil if no Node follows the given Node.
	/// </summary>
	/// <param name="n">Node to get the next sequential node of.</param>
	/// <returns>Next node sequentially from the given node.</returns>
	private Node Next(Node n)
	{
		if (n.R != Nil)
		{
			n = n.R;
			while (n.L != Nil)
				n = n.L;
			return n;
		}

		var p = n.Parent;
		while (p != Nil && p.R == n)
		{
			n = p;
			p = n.Parent;
		}

		return p;
	}

	/// <summary>
	/// Gets the Node that is previous sequentially from the given Node.
	/// Returns Nil if no Node precedes the given Node.
	/// </summary>
	/// <param name="n">Node to get the previous sequential node of.</param>
	/// <returns>Previous node sequentially from the given node.</returns>
	private Node Prev(Node n)
	{
		if (n.L != Nil)
		{
			n = n.L;
			while (n.R != Nil)
				n = n.R;
			return n;
		}

		var p = n.Parent;
		while (p != Nil && p.L == n)
		{
			n = p;
			p = n.Parent;
		}

		return p;
	}

	/// <summary>
	/// Inserts a value into the tree for the given interval.
	/// The intervals low value must be unique.
	/// </summary>
	/// <param name="value">Value to insert.</param>
	/// <param name="low">Low value of the interval associated with the given value.</param>
	/// <param name="high">High value of the interval associated with the given value.</param>
	/// <returns>IIntervalTreeEnumerator to the inserted value.</returns>
	/// <exception cref="ArgumentException">Throws ArgumentException if the given low value is not unique.</exception>
	public IIntervalTreeEnumerator Insert(TValue value, TKey low, TKey high)
	{
		Count++;
		var n = new Node
		{
			Value = value,
			Low = low,
			High = high,
			SubTreeHighest = high,
			Parent = Nil,
			R = Nil,
			L = Nil,
		};
		if (IsNull(Root))
		{
			Root = n;
			return new Enumerator(this, n);
		}

		var x = Root;
		var p = Nil;
		while (x != Nil)
		{
			p = x;
			x = n.Low.CompareTo(x.Low) < 0 ? x.L : x.R;
		}

		n.Parent = p;
		var comparison = n.Low.CompareTo(p.Low);
		if (comparison < 0)
		{
			p.L = n;
		}
		else if (comparison > 0)
		{
			p.R = n;
		}
		else
		{
			Count--;
			throw new ArgumentException($"Low value {low} already exists in IntervalTree.");
		}

		n.Red = true;

		// Adjust SubTreeHighest.
		x = p;
		while (x != Nil)
		{
			if (x.SubTreeHighest.CompareTo(n.SubTreeHighest) < 0)
				x.SubTreeHighest = n.SubTreeHighest;
			else
				break;
			x = x.Parent;
		}

		if (n.Parent.Parent == Nil)
			return new Enumerator(this, n);

		InsertFix(n);

		return new Enumerator(this, n);
	}

	/// <summary>
	/// Method to balance the tree after inserting a new Node.
	/// </summary>
	/// <param name="n">Node inserted.</param>
	private void InsertFix(Node n)
	{
		while (n != Root && n.Parent.Red)
		{
			if (n.Parent == n.Parent.Parent.L)
			{
				var u = n.Parent.Parent.R;

				// Case 1
				if (u != Nil && u.Red)
				{
					u.Red = false;
					n.Parent.Red = false;
					n.Parent.Parent.Red = true;
					n = n.Parent.Parent;
				}

				else
				{
					// Case 2
					if (n == n.Parent.R)
					{
						n = n.Parent;
						RotateLeft(n);
					}

					// Case 3
					n.Parent.Red = false;
					n.Parent.Parent.Red = true;
					RotateRight(n.Parent.Parent);
				}
			}
			else
			{
				var u = n.Parent.Parent.L;

				// Case 1
				if (u != Nil && u.Red)
				{
					u.Red = false;
					n.Parent.Red = false;
					n.Parent.Parent.Red = true;
					n = n.Parent.Parent;
				}
				else
				{
					// Case 2
					if (n == n.Parent.L)
					{
						n = n.Parent;
						RotateRight(n);
					}

					// Case 3
					n.Parent.Red = false;
					n.Parent.Parent.Red = true;
					RotateLeft(n.Parent.Parent);
				}
			}
		}

		Root.Red = false;
	}

	/// <summary>
	/// Finds the Node with the given interval.
	/// </summary>
	/// <param name="low">Low value of the interval whose value to find.</param>
	/// <param name="high">High value of the interval whose value to find.</param>
	/// <returns>Node associated with the given interval or Nil if no Node exists with that interval.</returns>
	private Node FindNode(TKey low, TKey high)
	{
		var n = Root;
		while (n != Nil)
		{
			var c = low.CompareTo(n.Low);
			if (c == 0)
			{
				if (high.CompareTo(n.High) == 0)
					return n;
				return Nil;
			}

			n = c < 0 ? n.L : n.R;
		}

		return n;
	}

	/// <summary>
	/// Finds the value for the given interval low and high values.
	/// </summary>
	/// <param name="low">Low value of interval to find the value for.</param>
	/// <param name="high">High value of interval to find the value for.</param>
	/// <returns>Enumerator to value or null if not found.</returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator Find(TKey low, TKey high)
	{
		return FindMutable(low, high);
	}

	/// <summary>
	/// Finds the value for the given interval low and high values.
	/// </summary>
	/// <param name="low">Low value of interval to find the value for.</param>
	/// <param name="high">High value of interval to find the value for.</param>
	/// <returns>Enumerator to value or null if not found.</returns>
	public IIntervalTreeEnumerator FindMutable(TKey low, TKey high)
	{
		var n = FindNode(low, high);
		return IsNull(n) ? null : new Enumerator(this, n);
	}

	/// <summary>
	/// Finds all intervals overlapping the given key value.
	/// </summary>
	/// <param name="key">Value to check for interval overlaps.</param>
	/// <param name="lowInclusive">Whether or not to consider the low values of intervals as inclusive.</param>
	/// <param name="highInclusive">Whether or not to consider the high values of intervals as inclusive.</param>
	/// <returns>
	/// List of all values associated with intervals overlapping the given key value.
	/// This list is sorted by the interval start values.
	/// </returns>
	public List<TValue> FindAllOverlapping(TKey key, bool lowInclusive = true, bool highInclusive = true)
	{
		var overlappingValues = new List<TValue>();
		if (Count == 0)
			return overlappingValues;
		FindAllOverlappingNode(Root, key, lowInclusive, highInclusive, overlappingValues);
		return overlappingValues;
	}

	/// <summary>
	/// Finds all intervals overlapping the given key value for the given node and its descendents.
	/// Overlapping values are stored in the given overlappingValues list in the order of they interval low values.
	/// </summary>
	/// <param name="n">Node to check.</param>
	/// <param name="key">Value to check for interval overlaps.</param>
	/// <param name="lowInclusive">Whether or not to consider the low values of intervals as inclusive.</param>
	/// <param name="highInclusive">Whether or not to consider the high values of intervals as inclusive.</param>
	/// <param name="overlappingValues">List of values to append results to.</param>
	private void FindAllOverlappingNode(Node n, TKey key, bool lowInclusive, bool highInclusive, List<TValue> overlappingValues)
	{
		// If the desired value is greater than the highest subtree value then return.
		var comparisonToSubTreeHighest = key.CompareTo(n.SubTreeHighest);
		if ((highInclusive && comparisonToSubTreeHighest > 0) || (!highInclusive && comparisonToSubTreeHighest >= 0))
			return;

		// Search the left subtree.
		if (n.L != Nil)
			FindAllOverlappingNode(n.L, key, lowInclusive, highInclusive, overlappingValues);

		// Add this node's value to the results if it overlaps.
		var comparisonToLow = key.CompareTo(n.Low);
		var comparisonToHigh = key.CompareTo(n.High);
		var lowPasses = lowInclusive ? comparisonToLow >= 0 : comparisonToLow > 0;
		var highPasses = highInclusive ? comparisonToHigh <= 0 : comparisonToHigh < 0;
		if (lowPasses && highPasses)
			overlappingValues.Add(n.Value);

		// If the desired value is greater then this node's low, search the right subtree.
		if (comparisonToLow > 0 && n.R != Nil)
			FindAllOverlappingNode(n.R, key, lowInclusive, highInclusive, overlappingValues);
	}

	/// <summary>
	/// Finds the interval with the greatest low key value preceding the given low key value.
	/// </summary>
	/// <param name="low">Low key value to use for finding.</param>
	/// <param name="orEqualTo">If true, also include an interval if its low value is equal to the given low key value.</param>
	/// <returns>
	/// Enumerator to interval with the greatest low key value preceding the given low key value or null if not found.
	/// </returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator FindGreatestPreceding(TKey low,
		bool orEqualTo = false)
	{
		return FindGreatestPrecedingMutable(low, orEqualTo);
	}

	/// <summary>
	/// Finds the interval with the greatest low key value preceding the given low key value.
	/// </summary>
	/// <param name="low">Low key value to use for finding.</param>
	/// <param name="orEqualTo">If true, also include an interval if its low value is equal to the given low key value.</param>
	/// <returns>
	/// Enumerator to interval with the greatest low key value preceding the given low key value or null if not found.
	/// </returns>
	public IIntervalTreeEnumerator FindGreatestPrecedingMutable(TKey low, bool orEqualTo = false)
	{
		var p = Nil;
		var n = Root;
		Node prev;
		while (n != Nil)
		{
			var c = low.CompareTo(n.Low);
			if (c == 0)
			{
				if (orEqualTo)
					return new Enumerator(this, n);
				prev = Prev(n);
				if (prev == Nil)
					return null;
				return new Enumerator(this, prev);
			}

			p = n;
			n = c < 0 ? n.L : n.R;
		}

		if (p == Nil)
			return null;

		if (low.CompareTo(p.Low) > 0)
			return new Enumerator(this, p);

		prev = Prev(p);
		if (prev == Nil)
			return null;
		return new Enumerator(this, prev);
	}

	/// <summary>
	/// Finds the interval with the least low key value following the given low key value.
	/// </summary>
	/// <param name="low">Low key value to use for finding.</param>
	/// <param name="orEqualTo">If true, also include an interval if its low value is equal to the given low key value.</param>
	/// <returns>
	/// Enumerator to interval with the least low key value following the given low key value or null if not found.
	/// </returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator FindLeastFollowing(TKey low,
		bool orEqualTo = false)
	{
		return FindLeastFollowingMutable(low, orEqualTo);
	}

	/// <summary>
	/// Finds the interval with the least low key value following the given low key value.
	/// </summary>
	/// <param name="low">Low key value to use for finding.</param>
	/// <param name="orEqualTo">If true, also include an interval if its low value is equal to the given low key value.</param>
	/// <returns>
	/// Enumerator to interval with the least low key value following the given low key value or null if not found.
	/// </returns>
	public IIntervalTreeEnumerator FindLeastFollowingMutable(TKey low, bool orEqualTo = false)
	{
		var p = Nil;
		var n = Root;
		Node next;
		while (n != Nil)
		{
			var c = low.CompareTo(n.Low);
			if (c == 0)
			{
				if (orEqualTo)
					return new Enumerator(this, n);
				next = Next(n);
				if (next == Nil)
					return null;
				return new Enumerator(this, next);
			}

			p = n;
			n = c < 0 ? n.L : n.R;
		}

		if (p == Nil)
			return null;

		if (low.CompareTo(p.Low) < 0)
			return new Enumerator(this, p);

		next = Next(p);
		if (next == Nil)
			return null;
		return new Enumerator(this, next);
	}

	/// <summary>
	/// Updates the SubTreeHighest values on the given node based on its children.
	/// Assumes that the SubTreeHighest values on its children are correct.
	/// </summary>
	/// <param name="n">Node to update.</param>
	/// <returns>True if the node's SubTreeHighest was changed and false otherwise.</returns>
	private bool UpdateSubTreeHighest(Node n)
	{
		var original = n.SubTreeHighest;
		n.SubTreeHighest = n.High;
		if (n.L != Nil && n.L.SubTreeHighest.CompareTo(n.SubTreeHighest) > 0)
		{
			n.SubTreeHighest = n.L.SubTreeHighest;
		}

		if (n.R != Nil && n.R.SubTreeHighest.CompareTo(n.SubTreeHighest) > 0)
		{
			n.SubTreeHighest = n.R.SubTreeHighest;
		}

		return n.SubTreeHighest.CompareTo(original) != 0;
	}

	/// <summary>
	/// Deletes the value associated with the given interval.
	/// </summary>
	/// <param name="low">Low value of interval to delete the value for.</param>
	/// <param name="high">High value of interval to delete the value for.</param>
	/// <returns>True if a value was found and deleted and false otherwise.</returns>
	public bool Delete(TKey low, TKey high)
	{
		var n = FindNode(low, high);
		if (IsNull(n))
			return false;

		Delete(n);
		return true;
	}

	/// <summary>
	/// Deletes the given node.
	/// </summary>
	/// <param name="n">Node to delete.</param>
	private void Delete(Node n)
	{
		Count--;

		Node y;
		if (n.L == Nil || n.R == Nil)
			y = n;
		else
			y = Next(n);

		var p = y.Parent;
		var x = y.L != Nil ? y.L : y.R;
		x.Parent = p;
		if (p == Nil)
			Root = x;
		else if (y == p.L)
			p.L = x;
		else
			p.R = x;

		if (y != n)
		{
			n.Low = y.Low;
			n.High = y.High;
			n.Value = y.Value;
		}

		// Update SubTreeHighest.
		var hasReachedY = false;
		var c = x.Parent;
		while (true)
		{
			if (c == Nil)
				break;
			if (!UpdateSubTreeHighest(c) && hasReachedY)
				break;
			c = c.Parent;
			if (!hasReachedY)
				hasReachedY = c == y;
		}

		if (!y.Red)
			DeleteFix(x);
	}

	/// <summary>
	/// Method to balance the tree after deleting new Node.
	/// </summary>
	/// <param name="x">Node to be balanced.</param>
	private void DeleteFix(Node x)
	{
		while (x != Root && !x.Red)
		{
			if (x == x.Parent.L)
			{
				var s = x.Parent.R;

				// Case 1
				if (s.Red)
				{
					s.Red = false;
					x.Parent.Red = true;
					RotateLeft(x.Parent);
					s = x.Parent.R;
				}

				// Case 2
				if (!s.L.Red && !s.R.Red)
				{
					s.Red = true;
					x = x.Parent;
				}
				else
				{
					// Case 3
					if (!s.R.Red)
					{
						s.L.Red = false;
						s.Red = true;
						RotateRight(s);
						s = x.Parent.R;
					}

					// Case 4
					s.Red = x.Parent.Red;
					x.Parent.Red = false;
					s.R.Red = false;
					RotateLeft(x.Parent);
					x = Root;
				}
			}
			else
			{
				var s = x.Parent.L;

				// Case 1
				if (s.Red)
				{
					s.Red = false;
					x.Parent.Red = true;
					RotateRight(x.Parent);
					s = x.Parent.L;
				}

				// Case 2
				if (!s.L.Red && !s.R.Red)
				{
					s.Red = true;
					x = x.Parent;
				}
				else
				{
					// Case 3
					if (!s.L.Red)
					{
						s.R.Red = false;
						s.Red = true;
						RotateLeft(s);
						s = x.Parent.L;
					}

					// Case 4
					s.Red = x.Parent.Red;
					x.Parent.Red = false;
					s.L.Red = false;
					RotateRight(x.Parent);
					x = Root;
				}
			}
		}

		x.Red = false;
	}

	/// <summary>
	/// Returns an IReadOnlyIntervalTreeEnumerator to the first value in the tree.
	/// </summary>
	/// <returns>IReadOnlyIntervalTreeEnumerator to the first value in the tree.</returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator First()
	{
		return FirstMutable();
	}

	/// <summary>
	/// Returns an IIntervalTreeEnumerator to the first value in the tree.
	/// </summary>
	/// <returns>IIntervalTreeEnumerator to the first value in the tree.</returns>
	public IIntervalTreeEnumerator FirstMutable()
	{
		return new Enumerator(this);
	}

	/// <summary>
	/// Returns an IReadOnlyIntervalTreeEnumerator to the last value in the tree.
	/// </summary>
	/// <returns>IReadOnlyIntervalTreeEnumerator to the last value in the tree.</returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator Last()
	{
		return LastMutable();
	}

	/// <summary>
	/// Returns an IIntervalTreeEnumerator to the last value in the tree.
	/// </summary>
	/// <returns>IIntervalTreeEnumerator to the last value in the tree.</returns>
	public IIntervalTreeEnumerator LastMutable()
	{
		var currentNode = Root;
		if (IsNull(currentNode))
			return new Enumerator(this, currentNode);
		while (!IsNull(currentNode.R))
			currentNode = currentNode.R;
		return new Enumerator(this, currentNode);
	}

	#region IEnumerable

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<TValue> GetEnumerator()
	{
		return new Enumerator(this);
	}

	#endregion IEnumerable

	#region Enumerator

	/// <summary>
	/// IIntervalTreeEnumerator implementation.
	/// </summary>
	private sealed class Enumerator : IIntervalTreeEnumerator
	{
		private readonly IntervalTree<TKey, TValue> Tree;
		private Node CurrentNode;
		private bool IsUnset;
		private bool BeforeFirst;
		private bool AfterLast;

		public Enumerator(IntervalTree<TKey, TValue> tree)
		{
			Tree = tree;
			CurrentNode = Tree.Nil;
			Reset();
		}

		public Enumerator(IntervalTree<TKey, TValue> tree, Node n)
		{
			Tree = tree;
			CurrentNode = n;
			IsUnset = true;
		}

		private Enumerator(Enumerator e)
		{
			Tree = e.Tree;
			CurrentNode = e.CurrentNode;
			IsUnset = e.IsUnset;
			BeforeFirst = e.BeforeFirst;
			AfterLast = e.AfterLast;
		}

		IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator
			IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator.Clone()
		{
			return Clone();
		}

		public IIntervalTreeEnumerator Clone()
		{
			return new Enumerator(this);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// ReSharper disable once MemberCanBeMadeStatic.Local
		// ReSharper disable once UnusedParameter.Local
		private void Dispose(bool disposing)
		{
		}

		~Enumerator()
		{
			Dispose(false);
		}

		public bool MoveNext()
		{
			if (IsUnset)
			{
				IsUnset = false;
			}
			else
			{
				if (BeforeFirst)
				{
					CurrentNode = Tree.GetRoot();
					if (!Tree.IsNull(CurrentNode))
					{
						while (!Tree.IsNull(CurrentNode.L))
							CurrentNode = CurrentNode.L;
					}

					BeforeFirst = false;
				}
				else
				{
					CurrentNode = Tree.Next(CurrentNode);
				}
			}

			AfterLast = Tree.IsNull(CurrentNode);
			return !AfterLast;
		}

		public bool MovePrev()
		{
			if (IsUnset)
			{
				IsUnset = false;
			}
			else
			{
				if (AfterLast)
				{
					CurrentNode = Tree.GetRoot();
					if (!Tree.IsNull(CurrentNode))
					{
						while (!Tree.IsNull(CurrentNode.R))
							CurrentNode = CurrentNode.R;
					}

					AfterLast = false;
				}
				else
				{
					CurrentNode = Tree.Prev(CurrentNode);
				}
			}

			BeforeFirst = Tree.IsNull(CurrentNode);
			return !BeforeFirst;
		}

		public void Reset()
		{
			IsUnset = true;
			BeforeFirst = false;
			AfterLast = false;
			CurrentNode = Tree.GetRoot();
			if (!Tree.IsNull(CurrentNode))
			{
				while (!Tree.IsNull(CurrentNode.L))
					CurrentNode = CurrentNode.L;
			}
		}

		public void Unset()
		{
			IsUnset = true;
		}

		public bool IsCurrentValid()
		{
			return !(IsUnset || BeforeFirst || AfterLast);
		}

		public void Delete()
		{
			if (IsUnset)
				throw new InvalidOperationException();
			Tree.Delete(CurrentNode);
			Reset();
		}

		object IEnumerator.Current => Current;

		public TValue Current
		{
			get
			{
				if (IsUnset)
					throw new InvalidOperationException();
				return CurrentNode.Value;
			}
		}
	}

	#endregion Enumerator

	#region Test Helpers

	/// <summary>
	/// Unit test helper for checking whether this tree is a valid interval tree.
	/// A valid interval tree must meet all red black tree validity rules and every
	/// node's SubTreeHighest must be equal to the highest high interval value of itself
	/// and all its descendents.
	/// </summary>
	/// <returns>
	/// True if this tree is valid and false otherwise.
	/// </returns>
	public bool IsValid()
	{
		var r = GetRoot();
		if (Count == 0)
		{
			return IsNull(r);
		}

		// The root must be black.
		if (IsNull(r))
			return false;
		if (!IsNull(r.Parent))
			return false;
		if (r.Red)
			return false;

		// Check every node.
		var expectedLeafBlackCount = -1;
		var numNodes = 0;
		var previousMin = default(TKey);
		var (valid, _, _) = IsNodeValid(r, ref previousMin, ref numNodes, ref expectedLeafBlackCount, 0);
		if (!valid)
			return false;

		return numNodes == Count;
	}

	/// <summary>
	/// Recursive helper unit tests that checks validity of a Node.
	/// </summary>
	/// <returns>
	/// True if the given Node and all its children are valid and false otherwise.
	/// </returns>
	private (bool, TKey, TKey) IsNodeValid(
		Node n,
		ref TKey previousMin,
		ref int numNodes,
		ref int expectedLeafBlackCount,
		int currentLeafBlackCount)
	{
		numNodes++;

		// All red nodes must have two black children where null children are considered black.
		if (n.Red)
		{
			if (!(IsNull(n.L) || !n.L.Red))
				return (false, n.Low, n.High);
			if (!(IsNull(n.R) || !n.R.Red))
				return (false, n.Low, n.High);
		}
		else
		{
			currentLeafBlackCount++;
		}

		// Leaf node checks.
		if (IsNull(n.L) && IsNull(n.R))
		{
			var firstLeaf = expectedLeafBlackCount == -1;

			// Leaf nodes must all have the same number of black ancestors.
			if (expectedLeafBlackCount == -1)
				expectedLeafBlackCount = currentLeafBlackCount;
			if (expectedLeafBlackCount != currentLeafBlackCount)
				return (false, default, default);

			// From left to right, leaf nodes should be sorted.
			if (!firstLeaf)
			{
				if (n.Low.CompareTo(previousMin) < 0)
					return (false, default, default);
			}

			previousMin = n.Low;
		}

		var highestSubTreeValue = n.High;
		var lowestSubTreeValue = n.Low;

		// Check left node.
		if (!IsNull(n.L))
		{
			if (n.L.Parent != n)
				return (false, default, default);
			var (leftValid, leftLow, leftHigh) =
				IsNodeValid(n.L, ref previousMin, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount);
			if (!leftValid)
				return (false, default, default);
			highestSubTreeValue = leftHigh.CompareTo(highestSubTreeValue) > 0 ? leftHigh : highestSubTreeValue;
			lowestSubTreeValue = leftLow.CompareTo(lowestSubTreeValue) < 0 ? leftLow : lowestSubTreeValue;
		}

		// Check right node.
		if (!IsNull(n.R))
		{
			if (n.R.Parent != n)
				return (false, default, default);
			var (rightValid, rightLow, rightHigh) =
				IsNodeValid(n.R, ref previousMin, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount);
			if (!rightValid)
				return (false, default, default);
			highestSubTreeValue = rightHigh.CompareTo(highestSubTreeValue) > 0 ? rightHigh : highestSubTreeValue;
			lowestSubTreeValue = rightLow.CompareTo(lowestSubTreeValue) < 0 ? rightLow : lowestSubTreeValue;
		}

		if (highestSubTreeValue.CompareTo(n.SubTreeHighest) != 0)
			return (false, default, default);

		return (true, lowestSubTreeValue, highestSubTreeValue);
	}

	#endregion Test Helpers
}
