using System;
using System.Text;

namespace Fumen;

/// <summary>
/// Class for offering C++ printf functionality.
/// </summary>
public static class Printf
{
	private class FormatSpecifier
	{
		public string Flags { get; set; } = "";
		public int? Width { get; set; }
		public int? Precision { get; set; }
		public string Length { get; set; } = "";
		public char Specifier { get; set; }
	}

	private static bool IsFlag(char c)
	{
		return "+-#0 ".Contains(c);
	}

	private static bool IsLength(char c)
	{
		// ReSharper disable once StringLiteralTypo
		return "hlLzjt".Contains(c);
	}

	private static bool IsNumeric(object obj)
	{
		return obj is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
	}

	public static string Sprintf(string format, params object[] args)
	{
		var sb = new StringBuilder();
		var argIndex = 0;

		for (var i = 0; i < format?.Length; i++)
		{
			if (format[i] != '%')
			{
				sb.Append(format[i]);
				continue;
			}

			if (i + 1 >= format.Length)
				break;

			var spec = ParseFormatSpecifier(format, ref i);
			if (spec.Specifier == '%')
			{
				sb.Append('%');
				continue;
			}

			if (args != null)
			{
				if (argIndex >= args.Length)
					break;
				sb.Append(FormatArgumentToString(args[argIndex++], spec));
			}
		}

		return sb.ToString();
	}

	private static FormatSpecifier ParseFormatSpecifier(string format, ref int i)
	{
		var spec = new FormatSpecifier();

		// Skip %.
		i++;

		while (i < format.Length && IsFlag(format[i]))
			spec.Flags += format[i++];

		var widthStr = "";
		while (i < format.Length && char.IsDigit(format[i]))
			widthStr += format[i++];
		if (!string.IsNullOrEmpty(widthStr))
			spec.Width = int.Parse(widthStr);

		if (i < format.Length && format[i] == '.')
		{
			i++;
			var precisionStr = "";
			while (i < format.Length && char.IsDigit(format[i]))
				precisionStr += format[i++];
			if (!string.IsNullOrEmpty(precisionStr))
				spec.Precision = int.Parse(precisionStr);
		}

		while (i < format.Length && IsLength(format[i]))
			spec.Length += format[i++];

		if (i < format.Length)
			spec.Specifier = format[i];

		return spec;
	}

	private static string FormatNumber(string numStr, FormatSpecifier spec, bool isNegative = false)
	{
		var sign = "";
		if (isNegative)
			sign = "-";
		else if (spec.Flags.Contains('+'))
			sign = "+";
		else if (spec.Flags.Contains(' '))
			sign = " ";

		var effectiveWidth = spec.Width.HasValue ? spec.Width.Value - sign.Length : 0;

		if (effectiveWidth > numStr.Length && !spec.Flags.Contains('-'))
		{
			var padChar = spec.Flags.Contains('0') ? '0' : ' ';
			numStr = numStr.PadLeft(effectiveWidth, padChar);
		}

		var result = sign + numStr;

		if (effectiveWidth > numStr.Length && spec.Flags.Contains('-'))
			result = result.PadRight(spec.Width!.Value);

		return result;
	}

	private static string FormatArgumentToString(object arg, FormatSpecifier spec)
	{
		switch (spec.Specifier)
		{
			case 'd':
			case 'i':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToInt64(arg);
					return FormatNumber(Math.Abs(number).ToString(), spec, number < 0);
				}

				break;
			}
			case 'u':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToUInt64(arg);
					return FormatNumber(number.ToString(), spec);
				}

				break;
			}
			case 'f':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToDouble(arg);
					var format = "F" + (spec.Precision ?? 6);
					var numStr = Math.Abs(number).ToString(format);
					return FormatNumber(numStr, spec, number < 0);
				}

				break;
			}
			case 'e':
			case 'E':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToDouble(arg);
					var format = spec.Specifier + (spec.Precision ?? 6).ToString();
					var numStr = Math.Abs(number).ToString(format);
					return FormatNumber(numStr, spec, number < 0);
				}

				break;
			}
			case 'g':
			case 'G':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToDouble(arg);
					var format = spec.Specifier + (spec.Precision ?? 6).ToString();
					var numStr = Math.Abs(number).ToString(format);
					return FormatNumber(numStr, spec, number < 0);
				}

				break;
			}
			case 'x':
			case 'X':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToInt64(arg);
					var format = spec.Specifier.ToString();
					var numStr = Math.Abs(number).ToString(format);
					if (spec.Flags.Contains('#') && number != 0)
						numStr = "0" + spec.Specifier + numStr;
					return FormatNumber(numStr, spec, number < 0);
				}

				break;
			}

			case 'o':
			{
				if (IsNumeric(arg))
				{
					var number = Convert.ToInt64(arg);
					var numStr = Convert.ToString(Math.Abs(number), 8);
					if (spec.Flags.Contains('#') && number != 0)
						numStr = "0" + numStr;
					return FormatNumber(numStr, spec, number < 0);
				}

				break;
			}

			case 's':
			{
				var str = arg?.ToString() ?? "";
				if (spec.Precision.HasValue)
					str = str.Substring(0, Math.Min(str.Length, spec.Precision.Value));
				if (spec.Width.HasValue)
				{
					if (spec.Flags.Contains('-'))
						str = str.PadRight(spec.Width.Value);
					else
						str = str.PadLeft(spec.Width.Value);
				}

				return str;
			}

			case 'c':
			{
				var c = Convert.ToChar(arg).ToString();
				if (spec.Width.HasValue)
				{
					if (spec.Flags.Contains('-'))
						c = c.PadRight(spec.Width.Value);
					else
						c = c.PadLeft(spec.Width.Value);
				}

				return c;
			}
		}

		return "";
	}
}
