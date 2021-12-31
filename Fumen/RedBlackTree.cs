﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Fumen
{
	/// <summary>
	/// Red Black Tree.
	/// O(log(N)) time complexity inserts.
	/// O(log(N)) time complexity deletes.
	/// O(log(N)) time complexity finds.
	/// Amortized O(1) time complexity enumeration.
	/// O(log(N)) memory usage.
	/// Not thread safe.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RedBlackTree<T> : IEnumerable where T : IComparable<T>
	{
		/// <summary>
		/// Red Black Tree Node.
		/// </summary>
		/// <remarks>
		/// Public to allow the Enumerator to access a Node via construction.
		/// Public scoping also allows for unit tests which assert that the tree is structured as expected.
		/// </remarks>
		public class Node
		{
			public T Data;
			public Node Parent;
			public Node L;
			public Node R;
			public bool Red;
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
		/// Custom IComparer function to override default IComparable comparisons on T.
		/// </summary>
		private readonly IComparer<T> CustomComparer;
		/// <summary>
		/// Number of elements in the Red Black Tree.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="customComparer">
		/// Optional IComparer. If not null, this IComparer will be used instead of the IComparable methods on T.
		/// </param>
		public RedBlackTree(IComparer<T> customComparer = null)
		{
			CustomComparer = customComparer;
			Nil = new Node();
			Root = Nil;
			Count = 0;
		}

		/// <summary>
		/// Root Accessor.
		/// </summary>
		/// <remarks>Public for Enumerator and unit tests.</remarks>
		/// <returns>Root node.</returns>
		public Node GetRoot()
		{
			return Root;
		}

		private int Compare(Node n1, Node n2)
		{
			return CustomComparer?.Compare(n1.Data, n2.Data) ?? n1.Data.CompareTo(n2.Data);
		}

		private int Compare(T t1, T t2)
		{
			return CustomComparer?.Compare(t1, t2) ?? t1.CompareTo(t2);
		}

		/// <summary>
		/// Returns whether the node should be considered null.
		/// Null nodes, or the Nil sentinel Node will be considered null.
		/// </summary>
		/// <remarks>Public for Enumerator and unit tests.</remarks>
		/// <param name="n">Node to check.</param>
		/// <returns>Whether or not the Node should be considered null or not.</returns>
		public bool IsNull(Node n)
		{
			return n == null || n == Nil;
		}

		private void RotateLeft(Node x)
		{
			var y = x.R;
			x.R = y.L;
			if (y.L != Nil)
				y.L.Parent = x;
			y.Parent = x.Parent;
			if (x.Parent == Nil)
				Root = y;
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

		private void RotateRight(Node x)
		{
			var y = x.L;
			x.L = y.R;
			if (y.R != Nil)
				y.R.Parent = x;
			y.Parent = x.Parent;
			if (x.Parent == Nil)
				Root = y;
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

		public void Insert(T data)
		{
			Count++;
			var n = new Node
			{
				Data = data,
				Parent = Nil,
				R = Nil,
				L = Nil
			};
			if (IsNull(Root))
			{
				Root = n;
				return;
			}

			var x = Root;
			Node p = Nil;
			while (x != Nil)
			{
				p = x;
				x = Compare(n, x) < 0 ? x.L : x.R;
			}

			n.Parent = p;
			if (Compare(n, p) < 0)
				p.L = n;
			else
				p.R = n;
			n.Red = true;

			if (n.Parent.Parent == Nil)
				return;

			InsertFix(n);
		}

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

		private Node FindNode(T data)
		{
			var n = Root;
			while (n != Nil)
			{
				var c = Compare(data, n.Data);
				if (c == 0)
					return n;
				n = c < 0 ? n.L : n.R;
			}

			return null;
		}

		public Enumerator Find(T data)
		{
			var n = FindNode(data);
			return n == null ? null : new Enumerator(this, n);
		}

		public Enumerator FindGreatestPreceding(T data)
		{
			var p = Nil;
			var n = Root;
			Node prev;
			while (n != Nil)
			{
				var c = Compare(data, n.Data);
				if (c == 0)
				{
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

			if (Compare(data, p.Data) > 0)
				return new Enumerator(this, p);

			prev = Prev(p);
			if (prev == Nil)
				return null;
			return new Enumerator(this, prev);
		}

		public Enumerator FindLeastFollowing(T data)
		{
			var p = Nil;
			var n = Root;
			Node next;
			while (n != Nil)
			{
				var c = Compare(data, n.Data);
				if (c == 0)
				{
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

			if (Compare(data, p.Data) < 0)
				return new Enumerator(this, p);

			next = Next(p);
			if (next == Nil)
				return null;
			return new Enumerator(this, next);
		}

		public void Delete(T data)
		{
			var n = FindNode(data);
			if (n == null)
				return;

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
				n.Data = y.Data;
			if (!y.Red)
				DeleteFix(x);
		}

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

		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion IEnumerable

		/// <summary>
		/// Enumerator for a Red Black Tree.
		/// </summary>
		public class Enumerator : IEnumerator
		{
			private readonly RedBlackTree<T> Tree;
			private Node CurrentNode;
			private bool BeforeFirst;

			public Enumerator(RedBlackTree<T> tree)
			{
				Tree = tree;
				Reset();
			}

			public Enumerator(RedBlackTree<T> tree, Node n)
			{
				Tree = tree;
				CurrentNode = n;
				BeforeFirst = true;
			}

			public Enumerator(Enumerator e)
			{
				Tree = e.Tree;
				CurrentNode = e.CurrentNode;
				BeforeFirst = e.BeforeFirst;
			}

			public bool MoveNext()
			{
				if (BeforeFirst)
					BeforeFirst = false;
				else
					CurrentNode = Tree.Next(CurrentNode);
				return !Tree.IsNull(CurrentNode);
			}

			public bool MovePrev()
			{
				if (BeforeFirst)
					BeforeFirst = false;
				else
					CurrentNode = Tree.Prev(CurrentNode);
				return !Tree.IsNull(CurrentNode);
			}

			public void Reset()
			{
				BeforeFirst = true;
				CurrentNode = Tree.GetRoot();
				while (!Tree.IsNull(CurrentNode.L))
					CurrentNode = CurrentNode.L;
			}

			object IEnumerator.Current => Current;

			public T Current
			{
				get
				{
					if (BeforeFirst)
						throw new InvalidOperationException();
					return CurrentNode.Data;
				}
			}
		}
	}
}