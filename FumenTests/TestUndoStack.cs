using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FumenTests;

[TestClass]
public class TestUndoStack
{
	[TestMethod]
	public void TestValidSize()
	{
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new UndoStack<int>(-1, true));
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new UndoStack<int>(0, true));
		var us = new UndoStack<int>(1, true);
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => us.Resize(-1));
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => us.Resize(0));
		us.Resize(1);
	}

	[TestMethod]
	public void TestSizeOne()
	{
		var us = new UndoStack<int>(1, true);

		void VerifyPushPopRepushOneElement(int currentAbsoluteIndex)
		{
			us.Push(10);
			Assert.AreEqual(currentAbsoluteIndex + 1, us.GetAbsoluteIndex());
			Assert.AreEqual(10, us.GetCurrent());
			Assert.IsTrue(us.CanPop());
			Assert.IsFalse(us.CanRepush());

			Assert.IsTrue(us.Pop(out var popped));
			Assert.AreEqual(10, popped);
			Assert.AreEqual(currentAbsoluteIndex, us.GetAbsoluteIndex());
			Assert.AreEqual(0, us.GetCurrent());
			Assert.IsFalse(us.CanPop());
			Assert.IsTrue(us.CanRepush());

			Assert.IsTrue(us.Repush(out var repushed));
			Assert.AreEqual(10, repushed);
			Assert.AreEqual(currentAbsoluteIndex + 1, us.GetAbsoluteIndex());
			Assert.AreEqual(10, us.GetCurrent());
			Assert.IsTrue(us.CanPop());
			Assert.IsFalse(us.CanRepush());

			Assert.IsTrue(us.Pop(out _));
		}

		// Verify state when the UndoStack has one element.
		VerifyPushPopRepushOneElement(us.GetAbsoluteIndex());

		// Verify state when the UndoStack has more elements than its size.
		us.Push(11);
		us.Push(12);
		VerifyPushPopRepushOneElement(us.GetAbsoluteIndex());
	}

	[TestMethod]
	public void TestGetAbsoluteIndex()
	{
		var expectedIndex = 0;
		var us = new UndoStack<int>(5, false);
		Assert.AreEqual(expectedIndex++, us.GetAbsoluteIndex());
		for (var i = 0; i < 100; i++)
		{
			us.Push(i);
			Assert.AreEqual(expectedIndex++, us.GetAbsoluteIndex());
		}

		expectedIndex--;

		while (us.CanPop())
		{
			us.Pop(out _);
			Assert.AreEqual(--expectedIndex, us.GetAbsoluteIndex());
		}
	}

	[TestMethod]
	public void TestReset()
	{
		const int stackSize = 5;

		var us = new UndoStack<int>(stackSize, false);

		void AssertReset()
		{
			us.Reset();
			Assert.AreEqual(0, us.GetAbsoluteIndex());
			Assert.AreEqual(0, us.GetCurrent());
			Assert.IsFalse(us.CanPop());
			Assert.IsFalse(us.CanRepush());
		}

		// Verify resetting a default UndoStack.
		AssertReset();

		// Verify resetting a full UndoStack.
		for (var i = 0; i < stackSize; i++)
		{
			us.Push(i + 1);
		}

		AssertReset();

		// Verify resetting a full UndoStack with more elements than its size.
		for (var i = 0; i < stackSize * 2; i++)
		{
			us.Push(i + 1);
		}

		AssertReset();

		// Verify resetting an UndoStack that can be repushed.
		for (var i = 0; i < stackSize; i++)
		{
			us.Push(i + 1);
		}

		us.Pop(out _);
		AssertReset();
	}

	[TestMethod]
	public void TestPush()
	{
		var us = new UndoStack<int>(5, false);
		for (var i = 0; i < 100; i++)
		{
			us.Push(i);
			Assert.AreEqual(i, us.GetCurrent());
		}
	}

	[TestMethod]
	public void TestPushAndPop()
	{
		var stackSize = 5;

		var us = new UndoStack<int>(stackSize, false);
		Assert.IsFalse(us.CanPop());
		Assert.IsFalse(us.Pop(out var val));
		Assert.AreEqual(0, val);

		// Push some values and verify they can be popped.
		us.Push(0);
		Assert.AreEqual(0, us.GetCurrent());
		Assert.AreEqual(1, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());

		us.Push(1);
		Assert.AreEqual(1, us.GetCurrent());
		Assert.AreEqual(2, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());

		us.Push(2);
		Assert.AreEqual(2, us.GetCurrent());
		Assert.AreEqual(3, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());

		// Pop the values.
		Assert.IsTrue(us.Pop(out val));
		Assert.AreEqual(2, val);
		Assert.AreEqual(2, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());

		Assert.IsTrue(us.Pop(out val));
		Assert.AreEqual(1, val);
		Assert.AreEqual(1, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());

		Assert.IsTrue(us.Pop(out val));
		Assert.AreEqual(0, val);
		Assert.AreEqual(0, us.GetAbsoluteIndex());
		Assert.IsFalse(us.CanPop());
		Assert.IsFalse(us.Pop(out val));
		Assert.AreEqual(0, val);

		// Push beyond the stack size.
		for (var i = 0; i < stackSize * 2; i++)
		{
			us.Push(i);
			Assert.AreEqual(i + 1, us.GetAbsoluteIndex());
		}

		// Pop the maximum number of poppable values.
		for (var i = stackSize * 2 - 1; i >= stackSize; i--)
		{
			Assert.IsTrue(us.CanPop());
			Assert.IsTrue(us.Pop(out val));
			Assert.AreEqual(i, val);
			Assert.AreEqual(i, us.GetAbsoluteIndex());
		}

		Assert.IsFalse(us.CanPop());
		Assert.IsFalse(us.Pop(out val));
		Assert.AreEqual(0, val);

		// Push one more and verify it can be popped.
		us.Push(stackSize);
		Assert.AreEqual(stackSize + 1, us.GetAbsoluteIndex());
		Assert.IsTrue(us.CanPop());
	}

	[TestMethod]
	public void TestRepush()
	{
		var stackSize = 5;

		void VerifyRepush(int fillCount)
		{
			var us = new UndoStack<int>(stackSize, false);
			for (var i = 0; i < fillCount; i++)
			{
				us.Push(i);
				Assert.IsFalse(us.CanRepush());
			}

			var maxPopCount = Math.Min(stackSize, fillCount);
			for (var popCount = 0; popCount < maxPopCount; popCount++)
			{
				for (var i = 0; i < popCount; i++)
				{
					Assert.IsTrue(us.Pop(out var popped));
					Assert.AreEqual(fillCount - 1 - i, popped);
				}

				for (var i = 0; i < popCount; i++)
				{
					Assert.IsTrue(us.CanRepush());
					Assert.IsTrue(us.Repush(out var repushed));
					Assert.AreEqual(fillCount - (popCount - i), repushed);
				}

				Assert.IsFalse(us.CanRepush());
			}
		}

		for (var testFillCount = 0; testFillCount < stackSize * 5; testFillCount++)
		{
			VerifyRepush(testFillCount);
		}
	}

	[TestMethod]
	public void TestResize()
	{
		void VerifyResize(UndoStack<int> undoStack, int newSize, int expectedRepushCount, int expectedPopCount)
		{
			var beforeIndex = undoStack.GetAbsoluteIndex();
			var beforeCurrent = undoStack.GetCurrent();
			undoStack.Resize(newSize);
			Assert.AreEqual(beforeIndex, undoStack.GetAbsoluteIndex());
			Assert.AreEqual(beforeCurrent, undoStack.GetCurrent());

			for (var i = 0; i < expectedRepushCount; i++)
			{
				Assert.IsTrue(undoStack.CanRepush());
				undoStack.Repush(out var repushed);
				Assert.AreEqual(beforeCurrent + i + 1, repushed);
			}

			Assert.IsFalse(undoStack.CanRepush());

			for (var i = 0; i < expectedRepushCount + expectedPopCount; i++)
			{
				Assert.IsTrue(undoStack.CanPop());
				undoStack.Pop(out var popped);
				Assert.AreEqual(beforeCurrent + expectedRepushCount - i, popped);
			}

			Assert.IsFalse(undoStack.CanPop());
		}

		// Resize larger - empty.
		var us = new UndoStack<int>(5, false);
		VerifyResize(us, 10, 0, 0);

		// Resize larger - under the size.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 3; i++)
			us.Push(i);
		VerifyResize(us, 10, 0, 3);

		// Resize larger - under the size and repushable.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 3; i++)
			us.Push(i);
		us.Pop(out _);
		VerifyResize(us, 10, 1, 2);

		// Resize larger - at size.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 5; i++)
			us.Push(i);
		VerifyResize(us, 10, 0, 5);

		// Resize larger - at size and repushable.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 5; i++)
			us.Push(i);
		us.Pop(out _);
		us.Pop(out _);
		VerifyResize(us, 10, 2, 3);

		// Resize larger - over the size.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 13; i++)
			us.Push(i);
		VerifyResize(us, 10, 0, 5);

		// Resize larger - over the size and repushable.
		us = new UndoStack<int>(5, false);
		for (var i = 0; i < 13; i++)
			us.Push(i);
		us.Pop(out _);
		us.Pop(out _);
		VerifyResize(us, 10, 2, 3);

		// Resize smaller - empty.
		us = new UndoStack<int>(10, false);
		VerifyResize(us, 5, 0, 0);

		// Resize smaller - under new size.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 3; i++)
			us.Push(i);
		VerifyResize(us, 5, 0, 3);

		// Resize smaller - under new size and repushable.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 3; i++)
			us.Push(i);
		us.Pop(out _);
		VerifyResize(us, 5, 1, 2);

		// Resize smaller - at new size.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 5; i++)
			us.Push(i);
		VerifyResize(us, 5, 0, 5);

		// Resize smaller - at new size and repushable.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 5; i++)
			us.Push(i);
		us.Pop(out _);
		us.Pop(out _);
		VerifyResize(us, 5, 2, 3);

		// Resize smaller - over new size.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 8; i++)
			us.Push(i);
		VerifyResize(us, 5, 0, 5);

		// Resize smaller - over new size and repushable.
		// This should prioritize the undo history over the future repushable history.
		{
			// 6 past 2 future resized to 5 should be 5 past 0 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 8; i++)
				us.Push(i);
			for (var i = 0; i < 2; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 0, 5);

			// 4 past 2 future resized to 5 should be 4 past 1 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 6; i++)
				us.Push(i);
			for (var i = 0; i < 2; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 1, 4);

			// 5 past 5 future resized to 5 should be 5 past 0 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 10; i++)
				us.Push(i);
			for (var i = 0; i < 5; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 0, 5);
		}

		// Resize smaller - over both new and old size.
		us = new UndoStack<int>(10, false);
		for (var i = 0; i < 15; i++)
			us.Push(i);
		VerifyResize(us, 5, 0, 5);

		// Resize smaller - over both new and old size and repushable.
		// This should prioritize the undo history over the future repushable history.
		{
			// 5 past 5 future resized to 5 should be 5 past 0 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 15; i++)
				us.Push(i);
			for (var i = 0; i < 5; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 0, 5);

			// 1 past 9 future resized to 5 should be 1 past 4 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 15; i++)
				us.Push(i);
			for (var i = 0; i < 9; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 4, 1);

			// 7 past 3 future resized to 5 should be 5 past 0 future.
			us = new UndoStack<int>(10, false);
			for (var i = 0; i < 15; i++)
				us.Push(i);
			for (var i = 0; i < 3; i++)
				us.Pop(out _);
			VerifyResize(us, 5, 0, 5);
		}
	}
}
