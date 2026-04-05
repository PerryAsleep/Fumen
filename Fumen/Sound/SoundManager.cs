using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Concentus.Structs;
using FMOD;

namespace Fumen;

/// <summary>
/// Class for managing low-level sound functionality.
/// Wraps FMOD functionality.
/// Expected Usage:
///  Call Update once each frame.
///  If creating a DSP through CreateDSP, call DestroyDsp later to dispose of it.
/// </summary>
public class SoundManager
{
	/// <summary>
	/// FMOD System.
	/// </summary>
	private FMOD.System System;

	/// <summary>
	/// All DspHandles.
	/// </summary>
	private readonly Dictionary<string, DspHandle> DspHandles = new();

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="dspBufferSize">Size of the DSP buffers in samples.</param>
	/// <param name="dspNumBuffers">Number of DSP buffers.</param>
	public SoundManager(uint dspBufferSize, int dspNumBuffers)
	{
		ErrCheck(Factory.System_Create(out System));
		ErrCheck(System.setDSPBufferSize(dspBufferSize, dspNumBuffers));
		ErrCheck(System.init(100, INITFLAGS.NORMAL, IntPtr.Zero));
		//System.setOutput(OUTPUTTYPE.WAVWRITER);
	}

	/// <summary>
	/// Asynchronously load a sound file from disk into an FMOD Sound.
	/// </summary>
	/// <param name="fileName"></param>
	/// <param name="mode"></param>
	/// <returns>Loaded Sound.</returns>
	public async Task<Sound> LoadAsync(string fileName, MODE mode = MODE.DEFAULT)
	{
		return await Task.Run(() =>
		{
			ErrCheck(System.createSound(fileName, mode, out var sound), $"Failed to load {fileName}");
			return sound;
		});
	}

	/// <summary>
	/// Creates a ChannelGroup identified by the given name.
	/// </summary>
	/// <param name="name">Name of the ChannelGroup.</param>
	/// <param name="channelGroup">Created ChannelGroup.</param>
	public void CreateChannelGroup(string name, out ChannelGroup channelGroup)
	{
		ErrCheck(System.createChannelGroup(name, out channelGroup));
	}

	/// <summary>
	/// Plays a Sound on the given ChannelGroup.
	/// </summary>
	/// <param name="sound">Sound to play.</param>
	/// <param name="channelGroup">ChannelGroup to play the Sound on.</param>
	/// <param name="channel">Channel assigned to the Sound.</param>
	public void PlaySound(Sound sound, ChannelGroup channelGroup, out Channel channel)
	{
		ErrCheck(System.playSound(sound, channelGroup, true, out channel));
	}

	/// <summary>
	/// Perform time-dependent updates.
	/// </summary>
	public void Update()
	{
		// Update the FMOD System.
		ErrCheck(System.update());
	}

	/// <summary>
	/// Checks the given FMOD RESULT for error values and logs error messages.
	/// </summary>
	/// <param name="result">FMOD RESULT.</param>
	/// <param name="failureMessage">Optional message to append to error log messages.</param>
	/// <returns>True if the results is not an error and false if it is an error.</returns>
	public static bool ErrCheck(RESULT result, string failureMessage = null)
	{
		if (result != RESULT.OK)
		{
			if (!string.IsNullOrEmpty(failureMessage))
			{
				Logger.Error($"{failureMessage} {result:G}");
			}
			else
			{
				Logger.Error($"FMOD error: {result:G}");
			}

			return false;
		}

		return true;
	}

	#region Resampling

	/// <summary>
	/// Gets the sample rate of the engine that all sounds ultimately process at.
	/// </summary>
	/// <returns>System sample rate.</returns>
	public uint GetSampleRate()
	{
		ErrCheck(System.getSoftwareFormat(out var sampleRate, out _, out _));
		return (uint)sampleRate;
	}

	/// <summary>
	/// Allocates a buffer to hold PCM float data for the given sound at the given sample rate.
	/// The resulting buffer can then be filled using FillSamplesAsync.
	/// </summary>
	/// <param name="sound">Sound to allocate the buffer for.</param>
	/// <param name="sampleRate">The desired output sample rate.</param>
	/// <param name="samples">Buffer of PCM float data to allocate.</param>
	/// <param name="numChannels">Number of channels in the buffer.</param>
	/// <returns>True if successful and false if an error was encountered.</returns>
	public static bool AllocateSampleBuffer(Sound sound, uint sampleRate, out float[] samples, out int numChannels)
	{
		numChannels = 0;
		samples = null;

		if (!ErrCheck(sound.getDefaults(out var soundFrequency, out _)))
			return false;
		var inputSampleRate = (uint)soundFrequency;
		if (!ErrCheck(sound.getLength(out var inputLength, TIMEUNIT.PCMBYTES)))
			return false;
		if (!ErrCheck(sound.getFormat(out _, out var format, out numChannels, out var bits)))
			return false;
		if (!ErrCheck(sound.@lock(0, inputLength, out var ptr1, out var ptr2, out var len1, out var len2)))
			return false;

		// Early out for data that can't be parsed.
		if (!ValidateFormat(format, numChannels, bits))
		{
			ErrCheck(sound.unlock(ptr1, ptr2, len1, len2));
			return false;
		}

		var bitsPerSample = (uint)bits * (uint)numChannels;
		var bytesPerSample = bitsPerSample >> 3;
		var totalNumInputSamples = inputLength / bytesPerSample;
		var sampleRateRatio = (double)sampleRate / inputSampleRate;
		var totalNumOutputSamples = (uint)(totalNumInputSamples * sampleRateRatio);
		var outputLength = totalNumOutputSamples * (uint)numChannels;
		samples = new float[outputLength];

		ErrCheck(sound.unlock(ptr1, ptr2, len1, len2));
		return true;
	}

	/// <summary>
	/// Helper function for validating SOUND_FORMAT and other common properties of a Sound needed
	/// for parsing its data.
	/// </summary>
	/// <param name="format">SOUND_FORMAT of the Sound.</param>
	/// <param name="numChannels">Number of channels of the Sound.</param>
	/// <param name="bitsPerSample">Bits per sample of the Sound</param>
	/// <returns>True if the data is valid for parsing and false otherwise.</returns>
	public static bool ValidateFormat(SOUND_FORMAT format, int numChannels, int bitsPerSample)
	{
		// Early outs for data that can't be parsed.
		if (format != SOUND_FORMAT.PCM8
		    && format != SOUND_FORMAT.PCM16
		    && format != SOUND_FORMAT.PCM24
		    && format != SOUND_FORMAT.PCM32
		    && format != SOUND_FORMAT.PCMFLOAT)
		{
			Logger.Warn($"Unsupported sound format: {format:G}");
			return false;
		}

		if (numChannels < 1)
		{
			Logger.Warn($"Sound has {numChannels} channels. Expected at least one.");
			return false;
		}

		if (bitsPerSample < 1)
		{
			Logger.Warn($"Sound has {bitsPerSample} bits per sample. Expected at least one.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Gets a function to use for parsing a sample out of a byte array into floats.
	/// </summary>
	/// <param name="format">SOUND_FORMAT of the byte array.</param>
	/// <param name="ptr">Byte array.</param>
	/// <returns>Function for parsing the byte array into floats.</returns>
	public static unsafe Func<long, float> GetParseSampleFunc(SOUND_FORMAT format, byte* ptr)
	{
		// Constants for converting sound formats to floats.
		const float invPcm8Max = 1.0f / byte.MaxValue;
		const float invPcm16Max = 1.0f / short.MaxValue;
		const float invPcm24Max = 1.0f / 8388607;
		const float invPcm32Max = 1.0f / int.MaxValue;

		// Get a function for parsing samples.
		// In practice this more performant than using the switch in the loop below.
		switch (format)
		{
			case SOUND_FORMAT.PCM8:
			{
				return i => ptr[i] * invPcm8Max;
			}
			case SOUND_FORMAT.PCM16:
			{
				return i => (ptr[i]
				             + (short)(ptr[i + 1] << 8)) * invPcm16Max;
			}
			case SOUND_FORMAT.PCM24:
			{
				return i => (((ptr[i] << 8)
				              + (ptr[i + 1] << 16)
				              + (ptr[i + 2] << 24)) >> 8) * invPcm24Max;
			}
			case SOUND_FORMAT.PCM32:
			{
				return i => (ptr[i]
				             + (ptr[i + 1] << 8)
				             + (ptr[i + 2] << 16)
				             + (ptr[i + 3] << 24)) * invPcm32Max;
			}
			case SOUND_FORMAT.PCMFLOAT:
			{
				return i => ((float*)ptr)[i >> 2];
			}
			default:
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Fills a buffer allocated previously from AllocateSampleBuffer with float PCM data
	/// from the given sound at the given sample rate.
	/// </summary>
	/// <param name="sound">Sound to read the sample data from.</param>
	/// <param name="sampleRate">Sample rate of the buffer.</param>
	/// <param name="samples">Buffer to fill.</param>
	/// <param name="numChannels">Number of channels in the buffer.</param>
	/// <param name="token">CancellationToken for cancelling the work.</param>
	/// <returns>True if successful and false if an error was encountered.</returns>
	public static async Task<bool> FillSamplesAsync(Sound sound, uint sampleRate, float[] samples, int numChannels,
		CancellationToken token)
	{
		var result = false;
		await Task.Run(() =>
		{
			try
			{
				result = FillSamples(sound, sampleRate, samples, numChannels, token);
			}
			catch (OperationCanceledException)
			{
				// Ignored.
			}
		}, token);
		token.ThrowIfCancellationRequested();
		return result;
	}

	/// <summary>
	/// Fills a buffer allocated previously from AllocateSampleBuffer with float PCM data
	/// from the given sound at the given sample rate.
	/// </summary>
	/// <param name="sound">Sound to read the sample data from.</param>
	/// <param name="sampleRate">Sample rate of the buffer.</param>
	/// <param name="samples">Buffer to fill.</param>
	/// <param name="numChannels">Number of channels in the buffer.</param>
	/// <param name="token">CancellationToken for cancelling the work.</param>
	/// <returns>True if successful and false if an error was encountered.</returns>
	private static unsafe bool FillSamples(Sound sound, uint sampleRate, float[] samples, int numChannels,
		CancellationToken token)
	{
		if (!ErrCheck(sound.getDefaults(out var soundFrequency, out _)))
			return false;
		var inputSampleRate = (uint)soundFrequency;
		if (!ErrCheck(sound.getLength(out var inputLength, TIMEUNIT.PCMBYTES)))
			return false;
		if (!ErrCheck(sound.getFormat(out _, out var format, out var soundNumChannels, out var bits)))
			return false;
		if (soundNumChannels != numChannels)
		{
			Logger.Warn($"Provided channel count {numChannels} does not match expected channel count {soundNumChannels}.");
			return false;
		}

		if (!ErrCheck(sound.@lock(0, inputLength, out var ptr1, out var ptr2, out var len1, out var len2)))
			return false;

		// Early out for data that can't be parsed.
		if (!ValidateFormat(format, numChannels, bits))
		{
			ErrCheck(sound.unlock(ptr1, ptr2, len1, len2));
			return false;
		}

		var ptr = (byte*)ptr1.ToPointer();

		var bitsPerSample = (uint)bits * (uint)numChannels;
		var bytesPerSample = bitsPerSample >> 3;
		var bytesPerChannelPerSample = (uint)(bits >> 3);
		var totalNumInputSamples = inputLength / bytesPerSample;
		var sampleRateRatio = (double)sampleRate / inputSampleRate;
		var totalNumOutputSamples = (uint)(totalNumInputSamples * sampleRateRatio);
		var outputLength = totalNumOutputSamples * (uint)numChannels;
		if (samples.Length != outputLength)
		{
			Logger.Warn($"Provided buffer length {samples.Length} does match expected buffer length {outputLength}.");
			ErrCheck(sound.unlock(ptr1, ptr2, len1, len2));
			return false;
		}

		// Get a function for parsing samples.
		var parseSample = GetParseSampleFunc(format, ptr);

		try
		{
			// If the same rates match, just parse each sample with no interpolation.
			if (sampleRate == inputSampleRate)
			{
				for (var i = 0; i < outputLength; i++)
					samples[i] = parseSample(i * bytesPerChannelPerSample);
			}
			else
			{
				// Parse the raw sound data into float samples at the input sample rate so we can resample it.
				var inputLengthSamples = totalNumInputSamples * (uint)numChannels;
				var inputFloats = new float[inputLengthSamples];
				for (var i = 0; i < inputLengthSamples; i++)
					inputFloats[i] = parseSample(i * bytesPerChannelPerSample);

				// Resample to the target sample rate and copy into the output buffer.
				var resampled = ResampleFloat(inputFloats, numChannels, inputSampleRate, sampleRate, token);
				var copyLength = Math.Min(resampled.Length, samples.Length);
				Array.Copy(resampled, 0, samples, 0, copyLength);
			}
		}
		finally
		{
			ErrCheck(sound.unlock(ptr1, ptr2, len1, len2));
		}

		return true;
	}

	/// <summary>
	/// Resamples interleaved float PCM data from one sample rate to another using four-point
	/// Hermite spline interpolation.
	/// </summary>
	/// <param name="input">Input interleaved float PCM samples.</param>
	/// <param name="numChannels">Number of interleaved channels.</param>
	/// <param name="inputSampleRate">Sample rate of the input data.</param>
	/// <param name="outputSampleRate">Desired output sample rate.</param>
	/// <param name="token">CancellationToken for cancelling the work.</param>
	/// <returns>Resampled interleaved float PCM samples.</returns>
	public static float[] ResampleFloat(float[] input, int numChannels, uint inputSampleRate, uint outputSampleRate,
		CancellationToken token)
	{
		if (inputSampleRate == outputSampleRate)
			return input;

		var totalNumInputSamples = (uint)(input.Length / numChannels);
		var sampleRateRatio = (double)outputSampleRate / inputSampleRate;
		var totalNumOutputSamples = (uint)(totalNumInputSamples * sampleRateRatio);
		var outputLength = totalNumOutputSamples * (uint)numChannels;
		var output = new float[outputLength];

		// To resample we perform four point hermite spline interpolation.
		// Set up data for storing the source points for resampling.
		const int numHermitePoints = 4;
		var hermiteTimeRange = 1.0f / inputSampleRate;
		var hermitePoints = new float[numHermitePoints];
		var maxInputSampleIndex = input.Length - numChannels;

		var sampleIndex = 0;
		while (sampleIndex < totalNumOutputSamples)
		{
			// Determine the time of the desired sample.
			var t = (double)sampleIndex / outputSampleRate;
			// Find the start of the four points in the original data corresponding to this time
			// so we can use them for hermite spline interpolation. Note the minus 1 here is to
			// account for four samples. The floor and the minus one result in getting the sample
			// two indexes preceding the desired time.
			var startInputSampleIndex = (int)(t * inputSampleRate) - 1;
			// Determine the time of the x1 sample in order to find the normalized time.
			var x1Time = (double)(startInputSampleIndex + 1) / inputSampleRate;
			// Determine the normalized time for the interpolation.
			var normalizedTime = (float)((t - x1Time) / hermiteTimeRange);

			for (var channel = 0; channel < numChannels; channel++)
			{
				// Get all four input points for the interpolation.
				for (var hermiteIndex = 0; hermiteIndex < numHermitePoints; hermiteIndex++)
				{
					// Get the input index. We need to clamp as it is expected at the ends for the range to exceed the
					// range of the input data.
					var inputIndex = Math.Clamp(
						(startInputSampleIndex + hermiteIndex) * numChannels + channel,
						0, maxInputSampleIndex);
					hermitePoints[hermiteIndex] = input[inputIndex];
				}

				// Now that all four samples are known, interpolate them and store the result.
				output[sampleIndex * numChannels + channel] = Interpolation.HermiteInterpolate(
					hermitePoints[0], hermitePoints[1], hermitePoints[2], hermitePoints[3], normalizedTime);
			}

			sampleIndex++;

			// Periodically check for cancellation.
			if (sampleIndex % 524288 == 0)
				token.ThrowIfCancellationRequested();
		}

		return output;
	}

	#endregion Resampling

	#region Opus

	/// <summary>
	/// Checks if a file at the given path is an Ogg container with the Opus codec
	/// by inspecting the first packet's magic bytes. This function involves synchronous
	/// file I/O.
	/// </summary>
	/// <param name="filePath">Path to the file to check.</param>
	/// <returns>True if the file is an Ogg file containing Opus audio.</returns>
	private static bool IsOggOpus(string filePath)
	{
		try
		{
			var buf = new byte[128];
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var bytesRead = fs.Read(buf, 0, buf.Length);

			// Verify OggS capture pattern.
			if (bytesRead < 28 || buf[0] != 'O' || buf[1] != 'g' || buf[2] != 'g' || buf[3] != 'S')
				return false;

			// Segment count is at offset 26; first packet data starts after the segment table.
			var numSegments = buf[26];
			var packetOffset = 27 + numSegments;

			if (bytesRead < packetOffset + 8)
				return false;

			// Check for "OpusHead" magic.
			return buf[packetOffset] == 'O'
			       && buf[packetOffset + 1] == 'p'
			       && buf[packetOffset + 2] == 'u'
			       && buf[packetOffset + 3] == 's'
			       && buf[packetOffset + 4] == 'H'
			       && buf[packetOffset + 5] == 'e'
			       && buf[packetOffset + 6] == 'a'
			       && buf[packetOffset + 7] == 'd';
		}
		catch (Exception e)
		{
			Logger.Warn($"Failed to check if file is Ogg Opus: {e}");
			return false;
		}
	}

	/// <summary>
	/// Returns whether the given file should be decoded using the Opus decode path.
	/// This is true if the file extension is .opus, or if the extension is .ogg/.oga
	/// and the file contains Opus-encoded audio. This function involves synchronous
	/// file I/O.
	/// </summary>
	/// <param name="filePath">Path to the audio file.</param>
	/// <returns>True if the file should be decoded as Opus.</returns>
	public static bool IsOpusFile(string filePath)
	{
		var extension = global::System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
		if (extension == ".opus")
			return true;
		if (extension is ".ogg" or ".oga")
			return IsOggOpus(filePath);
		return false;
	}

	/// <summary>
	/// Reads the channel count from the OpusHead header in an Ogg Opus file.
	/// The channel count is at byte offset 9 within the OpusHead packet.
	/// </summary>
	/// <param name="stream">FileStream to the Opus audio file.</param>
	/// <param name="channels">Channel count to set.</param>
	/// <returns>True if the channels could be read and false otherwise.</returns>
	private static bool GetOpusChannelCount(FileStream stream, out int channels)
	{
		channels = 0;
		try
		{
			stream.Seek(0, SeekOrigin.Begin);

			var buf = new byte[128];
			var bytesRead = stream.Read(buf, 0, buf.Length);

			if (bytesRead < 28)
				return false;

			var numSegments = buf[26];
			var packetOffset = 27 + numSegments;

			// OpusHead format: "OpusHead" (8 bytes) + version (1 byte) + channel count (1 byte)
			if (bytesRead < packetOffset + 10)
				return false;
			if (buf[packetOffset] != 'O' || buf[packetOffset + 1] != 'p')
				return false;

			var channelCount = buf[packetOffset + 9];
			channels = channelCount > 0 ? channelCount : 2;
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Reads all raw Opus audio packets from an Ogg container, skipping header and tags packets.
	/// This replaces Concentus.OggFile's OpusOggReadStream which has a bug parsing OpusTags packets
	/// spanning multiple Ogg pages (https://github.com/lostromb/concentus.oggfile/issues/6).
	/// </summary>
	/// <param name="stream">Seekable stream positioned at the start of the Ogg container.</param>
	/// <returns>List of raw Opus packet byte arrays.</returns>
	private static List<byte[]> ReadOggOpusPackets(FileStream stream)
	{
		var packets = new List<byte[]>();
		var pageHeader = new byte[27];
		byte[] segmentTable = null;
		var headerPacketsSeen = 0;
		byte[] continuationBuffer = null;

		stream.Seek(0, SeekOrigin.Begin);

		while (true)
		{
			// Read the fixed 27-byte Ogg page header.
			var bytesRead = stream.Read(pageHeader, 0, 27);
			if (bytesRead < 27)
				break;

			// Verify OggS capture pattern.
			if (pageHeader[0] != 'O' || pageHeader[1] != 'g' || pageHeader[2] != 'g' || pageHeader[3] != 'S')
				break;

			var numSegments = pageHeader[26];

			// Read the segment table.
			if (segmentTable == null || segmentTable.Length < numSegments)
				segmentTable = new byte[numSegments];
			if (stream.Read(segmentTable, 0, numSegments) < numSegments)
				break;

			// Calculate total data size and read all page data.
			var dataSize = 0;
			for (var i = 0; i < numSegments; i++)
				dataSize += segmentTable[i];

			var pageData = new byte[dataSize];
			if (dataSize > 0 && stream.Read(pageData, 0, dataSize) < dataSize)
				break;

			// Extract packets from the page data using the segment table.
			var dataOffset = 0;
			var packetStart = 0;
			for (var i = 0; i < numSegments; i++)
			{
				dataOffset += segmentTable[i];

				// A segment < 255 bytes signals the end of a packet.
				if (segmentTable[i] < 255)
				{
					var packetLen = dataOffset - packetStart;
					byte[] packet;

					if (continuationBuffer != null)
					{
						// Append this segment to the continuation buffer.
						packet = new byte[continuationBuffer.Length + packetLen];
						Array.Copy(continuationBuffer, 0, packet, 0, continuationBuffer.Length);
						Array.Copy(pageData, packetStart, packet, continuationBuffer.Length, packetLen);
						continuationBuffer = null;
					}
					else
					{
						packet = new byte[packetLen];
						Array.Copy(pageData, packetStart, packet, 0, packetLen);
					}

					packetStart = dataOffset;

					// Skip the first two packets (OpusHead and OpusTags).
					if (headerPacketsSeen < 2)
					{
						headerPacketsSeen++;
						continue;
					}

					if (packet.Length > 0)
						packets.Add(packet);
				}
			}

			// If the last segment is 255, the packet continues on the next page.
			if (numSegments > 0 && segmentTable[numSegments - 1] == 255)
			{
				var remaining = dataOffset - packetStart;
				if (remaining > 0)
				{
					if (continuationBuffer != null)
					{
						var newBuf = new byte[continuationBuffer.Length + remaining];
						Array.Copy(continuationBuffer, 0, newBuf, 0, continuationBuffer.Length);
						Array.Copy(pageData, packetStart, newBuf, continuationBuffer.Length, remaining);
						continuationBuffer = newBuf;
					}
					else
					{
						continuationBuffer = new byte[remaining];
						Array.Copy(pageData, packetStart, continuationBuffer, 0, remaining);
					}
				}
			}
		}

		return packets;
	}

	/// <summary>
	/// Asynchronously decodes an Opus audio file to interleaved PCM float data.
	/// </summary>
	/// <param name="fileName">Path to the Opus audio file.</param>
	/// <param name="targetSampleRate">Desired output sample rate.</param>
	/// <param name="token">CancellationToken for cancelling the work.</param>
	/// <returns>Tuple of (interleaved float PCM samples, number of channels).</returns>
	public static async Task<(float[] Samples, int NumChannels)> DecodeOpusAsync(
		string fileName, uint targetSampleRate, CancellationToken token)
	{
		return await Task.Run(() =>
		{
			try
			{
				// Opus always decodes at 48000 Hz.
				const uint opusSampleRate = 48000;

				List<byte[]> rawPackets;
				int numChannels;
				using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					rawPackets = ReadOggOpusPackets(fileStream);
					if (rawPackets.Count == 0)
					{
						throw new Exception($"Failed to read any Opus packets from {fileName}.");
					}

					// Read the channel count from the OpusHead header before creating the decoder.
					if (!GetOpusChannelCount(fileStream, out numChannels))
					{
						throw new Exception("Could not determine channel count.");
					}
				}

				token.ThrowIfCancellationRequested();

				// Decode all raw Opus packets.
#pragma warning disable CS0618 // Type or member is obsolete
				var decoder = new OpusDecoder((int)opusSampleRate, numChannels);
#pragma warning restore CS0618 // Type or member is obsolete
				var allSamples = new List<float[]>();
				var totalSamples = 0;
				foreach (var rawPacket in rawPackets)
				{
					try
					{
						var numSamples = OpusPacketInfo.GetNumSamples(
							(ReadOnlySpan<byte>)rawPacket, (int)opusSampleRate);
						var floatPacket = new float[numSamples * numChannels];
#pragma warning disable CS0618 // Type or member is obsolete
						decoder.Decode(rawPacket, 0, rawPacket.Length, floatPacket, 0, numSamples);
#pragma warning restore CS0618 // Type or member is obsolete
						allSamples.Add(floatPacket);
						totalSamples += floatPacket.Length;
					}
					catch (Exception e)
					{
						Logger.Warn($"Failed to decode Opus packet: {e}");
					}
				}

				token.ThrowIfCancellationRequested();

				// Combine all packets into a single buffer.
				var samples = new float[totalSamples];
				var offset = 0;
				foreach (var packet in allSamples)
				{
					Array.Copy(packet, 0, samples, offset, packet.Length);
					offset += packet.Length;
				}

				// Resample if the target sample rate differs from Opus's native 48000 Hz.
				if (targetSampleRate != opusSampleRate)
				{
					samples = ResampleFloat(samples, numChannels, opusSampleRate, targetSampleRate, token);
				}

				return (samples, numChannels);
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to decode opus file. {e}");
			}

			return (Array.Empty<float>(), 0);
		}, token);
	}

	#endregion Opus

	#region DSP

	/// <summary>
	/// Creates a new DSP on the master ChannelGroup.
	/// The given readCallback will be called with the given userData for DSP processing.
	/// It is expected that DestroyDsp is called later to clean up the DSP.
	/// </summary>
	/// <param name="name">DSP name identifier. Must be unique.</param>
	/// <param name="readCallback">DSP_READCALLBACK to invoke for DSP processing.</param>
	/// <param name="userData">User data to pass into the callback.</param>
	public void CreateDsp(string name, DSP_READCALLBACK readCallback, object userData)
	{
		ErrCheck(System.getMasterChannelGroup(out var mainGroup));
		CreateDsp(name, mainGroup, readCallback, userData);
	}

	/// <summary>
	/// Creates a new DSP on the given ChannelGroup.
	/// The given readCallback will be called with the given userData for DSP processing.
	/// It is expected that DestroyDsp is called later to clean up the DSP.
	/// </summary>
	/// <param name="name">DSP name identifier. Must be unique.</param>
	/// <param name="channelGroup">ChannelGroup to attach the DSP to.</param>
	/// <param name="readCallback">DSP_READCALLBACK to invoke for DSP processing.</param>
	/// <param name="userData">User data to pass into the callback.</param>
	public void CreateDsp(string name, ChannelGroup channelGroup, DSP_READCALLBACK readCallback, object userData)
	{
		var handle = new DspHandle(System, readCallback, userData);
		DspHandles.Add(name, handle);
		ErrCheck(channelGroup.addDSP(0, handle.GetDsp()));
	}

	/// <summary>
	/// Destroys the DSP identified by the given name.
	/// </summary>
	/// <param name="name">DSP name.</param>
	public void DestroyDsp(string name)
	{
		if (!DspHandles.Remove(name, out var handle))
		{
			throw new ArgumentException($"No DSP found for {name}");
		}

		ErrCheck(System.getMasterChannelGroup(out var mainGroup));
		ErrCheck(mainGroup.removeDSP(handle!.GetDsp()));
	}

	#endregion DSP
}
