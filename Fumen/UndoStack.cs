using System;
using System.Collections.Generic;

namespace Fumen;

/// <summary>
/// An stack with methods to Push, Pop, and Repush elements.
/// When Popping an element, the UndoStack will still maintain the element internally
/// so that it can be Repushed later to support redoing undone operations.
/// UndoStack uses an underlying circular buffer to allow an unbounded number of
/// elements to be Pushed, with a finite storage for undo history.
/// </summary>
/// <typeparam name="T">Type of elements to store.</typeparam>
public sealed class UndoStack<T>
{
	/// <summary>
	/// The size of the circular buffer.
	/// </summary>
	private int BufferSize;

	/// <summary>
	/// Circular buffer.
	/// </summary>
	private List<T> Buffer;

	/// <summary>
	/// The absolute index of the current element.
	/// It is expected for this to become larger than BufferSize.
	/// </summary>
	private int AbsoluteIndex;

	/// <summary>
	/// The current index in the circular buffer.
	/// This is the index where the next element will be added, one beyond the last element.
	/// </summary>
	private int BufferIndex;

	/// <summary>
	/// The start index of the circular buffer.
	/// This is the furthest back in the buffer that can be undone to.
	/// </summary>
	private int BufferStartIndex;

	/// <summary>
	/// Flag for whether or not the internal buffer is full.
	/// </summary>
	private bool IsFull;

	/// <summary>
	/// Number of elements currently Popped that can be Repushed.
	/// </summary>
	private int PopCount;

	/// <summary>
	/// Whether or not unreachable elements should be reset to default(T).
	/// </summary>
	private readonly bool ResetUnreachableElements;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="size">The size of the buffer.</param>
	/// <param name="resetUnreachableElements">
	/// Whether or not unreachable elements should be reset when adding an element to the buffer
	/// that causes future repushable elements to become unreachable. For reference types, this will
	/// allow the garbage collector to reclaim unreachable element memory.
	/// </param>
	public UndoStack(int size, bool resetUnreachableElements)
	{
		if (size <= 0)
			throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");

		BufferSize = size;
		Buffer = new List<T>(BufferSize);
		IsFull = false;
		PopCount = 0;
		for (var i = 0; i < size; i++)
			Buffer.Add(default);
		ResetUnreachableElements = resetUnreachableElements;
	}

	/// <summary>
	/// Resets the UndoStack.
	/// If configured to reset unreachable elements the existing elements
	/// will be reset to their default values.
	/// Sets the absolute index back to 0.
	/// </summary>
	public void Reset()
	{
		if (ResetUnreachableElements)
			ClearRepushHistory();
		AbsoluteIndex = 0;
		BufferIndex = 0;
		BufferStartIndex = 0;
		IsFull = false;
		PopCount = 0;
	}

	/// <summary>
	/// Resize the stack to the given size.
	/// If the new size is smaller this may result in some elements being removed.
	/// Past elements will be prioritized first, with the most recent elements prioritized
	/// above the oldest elements.
	/// Future elements will be prioritized after past elements, with the most recent
	/// elements being prioritized above the furthest future elements.
	/// </summary>
	/// <param name="size">The new size.</param>
	public void Resize(int size)
	{
		if (size <= 0)
			throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");
		if (size == BufferSize)
			return;

		// Create a new buffer of the appropriate length.
		var newBuffer = new List<T>(size);
		var i = 0;
		while (i < size)
		{
			newBuffer.Add(default);
			i++;
		}

		// Determine the number of past elements in the current stack.
		var numPastEvents = 0;
		i = BufferIndex;
		var full = IsFull;
		while (i != BufferStartIndex || full)
		{
			full = false;
			numPastEvents++;
			DecrementBufferIndex(ref i);
		}

		// Determine the number of future elements in the current stack.
		var numFutureElements = PopCount;

		// Copy as many past elements as will fit into the new stack, prioritizing the
		// most recent elements. This may result in throwing out the oldest elements.
		var numPastElementsToCopy = Math.Min(numPastEvents, size);
		i = numPastElementsToCopy - 1;
		var bufferIndex = BufferIndex;
		DecrementBufferIndex(ref bufferIndex);
		while (i >= 0)
		{
			newBuffer[i] = Buffer[bufferIndex];
			i--;
			DecrementBufferIndex(ref bufferIndex);
		}

		// Copy as many future elements as will fit into the new stack, prioritizing
		// the most recent elements. This may result in throwing out the furthest future
		// elements.
		var numFutureElementsToCopy = Math.Min(size - numPastElementsToCopy, numFutureElements);
		i = 0;
		var newBufferIndex = numPastElementsToCopy;
		bufferIndex = BufferIndex;
		while (i < numFutureElementsToCopy)
		{
			newBuffer[newBufferIndex] = Buffer[bufferIndex];
			i++;
			newBufferIndex++;
			IncrementBufferIndex(ref bufferIndex);
		}

		// Swap to the new buffer.
		Buffer = newBuffer;
		BufferSize = size;
		BufferStartIndex = 0;
		BufferIndex = numPastElementsToCopy % size;
		IsFull = BufferIndex == BufferStartIndex && numPastElementsToCopy > 0;
		PopCount = numFutureElementsToCopy;
	}

	/// <summary>
	/// Push a value onto the stack.
	/// </summary>
	/// <param name="val">Value to push.</param>
	public void Push(T val)
	{
		// If pushing this element would cause future elements to become unreachable, reset
		// them to their default value if configured to do so.
		if (ResetUnreachableElements)
			ClearRepushHistory();

		// Push the new value.
		Buffer[BufferIndex] = val;
		IncrementBufferIndex(ref BufferIndex);

		// If the buffer is full, advance the start index.
		if (!IsFull && BufferIndex == BufferStartIndex)
			IsFull = true;
		if (IsFull)
			BufferStartIndex = BufferIndex;

		// When pushing a new value, any future history is always invalidated nad the new
		// value becomes the further value that can be repushed.
		PopCount = 0;
		AbsoluteIndex++;
	}

	/// <summary>
	/// Gets the absolute index of the UndoStack.
	/// This is equal to the number of Pushed elements that have not been Popped.
	/// This may exceed the UndoStack's size.
	/// </summary>
	/// <returns></returns>
	public int GetAbsoluteIndex()
	{
		return AbsoluteIndex;
	}

	/// <summary>
	/// Gets the most recently Pushed element that has not been Popped.
	/// </summary>
	/// <returns>
	/// The most recently Pushed element that has not been Popped, or default(T)
	/// if there are no elements.
	/// </returns>
	public T GetCurrent()
	{
		// If there are no elements, return the default.
		if (!IsFull && BufferIndex == BufferStartIndex)
			return default;
		// Decrement the current buffer index as it is one beyond the most recent element.
		var index = BufferIndex;
		DecrementBufferIndex(ref index);
		return Buffer[index];
	}

	/// <summary>
	/// Returns whether or not the UndoStack can be Popped.
	/// </summary>
	/// <returns></returns>
	public bool CanPop()
	{
		return IsFull || BufferIndex != BufferStartIndex;
	}

	/// <summary>
	/// Pops the current element off the UndoStack.
	/// </summary>
	/// <param name="val">The Popped element or default(T) if no element was Popped.</param>
	/// <returns>True if an element was Popped and false otherwise.</returns>
	public bool Pop(out T val)
	{
		val = default;
		if (!CanPop())
			return false;
		DecrementBufferIndex(ref BufferIndex);
		val = Buffer[BufferIndex];
		AbsoluteIndex--;
		IsFull = false;
		PopCount++;
		return true;
	}

	/// <summary>
	/// Returns whether or not the most recently Popped element of the UndoStack that has
	/// not been Repushed can be Repushed unto the UndoStack.
	/// </summary>
	/// <returns>True if a Repush can happen and false otherwise.</returns>
	public bool CanRepush()
	{
		return PopCount > 0;
	}

	/// <summary>
	/// Repushes the most recently Popped element of the UndoStack that has not been Repushed
	/// back onto the UndoStack
	/// </summary>
	/// <param name="val">
	/// The element that was Repushed or default(T) if no element could be Repushed.
	/// </param>
	/// <returns>True if an element was Repushed and false otherwise.</returns>
	public bool Repush(out T val)
	{
		val = default;
		if (!CanRepush())
			return false;
		val = Buffer[BufferIndex];
		IncrementBufferIndex(ref BufferIndex);
		IsFull = BufferIndex == BufferStartIndex;
		AbsoluteIndex++;
		PopCount--;
		return true;
	}

	/// <summary>
	/// Clears the history of elements which could be Repushed, setting
	/// those elements to their default value.
	/// </summary>
	private void ClearRepushHistory()
	{
		var i = BufferIndex;
		while (PopCount > 0)
		{
			Buffer[i] = default;
			PopCount--;
			IncrementBufferIndex(ref i);
		}
	}

	/// <summary>
	/// Increments the given buffer index.
	/// </summary>
	/// <param name="index">Index to increment.</param>
	private void IncrementBufferIndex(ref int index)
	{
		index = (index + 1) % BufferSize;
	}

	/// <summary>
	/// Decrements the given buffer index.
	/// </summary>
	/// <param name="index">Index to decrement.</param>
	private void DecrementBufferIndex(ref int index)
	{
		index--;
		if (index < 0)
			index = BufferSize - 1;
	}
}
