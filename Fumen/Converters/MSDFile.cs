﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fumen.Converters
{
	/// <summary>
	/// Stepmania files are based off of an "msd" file format.
	/// This format defines values and parameters using control characters.
	/// For example:
	/// #VALUE_A:PARAM_A1,PARAM_A2;
	/// #VALUE_B:PARAM_B1;
	/// Stepmania allows for parsing this format with missing ; control character
	/// when the next line's first non-whitespace character is the # control character.
	/// This behavior is preserved here.
	/// This format allows for escaping any character using the \ control character.
	/// This format allows for comments using the sequence //.
	/// This format allows for arbitrary characters outside of regions marked by the
	/// # and ; control characters.
	/// </summary>
	public class MSDFile
	{
		public const char ValueStartMarker = '#';
		public const char ValueEndMarker = ';';
		public const char ParamMarker = ':';
		public const string CommentMarker = @"//";
		public const char EscapeMarker = '\\';

		public static string Escape(string input)
		{
			return input
				.Replace($"{ValueEndMarker}", $"{EscapeMarker}{ValueEndMarker}")
				.Replace($"{ParamMarker}", $"{EscapeMarker}{ParamMarker}")
				.Replace($"{EscapeMarker}", $"{EscapeMarker}{EscapeMarker}")
				.Replace($"{CommentMarker}", @"\/\/");
		}

		/// <summary>
		/// A value in the file with an arbitrary number of parameters.
		/// </summary>
		public class Value
		{
			public List<string> Params = new List<string>();
		}

		/// <summary>
		/// The values parsed from the file.
		/// </summary>
		public List<Value> Values = new List<Value>();

		/// <summary>
		/// Asynchronously laod and parse the given msd file.
		/// </summary>
		/// <param name="filePath">Path to the msd file.</param>
		/// <returns>Whether the file was loaded and parsed successfully or not.</returns>
		public async Task<bool> Load(string filePath)
		{
			Values.Clear();

			// Load the file into a string buffer.
			string buffer;
			try
			{
				using (var reader = System.IO.File.OpenText(filePath))
				{
					buffer = await reader.ReadToEndAsync();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to open {filePath}.");
				Logger.Error(e.ToString());
				return false;
			}

			// Parse the buffer.
			var i = 0;
			var bufferLen = buffer.Length;
			var parsingValue = false;
			var currentValueSB = new StringBuilder();
			while (i < bufferLen)
			{
				// Parse comments.
				if (i + CommentMarker.Length - 1 < bufferLen && buffer.Substring(i, CommentMarker.Length) == CommentMarker)
				{
					while (i < bufferLen && buffer[i] != '\n')
						i++;
				}

				// Handle missing ValueEndMarkers when encountering a ValueStartMarker while parsing a value.
				if (parsingValue && buffer[i] == ValueStartMarker)
				{
					// The ValueStartMarker must be the first non-whitespace character on the line to be
					// considered as an intentional start to a new value after a missing ValueEndMarker.
					var valueStartMarkerFirstCharOnLine = false; 
					var j = currentValueSB.Length - 1;
					if (j > 0)
					{
						valueStartMarkerFirstCharOnLine = true;
						var c = currentValueSB[j];
						while (j > 0 && c != '\r' && c != '\n')
						{
							if (c == ' ' || c == '\t')
							{
								c = currentValueSB[--j];
							}
							else
							{
								valueStartMarkerFirstCharOnLine = false;
								break;
							}
						}
					}

					if (!valueStartMarkerFirstCharOnLine)
					{
						currentValueSB.Append(buffer[i++]);
						continue;
					}

					// Add the final param for the current value, timming all whitespace that preceded
					// the ValueEndMarker.
					Values[Values.Count - 1].Params.Add(currentValueSB.ToString().TrimEnd(SMCommon.SMAllWhiteSpace));
					// Finish parsing the value, but continue to start parsing the new value below.
					parsingValue = false;
				}

				// Normal ValueStartMarker handing, start parsing a value.
				if (!parsingValue && buffer[i] == ValueStartMarker)
				{
					Values.Add(new Value());
					parsingValue = true;
				}

				// Not parsing a value, skip the current character.
				if (!parsingValue)
				{
					// Skip two if escaping.
					i += buffer[i] == EscapeMarker ? 2 : 1;
					continue;
				}

				// Handle parsing characters within a value.

				// Handle ending a parameter.
				if (currentValueSB.Length > 0 && (buffer[i] == ParamMarker || buffer[i] == ValueEndMarker))
				{
					Values[Values.Count - 1].Params.Add(currentValueSB.ToString());
					// Continue to start parsing a new parameter below.
				}

				// Handle starting a parameter.
				if (buffer[i] == ParamMarker || buffer[i] == ValueStartMarker)
				{
					i++;
					currentValueSB.Clear();
					continue;
				}

				// Handle ending a value. Last parameter capture above.
				if (buffer[i] == ValueEndMarker)
				{
					i++;
					parsingValue = false;
					continue;
				}

				// Normal character within a parameter.
				if (buffer[i] == EscapeMarker)
					i++;
				if (i < bufferLen)
					currentValueSB.Append(buffer[i++]);
			}

			// Add final parameter.
			if (parsingValue)
				Values[Values.Count - 1].Params.Add(currentValueSB.ToString());

			return true;
		}
	}
}
