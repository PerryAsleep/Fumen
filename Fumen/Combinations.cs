using System;
using System.Collections.Generic;
using System.Linq;

namespace Fumen;

public class Combinations
{
	/// <summary>
	/// Creates a List of all length <paramref name="size"/> combinations of the
	/// values enumerated in <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Enum to create combinations of.</typeparam>
	/// <param name="size">Length of combination arrays to return.</param>
	/// <returns>
	/// List of all length <paramref name="size"/> combinations of the values
	/// enumerated in <typeparamref name="T"/>.
	/// </returns>
	public static List<T[]> CreateCombinations<T>(int size) where T : Enum
	{
		return CreateCombinations(Enum.GetValues(typeof(T)).Cast<T>().ToList(), size);
	}

	/// <summary>
	/// Creates a List of all length <paramref name="size"/> combinations of
	/// the given <paramref name="elements"/>.
	/// </summary>
	/// <typeparam name="T">Type of element in <paramref name="elements"/></typeparam>
	/// <param name="elements">IEnumerable of all elements to make combinations of.</param>
	/// <param name="size">Length of combination arrays to return.</param>
	/// <returns>
	/// List of all length <paramref name="size"/> combinations of the given
	/// <paramref name="elements"/>.
	/// </returns>
	public static List<T[]> CreateCombinations<T>(IEnumerable<T> elements, int size)
	{
		var result = new List<T[]>();
		if (size < 1)
			return result;

		var elementList = elements.ToList();
		var len = elementList.Count;
		var indices = new int[size];

		bool Inc()
		{
			var i = size - 1;
			while (i >= 0 && indices[i] == len - 1)
			{
				indices[i] = 0;
				i--;
			}

			if (i < 0)
				return false;
			indices[i]++;
			return true;
		}

		do
		{
			var r = new T[size];
			for (var i = 0; i < size; i++)
				r[i] = elementList[indices[i]];
			result.Add(r);
		} while (Inc());

		return result;
	}

	/// <summary>
	/// Creates a List of all length <paramref name="size"/> unique combinations of
	/// the given <paramref name="elements"/>.
	/// </summary>
	/// <typeparam name="T">Type of element in <paramref name="elements"/></typeparam>
	/// <param name="elements">IEnumerable of all elements to make combinations of.</param>
	/// <param name="size">Length of combination arrays to return.</param>
	/// <returns>
	/// List of all length <paramref name="size"/> unique combinations of the given
	/// <paramref name="elements"/>.
	/// </returns>
	public static List<T[]> CreateUniqueCombinations<T>(IEnumerable<T> elements, int size)
	{
		var result = new List<T[]>();
		if (size < 1)
			return result;

		var elementList = elements.ToList();
		var len = elementList.Count;
		if (len < size)
			return result;

		var indices = new int[size];
		var max = new int[size];
		for (var i = 0; i < size; i++)
		{
			indices[i] = i;
			max[i] = i + (len - size);
		}

		bool Inc()
		{
			var i = size - 1;
			while (i >= 0 && indices[i] == max[i])
			{
				i--;
				if (i >= 0)
				{
					for (var j = i + 1; j < size; j++)
					{
						if (j == i + 1)
							indices[j] = indices[j - 1] + 2;
						else
							indices[j] = indices[j - 1] + 1;
					}
				}
			}

			if (i < 0)
				return false;
			indices[i]++;
			return true;
		}

		do
		{
			var r = new T[size];
			for (var i = 0; i < size; i++)
				r[i] = elementList[indices[i]];
			result.Add(r);
		} while (Inc());

		return result;
	}
}
