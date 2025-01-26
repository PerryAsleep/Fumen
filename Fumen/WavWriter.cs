using System;
using System.IO;

namespace Fumen;

/// <summary>
/// Class for writing wav files.
/// </summary>
public sealed class WavWriter
{
	private static readonly char[] RiffHeader = ['R', 'I', 'F', 'F'];
	private static readonly char[] WaveFormatHeader = ['W', 'A', 'V', 'E', 'f', 'm', 't', ' '];
	private static readonly char[] DataHeader = ['d', 'a', 't', 'a'];
	private const int FormatDataLength = 16;
	private const short AudioFormat = 1; // PCM
	private const short BitsPerSample = 16;

	/// <summary>
	/// Synchronously writes the given sample data as a wav file to disk.
	/// The file will be 16bit PCM.
	/// </summary>
	/// <param name="filePath">Path to file to write.</param>
	/// <param name="sampleData">Floating point sample data to write.</param>
	/// <param name="sampleRate">Sample rate in Hz.</param>
	/// <param name="numChannels">Number of channels in the sample data.</param>
	public static void WriteWavFile(string filePath, float[] sampleData, int sampleRate, int numChannels)
	{
		using var fileStream = new FileStream(filePath, FileMode.Create);
		using var writer = new BinaryWriter(fileStream);

		var fileSize = 36 + sampleData.Length * 2;
		var byteRate = sampleRate * numChannels * 2;
		var blockAlignment = (short)(numChannels * 2);
		var dataLength = sampleData.Length * 2;

		// Write header.
		writer.Write(RiffHeader);
		writer.Write(fileSize);
		writer.Write(WaveFormatHeader);
		writer.Write(FormatDataLength);
		writer.Write(AudioFormat);
		writer.Write((short)numChannels);
		writer.Write(sampleRate);
		writer.Write(byteRate);
		writer.Write(blockAlignment);
		writer.Write(BitsPerSample);
		writer.Write(DataHeader);
		writer.Write(dataLength);

		// Write samples.
		foreach (var sample in sampleData)
			writer.Write((short)Math.Clamp(sample * 32767, short.MinValue, short.MaxValue));
	}
}
