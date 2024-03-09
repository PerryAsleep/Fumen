using System;

namespace Fumen;

/// <summary>
/// Class to assist with persisting a file in an "atomic" manner where crashes or failures mid-write
/// will not result in any corruption to the existing file.
/// Expected usage:
///  Create an AtomicPersistenceFile in a using block with the file to be saved.
///  Call GetFilePathToSaveTo and save to that file location.
///  When the AtomicPersistenceFile is disposed, it will swap the temporary file to the desired file.
/// </summary>
public sealed class AtomicPersistenceFile : IDisposable
{
	/// <summary>
	/// Whether or not the temporary file has been committed to the actual save location.
	/// </summary>
	private bool Committed;

	/// <summary>
	/// The actual path to save to.
	/// </summary>
	private readonly string FilePath;

	/// <summary>
	/// Temporary file path that will be saved to first.
	/// </summary>
	private readonly string TempFilePath;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="filePath">Desired file path to save to.</param>
	public AtomicPersistenceFile(string filePath)
	{
		FilePath = filePath;
		TempFilePath = $"{FilePath}.tmp";
	}

	/// <summary>
	/// Gets the file path that should be saved to.
	/// This is a temporary file. Failures to write to this temporary file will not affect any existing file.
	/// </summary>
	/// <returns>File path of temporary file to save to.</returns>
	public string GetFilePathToSaveTo()
	{
		return TempFilePath;
	}

	/// <summary>
	/// Finalizer.
	/// </summary>
	~AtomicPersistenceFile()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	// ReSharper disable once UnusedParameter.Local
	private void Dispose(bool disposing)
	{
		// Commit the file if it hasn't been committed.
		Commit();
	}

	/// <summary>
	/// Commit the temporary file over the actual file.
	/// </summary>
	public void Commit()
	{
		// Early out. Only commit once.
		if (Committed)
			return;
		Committed = true;

		// Move the temp file on top of the actual file to write to.
		// This will automatically clean up the temp file if successful.
		System.IO.File.Move(TempFilePath, FilePath, true);
	}
}
