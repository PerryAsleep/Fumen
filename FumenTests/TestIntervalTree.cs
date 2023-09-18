using System;
using System.Collections.Generic;
using System.Linq;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToModifiedClosure

namespace FumenTests;

[TestClass]
public class TestIntervalTree
{
	[TestMethod]
	public void TestEmpty()
	{
		var t = new IntervalTree<int, double>();
		Assert.IsTrue(t.IsValid());
	}

	[TestMethod]
	public void TestInOrderInsertDelete()
	{
		var t = new IntervalTree<int, double>();

		const int num = 10000;

		// Insert ascending, delete ascending
		for (var i = 0; i < num; i++)
		{
			t.Insert(i, i, i + 10);
			Assert.AreEqual(i + 1, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		for (var i = 0; i < num; i++)
		{
			Assert.IsTrue(t.Delete(i, i + 10));
			Assert.AreEqual(num - 1 - i, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		// Insert ascending, delete descending
		for (var i = 0; i < num; i++)
		{
			t.Insert(i, i, i + 10);
			Assert.AreEqual(i + 1, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		for (var i = num - 1; i >= 0; i--)
		{
			Assert.IsTrue(t.Delete(i, i + 10));
			Assert.AreEqual(i, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		// Insert descending, delete ascending
		for (var i = num - 1; i >= 0; i--)
		{
			t.Insert(i, i, i + 10);
			Assert.AreEqual(num - i, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		for (var i = 0; i < num; i++)
		{
			Assert.IsTrue(t.Delete(i, i + 10));
			Assert.AreEqual(num - 1 - i, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		// Insert descending, delete descending
		for (var i = num - 1; i >= 0; i--)
		{
			t.Insert(i, i, i + 10);
			Assert.AreEqual(num - i, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		for (var i = num - 1; i >= 0; i--)
		{
			Assert.IsTrue(t.Delete(i, i + 10));
			Assert.AreEqual(i, t.Count);
			Assert.IsTrue(t.IsValid());
		}
	}

	[TestMethod]
	public void TestDuplicates()
	{
		var t = new IntervalTree<int, double>();

		// Duplicate interval with same values.
		Assert.IsNotNull(t.Insert(4.0, 0, 10));
		Assert.AreEqual(1, t.Count);
		Assert.IsTrue(t.IsValid());

		Assert.ThrowsException<ArgumentException>(() => t.Insert(4.0, 0, 10));
		Assert.AreEqual(1, t.Count);
		Assert.IsTrue(t.IsValid());

		Assert.IsTrue(t.Delete(0, 10));
		Assert.AreEqual(0, t.Count);
		Assert.IsTrue(t.IsValid());

		// Duplicate interval with different values.
		Assert.IsNotNull(t.Insert(1.0, 0, 10));
		Assert.AreEqual(1, t.Count);
		Assert.IsTrue(t.IsValid());

		Assert.ThrowsException<ArgumentException>(() => t.Insert(2.0, 0, 10));
		Assert.AreEqual(1, t.Count);
		Assert.IsTrue(t.IsValid());

		Assert.IsTrue(t.Delete(0, 10));
		Assert.AreEqual(0, t.Count);
		Assert.IsTrue(t.IsValid());
	}

	[TestMethod]
	public void TestDeleteReturnValue()
	{
		var t = new IntervalTree<int, double>();

		t.Insert(1, 1, 2);
		t.Insert(2, 2, 3);
		t.Insert(3, 3, 4);

		Assert.IsFalse(t.Delete(0, 2));
		Assert.IsFalse(t.Delete(1, 3));
		Assert.IsFalse(t.Delete(2, 2));
		Assert.IsFalse(t.Delete(2, 4));

		Assert.IsTrue(t.Delete(1, 2));
		Assert.IsTrue(t.Delete(2, 3));
		Assert.IsTrue(t.Delete(3, 4));

		Assert.IsFalse(t.Delete(1, 2));
		Assert.IsFalse(t.Delete(2, 3));
		Assert.IsFalse(t.Delete(3, 4));
	}

	[TestMethod]
	public void TestRandom()
	{
		var t = new IntervalTree<int, double>();

		const int randomSeed = 623956004;
		var random = new Random(randomSeed);
		const int num = 10000;

		// Insert and delete the same set of random numbers.
		var insertList = new List<Tuple<int, int, int>>();
		var deleteList = new List<Tuple<int, int>>();
		for (var i = 0; i < num; i++)
		{
			var val = random.Next();
			var low = random.Next();
			var high = random.Next();
			if (low > high)
			{
				(low, high) = (high, low);
			}

			insertList.Add(new Tuple<int, int, int>(val, low, high));
			deleteList.Add(new Tuple<int, int>(low, high));
		}

		deleteList = deleteList.OrderBy(_ => random.Next()).ToList();
		var expectedCount = 0;
		foreach (var val in insertList)
		{
			t.Insert(val.Item1, val.Item2, val.Item3);
			expectedCount++;
			Assert.AreEqual(expectedCount, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			Assert.IsTrue(t.Delete(val.Item1, val.Item2));
			expectedCount--;
			Assert.AreEqual(expectedCount, t.Count);
			Assert.IsTrue(t.IsValid());
		}

		// Insert and delete different sets of random numbers.
		t = new IntervalTree<int, double>();
		insertList = new List<Tuple<int, int, int>>();
		deleteList = new List<Tuple<int, int>>();
		for (var i = 0; i < num; i++)
		{
			var val = random.Next();
			var low = random.Next();
			var high = random.Next();
			if (low > high)
			{
				(low, high) = (high, low);
			}

			insertList.Add(new Tuple<int, int, int>(val, low, high));

			low = random.Next();
			high = random.Next();
			if (low > high)
			{
				(low, high) = (high, low);
			}

			deleteList.Add(new Tuple<int, int>(low, high));
		}

		foreach (var val in insertList)
		{
			t.Insert(val.Item1, val.Item2, val.Item3);
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			t.Delete(val.Item1, val.Item2);
			Assert.IsTrue(t.IsValid());
		}

		// Insert and delete different sets of random numbers with some overlap.
		t = new IntervalTree<int, double>();
		insertList = new List<Tuple<int, int, int>>();
		deleteList = new List<Tuple<int, int>>();
		for (var i = 0; i < num; i++)
		{
			var val = random.Next();
			var low = random.Next();
			var high = random.Next();
			if (low > high)
			{
				(low, high) = (high, low);
			}

			insertList.Add(new Tuple<int, int, int>(val, low, high));

			if (i % 2 == 0)
			{
				deleteList.Add(new Tuple<int, int>(low, high));
			}
			else
			{
				low = random.Next();
				high = random.Next();
				if (low > high)
				{
					(low, high) = (high, low);
				}

				deleteList.Add(new Tuple<int, int>(low, high));
			}
		}

		deleteList = deleteList.OrderBy(_ => random.Next()).ToList();
		foreach (var val in insertList)
		{
			t.Insert(val.Item1, val.Item2, val.Item3);
			Assert.IsTrue(t.IsValid());
		}

		foreach (var val in deleteList)
		{
			t.Delete(val.Item1, val.Item2);
			Assert.IsTrue(t.IsValid());
		}
	}

	[TestMethod]
	public void TestEnumerator()
	{
		var t = new IntervalTree<int, int>();

		// Iterate over empty tree.
		var numCounted = 0;
		foreach (var _ in t)
		{
			numCounted++;
		}

		Assert.AreEqual(0, t.Count);
		Assert.AreEqual(0, numCounted);

		// Iterate over 1 element.
		t.Insert(0, 0, 10);
		numCounted = 0;
		foreach (var i in t)
		{
			Assert.AreEqual(0, i);
			numCounted++;
		}

		Assert.AreEqual(1, t.Count);
		Assert.AreEqual(1, numCounted);

		const int num = 10000;

		// Iterate over multiple elements inserted in increasing order.
		t = new IntervalTree<int, int>();
		for (var i = 0; i < num; i++)
			t.Insert(i, i, i + 10);
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
		t = new IntervalTree<int, int>();
		for (var i = num - 1; i >= 0; i--)
			t.Insert(i, i, i + 10);
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
		t = new IntervalTree<int, int>();
		const int randomSeed = 73329691;
		var random = new Random(randomSeed);
		var insertList = new List<Tuple<int, int, int>>();
		for (var i = 0; i < num; i++)
			insertList.Add(new Tuple<int, int, int>(i, i, i + 10));
		insertList = insertList.OrderBy(_ => random.Next()).ToList();
		foreach (var i in insertList)
			t.Insert(i.Item1, i.Item2, i.Item3);
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
		var t = new IntervalTree<int, int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i, i, i + 10);

		// Finding an element should return an enumerator that needs to be
		// advanced to the starting element.
		var e = t.Find(0, 10);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MovePrev());
		Assert.AreEqual(0, e.Current);
		Assert.IsFalse(e.MovePrev());

		// Finding the last element and iterating via MovePrev should move
		// in descending order through the tree.
		e = t.Find(9, 19);
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
		var t = new IntervalTree<int, int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i, i, i + 10);

		var e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		while (e.MoveNext())
		{
			Assert.IsTrue(e.IsCurrentValid());
		}

		Assert.IsFalse(e.IsCurrentValid());

		e = t.Find(5, 15);
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
		var t = new IntervalTree<int, int>();
		for (var i = 0; i < 10; i++)
			t.Insert(i, i, i + 10);

		const int deletedValue = 5;
		var e = t.Find(deletedValue, deletedValue + 10);

		// Deleting unset enumerator should throw an exception and not delete a value.
		Assert.IsFalse(e.IsCurrentValid());
		Assert.ThrowsException<InvalidOperationException>(e.Delete);
		Assert.AreEqual(10, t.Count);

		// Deleting a set enumerator should delete a value and unset the enumerator.
		e.MoveNext();
		Assert.IsTrue(e.IsCurrentValid());
		e.Delete();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.AreEqual(9, t.Count);
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
		var t = new IntervalTree<int, int>();
		var e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsFalse(e.MoveNext());
		Assert.IsFalse(e.IsCurrentValid());

		for (var i = 0; i < 10; i++)
			t.Insert(i, i, i + 10);
		e = t.First();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestLast()
	{
		var t = new IntervalTree<int, int>();
		var e = t.Last();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsFalse(e.MovePrev());
		Assert.IsFalse(e.IsCurrentValid());

		for (var i = 0; i < 10; i++)
			t.Insert(i, i, i + 10);
		e = t.Last();
		Assert.IsFalse(e.IsCurrentValid());
		Assert.IsTrue(e.MovePrev());
		Assert.AreEqual(9, e.Current);
	}

	[TestMethod]
	public void TestFind()
	{
		// Finding in an empty tree should return null.
		var t = new IntervalTree<int, int>();
		Assert.IsNull(t.Find(0, 1));

		for (var i = 0; i < 10; i += 2)
			t.Insert(i, i, i + 10);

		// Finding elements outside of the range of the tree should return null.
		Assert.IsNull(t.Find(-1, 9));
		Assert.IsNull(t.Find(9, 19));

		// Finding elements in the tree should return Enumerators to those elements.
		for (var i = 0; i < 10; i += 2)
		{
			var e = t.Find(i, i + 10);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(i, e.Current);
		}

		// Finding elements not in the tree should return null
		for (var i = 1; i < 10; i += 2)
			Assert.IsNull(t.Find(i, i + 10));
	}

	[TestMethod]
	public void TestFindAllOverlapping()
	{
		var t = new IntervalTree<int, char>();

		var intervals = new List<Tuple<char, int, int>>
		{
			new('a', 0, 5),
			new('b', 2, 22),
			new('c', 5, 18),
			new('d', 10, 13),
			new('e', 16, 19),
			new('f', 17, 26),
			new('g', 19, 21),
			new('h', 23, 25),
		};
		foreach (var interval in intervals)
		{
			t.Insert(interval.Item1, interval.Item2, interval.Item3);
		}

		for (var i = -1; i < 30; i++)
		{
			var inclusiveOverlaps = t.FindAllOverlapping(i);
			var exclusiveOverlaps = t.FindAllOverlapping(i, false, false);

			var expectedInclusiveResults = new List<char>();
			var expectedExclusiveResults = new List<char>();
			foreach (var interval in intervals)
			{
				if (interval.Item2 <= i && interval.Item3 >= i)
					expectedInclusiveResults.Add(interval.Item1);
				if (interval.Item2 < i && interval.Item3 > i)
					expectedExclusiveResults.Add(interval.Item1);
			}

			Assert.AreEqual(expectedInclusiveResults.Count, inclusiveOverlaps.Count);
			for (var overlapIndex = 0; overlapIndex < expectedInclusiveResults.Count; overlapIndex++)
			{
				Assert.AreEqual(expectedInclusiveResults[overlapIndex], inclusiveOverlaps[overlapIndex]);
			}

			Assert.AreEqual(expectedExclusiveResults.Count, exclusiveOverlaps.Count);
			for (var overlapIndex = 0; overlapIndex < expectedExclusiveResults.Count; overlapIndex++)
			{
				Assert.AreEqual(expectedExclusiveResults[overlapIndex], exclusiveOverlaps[overlapIndex]);
			}
		}
	}
}
