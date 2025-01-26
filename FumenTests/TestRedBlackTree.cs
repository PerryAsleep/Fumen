using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToModifiedClosure

namespace FumenTests;

[TestClass]
public class TestRedBlackTree
{
	internal sealed class BoxedDouble : IComparable<BoxedDouble>, IComparable<BoxedInt>
	{
		private readonly double Value;

		public BoxedDouble(double value)
		{
			Value = value;
		}

		public double GetValue()
		{
			return Value;
		}

		public int CompareTo(BoxedDouble other)
		{
			return Value.CompareTo(other.GetValue());
		}

		public int CompareTo(BoxedInt other)
		{
			return Value.CompareTo(other.GetValue());
		}
	}

	internal sealed class BoxedInt : IComparable<BoxedInt>, IComparable<BoxedDouble>
	{
		private readonly int Value;

		public BoxedInt(int value)
		{
			Value = value;
		}

		public int GetValue()
		{
			return Value;
		}

		public int CompareTo(BoxedInt other)
		{
			return Value.CompareTo(other.GetValue());
		}

		public int CompareTo(BoxedDouble other)
		{
			return Value.CompareTo((int)other.GetValue());
		}
	}

	private RedBlackTree<int> CreateEvenIntTree(int max)
	{
		Debug.Assert(max % 2 == 0);
		var t = new RedBlackTree<int>();
		for (var i = 0; i <= max; i += 2)
			t.Insert(i);
		return t;
	}

	private RedBlackTree<BoxedDouble> CreateEvenBoxedDoubleTree(int max)
	{
		Debug.Assert(max % 2 == 0);
		var t = new RedBlackTree<BoxedDouble>();
		for (var i = 0; i <= max; i += 2)
			t.Insert(new BoxedDouble(i));
		return t;
	}

	private static int CustomIntCompare(int a, int b)
	{
		return a.CompareTo(b);
	}

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
		insertList = [];
		deleteList = [];
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
		insertList = [];
		deleteList = [];
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
	public void TestFirstValue()
	{
		var t = new RedBlackTree<int>();
		Assert.IsFalse(t.FirstValue(out var r));

		for (var i = 0; i < 10; i++)
			t.Insert(i);
		Assert.IsTrue(t.FirstValue(out r));
		Assert.AreEqual(0, r);
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
	public void TestLastValue()
	{
		var t = new RedBlackTree<int>();
		Assert.IsFalse(t.LastValue(out var r));

		for (var i = 0; i < 10; i++)
			t.Insert(i);
		Assert.IsTrue(t.LastValue(out r));
		Assert.AreEqual(9, r);
	}

	[TestMethod]
	public void TestFind()
	{
		// Finding in an empty tree should return null.
		var t = new RedBlackTree<int>();
		Assert.IsNull(t.Find(0));

		for (var i = 0; i < 10; i += 2)
			t.Insert(i);

		// Finding elements outside the range of the tree should return null.
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
	public void TestFindGreatestPreceding_Empty_ReturnsNull()
	{
		var t = new RedBlackTree<int>();
		Assert.IsNull(t.FindGreatestPreceding(0));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(-1));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_OrEqual_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(-1, true));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_EqualToLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(0));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindGreatestPreceding(0, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max + 1);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_OrEqual_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max + 1, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_EqualToGreatest_ReturnsPreceding()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max - 2, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_MidRange()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		for (var i = 1; i <= max; i++)
		{
			// Check without equals.
			var expected = i - 1;
			if (expected % 2 == 1)
				expected -= 1;
			var e = t.FindGreatestPreceding(i);
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
	public void TestFindGreatestPreceding_Comparable_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		Assert.IsNull(t.FindGreatestPreceding(new BoxedInt(-1)));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_OrEqual_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		Assert.IsNull(t.FindGreatestPreceding(new BoxedInt(-1), true));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_EqualToLeast_ReturnsNull()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		Assert.IsNull(t.FindGreatestPreceding(new BoxedInt(0)));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		var e = t.FindGreatestPreceding(new BoxedInt(0), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(0.0));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		var e = t.FindGreatestPreceding(new BoxedInt(max + 1));
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(max));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_OrEqual_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		var e = t.FindGreatestPreceding(new BoxedInt(max + 1), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(max));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_EqualToGreatest_ReturnsPreceding()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		var e = t.FindGreatestPreceding(new BoxedInt(max));
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(max - 2));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		var e = t.FindGreatestPreceding(new BoxedInt(max), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(max));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_Comparable_MidRange()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		for (var i = 1; i <= max; i++)
		{
			// Check without equals.
			var expected = i - 1;
			if (expected % 2 == 1)
				expected -= 1;
			var e = t.FindGreatestPreceding(new BoxedInt(i));
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.Current!.GetValue().DoubleEquals(expected));

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected -= 1;
			e = t.FindGreatestPreceding(new BoxedInt(i), true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.Current!.GetValue().DoubleEquals(expected));
		}
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(-1, CustomIntCompare));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_OrEqual_LessThanLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(-1, CustomIntCompare, true));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_EqualToLeast_ReturnsNull()
	{
		var t = CreateEvenIntTree(100);

		Assert.IsNull(t.FindGreatestPreceding(0, CustomIntCompare));
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindGreatestPreceding(0, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max + 1, CustomIntCompare);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_OrEqual_GreaterThanGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max + 1, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_EqualToGreatest_ReturnsPreceding()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max, CustomIntCompare);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max - 2, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindGreatestPreceding(max, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindGreatestPreceding_CustomComparer_MidRange()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		for (var i = 1; i <= max; i++)
		{
			// Check without equals.
			var expected = i - 1;
			if (expected % 2 == 1)
				expected -= 1;
			var e = t.FindGreatestPreceding(i, CustomIntCompare);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected -= 1;
			e = t.FindGreatestPreceding(i, CustomIntCompare, true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);
		}
	}

	[TestMethod]
	public void TestFindLeastFollowing_Empty_ReturnsNull()
	{
		var t = new RedBlackTree<int>();
		Assert.IsNull(t.FindLeastFollowing(0));
	}

	[TestMethod]
	public void TestFindLeastFollowing_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(-1);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_OrEqual_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(-1, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_EqualToLeast_ReturnsFollowing()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(0);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(2, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(0, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max + 1));
	}

	[TestMethod]
	public void TestFindLeastFollowing_OrEqual_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max + 1, true));
	}

	[TestMethod]
	public void TestFindLeastFollowing_EqualToGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max));
	}

	[TestMethod]
	public void TestFindLeastFollowing_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindLeastFollowing(max, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_MidRange()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		for (var i = 0; i < max - 2; i++)
		{
			// Check without equals.
			var expected = i + 1;
			if (expected % 2 == 1)
				expected += 1;
			var e = t.FindLeastFollowing(i);
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
	public void TestFindLeastFollowing_Comparable_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		var e = t.FindLeastFollowing(new BoxedInt(-1));
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(0.0));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_OrEqual_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		var e = t.FindLeastFollowing(new BoxedInt(-1), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(0.0));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_EqualToLeast_ReturnsFollowing()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		var e = t.FindLeastFollowing(new BoxedInt(0));
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(2.0));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenBoxedDoubleTree(100);

		var e = t.FindLeastFollowing(new BoxedInt(0), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(0.0));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		Assert.IsNull(t.FindLeastFollowing(new BoxedInt(max + 1)));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_OrEqual_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		Assert.IsNull(t.FindLeastFollowing(new BoxedInt(max + 1), true));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_EqualToGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		Assert.IsNull(t.FindLeastFollowing(new BoxedInt(max)));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		var e = t.FindLeastFollowing(new BoxedInt(max), true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.IsTrue(e.Current!.GetValue().DoubleEquals(max));
	}

	[TestMethod]
	public void TestFindLeastFollowing_Comparable_MidRange()
	{
		const int max = 100;
		var t = CreateEvenBoxedDoubleTree(max);

		for (var i = 0; i < max - 2; i++)
		{
			// Check without equals.
			var expected = i + 1;
			if (expected % 2 == 1)
				expected += 1;
			var e = t.FindLeastFollowing(new BoxedInt(i));
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.Current!.GetValue().DoubleEquals(expected));

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected += 1;
			e = t.FindLeastFollowing(new BoxedInt(i), true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.Current!.GetValue().DoubleEquals(expected));
		}
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(-1, CustomIntCompare);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_OrEqual_LessThanLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(-1, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_EqualToLeast_ReturnsFollowing()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(0, CustomIntCompare);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(2, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_OrEqual_EqualToLeast_ReturnsLeast()
	{
		var t = CreateEvenIntTree(100);

		var e = t.FindLeastFollowing(0, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(0, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max + 1, CustomIntCompare));
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_OrEqual_GreaterThanGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max + 1, CustomIntCompare, true));
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_EqualToGreatest_ReturnsNull()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		Assert.IsNull(t.FindLeastFollowing(max, CustomIntCompare));
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_OrEqual_EqualToGreatest_ReturnsGreatest()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		var e = t.FindLeastFollowing(max, CustomIntCompare, true);
		Assert.IsNotNull(e);
		Assert.ThrowsException<InvalidOperationException>(() => e.Current);
		Assert.IsTrue(e.MoveNext());
		Assert.AreEqual(max, e.Current);
	}

	[TestMethod]
	public void TestFindLeastFollowing_CustomComparer_MidRange()
	{
		const int max = 100;
		var t = CreateEvenIntTree(max);

		for (var i = 0; i < max - 2; i++)
		{
			// Check without equals.
			var expected = i + 1;
			if (expected % 2 == 1)
				expected += 1;
			var e = t.FindLeastFollowing(i, CustomIntCompare);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(expected, e.Current);

			// Check with equals.
			expected = i;
			if (expected % 2 == 1)
				expected += 1;
			e = t.FindLeastFollowing(i, CustomIntCompare, true);
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
