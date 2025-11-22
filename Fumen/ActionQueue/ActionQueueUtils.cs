using System.Collections.Generic;

namespace Fumen;

/// <summary>
/// Class for common static utility functions used by UndoableActions.
/// </summary>
internal sealed class ActionQueueUtils
{
	private const string Empty = "<empty>";

	/// <summary>
	/// Return string representing setting an object's field or property from an old value to a new value.
	/// The majority of UndoableAction in practice use this, and we want the string to look natural to a user.
	/// </summary>
	public static string GetSetFieldOrPropertyStringForClass<T>(object o, string fieldOrPropertyName, T previousValue,
		T currentValue)
		where T : class
	{
		return GetSetFieldOrPropertyString(
			GetPrettyLogStringForObject(o),
			fieldOrPropertyName,
			GetPrettyLogStringForClassValue(previousValue),
			GetPrettyLogStringForClassValue(currentValue));
	}

	/// <summary>
	/// Return string representing setting an object's field or property from an old value to a new value.
	/// The majority of UndoableAction in practice use this, and we want the string to look natural to a user.
	/// </summary>
	public static string GetSetFieldOrPropertyStringForStruct<T>(object o, string fieldOrPropertyName, T previousValue,
		T currentValue)
		where T : struct
	{
		return GetSetFieldOrPropertyString(
			GetPrettyLogStringForObject(o),
			fieldOrPropertyName,
			GetPrettyLogStringForStructValue(previousValue),
			GetPrettyLogStringForStructValue(currentValue));
	}

	private static string GetSetFieldOrPropertyString(string objectString, string propertyString, string previousString,
		string currentString)
	{
		if (!string.IsNullOrEmpty(previousString) && !string.IsNullOrEmpty(currentString))
		{
			if (!string.IsNullOrEmpty(propertyString))
				return $"Update {objectString} {propertyString} From {previousString} To {currentString}.";
			return $"Update {objectString} From {previousString} To {currentString}.";
		}

		if (!string.IsNullOrEmpty(propertyString))
			return $"Update {objectString} {propertyString}";
		return $"Update {objectString}";
	}

	private static string GetPrettyLogStringForObject(object o)
	{
		return o.GetType().Name;
	}

	private static string GetPrettyLogStringForClassValue<T>(T value) where T : class
	{
		if (value == null)
			return Empty;

		// HashSets look nasty when logged and there is no good way to clean them up.
		if (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(HashSet<>))
			return null;

		var result = value.ToString();
		if (string.IsNullOrEmpty(result))
			result = Empty;
		return result;
	}

	private static string GetPrettyLogStringForStructValue<T>(T value) where T : struct
	{
		var result = value.ToString();
		if (string.IsNullOrEmpty(result))
			result = Empty;
		return result;
	}
}
