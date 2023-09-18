using System;
using System.Collections;
using System.Collections.Generic;

namespace Fumen;

/// <summary>
/// Red Black Tree.
/// Self-balancing binary search tree.
/// O(log(N)) time complexity inserts.
/// O(log(N)) time complexity deletes.
/// O(log(N)) time complexity finds.
/// Amortized O(1) time complexity enumeration.
/// O(N) memory usage.
/// Not thread safe.
/// Duplicate values are not supported.
/// </summary>
/// <typeparam name="T">Type of data stored in the tree.</typeparam>
public class RedBlackTree<T> : IEnumerable<T> where T : IComparable<T>
{
	/// <summary>
	/// RedBlackTree Node.
	/// </summary>
	private class Node
	{
		public T Value;
		public Node Parent;
		public Node L;
		public Node R;
		public bool Red;
	}

	/// <summary>
	/// Enumerator interface.
	/// </summary>
	public interface IRedBlackTreeEnumerator : IEnumerator<T>
	{
		public IRedBlackTreeEnumerator Clone();
		public bool MovePrev();
		public void Unset();
		public bool IsCurrentValid();
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
	/// Number of elements in the Red Black Tree.
	/// </summary>
	public int Count { get; private set; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public RedBlackTree()
	{
		Nil = new Node();
		Root = Nil;
		Count = 0;
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
	/// Inserts a value into the tree.
	/// The value must be unique.
	/// </summary>
	/// <param name="value">Value to insert.</param>
	/// <returns>IRedBlackTreeEnumerator to the inserted value.</returns>
	/// <exception cref="ArgumentException">Throws ArgumentException if the given value is not unique.</exception>
	public IRedBlackTreeEnumerator Insert(T value)
	{
		Count++;
		var n = new Node
		{
			Value = value,
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
			x = n.Value.CompareTo(x.Value) < 0 ? x.L : x.R;
		}

		n.Parent = p;
		var comparison = n.Value.CompareTo(p.Value);
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
			throw new ArgumentException($"Duplicate value inserted into RedBlackTree: {value}");
		}

		n.Red = true;

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
	/// Finds the Node with the given value.
	/// </summary>
	/// <param name="value">Value to find.</param>
	/// <returns>Node associated with the given value or Nil if no Node exists with that value.</returns>
	private Node FindNode(T value)
	{
		var n = Root;
		while (n != Nil)
		{
			var c = value.CompareTo(n.Value);
			if (c == 0)
				return n;
			n = c < 0 ? n.L : n.R;
		}

		return n;
	}

	/// <summary>
	/// Finds the given value.
	/// </summary>
	/// <param name="value">Value to find.</param>
	/// <returns>Enumerator to value or null if not found.</returns>
	public IRedBlackTreeEnumerator Find(T value)
	{
		var n = FindNode(value);
		return IsNull(n) ? null : new Enumerator(this, n);
	}

	/// <summary>
	/// Finds the greatest value preceding the given value.
	/// </summary>
	/// <param name="value">Value to use to find the greatest preceding value.</param>
	/// <param name="orEqualTo">If true, also include a value if it is equal to the given value.</param>
	/// <returns>Enumerator to greatest preceding value or null if not found.</returns>
	public IRedBlackTreeEnumerator FindGreatestPreceding(T value, bool orEqualTo = false)
	{
		var p = Nil;
		var n = Root;
		Node prev;
		while (n != Nil)
		{
			var c = value.CompareTo(n.Value);
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

		if (value.CompareTo(p.Value) > 0)
			return new Enumerator(this, p);

		prev = Prev(p);
		if (prev == Nil)
			return null;
		return new Enumerator(this, prev);
	}

	/// <summary>
	/// Finds the least value following the given value.
	/// </summary>
	/// <param name="value">Value to use to find the least following value.</param>
	/// <param name="orEqualTo">If true, also include a value if it is equal to the given value.</param>
	/// <returns>Enumerator to least following value or null if not found.</returns>
	public IRedBlackTreeEnumerator FindLeastFollowing(T value, bool orEqualTo = false)
	{
		var p = Nil;
		var n = Root;
		Node next;
		while (n != Nil)
		{
			var c = value.CompareTo(n.Value);
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

		if (value.CompareTo(p.Value) < 0)
			return new Enumerator(this, p);

		next = Next(p);
		if (next == Nil)
			return null;
		return new Enumerator(this, next);
	}

	/// <summary>
	/// Deletes the given value.
	/// </summary>
	/// <param name="value">Value to delete.</param>
	/// <returns>True if the value was found and deleted and false otherwise.</returns>
	public bool Delete(T value)
	{
		var n = FindNode(value);
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

		var x = y.L != Nil ? y.L : y.R;
		x.Parent = y.Parent;
		if (y.Parent == Nil)
			Root = x;
		else if (y == y.Parent.L)
			y.Parent.L = x;
		else
			y.Parent.R = x;
		if (y != n)
			n.Value = y.Value;
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
	/// Returns an IRedBlackTreeEnumerator to the first value in the tree.
	/// </summary>
	/// <returns>IRedBlackTreeEnumerator to the first value in the tree.</returns>
	public IRedBlackTreeEnumerator First()
	{
		return new Enumerator(this);
	}

	/// <summary>
	/// Returns an IRedBlackTreeEnumerator to the last value in the tree.
	/// </summary>
	/// <returns>IRedBlackTreeEnumerator to the last value in the tree.</returns>
	public IRedBlackTreeEnumerator Last()
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

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	#endregion IEnumerable

	#region Enumerator

	/// <summary>
	/// Enumerator for a Red Black Tree.
	/// </summary>
	private sealed class Enumerator : IRedBlackTreeEnumerator
	{
		private readonly RedBlackTree<T> Tree;
		private Node CurrentNode;
		private bool IsUnset;
		private bool BeforeFirst;
		private bool AfterLast;

		public Enumerator(RedBlackTree<T> tree)
		{
			Tree = tree;
			CurrentNode = Tree.Nil;
			Reset();
		}

		public Enumerator(RedBlackTree<T> tree, Node n)
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

		public IRedBlackTreeEnumerator Clone()
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

		public T Current
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
	/// Unit Test helper for checking whether this tree is a valid red black tree.
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
		var previousValue = default(T);
		if (!IsNodeValid(r, ref previousValue, ref numNodes, ref expectedLeafBlackCount, 0))
			return false;

		return numNodes == Count;
	}

	/// <summary>
	/// Recursive helper unit tests that checks validity of a Node.
	/// </summary>
	/// <returns>
	/// True if the given Node and all its children are valid and false otherwise.
	/// </returns>
	private bool IsNodeValid(
		Node n,
		ref T previousValue,
		ref int numNodes,
		ref int expectedLeafBlackCount,
		int currentLeafBlackCount)
	{
		numNodes++;

		// All red nodes must have two black children where null children are considered black.
		if (n.Red)
		{
			if (!(IsNull(n.L) || !n.L.Red))
				return false;
			if (!(IsNull(n.R) || !n.R.Red))
				return false;
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
				return false;

			// From left to right, leaf nodes should be sorted.
			if (!firstLeaf)
			{
				if (n.Value.CompareTo(previousValue) < 0)
					return false;
			}

			previousValue = n.Value;
		}

		// Check left node.
		if (!IsNull(n.L))
		{
			if (n.L.Parent != n)
				return false;
			if (!IsNodeValid(n.L, ref previousValue, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount))
				return false;
		}

		// Check right node.
		if (!IsNull(n.R))
		{
			if (n.R.Parent != n)
				return false;
			if (!IsNodeValid(n.R, ref previousValue, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount))
				return false;
		}

		return true;
	}

	#endregion Test Helpers
}
