using System;
using System.Runtime.CompilerServices;

namespace Fumen;

/// <summary>
/// Readonly interface for a DynamicArray.
/// </summary>
/// <typeparam name="T">Type of data to store in the DynamicArray.</typeparam>
public interface IReadOnlyDynamicArray<out T>
{
	public int GetSize();

	/// <summary>
	/// Returns the underlying array.
	/// </summary>
	/// <remarks>
	/// Ideally this would expose a readonly array but readonly arrays are not supported in C#.
	/// We want to expose an array so that APIs requiring an array can access the data directly
	/// without needing to copy or loop. Exposing the array is violating a true readonly contract
	/// but the performance gains are the entire purpose of DynamicArray.
	/// </remarks>
	/// <returns>Underlying array.</returns>
	public T[] GetArray();
}

/// <summary>
/// DynamicArray wraps an array and offers automatic resizing.
/// </summary>
/// <remarks>
/// The only benefit over a List is that DynamicArray exposes the underlying array, allowing
/// this data structure to be used with APIs which require an array without extra allocations
/// or expensive copy operations. DynamicArray isn't as fully-featured as a List, but more
/// List-like behavior can be added later as needed.
/// </remarks>
/// <typeparam name="T">Type of data to store in the DynamicArray.</typeparam>
public class DynamicArray<T> : IReadOnlyDynamicArray<T>
{
	/// <summary>
	/// Size of the underlying Array, representing the maximum number of elements that can be
	/// contained without resizing.
	/// </summary>
	private int Capacity;

	/// <summary>
	/// The number of elements in the underlying Array from a consumer's perspective.
	/// </summary>
	private int Size;

	/// <summary>
	/// Underlying array.
	/// </summary>
	private T[] Array;

	/// <summary>
	/// Default constructor.
	/// </summary>
	public DynamicArray()
	{
		Capacity = 1024;
		Array = new T[Capacity];
	}

	/// <summary>
	/// Constructor with explicit initial capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity to use for the DynamicArray.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when the given capacity is less than one.</exception>
	public DynamicArray(int capacity)
	{
		if (capacity < 1)
			throw new IndexOutOfRangeException();
		Capacity = capacity;
		Array = new T[Capacity];
	}

	/// <summary>
	/// Index operator.
	/// </summary>
	/// <param name="index">Index of value to get or set.</param>
	/// <returns>Value at given index.</returns>
	/// <exception cref="IndexOutOfRangeException">Thrown when the given index is out of range.</exception>
	public T this[int index]
	{
		get
		{
			if (index < 0 || index > Size)
				throw new IndexOutOfRangeException();
			return Array[index];
		}
		set
		{
			if (index < 0 || index > Size)
				throw new IndexOutOfRangeException();
			Array[index] = value;
		}
	}

	/// <summary>
	/// Gets the size of the underlying array.
	/// This represents the number of elements which can be accessed.
	/// </summary>
	/// <returns>Size of the underlying array.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetSize()
	{
		return Size;
	}

	/// <summary>
	/// Gets the underlying array.
	/// The array's size may be larger than the DynamicArray's size.
	/// </summary>
	/// <returns>Underlying array.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T[] GetArray()
	{
		return Array;
	}

	/// <summary>
	/// Sets the size to 0.
	/// This is an O(1) operation which does not affect the underlying array.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear()
	{
		Size = 0;
	}

	/// <summary>
	/// Replaces the contents of this DynamicArray with the contents from the given array.
	/// Will potentially grow the DynamicArray to fit the given array.
	/// </summary>
	/// <param name="array">Array to copy.</param>
	public void CopyFrom(T[] array)
	{
		GrowToAtLeastWithoutCopying(array.Length);
		System.Array.Copy(array, Array, array.Length);
		Size = array.Length;
	}

	/// <summary>
	/// Replaces the contents of this DynamicArray with the contents from the given DynamicArray.
	/// Will potentially grow the DynamicArray to fit the given DynamicArray.
	/// </summary>
	/// <param name="array">DynamicArray to copy.</param>
	public void CopyFrom(IReadOnlyDynamicArray<T> array)
	{
		GrowToAtLeastWithoutCopying(array.GetSize());
		System.Array.Copy(array.GetArray(), Array, array.GetSize());
		Size = array.GetSize();
	}

	/// <summary>
	/// Adds the given value to the end of this DynamicArray.
	/// Will potentially grow the DynamicArray to fit the new value.
	/// </summary>
	/// <param name="item">Value to add.</param>
	public void Add(T item)
	{
		GrowToAtLeast(Size + 1);
		Array[Size] = item;
		Size++;
	}

	/// <summary>
	/// If needed, will resize the DynamicArray to fit the given target capacity.
	/// This may increase or decrease the DynamicArray's capacity.
	/// This may reduce the DynamicArray's size, effectively removing elements.
	/// This will not update the capacity to be the given capacity precisely.
	/// </summary>
	/// <param name="capacity">New target capacity.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when the given capacity is less than one.</exception>
	public void UpdateCapacity(int capacity)
	{
		if (capacity < 1)
			throw new IndexOutOfRangeException();
		GrowToAtLeast(capacity);
		ShrinkTo(capacity);

		// Ensure that reducing the capacity doesn't let the size exceed the capacity.
		Size = Math.Min(Size, capacity);
	}

	/// <summary>
	/// Sets the size directly.
	/// Will update capacity as needed.
	/// </summary>
	/// <param name="size">New size.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when the given size is less than zero.</exception>
	public void SetSize(int size)
	{
		if (size < 0)
			throw new IndexOutOfRangeException();
		GrowToAtLeast(size);
		ShrinkTo(size);
		Size = size;
	}

	/// <summary>
	/// Sets the size directly without updating the capacity.
	/// </summary>
	/// <param name="size">New size.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when the given size is less than zero or greater than the capacity.</exception>
	public void SetSizeWithoutUpdatingCapacity(int size)
	{
		if (size < 0 || size > Capacity)
			throw new IndexOutOfRangeException();
		Size = size;
	}

	#region Resizing

	/// <summary>
	/// If needed, grow the underlying Array to fit the given desired capacity.
	/// </summary>
	/// <param name="desiredCapacity">New capacity.</param>
	private void GrowToAtLeast(int desiredCapacity)
	{
		if (desiredCapacity <= Capacity)
			return;
		while (desiredCapacity > Capacity)
			Capacity <<= 1;
		var newArray = new T[Capacity];
		System.Array.Copy(Array, newArray, Size);
		Array = newArray;
	}

	/// <summary>
	/// If needed, grow the underlying Array to fit the given desired capacity.
	/// Will not copying values when growing.
	/// Should only be used when callers will set the contents of the Array.
	/// </summary>
	/// <param name="desiredCapacity">New capacity.</param>
	private void GrowToAtLeastWithoutCopying(int desiredCapacity)
	{
		if (desiredCapacity <= Capacity)
			return;
		while (desiredCapacity > Capacity)
			Capacity <<= 1;
		Array = new T[Capacity];
	}

	/// <summary>
	/// If needed, shrink the underlying Array while ensuring it still fits the given desired capacity.
	/// </summary>
	/// <param name="desiredCapacity">New capacity.</param>
	private void ShrinkTo(int desiredCapacity)
	{
		if (desiredCapacity >= Capacity >> 1)
			return;
		while (desiredCapacity < Capacity >> 1 && Capacity >> 1 > 0)
			Capacity >>= 1;
		var newArray = new T[Capacity];
		System.Array.Copy(Array, newArray, Math.Min(Size, Capacity));
		Array = newArray;
	}

	#endregion Resizing
}
