using System;
using System.IO;

namespace Fumen.Compression;

public class Compression
{
	/// <summary>
	/// Compresses a byte array with Lzma compression.
	/// </summary>
	/// <param name="bytes">Byte array to compress.</param>
	/// <returns>Compressed array.</returns>
	public static byte[] CompressLzma(byte[] bytes)
	{
		var inputStream = new MemoryStream(bytes);
		var outputStream = new MemoryStream();
		CompressLzma(inputStream, outputStream);
		return outputStream.ToArray();
	}

	/// <summary>
	/// Compresses a Stream of data with Lzma compression.
	/// </summary>
	/// <param name="inputStream">Stream of data to compress.</param>
	/// <param name="outputStream">Stream to write compressed data to.</param>
	public static void CompressLzma(Stream inputStream, Stream outputStream)
	{
		var encoder = new SevenZip.Compression.LZMA.Encoder();

		// Write encoder properties.
		encoder.WriteCoderProperties(outputStream);

		// Write data length.
		outputStream.Write(BitConverter.GetBytes(inputStream.Length), 0, 8);

		encoder.Code(inputStream, outputStream, -1, -1, null);
	}

	/// <summary>
	/// Decompresses an LZMA encoded byte array.
	/// </summary>
	/// <param name="bytes">LZMA encoded byte array.</param>
	/// <returns>Decompressed bytes.</returns>
	public static byte[] DecompressLzma(byte[] bytes)
	{
		var inputStream = new MemoryStream(bytes);
		var outputStream = DecompressLzma(inputStream);

		// Return decoded bytes.
		return outputStream.ToArray();
	}

	/// <summary>
	/// Decompresses a Stream of LZMA encoded data.
	/// </summary>
	/// <param name="inputStream">Stream of LZMA encoded data to decompress.</param>
	/// <returns>MemoryStream containing decompressed bytes.</returns>
	public static MemoryStream DecompressLzma(Stream inputStream)
	{
		var outputStream = new MemoryStream();
		var decoder = new SevenZip.Compression.LZMA.Decoder();

		// Read decoder properties.
		var properties = new byte[5];
		if (inputStream.Read(properties, 0, 5) != 5)
			throw new Exception("Malformed LZMA data.");

		// Read data length.
		var dataLengthBytes = new byte[8];
		if (inputStream.Read(dataLengthBytes, 0, 8) != 8)
			throw new Exception("Malformed LZMA data.");
		var dataLength = BitConverter.ToInt64(dataLengthBytes, 0);

		// Decode.
		decoder.SetDecoderProperties(properties);
		decoder.Code(inputStream, outputStream, inputStream.Length, dataLength, null);

		// Return output stream
		outputStream.Seek(0, SeekOrigin.Begin);
		return outputStream;
	}
}
