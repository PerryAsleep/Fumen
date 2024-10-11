using System.Collections.Generic;
using System.Text.Json;
using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FumenTests;

[TestClass]
public class TestPermissiveEnumJsonConverterFactory
{
	public enum TestEnum
	{
		A,
		B,
		C,
	}

	public class TestClass
	{
		[System.Text.Json.Serialization.JsonInclude]
		public TestEnum TestValue { get; set; }

		[System.Text.Json.Serialization.JsonInclude]
		public Dictionary<TestEnum, int> TestDictionary { get; set; } = new();
	}

	[TestMethod]
	public void TestInvalidValuesAreIgnored()
	{
		var factory = new PermissiveEnumJsonConverterFactory();
		var options = new JsonSerializerOptions
		{
			Converters = { factory },
		};

		var jsonWithInvalidValues = """
		                            {
		                                "TestValue": "InvalidValue",
		                                "TestDictionary":
		                                {
		                                    "A": 10,
		                                    "B": 20,
		                                    "InvalidValue": 30,
		                                    "C": 40
		                                }
		                            }
		                            """;

		var deserializedObject = JsonSerializer.Deserialize<TestClass>(jsonWithInvalidValues, options);
		Assert.AreEqual(TestEnum.A, deserializedObject.TestValue);
		Assert.AreEqual(3, deserializedObject.TestDictionary.Count);
		Assert.AreEqual(10, deserializedObject.TestDictionary[TestEnum.A]);
		Assert.AreEqual(20, deserializedObject.TestDictionary[TestEnum.B]);
		Assert.AreEqual(40, deserializedObject.TestDictionary[TestEnum.C]);
	}

	[TestMethod]
	public void TestCustomDefaultValuesReplaceInvalidValues()
	{
		var factory = new PermissiveEnumJsonConverterFactory();
		factory.RegisterDefault(TestEnum.B);
		var options = new JsonSerializerOptions
		{
			Converters = { factory },
		};

		var jsonWithInvalidValues = """
		                            {
		                                "TestValue": "InvalidValue",
		                                "TestDictionary":
		                                {
		                                    "A": 10,
		                                    "B": 20,
		                                    "InvalidValue": 30,
		                                    "C": 40
		                                }
		                            }
		                            """;

		var deserializedObject = JsonSerializer.Deserialize<TestClass>(jsonWithInvalidValues, options);
		Assert.AreEqual(TestEnum.B, deserializedObject.TestValue);
		Assert.AreEqual(3, deserializedObject.TestDictionary.Count);
		Assert.AreEqual(10, deserializedObject.TestDictionary[TestEnum.A]);
		Assert.AreEqual(20, deserializedObject.TestDictionary[TestEnum.B]);
		Assert.AreEqual(40, deserializedObject.TestDictionary[TestEnum.C]);
	}
}
