using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChartGeneratorTests
{
	[TestClass]
	public class TestRedBlackTree
	{
		/// <summary>
		/// Helper method to assert that the given RedBlackTree is structured as expected.
		/// </summary>
		/// <typeparam name="T">Type of tree.</typeparam>
		/// <param name="t">Tree to check.</param>
		private void CheckTreeValid<T>(RedBlackTree<T> t) where T : IComparable<T>
		{
			var r = t.GetRoot();
			if (t.Count == 0)
			{
				Assert.IsTrue(t.IsNull(r));
				return;
			}

			// The root must be black.
			Assert.IsNotNull(r);
			Assert.IsTrue(t.IsNull(r.Parent));
			Assert.IsTrue(!r.Red);

			// Check every node.
			var expectedLeafBlackCount = -1;
			var numNodes = 0;
			T previousValue = default(T);
			CheckNode(t, r, ref previousValue, ref numNodes, ref expectedLeafBlackCount, 0);

			Assert.AreEqual(numNodes, t.Count);
		}

		/// <summary>
		/// Recursive helper for checking a Node of a RedBlackTree.
		/// </summary>
		private void CheckNode<T>(
			RedBlackTree<T> t,
			RedBlackTree<T>.Node n,
			ref T previousValue,
			ref int numNodes,
			ref int expectedLeafBlackCount,
			int currentLeafBlackCount) where T : IComparable<T>
		{
			numNodes++;

			// All red nodes must have two black children where null children are considered black.
			if (n.Red)
			{
				Assert.IsTrue(t.IsNull(n.L) || !n.L.Red);
				Assert.IsTrue(t.IsNull(n.R) || !n.R.Red);
			}
			else
			{
				currentLeafBlackCount++;
			}

			// Leaf node checks.
			if (t.IsNull(n.L) && t.IsNull(n.R))
			{
				var firstLeaf = expectedLeafBlackCount == -1;

				// Leaf nodes must all have the same number of black ancestors.
				if (expectedLeafBlackCount == -1)
					expectedLeafBlackCount = currentLeafBlackCount;
				Assert.AreEqual(expectedLeafBlackCount, currentLeafBlackCount);

				// From left to right, leaf nodes should be sorted.
				if (!firstLeaf)
					Assert.IsTrue(n.Data.CompareTo(previousValue) >= 0);
				previousValue = n.Data;
			}

			// Check left node.
			if (!t.IsNull(n.L))
			{
				Assert.AreSame(n.L.Parent, n);
				CheckNode(t, n.L, ref previousValue, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount);
			}

			// Check right node.
			if (!t.IsNull(n.R))
			{
				Assert.AreSame(n.R.Parent, n);
				CheckNode(t, n.R, ref previousValue, ref numNodes, ref expectedLeafBlackCount, currentLeafBlackCount);
			}
		}

		[TestMethod]
		public void TestEmpty()
		{
			var t = new RedBlackTree<int>();
			CheckTreeValid(t);
		}

		[TestMethod]
		public void TestInOrderInsertDelete()
		{
			var t = new RedBlackTree<int>();

			var num = 10000;

			// Insert ascending, delete ascending
			for (var i = 0; i < num; i++)
			{
				t.Insert(i);
				Assert.AreEqual(i + 1, t.Count);
				CheckTreeValid(t);
			}

			for (var i = 0; i < num; i++)
			{
				t.Delete(i);
				Assert.AreEqual(num - 1 - i, t.Count);
				CheckTreeValid(t);
			}

			// Insert ascending, delete descending
			for (var i = 0; i < num; i++)
			{
				t.Insert(i);
				Assert.AreEqual(i + 1, t.Count);
				CheckTreeValid(t);
			}

			for (var i = num - 1; i >= 0; i--)
			{
				t.Delete(i);
				Assert.AreEqual(i, t.Count);
				CheckTreeValid(t);
			}

			// Insert descending, delete ascending
			for (var i = num - 1; i >= 0; i--)
			{
				t.Insert(i);
				Assert.AreEqual(num - i, t.Count);
				CheckTreeValid(t);
			}

			for (var i = 0; i < num; i++)
			{
				t.Delete(i);
				Assert.AreEqual(num - 1 - i, t.Count);
				CheckTreeValid(t);
			}

			// Insert descending, delete descending
			for (var i = num - 1; i >= 0; i--)
			{
				t.Insert(i);
				Assert.AreEqual(num - i, t.Count);
				CheckTreeValid(t);
			}

			for (var i = num - 1; i >= 0; i--)
			{
				t.Delete(i);
				Assert.AreEqual(i, t.Count);
				CheckTreeValid(t);
			}
		}

		[TestMethod]
		public void TestDuplicates()
		{
			var t = new RedBlackTree<int>();

			var num = 10000;
			var value = 4;

			for (var i = 0; i < num; i++)
			{
				t.Insert(value);
				Assert.AreEqual(i + 1, t.Count);
				CheckTreeValid(t);
			}

			for (var i = 0; i < num; i++)
			{
				t.Delete(value);
				Assert.AreEqual(num - 1 - i, t.Count);
				CheckTreeValid(t);
			}
		}

		[TestMethod]
		public void TestRandom()
		{
			var t = new RedBlackTree<int>();

			var randomSeed = 524614862;
			var random = new Random(randomSeed);
			var num = 10000;

			// Insert and delete the same set of random numbers.
			var insertList = new List<int>();
			var deleteList = new List<int>();
			for (var i = 0; i < num; i++)
			{
				var val = random.Next();
				insertList.Add(val);
				deleteList.Add(val);
			}

			deleteList = deleteList.OrderBy(x => random.Next()).ToList();
			var expectedCount = 0;
			foreach (var val in insertList)
			{
				t.Insert(val);
				expectedCount++;
				Assert.AreEqual(expectedCount, t.Count);
				CheckTreeValid(t);
			}

			foreach (var val in deleteList)
			{
				t.Delete(val);
				expectedCount--;
				Assert.AreEqual(expectedCount, t.Count);
				CheckTreeValid(t);
			}

			// Insert and delete different sets of random numbers.
			t = new RedBlackTree<int>();
			insertList = new List<int>();
			deleteList = new List<int>();
			for (var i = 0; i < num; i++)
			{
				insertList.Add(random.Next());
				deleteList.Add(random.Next());
			}

			foreach (var val in insertList)
			{
				t.Insert(val);
				CheckTreeValid(t);
			}

			foreach (var val in deleteList)
			{
				t.Delete(val);
				CheckTreeValid(t);
			}

			// Insert and delete different sets of random numbers with some overlap.
			t = new RedBlackTree<int>();
			insertList = new List<int>();
			deleteList = new List<int>();
			for (var i = 0; i < num; i++)
			{
				var val = random.Next();
				insertList.Add(val);
				deleteList.Add(i % 2 == 0 ? val : random.Next());
			}

			deleteList = deleteList.OrderBy(x => random.Next()).ToList();
			foreach (var val in insertList)
			{
				t.Insert(val);
				CheckTreeValid(t);
			}

			foreach (var val in deleteList)
			{
				t.Delete(val);
				CheckTreeValid(t);
			}
		}

		[TestMethod]
		public void TestEnumerator()
		{
			var t = new RedBlackTree<int>();

			// Iterate over empty tree.
			var numCounted = 0;
			foreach (var i in t)
			{
				numCounted++;
			}

			Assert.AreEqual(0, t.Count);
			Assert.AreEqual(0, numCounted);

			// Iterate over 1 element.
			t.Insert(0);
			numCounted = 0;
			foreach (var i in t)
			{
				Assert.AreEqual(0, i);
				numCounted++;
			}

			Assert.AreEqual(1, t.Count);
			Assert.AreEqual(1, numCounted);

			var num = 10000;

			// Iterate over multiple elements inserted in increasing order.
			t = new RedBlackTree<int>();
			for (var i = 0; i < num; i++)
				t.Insert(i);
			var expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected++, val);
				numCounted++;
			}

			Assert.AreEqual(num, t.Count);
			Assert.AreEqual(num, numCounted);

			// Iterate over multiple elements inserted in decreasing order.
			t = new RedBlackTree<int>();
			for (var i = num - 1; i >= 0; i--)
				t.Insert(i);
			expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected++, val);
				numCounted++;
			}

			Assert.AreEqual(num, t.Count);
			Assert.AreEqual(num, numCounted);

			// Iterate over multiple elements inserted in random order.
			t = new RedBlackTree<int>();
			var randomSeed = 13430297;
			var random = new Random(randomSeed);
			var insertList = new List<int>();
			for (var i = 0; i < num; i++)
				insertList.Add(i);
			insertList = insertList.OrderBy(x => random.Next()).ToList();
			foreach (var i in insertList)
				t.Insert(i);
			expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected++, val);
				numCounted++;
			}

			Assert.AreEqual(num, t.Count);
			Assert.AreEqual(num, numCounted);
		}

		[TestMethod]
		public void TestEnumeratorPrev()
		{
			var t = new RedBlackTree<int>();
			for (var i = 0; i < 10; i ++)
				t.Insert(i);

			// Finding an element should return an enumerator that needs to be
			// advanced to the starting element.
			var e = t.Find(0);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MovePrev());
			Assert.AreEqual(0, e.Current);
			Assert.IsFalse(e.MovePrev());

			// Finding the last element and iterating via MovePrev should move
			// in descending order through the tree.
			e = t.Find(9);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			for (var i = 9; i >= 0; i--)
			{
				Assert.IsTrue(e.MovePrev());
				Assert.AreEqual(i, e.Current);
			}
		}

		[TestMethod]
		public void TestFind()
		{
			// Finding in an empty tree should return null.
			var t = new RedBlackTree<int>();
			Assert.IsNull(t.Find(0));
			
			for(var i = 0; i < 10; i+=2)
				t.Insert(i);

			// Finding elements outside of the range of the tree should return null.
			Assert.IsNull(t.Find(-1));
			Assert.IsNull(t.Find(9));

			// Finding elements in the tree should return Enumerators to those elements.
			for (var i = 0; i < 10; i += 2)
			{
				var e = t.Find(i);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(i, e.Current);
			}

			// Finding elements not in the tree should return null
			for (var i = 1; i < 10; i += 2)
				Assert.IsNull(t.Find(i));
		}

		[TestMethod]
		public void TestFindGreatestPreceding()
		{
			// Finding in an empty tree should return null.
			var t = new RedBlackTree<int>();
			Assert.IsNull(t.FindGreatestPreceding(0));

			var num = 100;
			for (var i = 0; i < num; i += 2)
				t.Insert(i);

			// Finding element less than least element should return null.
			Assert.IsNull(t.FindGreatestPreceding(-1));

			// Finding least element should return null.
			Assert.IsNull(t.FindGreatestPreceding(0));

			// Find elements after the least element should return the greatest
			// preceding element.
			for (var i = 1; i < num + 1; i ++)
			{
				var expected = i - 1;
				if (expected % 2 == 1)
					expected -= 1;

				var e = t.FindGreatestPreceding(i);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);
			}
		}

		[TestMethod]
		public void TestFindLeastFollowing()
		{
			// Finding in an empty tree should return null.
			var t = new RedBlackTree<int>();
			Assert.IsNull(t.FindLeastFollowing(0));

			var num = 100;
			for (var i = 0; i < num; i += 2)
				t.Insert(i);

			// Finding element greater than greatest element should return null.
			Assert.IsNull(t.FindLeastFollowing(num));

			// Finding greatest element should return null.
			Assert.IsNull(t.FindLeastFollowing(num - 2));

			// Find elements before the greatest element should return the least
			// following element.
			for (var i = 0; i < num - 2; i++)
			{
				var expected = i + 1;
				if (expected % 2 == 1)
					expected += 1;

				var e = t.FindLeastFollowing(i);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);
			}
		}
	}
}
