using Fumen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FumenTests;

[TestClass]
public class TestPrintf
{
	[TestMethod]
	public void TestEdgeCases()
	{
		Assert.AreEqual("", Printf.Sprintf(null));
		Assert.AreEqual("", Printf.Sprintf(""));
		Assert.AreEqual("", Printf.Sprintf("%s", null));
		Assert.AreEqual("", Printf.Sprintf("%s", ""));
		Assert.AreEqual("", Printf.Sprintf("%s"));
		Assert.AreEqual("test", Printf.Sprintf("test"));
		Assert.AreEqual("test", Printf.Sprintf("test", "test-arg"));
		Assert.AreEqual("", Printf.Sprintf("%.0s", "test"));
	}

	[TestMethod]
	public void TestIntegerFormatting()
	{
		Assert.AreEqual("123", Printf.Sprintf("%d", 123));
		Assert.AreEqual("123", Printf.Sprintf("%i", 123));
		Assert.AreEqual("+123", Printf.Sprintf("%+d", 123));
		Assert.AreEqual("-123", Printf.Sprintf("%d", -123));
		Assert.AreEqual("00123", Printf.Sprintf("%05d", 123));
		Assert.AreEqual("123  ", Printf.Sprintf("%-5d", 123));
	}

	[TestMethod]
	public void TestUnsignedFormatting()
	{
		Assert.AreEqual("4294967295", Printf.Sprintf("%u", uint.MaxValue));
		Assert.AreEqual("00123", Printf.Sprintf("%05u", 123));
	}

	[TestMethod]
	public void TestFloatFormatting()
	{
		Assert.AreEqual("3.142", Printf.Sprintf("%.3f", 3.14159));
		Assert.AreEqual("+3.14", Printf.Sprintf("%+.2f", 3.14159));
		Assert.AreEqual("003.14", Printf.Sprintf("%06.2f", 3.14159));
		Assert.AreEqual("3.14  ", Printf.Sprintf("%-6.2f", 3.14159));
	}

	[TestMethod]
	public void TestScientificNotation()
	{
		Assert.AreEqual("1.234e+003", Printf.Sprintf("%.3e", 1234.0));
		Assert.AreEqual("1.234E+003", Printf.Sprintf("%.3E", 1234.0));
	}

	[TestMethod]
	public void TestGeneralFormat()
	{
		Assert.AreEqual("123.456", Printf.Sprintf("%g", 123.456));
		Assert.AreEqual("1.23457e+08", Printf.Sprintf("%g", 123456789.0));
		Assert.AreEqual("123.4568", Printf.Sprintf("%.7g", 123.456789));
	}

	[TestMethod]
	public void TestHexadecimalFormatting()
	{
		Assert.AreEqual("ff", Printf.Sprintf("%x", 255));
		Assert.AreEqual("FF", Printf.Sprintf("%X", 255));
		Assert.AreEqual("00ff", Printf.Sprintf("%04x", 255));
	}

	[TestMethod]
	public void TestOctalFormatting()
	{
		Assert.AreEqual("100", Printf.Sprintf("%o", 64));
		Assert.AreEqual("0100", Printf.Sprintf("%#o", 64));
	}

	[TestMethod]
	public void TestStringFormatting()
	{
		Assert.AreEqual("Test", Printf.Sprintf("%s", "Test"));
		Assert.AreEqual("Tes", Printf.Sprintf("%.3s", "Test"));
		Assert.AreEqual("Test      ", Printf.Sprintf("%-10s", "Test"));
		Assert.AreEqual("      Test", Printf.Sprintf("%10s", "Test"));
	}

	[TestMethod]
	public void TestCharacterFormatting()
	{
		Assert.AreEqual("A", Printf.Sprintf("%c", 'A'));
		Assert.AreEqual("A    ", Printf.Sprintf("%-5c", 'A'));
	}

	[TestMethod]
	public void TestMultipleFormatting()
	{
		Assert.AreEqual("123 Test 3.14", Printf.Sprintf("%d %s %.2f", 123, "Test", 3.14159));
	}

	[TestMethod]
	public void TestEscapedPercent()
	{
		Assert.AreEqual("123%", Printf.Sprintf("%d%%", 123));
		Assert.AreEqual("%", Printf.Sprintf("%%"));
	}

	[TestMethod]
	public void TestPrecisionAndWidth()
	{
		Assert.AreEqual("  3.14", Printf.Sprintf("%6.2f", 3.14159));
		Assert.AreEqual("003.14", Printf.Sprintf("%06.2f", 3.14159));
		Assert.AreEqual("3.14  ", Printf.Sprintf("%-6.2f", 3.14159));
	}

	[TestMethod]
	public void TestFlagCombinations()
	{
		Assert.AreEqual("+00123", Printf.Sprintf("%+06d", 123));
		Assert.AreEqual("+123  ", Printf.Sprintf("%-+6d", 123));
		Assert.AreEqual(" 123", Printf.Sprintf("% d", 123));
	}
}
