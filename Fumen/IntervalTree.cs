using System;
using System.Collections;
using System.Collections.Generic;

namespace Fumen;

/// <summary>
/// Read-only IntervalTree interface.
/// </summary>
/// <typeparam name="TKey">Type of key to use in tree.</typeparam>
/// <typeparam name="TValue">Type of value to use in tree.</typeparam>
public interface IReadOnlyIntervalTree<in TKey, TValue> : IEnumerable<TValue>
	where TKey : IComparable<TKey>
	where TValue : IEquatable<TValue>
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
	public IReadOnlyIntervalTreeEnumerator Find(TValue value, TKey low, TKey high);
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
/// Intervals beginning at the same low key are supported, though these scenarios will
/// result in O(N) operations where N is the number of values with the same low key.
/// </summary>
/// <typeparam name="TKey">Type of key to use in tree.</typeparam>
/// <typeparam name="TValue">Type of value to use in tree.</typeparam>
public class IntervalTree<TKey, TValue> : IReadOnlyIntervalTree<TKey, TValue>
	where TKey : IComparable<TKey>
	where TValue : IEquatable<TValue>
{
	/// <summary>
	/// IntervalTree Node.
	/// Nodes may contain more than one value and high key.
	/// </summary>
	private class Node
	{
		/// <summary>
		/// Sentinel invalid internal index value.
		/// </summary>
		public const int InvalidIndex = -1;

		/// <summary>
		/// Low key.
		/// </summary>
		public TKey Low;

		/// <summary>
		/// High key.
		/// </summary>
		private TKey High;

		/// <summary>
		/// Highest value of this Node's subtree.
		/// </summary>
		public TKey SubTreeHighest;

		/// <summary>
		/// Value.
		/// </summary>
		private TValue Value;

		/// <summary>
		/// Parent Node.
		/// </summary>
		public Node Parent;

		/// <summary>
		/// Left child Node.
		/// </summary>
		public Node L;

		/// <summary>
		/// Right child Node.
		/// </summary>
		public Node R;

		/// <summary>
		/// Whether this Node is red or black.
		/// </summary>
		public bool Red;

		/// <summary>
		/// Optional list of other values which share this Node's low key.
		/// </summary>
		private List<TValue> EqualValues;

		/// <summary>
		/// Optional list of other high keys for values which share this Node's low key.
		/// </summary>
		private List<TKey> EqualHighs;

		/// <summary>
		/// Empty constructor for Nil node.
		/// </summary>
		public Node()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="low">Low key.</param>
		/// <param name="high">High key.</param>
		/// <param name="value">Value.</param>
		/// <param name="nil">The Nil node.</param>
		public Node(TKey low, TKey high, TValue value, Node nil)
		{
			Value = value;
			Low = low;
			High = high;
			SubTreeHighest = high;
			Parent = nil;
			L = nil;
			R = nil;
		}

		/// <summary>
		/// Gets the number of values stored at this node.
		/// </summary>
		/// <returns>Number of values stored at this node.</returns>
		public int GetNumValues()
		{
			if (EqualValues == null)
				return 1;
			return EqualValues.Count + 1;
		}

		/// <summary>
		/// Gets the highest high key for values at this node.
		/// </summary>
		/// <returns>Highest high key for values at this node.</returns>
		public TKey GetHigh()
		{
			if (EqualHighs == null)
				return High;
			var high = High;
			for (var i = 0; i < EqualHighs.Count; i++)
				if (EqualHighs[i].CompareTo(high) > 0)
					high = EqualHighs[i];
			return high;
		}

		/// <summary>
		/// Gets the internal index of the given value and high key.
		/// </summary>
		/// <param name="high">High key value.</param>
		/// <param name="value">Value.</param>
		/// <returns>Index of given value and high key or InvalidIndex if not found.</returns>
		public int GetIndexOfValue(TKey high, TValue value)
		{
			if (High.CompareTo(high) == 0 && Value.Equals(value))
				return 0;
			if (EqualHighs == null)
				return InvalidIndex;
			for (var i = 0; i < EqualHighs.Count; i++)
				if (EqualHighs[i].CompareTo(high) == 0 && value.Equals(EqualValues[i]))
					return i + 1;
			return InvalidIndex;
		}

		/// <summary>
		/// Gets the value at the given internal index.
		/// Assumes the given index is valid.
		/// </summary>
		/// <param name="index">Internal index to get the value of.</param>
		/// <returns>Value at the given internal index.</returns>
		public TValue GetValueAtIndex(int index)
		{
			if (index == 0)
				return Value;
			return EqualValues[index - 1];
		}

		/// <summary>
		/// Adds another value to this node that has an equal low key.
		/// </summary>
		/// <param name="high">High key value.</param>
		/// <param name="value">Value.</param>
		public void AddEqualValue(TKey high, TValue value)
		{
			EqualValues ??= [];
			EqualValues.Add(value);
			EqualHighs ??= [];
			EqualHighs.Add(high);
		}

		/// <summary>
		/// Adds any Values overlapping the given key from this Node to the given list of overlappingValues.
		/// </summary>
		/// <param name="key">Key to check for value overlap.</param>
		/// <param name="lowInclusive">Whether or not low key values should be inclusive.</param>
		/// <param name="highInclusive">Whether or not high key values should be inclusive.</param>
		/// <param name="overlappingValues">List of overlapping Values to add to.</param>
		public void AddOverlappingValues(TKey key, bool lowInclusive, bool highInclusive, List<TValue> overlappingValues)
		{
			var comparisonToLow = key.CompareTo(Low);
			var lowPasses = lowInclusive ? comparisonToLow >= 0 : comparisonToLow > 0;
			if (!lowPasses)
				return;
			var comparisonToHigh = key.CompareTo(High);
			var highPasses = highInclusive ? comparisonToHigh <= 0 : comparisonToHigh < 0;
			if (highPasses)
			{
				overlappingValues.Add(Value);
			}

			if (EqualValues != null && EqualHighs != null)
			{
				for (var i = 0; i < EqualHighs.Count; i++)
				{
					comparisonToHigh = key.CompareTo(EqualHighs[i]);
					highPasses = highInclusive ? comparisonToHigh <= 0 : comparisonToHigh < 0;
					if (highPasses)
					{
						overlappingValues.Add(EqualValues[i]);
					}
				}
			}
		}

		/// <summary>
		/// Deletes the value at the given internal index.
		/// Assumes the given index is valid.
		/// </summary>
		/// <param name="index">Internal index to delete the value of.</param>
		/// <returns>
		/// Returns true if any values remain and false otherwise.
		/// </returns>
		public bool DeleteValueAtIndex(int index)
		{
			if (index > 0)
			{
				if (EqualValues.Count == 1)
				{
					EqualValues = null;
					EqualHighs = null;
					return true;
				}

				EqualValues.RemoveAt(index - 1);
				EqualHighs.RemoveAt(index - 1);
				return true;
			}

			if (EqualValues == null)
			{
				return false;
			}

			Value = EqualValues[0];
			High = EqualHighs[0];
			if (EqualValues.Count == 1)
			{
				EqualValues = null;
				EqualHighs = null;
			}
			else
			{
				EqualValues.RemoveAt(0);
				EqualHighs.RemoveAt(0);
			}

			return true;
		}

		/// <summary>
		/// Copies the internals of this Node to the given other Node.
		/// Used for node deletion algorithm.
		/// </summary>
		/// <param name="other">Other node to copy internals to.</param>
		public void CopyInternalsFrom(Node other)
		{
			Low = other.Low;
			High = other.High;
			Value = other.Value;
			EqualHighs = null;
			EqualValues = null;
			if (other.EqualHighs != null)
				EqualHighs = [..other.EqualHighs];
			if (other.EqualValues != null)
				EqualValues = [..other.EqualValues];
		}
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
	/// <exception cref="ArgumentException">Throws ArgumentException if the given low greater than the given high value.</exception>
	public IIntervalTreeEnumerator Insert(TValue value, TKey low, TKey high)
	{
		if (low.CompareTo(high) > 0)
		{
			throw new ArgumentException($"Low value {low} is greater than high value {high}.");
		}

		Count++;
		var n = new Node(low, high, value, Nil);
		if (IsNull(Root))
		{
			Root = n;
			return new Enumerator(this, n, 0);
		}

		var x = Root;
		var p = Nil;
		var comparison = 0;
		while (x != Nil)
		{
			p = x;
			comparison = n.Low.CompareTo(x.Low);
			if (comparison < 0)
				x = x.L;
			else if (comparison > 0)
				x = x.R;
			else
				break;
		}

		n.Red = true;
		if (comparison < 0)
		{
			n.Parent = p;
			p.L = n;
		}
		else if (comparison > 0)
		{
			n.Parent = p;
			p.R = n;
		}
		else
		{
			p.AddEqualValue(high, value);
			var nodeHigh = p.GetHigh();
			if (p.SubTreeHighest.CompareTo(nodeHigh) < 0)
				p.SubTreeHighest = nodeHigh;
			n = p;
			p = n.Parent;
		}

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

		if (n.Parent.Parent == Nil || comparison == 0)
			return new Enumerator(this, n, n.GetNumValues() - 1);

		InsertFix(n);

		return new Enumerator(this, n, n.GetNumValues() - 1);
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
	/// <param name="value">Value to find.</param>
	/// <param name="low">Low value of the interval whose value to find.</param>
	/// <param name="high">High value of the interval whose value to find.</param>
	/// <returns>Node associated with the given interval or Nil if no Node exists with that interval.</returns>
	private (Node, int) FindNode(TValue value, TKey low, TKey high)
	{
		var n = Root;
		while (n != Nil)
		{
			var c = low.CompareTo(n.Low);
			if (c == 0)
			{
				var index = n.GetIndexOfValue(high, value);
				if (index >= 0)
					return (n, index);
				return (Nil, Node.InvalidIndex);
			}

			n = c < 0 ? n.L : n.R;
		}

		return (n, Node.InvalidIndex);
	}

	/// <summary>
	/// Finds the value for the given interval low and high values.
	/// </summary>
	/// <param name="value">Value to find.</param>
	/// <param name="low">Low value of interval to find the value for.</param>
	/// <param name="high">High value of interval to find the value for.</param>
	/// <returns>Enumerator to value or null if not found.</returns>
	public IReadOnlyIntervalTree<TKey, TValue>.IReadOnlyIntervalTreeEnumerator Find(TValue value, TKey low, TKey high)
	{
		return FindMutable(value, low, high);
	}

	/// <summary>
	/// Finds the value for the given interval low and high values.
	/// </summary>
	/// <param name="value">Value to find.</param>
	/// <param name="low">Low value of interval to find the value for.</param>
	/// <param name="high">High value of interval to find the value for.</param>
	/// <returns>Enumerator to value or null if not found.</returns>
	public IIntervalTreeEnumerator FindMutable(TValue value, TKey low, TKey high)
	{
		var (node, valueIndex) = FindNode(value, low, high);
		return IsNull(node) ? null : new Enumerator(this, node, valueIndex);
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
	/// Finds all intervals overlapping the given key value for the given node and its descendants.
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
		n.AddOverlappingValues(key, lowInclusive, highInclusive, overlappingValues);

		// If the desired value is greater than this node's low, search the right subtree.
		if (key.CompareTo(n.Low) > 0 && n.R != Nil)
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
					return new Enumerator(this, n, n.GetNumValues() - 1);
				prev = Prev(n);
				if (prev == Nil)
					return null;
				return new Enumerator(this, prev, prev.GetNumValues() - 1);
			}

			p = n;
			n = c < 0 ? n.L : n.R;
		}

		if (p == Nil)
			return null;

		if (low.CompareTo(p.Low) > 0)
			return new Enumerator(this, p, p.GetNumValues() - 1);

		prev = Prev(p);
		if (prev == Nil)
			return null;
		return new Enumerator(this, prev, prev.GetNumValues() - 1);
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
					return new Enumerator(this, n, 0);
				next = Next(n);
				if (next == Nil)
					return null;
				return new Enumerator(this, next, 0);
			}

			p = n;
			n = c < 0 ? n.L : n.R;
		}

		if (p == Nil)
			return null;

		if (low.CompareTo(p.Low) < 0)
			return new Enumerator(this, p, 0);

		next = Next(p);
		if (next == Nil)
			return null;
		return new Enumerator(this, next, 0);
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
		n.SubTreeHighest = n.GetHigh();
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
	/// <param name="value">Value to delete.</param>
	/// <param name="low">Low value of interval to delete the value for.</param>
	/// <param name="high">High value of interval to delete the value for.</param>
	/// <returns>True if a value was found and deleted and false otherwise.</returns>
	public bool Delete(TValue value, TKey low, TKey high)
	{
		var (node, valueIndex) = FindNode(value, low, high);
		if (IsNull(node))
			return false;
		DeleteValueFromNode(node, valueIndex);
		return true;
	}

	/// <summary>
	/// Delete the value at the given index from the given Node.
	/// Assumes the index is valid.
	/// </summary>
	/// <param name="n">Node to delete value from.</param>
	/// <param name="valueIndex">Index of value to delete.</param>
	private void DeleteValueFromNode(Node n, int valueIndex)
	{
		Count--;
		var anyValuesRemain = n.DeleteValueAtIndex(valueIndex);
		if (!anyValuesRemain)
		{
			DeleteNode(n);
			return;
		}

		while (n != Nil)
		{
			if (!UpdateSubTreeHighest(n))
				return;
			n = n.Parent;
		}
	}

	/// <summary>
	/// Deletes the given node entirely from the tree.
	/// </summary>
	/// <param name="n">Node to delete.</param>
	private void DeleteNode(Node n)
	{
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
			n.CopyInternalsFrom(y);
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
			return new Enumerator(this, currentNode, 0);
		while (!IsNull(currentNode.R))
			currentNode = currentNode.R;
		return new Enumerator(this, currentNode, currentNode.GetNumValues() - 1);
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
		private int CurrentNodeIndex;
		private bool IsUnset;
		private bool BeforeFirst;
		private bool AfterLast;

		public Enumerator(IntervalTree<TKey, TValue> tree)
		{
			Tree = tree;
			CurrentNode = Tree.Nil;
			Reset();
		}

		public Enumerator(IntervalTree<TKey, TValue> tree, Node n, int index)
		{
			Tree = tree;
			CurrentNode = n;
			CurrentNodeIndex = index;
			IsUnset = true;
		}

		private Enumerator(Enumerator e)
		{
			Tree = e.Tree;
			CurrentNode = e.CurrentNode;
			CurrentNodeIndex = e.CurrentNodeIndex;
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
					CurrentNodeIndex++;
					if (CurrentNodeIndex >= CurrentNode.GetNumValues())
					{
						CurrentNodeIndex = 0;
						CurrentNode = Tree.Next(CurrentNode);
					}
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
						CurrentNodeIndex = CurrentNode.GetNumValues() - 1;
					}

					AfterLast = false;
				}
				else
				{
					if (CurrentNodeIndex > 0)
					{
						CurrentNodeIndex--;
					}
					else
					{
						CurrentNode = Tree.Prev(CurrentNode);
						CurrentNodeIndex = CurrentNode.GetNumValues() - 1;
					}
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
			CurrentNodeIndex = 0;
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
			Tree.DeleteValueFromNode(CurrentNode, CurrentNodeIndex);
			Reset();
		}

		object IEnumerator.Current => Current;

		public TValue Current
		{
			get
			{
				if (IsUnset)
					throw new InvalidOperationException();
				return CurrentNode.GetValueAtIndex(CurrentNodeIndex);
			}
		}
	}

	#endregion Enumerator

	#region Test Helpers

	/// <summary>
	/// Unit test helper for checking whether this tree is a valid interval tree.
	/// A valid interval tree must meet all red black tree validity rules and every
	/// node's SubTreeHighest must be equal to the highest high interval value of itself
	/// and all its descendants.
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
		var numValues = 0;
		var previousMin = default(TKey);
		var (valid, _, _) = IsNodeValid(r, ref previousMin, ref numValues, ref expectedLeafBlackCount, 0);
		if (!valid)
			return false;

		return numValues == Count;
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
		ref int numValues,
		ref int expectedLeafBlackCount,
		int currentLeafBlackCount)
	{
		numValues += n.GetNumValues();

		// All red nodes must have two black children where null children are considered black.
		if (n.Red)
		{
			if (!(IsNull(n.L) || !n.L.Red))
				return (false, n.Low, n.GetHigh());
			if (!(IsNull(n.R) || !n.R.Red))
				return (false, n.Low, n.GetHigh());
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

		var highestSubTreeValue = n.GetHigh();
		var lowestSubTreeValue = n.Low;

		// Check left node.
		if (!IsNull(n.L))
		{
			if (n.L.Parent != n)
				return (false, default, default);
			var (leftValid, leftLow, leftHigh) =
				IsNodeValid(n.L, ref previousMin, ref numValues, ref expectedLeafBlackCount, currentLeafBlackCount);
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
				IsNodeValid(n.R, ref previousMin, ref numValues, ref expectedLeafBlackCount, currentLeafBlackCount);
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
