using System;
using System.Runtime.InteropServices;

namespace Fumen;

/// <summary>
/// Class for wrapping a managed buffer with a pinned handle.
/// </summary>
public sealed class PinnedBuffer : IDisposable
{
	/// <summary>
	/// Pinned handle to managed data.
	/// </summary>
	private GCHandle Handle;

	/// <summary>
	/// Managed data.
	/// </summary>
	private readonly byte[] Data;

	public PinnedBuffer(byte[] data)
	{
		Data = data;
		Handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
	}

	~PinnedBuffer()
	{
		Dispose(false);
	}

	public byte[] GetPinnedData()
	{
		return Data;
	}

	public IntPtr GetPinnedDataHandle()
	{
		return Handle.AddrOfPinnedObject();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			Handle.Free();
		}
	}
}
