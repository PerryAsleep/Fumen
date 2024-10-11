using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumen;

/// <summary>
/// JsonConverterFactory for Enums which can safely ignore invalid values when reading.
/// Can handle converting Enums and will ignore invalid values, using the default value when an
/// invalid value is encountered.
/// Can handle converting Dictionaries with Enum keys. If an invalid Enum value is found as a key
/// then both the key and value for that entry will be ignored.
/// </summary>
public class PermissiveEnumJsonConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Explicit default values to use.
	/// </summary>
	private readonly Dictionary<Type, Enum> DefaultEnumValues = new();

	/// <summary>
	/// Registers a default value to use for an Enum when invalid values are encountered
	/// during deserialization.
	/// </summary>
	/// <typeparam name="T">Type of Enum.</typeparam>
	/// <param name="value">Enum value to use as a default.</param>
	public void RegisterDefault<T>(T value) where T : struct, Enum
	{
		DefaultEnumValues[typeof(T)] = value;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		// Enums can be converted.
		if (typeToConvert.IsEnum)
			return true;

		// Dictionary with enum keys can be converted.
		return typeToConvert.IsGenericType
		       && typeToConvert.GetGenericTypeDefinition() == typeof(Dictionary<,>)
		       && typeToConvert.GetGenericArguments()[0].IsEnum;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		// Create a converter for Enums.
		if (typeToConvert.IsEnum)
		{
			var converter = (JsonConverter)Activator.CreateInstance(
				typeof(EnumConverter<>).MakeGenericType(typeToConvert));
			((IDefaultConverter)converter)!.SetDefaults(DefaultEnumValues);
			return converter;
		}

		// Create a converter for Dictionaries with Enum keys.
		var keyType = typeToConvert.GetGenericArguments()[0];
		var valueType = typeToConvert.GetGenericArguments()[1];
		return (JsonConverter)Activator.CreateInstance(
			typeof(EnumDictionaryConverter<,,>).MakeGenericType(
				typeof(Dictionary<,>).MakeGenericType(keyType, valueType), keyType, valueType));
	}

	private interface IDefaultConverter
	{
		public void SetDefaults(IReadOnlyDictionary<Type, Enum> defaultEnumValues);
	}

	/// <summary>
	/// JsonConverter for Enums which can safely ignore invalid Enum values when reading.
	/// </summary>
	/// <typeparam name="T">Enum Type.</typeparam>
	private class EnumConverter<T> : JsonConverter<T>, IDefaultConverter where T : struct, Enum
	{
		/// <summary>
		/// Explicit default values to use.
		/// </summary>
		private IReadOnlyDictionary<Type, Enum> DefaultEnumValues;

		public void SetDefaults(IReadOnlyDictionary<Type, Enum> defaultEnumValues)
		{
			DefaultEnumValues = defaultEnumValues;
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				// Try to parse the enum value.
				var enumValue = reader.GetString();
				if (Enum.TryParse(enumValue, true, out T result))
					return result;

				// If an explicit default value exists for this enum, use it.
				if (DefaultEnumValues.TryGetValue(typeToConvert, out var value))
					return (T)value;
			}

			return default;
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}

	/// <summary>
	/// JsonConverter for Dictionaries with Enum keys which can safely ignore invalid Enum values when reading.
	/// </summary>
	/// <typeparam name="TDictionary">Dictionary Type.</typeparam>
	/// <typeparam name="TKey">Key Type.</typeparam>
	/// <typeparam name="TValue">Value Type.</typeparam>
	private class EnumDictionaryConverter<TDictionary, TKey, TValue> : JsonConverter<TDictionary>
		where TDictionary : Dictionary<TKey, TValue>
		where TKey : struct, Enum
	{
		public override TDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();

			var dictionary = new Dictionary<TKey, TValue>();
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					return (TDictionary)dictionary;
				if (reader.TokenType != JsonTokenType.PropertyName)
					throw new JsonException();

				var propertyName = reader.GetString();
				if (!Enum.TryParse<TKey>(propertyName, true, out var key))
				{
					// Skip this key value pair if the Enum value is invalid.
					reader.TrySkip();
					continue;
				}

				reader.Read();
				var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
				dictionary[key] = value;
			}

			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			foreach (var kvp in value)
			{
				writer.WritePropertyName(kvp.Key.ToString());
				JsonSerializer.Serialize(writer, kvp.Value, options);
			}

			writer.WriteEndObject();
		}
	}
}
