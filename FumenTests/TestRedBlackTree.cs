﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToModifiedClosure

namespace FumenTests;

[TestClass]
public class TestRedBlackTree
{
	[TestMethod]
	public void TestEmpty()
	{
		var t = new RedBlackTree<int>();
		Assert.IsTrue(t.IsValid());
	}

	[TestMethod]
	public void TestInOrderInsertDelete()
	{
		var t = new RedBlackTree<int>();

		const int num = 10000;

		// Insert ascending, delete ascending
		for (var i = 0; i < num; i++)
		{
			t.Insert(i);
			Assert.AreEqual(i + 1, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		for (var i = 0; i < num; i++)
		{
			Assert.IsTrue(t.Delete(i));
			Assert.AreEqual(num - 1 - i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		// Insert ascending, delete descending
		for (var i = 0; i < num; i++)
		{
			t.Insert(i);
			Assert.AreEqual(i + 1, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		for (var i = num - 1; i >= 0; i--)
		{
			Assert.IsTrue(t.Delete(i));
			Assert.AreEqual(i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		// Insert descending, delete ascending
		for (var i = num - 1; i >= 0; i--)
		{
			t.Insert(i);
			Assert.AreEqual(num - i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		for (var i = 0; i < num; i++)
		{
			Assert.IsTrue(t.Delete(i));
			Assert.AreEqual(num - 1 - i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		// Insert descending, delete descending
		for (var i = num - 1; i >= 0; i--)
		{
			t.Insert(i);
			Assert.AreEqual(num - i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		for (var i = num - 1; i >= 0; i--)
		{
			Assert.IsTrue(t.Delete(i));
			Assert.AreEqual(i, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}
	}

	[TestMethod]
	public void TestDuplicates()
	{
		var t = new RedBlackTree<int>();
		const int value = 4;

		Assert.IsNotNull(t.Insert(value));
		Assert.AreEqual(1, t.GetCount());
		Assert.IsTrue(t.IsValid());

		Assert.ThrowsException<ArgumentException>(() => t.Insert(value));
		Assert.AreEqual(1, t.GetCount());
		Assert.IsTrue(t.IsValid());

		Assert.IsTrue(t.Delete(value));
		Assert.AreEqual(0, t.GetCount());
		Assert.IsTrue(t.IsValid());
	}

	[TestMethod]
	public void TestDeleteReturnValue()
	{
		var t = new RedBlackTree<int>();

		t.Insert(1);
		t.Insert(2);
		t.Insert(3);

		Assert.IsFalse(t.Delete(0));
		Assert.IsFalse(t.Delete(4));

		Assert.IsTrue(t.Delete(1));
		Assert.IsTrue(t.Delete(2));
		Assert.IsTrue(t.Delete(3));

		Assert.IsFalse(t.Delete(1));
		Assert.IsFalse(t.Delete(2));
		Assert.IsFalse(t.Delete(3));
	}

	[TestMethod]
	public void TestRandom()
	{
		var t = new RedBlackTree<int>();

		const int randomSeed = 524614862;
		var random = new Random(randomSeed);
		const int num = 10000;

		// Insert and delete the same set of random numbers.
		var insertList = new List<int>();
		var deleteList = new List<int>();
		for (var i = 0; i < num; i++)
		{
			var val = random.Next();
			insertList.Add(val);
			deleteList.Add(val);
		}

		deleteList = deleteList.OrderBy(_ => random.Next()).ToList();
		var expectedCount = 0;
		foreach (var val in insertList)
		{
			t.Insert(val);
			expectedCount++;
			Assert.AreEqual(expectedCount, t.GetCount());
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			Assert.IsTrue(t.Delete(val));
			expectedCount--;
			Assert.AreEqual(expectedCount, t.GetCount());
			Assert.IsTrue(t.IsValid());
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
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			t.Delete(val);
			Assert.IsTrue(t.IsValid());
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

		deleteList = deleteList.OrderBy(_ => random.Next()).ToList();
		foreach (var val in insertList)
		{
			t.Insert(val);
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			t.Delete(val);
			Assert.IsTrue(t.IsValid());
		}
	}

	[TestMethod]
	public void TestEnumerator()
	{
		var t = new RedBlackTree<int>();

		// Iterate over empty tree.
		var numCounted = 0;
		foreach (var _ in t)
		{
			numCounted++;
		}

		Assert.AreEqual(0, t.GetCount());
		Assert.AreEqual(0, numCounted);

		// Iterate over 1 element.
		t.Insert(0);
		numCounted = 0;
		foreach (var i in t)
		{
			Assert.AreEqual(0, i);
			numCounted++;
		}

		Assert.AreEqual(1, t.GetCount());
		Assert.AreEqual(1, numCounted);

		const int num = 10000;

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

		Assert.AreEqual(num, t.GetCount());
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

		Assert.AreEqual(num, t.GetCount());
		Assert.AreEqual(num, numCounted);

		// Iterate over multiple elements inserted in random order.
		t = new RedBlackTree<int>();
		const int randomSeed = 13430297;
		var random = new Random(randomSeed);
		var insertList = new List<int>();
		for (var i = 0; i < num; i++)
			insertList.Add(i);
		insertList = insertList.OrderBy(_ => random.Next()).ToList();
		foreach (var i in insertList)
			t.Insert(i);
		expected = 0;
		numCounted = 0;
		foreach (var val in t)
		{
			Assert.AreEqual(expected++, val);
			numCounted++;
		}

		Assert.AreEqual(num, t.GetCount());
		Assert.AreEqual(num, numCounted);
	}

	[TestMethod]
	public void TestEnumeratorPrev()
	{
		var t = new RedBlackTree<int>();
		for (var i = 0; i < 10; i++)
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
	public void TestEnumeratorUnset()
	{
		var t = new RedBlackTree<int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i);

		var e = t.FirstMutable();
		Assert.IsFalse(e.IsCurrentValid());
		while (e.MoveNext())
		{
			Assert.IsTrue(e.IsCurrentValid());
		}

		Assert.IsFalse(e.IsCurrentValid());

		e = t.FindMutable(5);
		Assert.IsFalse(e.IsCurrentValid());
		e.MoveNext();
		Assert.IsTrue(e.IsCurrentValid());
		e.Unset();
		Assert.IsFalse(e.IsCurrentValid());
		e.MoveNext();
		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(5, e.Current);
	}

	[TestMethod]
	public void TestEnumeratorDelete()
	{
		var t = new RedBlackTree<int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i);

		const int deletedValue = 5;
		var e = t.FindMutable(deletedValue);

		// Deleting unset enumerator should throw an exception and not delete a value.
		Assert.IsFalse(e.IsCurrentValid());
		Assert.ThrowsException<InvalidOperationException>(e.Delete);
		Assert.AreEqual(10, t.GetCount());

		// Deleting a set enumerator should delete a value and unset the enumerator.
		e.MoveNext();
		Assert.IsTrue(e.IsCurrentValid());
		e.Delete();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.AreEqual(9, t.GetCount());
		var index = 0;
		foreach (var e2 in t)
		{
			if (index < deletedValue)
				Assert.AreEqual(index, e2);
			else
				Assert.AreEqual(index + 1, e2);
			index++;
		}
	}

	[TestMethod]
	public void TestFirst()
	{
		var t = new RedBlackTree<int>();
		var e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsFalse(e.MoveNext());
		Assert.IsFalse(e.IsCurrentValid());

		for (var i = 0; i < 10; i++)
			t.Insert(i);
		e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestLast()
	{
		var t = new RedBlackTree<int>();
		var e = t.Last();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsFalse(e.MovePrev());
		Assert.IsFalse(e.IsCurrentValid());

		for (var i = 0; i < 10; i++)
			t.Insert(i);
		e = t.Last();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MovePrev());
		Assert.AreEqual(9, e.Current);
	}

	[TestMethod]
	public void TestFind()
	{
		// Finding in an empty tree should return null.
		var t = new RedBlackTree<int>();
		Assert.IsNull(t.Find(0));

		for (var i = 0; i < 10; i += 2)
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

		const int num = 100;
		for (var i = 0; i < num; i += 2)
			t.Insert(i);

		// Finding element less than least element should return null.
		Assert.IsNull(t.FindGreatestPreceding(-1));
		Assert.IsNull(t.FindGreatestPreceding(-1, true));

		// Finding element less than greatest element should return greatest element.
		var e = t.FindGreatestPreceding(num + 1);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(num - 2, e.Current);
		e = t.FindGreatestPreceding(num + 1, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(num - 2, e.Current);

		// Finding least element should return null when not using orEqualTo=true.
		Assert.IsNull(t.FindGreatestPreceding(0));
		// Finding least element should return that element when using orEqualTo=true.
		e = t.FindGreatestPreceding(0, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);

		// Find elements after the least element should return the greatest
		// preceding element.
		for (var i = 1; i < num; i++)
		{
			// Check without equals.
			var expected = i - 1;
			if (expected % 2 == 1)
				expected -= 1;
			e = t.FindGreatestPreceding(i);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected -= 1;
			e = t.FindGreatestPreceding(i, true);
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

		const int num = 100;
		for (var i = 0; i < num; i += 2)
			t.Insert(i);

		// Finding element greater than least element should return least element.
		var e = t.FindLeastFollowing(-1);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
		e = t.FindLeastFollowing(-1, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);

		// Finding element greater than greatest element should return null.
		Assert.IsNull(t.FindLeastFollowing(num));
		Assert.IsNull(t.FindLeastFollowing(num, true));

		// Finding greatest element should return null when not using orEqualTo=true.
		Assert.IsNull(t.FindLeastFollowing(num - 2));
		// Finding greatest element should return that element when using orEqualTo=true.
		e = t.FindLeastFollowing(num - 2, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(num - 2, e.Current);

		// Find elements before the greatest element should return the least
		// following element.
		for (var i = 0; i < num - 2; i++)
		{
			// Check without equals.
			var expected = i + 1;
			if (expected % 2 == 1)
				expected += 1;
			e = t.FindLeastFollowing(i);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected += 1;
			e = t.FindLeastFollowing(i, true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);
		}
	}

	[TestMethod]
	public void TestCloneReadOnlyEnumerator()
	{
		var t = new RedBlackTree<int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i);

		var e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(0, e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(1, e.Current);

		var e2 = e.Clone();
		Assert.IsTrue(e2.IsCurrentValid());
		Assert.AreEqual(1, e2.Current);
		Assert.IsTrue(e2.MoveNext());
		Assert.IsTrue(e2.IsCurrentValid());
		Assert.AreEqual(2, e2.Current);

		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(1, e.Current);
	}

	[TestMethod]
	public void TestCloneEnumerator()
	{
		var t = new RedBlackTree<int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i);

		var e = t.FirstMutable();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(0, e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(1, e.Current);

		var e2 = e.Clone();
		Assert.IsTrue(e2.IsCurrentValid());
		Assert.AreEqual(1, e2.Current);
		Assert.IsTrue(e2.MoveNext());
		Assert.IsTrue(e2.IsCurrentValid());
		Assert.AreEqual(2, e2.Current);

		Assert.IsTrue(e.IsCurrentValid());
		Assert.AreEqual(1, e.Current);
	}
}
