using System;
using System.Collections.Generic;

namespace Fumen;

public class ShuffleUtils
{
	public static int[] MakeShuffledIndexArray(int size)
	{
		return MakeShuffledIndexArray(size, new Random());
	}

	public static int[] MakeShuffledIndexArray(int size, Random r)
	{
		var array = new int[size];
		for (var i = 0; i < size; i++)
			array[i] = i;
		Shuffle(array, r);
		return array;
	}

	public static List<int> MakeShuffledIndexList(int size)
	{
		return MakeShuffledIndexList(size, new Random());
	}

	public static List<int> MakeShuffledIndexList(int size, Random r)
	{
		var list = new List<int>(size);
		for (var i = 0; i < size; i++)
			list[i] = i;
		Shuffle(list);
		return list;
	}

	public static void Shuffle<T>(T[] array)
	{
		Shuffle(array, new Random());
	}

	public static void Shuffle<T>(T[] array, Random r)
	{
		for (var i = array.Length - 1; i >= 1; i--)
		{
			var j = r.Next(i - 1);
			(array[i], array[j]) = (array[j], array[i]);
		}
	}

	public static void Shuffle<T>(List<T> list)
	{
		Shuffle(list, new Random());
	}

	public static void Shuffle<T>(List<T> list, Random r)
	{
		for (var i = list.Count - 1; i >= 1; i--)
		{
			var j = r.Next(i - 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
