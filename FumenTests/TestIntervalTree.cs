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

		const int num = 1000;
		const int maxNumDuplicates = 3;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			// Insert ascending, delete ascending
			for (var n = 0; n < num; n++)
			{
				for (var d = 0; d < dMax; d++)
				{
					var i = n * dMax + d;
					t.Insert(i, i, i + 10);
					Assert.AreEqual(i + 1, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			for (var n = 0; n < num; n++)
			{
				for (var d = 0; d < dMax; d++)
				{
					var i = n * dMax + d;
					Assert.IsTrue(t.Delete(i, i, i + 10));
					Assert.AreEqual(num * dMax - 1 - i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			// Insert ascending, delete descending
			for (var n = 0; n < num; n++)
			{
				for (var d = 0; d < dMax; d++)
				{
					var i = n * dMax + d;
					t.Insert(i, i, i + 10);
					Assert.AreEqual(i + 1, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			for (var n = num - 1; n >= 0; n--)
			{
				for (var d = dMax - 1; d >= 0; d--)
				{
					var i = n * dMax + d;
					Assert.IsTrue(t.Delete(i, i, i + 10));
					Assert.AreEqual(i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			// Insert descending, delete ascending
			for (var n = num - 1; n >= 0; n--)
			{
				for (var d = dMax - 1; d >= 0; d--)
				{
					var i = n * dMax + d;
					t.Insert(i, i, i + 10);
					Assert.AreEqual(num * dMax - i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			for (var n = 0; n < num; n++)
			{
				for (var d = 0; d < dMax; d++)
				{
					var i = n * dMax + d;
					Assert.IsTrue(t.Delete(i, i, i + 10));
					Assert.AreEqual(num * dMax - 1 - i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			// Insert descending, delete descending
			for (var n = num - 1; n >= 0; n--)
			{
				for (var d = dMax - 1; d >= 0; d--)
				{
					var i = n * dMax + d;
					t.Insert(i, i, i + 10);
					Assert.AreEqual(num * dMax - i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}

			for (var n = num - 1; n >= 0; n--)
			{
				for (var d = dMax - 1; d >= 0; d--)
				{
					var i = n * dMax + d;
					Assert.IsTrue(t.Delete(i, i, i + 10));
					Assert.AreEqual(i, t.GetCount());
					Assert.IsTrue(t.IsValid());
				}
			}
		}
	}

	[TestMethod]
	public void TestInvalidRange()
	{
		var t = new IntervalTree<int, double>();

		// Zero ranges are supported.
		t.Insert(0.0, 10, 10);
		Assert.IsTrue(t.IsValid());

		// Negative ranges are not supported.
		Assert.ThrowsException<ArgumentException>(() => t.Insert(0.0, 10, 9));
		Assert.IsTrue(t.IsValid());
	}

	[TestMethod]
	public void TestDeleteReturnValue()
	{
		var t = new IntervalTree<int, double>();

		t.Insert(1, 1, 2);
		t.Insert(2, 2, 3);
		t.Insert(3, 3, 4);
		t.Insert(3, 3, 5);

		Assert.IsFalse(t.Delete(1, 0, 2));
		Assert.IsFalse(t.Delete(1, 1, 3));
		Assert.IsFalse(t.Delete(1, 2, 2));
		Assert.IsFalse(t.Delete(1, 2, 4));

		Assert.IsTrue(t.Delete(1, 1, 2));
		Assert.IsTrue(t.Delete(2, 2, 3));
		Assert.IsTrue(t.Delete(3, 3, 4));
		Assert.IsTrue(t.Delete(3, 3, 5));

		Assert.IsFalse(t.Delete(1, 1, 2));
		Assert.IsFalse(t.Delete(1, 2, 3));
		Assert.IsFalse(t.Delete(1, 3, 4));
		Assert.IsFalse(t.Delete(3, 3, 5));
	}

	[TestMethod]
	public void TestRandom()
	{
		const int randomSeed = 623956004;
		var random = new Random(randomSeed);
		const int num = 1000;
		const int maxNumDuplicates = 3;

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, double>();

			// Insert and delete the same set of random numbers.
			var insertList = new List<Tuple<int, int, int>>();
			var deleteList = new List<Tuple<int, int, int>>();
			for (var i = 0; i < num; i++)
			{
				var low = random.Next();
				var high = random.Next();
				if (low > high)
				{
					(low, high) = (high, low);
				}

				for (var d = 0; d < dMax; d++)
				{
					var val = random.Next();
					insertList.Add(new Tuple<int, int, int>(val, low, high));
					deleteList.Add(new Tuple<int, int, int>(val, low, high));
				}
			}

			deleteList = deleteList.OrderBy(_ => random.Next()).ToList();
			var expectedCount = 0;
			foreach (var val in insertList)
			{
				t.Insert(val.Item1, val.Item2, val.Item3);
				expectedCount++;
				Assert.AreEqual(expectedCount, t.GetCount());
				Assert.IsTrue(t.IsValid());
			}

			foreach (var val in deleteList)
			{
				Assert.IsTrue(t.Delete(val.Item1, val.Item2, val.Item3));
				expectedCount--;
				Assert.AreEqual(expectedCount, t.GetCount());
				Assert.IsTrue(t.IsValid());
			}

			// Insert and delete different sets of random numbers.
			t = new IntervalTree<int, double>();
			insertList = [];
			deleteList = [];
			for (var i = 0; i < num; i++)
			{
				var low = random.Next();
				var high = random.Next();
				if (low > high)
				{
					(low, high) = (high, low);
				}

				for (var d = 0; d < dMax; d++)
				{
					var val = random.Next();
					insertList.Add(new Tuple<int, int, int>(val, low, high));
				}

				low = random.Next();
				high = random.Next();
				if (low > high)
				{
					(low, high) = (high, low);
				}

				for (var d = 0; d < dMax; d++)
				{
					var val = random.Next();
					deleteList.Add(new Tuple<int, int, int>(val, low, high));
				}
			}

			foreach (var val in insertList)
			{
				t.Insert(val.Item1, val.Item2, val.Item3);
				Assert.IsTrue(t.IsValid());
			}

			foreach (var val in deleteList)
			{
				t.Delete(val.Item1, val.Item2, val.Item3);
				Assert.IsTrue(t.IsValid());
			}

			// Insert and delete different sets of random numbers with some overlap.
			t = new IntervalTree<int, double>();
			insertList = [];
			deleteList = [];
			for (var i = 0; i < num; i++)
			{
				var low = random.Next();
				var high = random.Next();
				if (low > high)
				{
					(low, high) = (high, low);
				}

				var val = random.Next();

				for (var d = 0; d < dMax; d++)
				{
					insertList.Add(new Tuple<int, int, int>(val, low, high));
				}

				if (i % 2 == 0)
				{
					for (var d = 0; d < dMax; d++)
					{
						deleteList.Add(new Tuple<int, int, int>(val, low, high));
					}
				}
				else
				{
					low = random.Next();
					high = random.Next();
					if (low > high)
					{
						(low, high) = (high, low);
					}

					for (var d = 0; d < dMax; d++)
					{
						deleteList.Add(new Tuple<int, int, int>(val, low, high));
					}
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
				t.Delete(val.Item1, val.Item2, val.Item3);
				Assert.IsTrue(t.IsValid());
			}
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

		Assert.AreEqual(0, t.GetCount());
		Assert.AreEqual(0, numCounted);

		// Iterate over 1 element.
		t.Insert(0, 0, 10);
		numCounted = 0;
		foreach (var i in t)
		{
			Assert.AreEqual(0, i);
			numCounted++;
		}

		Assert.AreEqual(1, t.GetCount());
		Assert.AreEqual(1, numCounted);

		const int num = 10000;
		const int maxNumDuplicates = 3;

		// Iterate over multiple elements inserted in increasing order.
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			t = new IntervalTree<int, int>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + 10);
					v++;
				}
			}

			var expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected++, val);
				numCounted++;
			}

			Assert.AreEqual(num * dMax, t.GetCount());
			Assert.AreEqual(num * dMax, numCounted);
		}

		// Iterate over multiple elements inserted in decreasing order.
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			t = new IntervalTree<int, int>();

			var v = (num - 1) * dMax;
			for (var i = num - 1; i >= 0; i--)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + 10);
					v++;
				}

				v -= dMax * 2;
			}

			var expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected++, val);
				numCounted++;
			}

			Assert.AreEqual(num * dMax, t.GetCount());
			Assert.AreEqual(num * dMax, numCounted);
		}

		// Iterate over multiple elements inserted in random order.
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			t = new IntervalTree<int, int>();
			const int randomSeed = 73329691;
			var random = new Random(randomSeed);
			var insertList = new List<Tuple<int, int, int>>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					insertList.Add(new Tuple<int, int, int>(v, i, i + 10));
				}

				v++;
			}

			insertList = insertList.OrderBy(_ => random.Next()).ToList();
			foreach (var i in insertList)
				t.Insert(i.Item1, i.Item2, i.Item3);
			var expected = 0;
			numCounted = 0;
			foreach (var val in t)
			{
				Assert.AreEqual(expected / dMax, val);
				expected++;
				numCounted++;
			}

			Assert.AreEqual(num * dMax, t.GetCount());
			Assert.AreEqual(num * dMax, numCounted);
		}
	}

	[TestMethod]
	public void TestEnumeratorPrev()
	{
		const int num = 10000;
		const int maxNumDuplicates = 3;
		const int interval = 10;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			// Finding an element should return an enumerator that needs to be
			// advanced to the starting element.
			var e = t.Find(0, 0, interval);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MovePrev());
			Assert.AreEqual(0, e.Current);
			Assert.IsFalse(e.MovePrev());

			// Finding the last element and iterating via MovePrev should move
			// in descending order through the tree.
			e = t.Last();
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			for (var i = num * dMax - 1; i >= 0; i--)
			{
				Assert.IsTrue(e.MovePrev());
				Assert.AreEqual(i, e.Current);
			}

			Assert.IsFalse(e.MovePrev());
		}
	}

	[TestMethod]
	public void TestEnumeratorUnset()
	{
		const int num = 10;
		const int interval = 10;
		const int maxNumDuplicates = 3;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			var e = t.FirstMutable();
			Assert.IsFalse(e.IsCurrentValid());
			while (e.MoveNext())
			{
				Assert.IsTrue(e.IsCurrentValid());
			}

			Assert.IsFalse(e.IsCurrentValid());

			v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					e = t.FindMutable(v, i, i + interval);
					Assert.IsFalse(e.IsCurrentValid());
					e.MoveNext();
					Assert.IsTrue(e.IsCurrentValid());
					e.Unset();
					Assert.IsFalse(e.IsCurrentValid());
					e.MoveNext();
					Assert.IsTrue(e.IsCurrentValid());
					Assert.AreEqual(v, e.Current);
					v++;
				}
			}
		}
	}

	[TestMethod]
	public void TestEnumeratorDelete()
	{
		const int num = 10;
		const int interval = 10;
		const int maxNumDuplicates = 3;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			for (var valueToDelete = 0; valueToDelete < num * dMax; valueToDelete++)
			{
				var t = new IntervalTree<int, int>();
				var v = 0;
				for (var i = 0; i < num; i++)
				{
					for (var d = 0; d < dMax; d++)
					{
						t.Insert(v, i, i + interval);
						v++;
					}
				}

				var deletedLow = valueToDelete / dMax;
				var e = t.FindMutable(valueToDelete, deletedLow, deletedLow + interval);

				// Deleting unset enumerator should throw an exception and not delete a value.
				Assert.IsFalse(e.IsCurrentValid());
				Assert.ThrowsException<InvalidOperationException>(e.Delete);
				Assert.AreEqual(num * dMax, t.GetCount());

				// Deleting a set enumerator should delete a value and unset the enumerator.
				e.MoveNext();
				Assert.IsTrue(e.IsCurrentValid());
				e.Delete();
				Assert.IsFalse(e.IsCurrentValid());
				Assert.AreEqual(num * dMax - 1, t.GetCount());
				var index = 0;
				foreach (var e2 in t)
				{
					if (index < valueToDelete)
						Assert.AreEqual(index, e2);
					else
						Assert.AreEqual(index + 1, e2);
					index++;
				}
			}
		}
	}

	[TestMethod]
	public void TestFirst()
	{
		const int num = 10;
		const int interval = 10;
		const int maxNumDuplicates = 3;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var e = t.First();
			Assert.IsFalse(e.IsCurrentValid());
			Assert.IsFalse(e.MoveNext());
			Assert.IsFalse(e.IsCurrentValid());

			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			e = t.First();
			Assert.IsFalse(e.IsCurrentValid());
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(0, e.Current);
		}
	}

	[TestMethod]
	public void TestLast()
	{
		const int num = 10;
		const int interval = 10;
		const int maxNumDuplicates = 3;
		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var e = t.Last();
			Assert.IsFalse(e.IsCurrentValid());
			Assert.IsFalse(e.MovePrev());
			Assert.IsFalse(e.IsCurrentValid());

			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			e = t.Last();
			Assert.IsFalse(e.IsCurrentValid());
			Assert.IsTrue(e.MovePrev());
			Assert.AreEqual(num * dMax - 1, e.Current);
		}
	}

	[TestMethod]
	public void TestFind()
	{
		const int num = 10;
		const int interval = 10;
		const int maxNumDuplicates = 3;

		// Finding in an empty tree should return null.
		var t = new IntervalTree<int, int>();
		Assert.IsNull(t.Find(0, 0, 1));

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var v = 0;
			for (var i = 0; i < num; i += 2)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			// Finding elements outside the range of the tree should return null.
			Assert.IsNull(t.Find(0, -1, num - 1));
			Assert.IsNull(t.Find(0, num - 1, num - 1 + interval));

			// Finding elements with incorrect values should return null.
			Assert.IsNull(t.Find(-1, 0, interval));
			Assert.IsNull(t.Find(dMax, 0, interval));

			// Finding elements in the tree should return Enumerators to those elements.
			v = 0;
			for (var i = 0; i < num; i += 2)
			{
				for (var d = 0; d < dMax; d++)
				{
					var e = t.Find(v, i, i + interval);
					Assert.IsNotNull(e);
					Assert.ThrowsException<InvalidOperationException>(() => e.Current);
					Assert.IsTrue(e.MoveNext());
					Assert.AreEqual(v, e.Current);
					v++;
				}
			}

			// Finding elements not in the tree should return null
			v = 0;
			for (var i = 1; i < num; i += 2)
			{
				for (var d = 0; d < dMax; d++)
				{
					Assert.IsNull(t.Find(v, i, i + interval));
					v++;
				}
			}
		}
	}

	[TestMethod]
	public void TestFindAllOverlapping()
	{
		var t = new IntervalTree<int, string>();

		var intervals = new List<Tuple<string, int, int>>
		{
			new("a", 0, 5),
			new("b", 2, 22),
			new("c", 5, 18),
			new("c2", 5, 18),
			new("c3", 5, 6),
			new("c4", 5, 7),
			new("d", 10, 13),
			new("e", 16, 19),
			new("e2", 16, 17),
			new("e3", 16, 18),
			new("e4", 16, 18),
			new("e5", 16, 18),
			new("e6", 16, 18),
			new("f", 17, 26),
			new("g", 19, 21),
			new("h", 23, 25),
		};
		foreach (var interval in intervals)
		{
			t.Insert(interval.Item1, interval.Item2, interval.Item3);
		}

		for (var i = -1; i < 30; i++)
		{
			var inclusiveOverlaps = t.FindAllOverlapping(i);
			var exclusiveOverlaps = t.FindAllOverlapping(i, false, false);

			var expectedInclusiveResults = new List<string>();
			var expectedExclusiveResults = new List<string>();
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

	[TestMethod]
	public void TestFindGreatestPreceding()
	{
		const int num = 100;
		const int interval = 10;
		const int maxNumDuplicates = 3;

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			// Finding in an empty tree should return null.
			var t = new IntervalTree<int, int>();
			Assert.IsNull(t.FindGreatestPreceding(0));

			var v = 0;
			for (var i = 0; i < num; i += 2)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			var maxValue = v - 1;

			// Finding with low less than least low should return null.
			Assert.IsNull(t.FindGreatestPreceding(-1));
			Assert.IsNull(t.FindGreatestPreceding(-1, true));

			// Finding with low greater than the greatest low should return the greatest value.
			// When there are multiple values at the greatest low, the last inserted one
			// should be returned.
			var e = t.FindGreatestPreceding(num);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(maxValue, e.Current);
			e = t.FindGreatestPreceding(num, true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(maxValue, e.Current);

			// Finding with least low should return null when not using orEqualTo=true.
			Assert.IsNull(t.FindGreatestPreceding(0));
			// Finding with least low should return the least value when using orEqualTo=true.
			// When there are multiple values at the least low, the last inserted one
			// should be returned.
			e = t.FindGreatestPreceding(0, true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(dMax - 1, e.Current);

			// Normal cases for FindGreatestPreceding should return the last inserted value
			// for the greatest low preceding the given low.
			for (var i = 1; i < num; i++)
			{
				// Check without equals.
				var expected = (i - 1) / 2 * dMax + (dMax - 1);
				e = t.FindGreatestPreceding(i);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);

				// Check with equals.
				expected = i / 2 * dMax + (dMax - 1);
				e = t.FindGreatestPreceding(i, true);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);
			}
		}
	}

	[TestMethod]
	public void TestFindLeastFollowing()
	{
		const int num = 100;
		const int interval = 10;
		const int maxNumDuplicates = 3;

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			// Finding in an empty tree should return null.
			var t = new IntervalTree<int, int>();
			Assert.IsNull(t.FindLeastFollowing(0));

			var v = 0;
			var firstValueAtGreatestLow = 0;
			for (var i = 0; i < num; i += 2)
			{
				firstValueAtGreatestLow = v;
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			// Finding with low less than least low should least value.
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

			// Finding with low greater than the greatest low should return null.
			Assert.IsNull(t.FindLeastFollowing(num));
			Assert.IsNull(t.FindLeastFollowing(num, true));

			// Finding with the greatest low should return null when not using orEqualTo=true.
			Assert.IsNull(t.FindLeastFollowing(num - 2));
			// Finding with the greatest low should return the greatest value when using orEqualTo=true.
			// When there are multiple values at the greatest low, the first inserted one
			// should be returned.
			e = t.FindLeastFollowing(num - 2, true);
			Assert.IsNotNull(e);
			Assert.ThrowsException<InvalidOperationException>(() => e.Current);
			Assert.IsTrue(e.MoveNext());
			Assert.AreEqual(firstValueAtGreatestLow, e.Current);

			// Normal cases for FindLeastFollowing should return the first inserted value
			// for the least low following the given low.
			for (var i = 0; i < num - 2; i++)
			{
				// Check without equals.
				var expected = (i / 2 + 1) * dMax;
				e = t.FindLeastFollowing(i);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);

				// Check with equals.
				expected = i == 0 ? 0 : ((i - 1) / 2 + 1) * dMax;
				e = t.FindLeastFollowing(i, true);
				Assert.IsNotNull(e);
				Assert.ThrowsException<InvalidOperationException>(() => e.Current);
				Assert.IsTrue(e.MoveNext());
				Assert.AreEqual(expected, e.Current);
			}
		}
	}

	[TestMethod]
	public void TestCloneReadOnlyEnumerator()
	{
		const int num = 100;
		const int interval = 10;
		const int maxNumDuplicates = 3;

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			var e = t.First();
			Assert.IsFalse(e.IsCurrentValid());
			for (var currentV = 0; currentV < v; currentV++)
			{
				Assert.IsTrue(e.MoveNext());
				Assert.IsTrue(e.IsCurrentValid());
				Assert.AreEqual(currentV, e.Current);

				var c = e.Clone();
				Assert.IsTrue(c.IsCurrentValid());
				Assert.AreEqual(currentV, c.Current);
				if (currentV + 1 < v)
				{
					Assert.IsTrue(c.MoveNext());
					Assert.IsTrue(c.IsCurrentValid());
					Assert.AreEqual(currentV + 1, c.Current);
				}
				else
				{
					Assert.IsFalse(c.MoveNext());
					Assert.IsFalse(c.IsCurrentValid());
				}
			}

			Assert.IsFalse(e.MoveNext());
			Assert.IsFalse(e.IsCurrentValid());
		}
	}

	[TestMethod]
	public void TestCloneEnumerator()
	{
		const int num = 100;
		const int interval = 10;
		const int maxNumDuplicates = 3;

		for (var dMax = 1; dMax < maxNumDuplicates + 1; dMax++)
		{
			var t = new IntervalTree<int, int>();
			var v = 0;
			for (var i = 0; i < num; i++)
			{
				for (var d = 0; d < dMax; d++)
				{
					t.Insert(v, i, i + interval);
					v++;
				}
			}

			var e = t.FirstMutable();
			Assert.IsFalse(e.IsCurrentValid());
			for (var currentV = 0; currentV < v; currentV++)
			{
				Assert.IsTrue(e.MoveNext());
				Assert.IsTrue(e.IsCurrentValid());
				Assert.AreEqual(currentV, e.Current);

				var c = e.Clone();
				Assert.IsTrue(c.IsCurrentValid());
				Assert.AreEqual(currentV, c.Current);
				if (currentV + 1 < v)
				{
					Assert.IsTrue(c.MoveNext());
					Assert.IsTrue(c.IsCurrentValid());
					Assert.AreEqual(currentV + 1, c.Current);
				}
				else
				{
					Assert.IsFalse(c.MoveNext());
					Assert.IsFalse(c.IsCurrentValid());
				}
			}

			Assert.IsFalse(e.MoveNext());
			Assert.IsFalse(e.IsCurrentValid());
		}
	}
}
