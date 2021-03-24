using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fumen
{
	/// <summary>
	/// Log levels.
	/// </summary>
	public enum LogLevel
	{
		Info,
		Warn,
		Error,
		None
	}

	/// <summary>
	/// Simple interface for logging methods.
	/// Useful when wanting to capture instance information in each log message
	/// and the caller does not have access to that information.
	/// </summary>
	public interface ILogger
	{
		void Info(string message);
		void Warn(string message);
		void Error(string message);
	}

	/// <summary>
	/// Provides methods for logging messages to the console and optionally to disk.
	/// </summary>
	public class Logger : IDisposable
	{
		/// <summary>
		/// Bounding capacity of the BlockingCollection queue.
		/// </summary>
		private const int QueueCapacity = 64;

		/// <summary>
		/// Static Logger instance for logging to disk.
		/// </summary>
		private static Logger Instance;
		/// <summary>
		/// Static log level.
		/// </summary>
		private static LogLevel LogLevel = LogLevel.Info;

		/// <summary>
		/// Queue of messages to write to the StreamWriter.
		/// </summary>
		private readonly BlockingCollection<string> LogQueue;
		/// <summary>
		/// StreamWriter for writing messages to the FileStream.
		/// </summary>
		private readonly StreamWriter StreamWriter;
		/// <summary>
		/// FileStream for writing the messages to disk.
		/// </summary>
		private readonly FileStream FileStream;
		/// <summary>
		/// Task for writing enqueued messages to the StreamWriter.
		/// </summary>
		private readonly Task WriteQueueTask;
		/// <summary>
		/// Timer for periodically flushing the StreamWriter to disk.
		/// </summary>
		private readonly Timer Timer;
		/// <summary>
		/// Object for locking the StreamWriter.
		/// </summary>
		private readonly object StreamWriterLock = new object();
		/// <summary>
		/// Whether or not to write to the console when logging.
		/// </summary>
		private readonly bool WriteToConsole;

		/// <summary>
		/// Start up the logger.
		/// The logger will log to the console and stream messages to disk.
		/// </summary>
		/// <param name="logLevel">Log level. Messages of lower priority will not be logged.</param>
		/// <param name="logFilePath">Path to the log file to stream to. Will be created if it does not exist.</param>
		/// <param name="flushIntervalSeconds">Interval in seconds to flush the log to disk.</param>
		/// <param name="bufferSizeBytes">Buffer size. Buffer will flush to disk when it is full.</param>
		/// <param name="writeToConsole">Whether or not to write the console in addition to the log file.</param>
		public static void StartUp(
			LogLevel logLevel,
			string logFilePath,
			int flushIntervalSeconds,
			int bufferSizeBytes,
			bool writeToConsole)
		{
			LogLevel = logLevel;

			Instance?.Dispose();
			Instance = new Logger(logFilePath, flushIntervalSeconds, bufferSizeBytes, writeToConsole);
		}

		/// <summary>
		/// Start up the logger.
		/// The logger will only log to the console.
		/// </summary>
		/// <param name="logLevel">Log level. Messages of lower priority will not be logged.</param>
		public static void StartUp(LogLevel logLevel)
		{
			LogLevel = logLevel;

			Instance?.Dispose();
			Instance = new Logger();
		}

		/// <summary>
		/// Shutdown the logger.
		/// Disposes the underlying instance if present, flushing any remaining messages to disk.
		/// </summary>
		public static void Shutdown()
		{
			Instance?.Dispose();
		}

		/// <summary>
		/// Public static method to log an info message.
		/// Writes the message to console, and enqueues it to write to disk if configured to do so.
		/// </summary>
		/// <param name="message">Message to log.</param>
		public static void Info(string message)
		{
			if (LogLevel > LogLevel.Info)
				return;
			var formattedMessage = FormatMessage("[INFO]", message);
			Instance?.Log(formattedMessage);
		}

		/// <summary>
		/// Public static method to log a warning message.
		/// Writes the message to console, and enqueues it to write to disk if configured to do so.
		/// </summary>
		/// <param name="message">Message to log.</param>
		public static void Warn(string message)
		{
			if (LogLevel > LogLevel.Warn)
				return;
			var formattedMessage = FormatMessage("[WARNING]", message);
			Instance?.Log(formattedMessage);
		}

		/// <summary>
		/// Public static method to log an error message.
		/// Writes the message to console, and enqueues it to write to disk if configured to do so.
		/// </summary>
		/// <param name="message">Message to log.</param>
		public static void Error(string message)
		{
			if (LogLevel > LogLevel.Error)
				return;
			var formattedMessage = FormatMessage("[ERROR]", message);
			Instance?.Log(formattedMessage);
		}

		/// <summary>
		/// Formats the given message for writing to the log.
		/// </summary>
		/// <param name="logLevel">Log level string.</param>
		/// <param name="message">Message string.</param>
		/// <returns>Formatted string.</returns>
		private static string FormatMessage(string logLevel, string message)
		{
			return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {logLevel} {message}";
		}

		/// <summary>
		/// Private Constructor.
		/// </summary>
		/// <remarks>The logger will write to a log file and optionally to the console.</remarks>
		/// <param name="logFilePath">Path to the log file to stream to. Will be created if it does not exist.</param>
		/// <param name="flushIntervalSeconds">Interval in seconds to flush the log to disk.</param>
		/// <param name="bufferSizeBytes">Buffer size. Buffer will flush to disk when it is full.</param>
		/// <param name="writeToConsole">Whether or not to write the console in addition to the log file.</param>
		private Logger(string logFilePath, int flushIntervalSeconds, int bufferSizeBytes, bool writeToConsole)
		{
			FileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
			StreamWriter = new StreamWriter(FileStream, Encoding.UTF8, bufferSizeBytes)
			{
				AutoFlush = false
			};
			LogQueue = new BlockingCollection<string>(QueueCapacity);
			WriteToConsole = writeToConsole;

			// Start a Task to write enqueued messages to the StreamWriter.
			WriteQueueTask = Task.Factory.StartNew(
				WriteQueue,
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);

			// Start a timer to flush the StreamWriter to disk periodically.
			if (flushIntervalSeconds > 0)
			{
				var periodMillis = flushIntervalSeconds * 1000;
				Timer = new Timer(FlushTimerCallback, null, periodMillis, periodMillis);
			}
		}

		/// <summary>
		/// Private Constructor.
		/// </summary>
		/// <remarks>The logger will write to the console.</remarks>
		private Logger()
		{
			LogQueue = new BlockingCollection<string>(QueueCapacity);
			WriteToConsole = true;
			WriteQueueTask = Task.Factory.StartNew(
				WriteQueue,
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);
		}

		/// <summary>
		/// IDisposable Dispose method to dispose IDisposable resources.
		/// </summary>
		public void Dispose()
		{
			Timer?.Dispose();
			LogQueue?.CompleteAdding();
			WriteQueueTask?.Wait();
			StreamWriter?.Dispose();
			FileStream?.Dispose();
			LogQueue?.Dispose();
		}

		/// <summary>
		/// Logs a message by enqueueing it for write to the underlying stream.
		/// </summary>
		/// <param name="message">Message to log.</param>
		private void Log(string message)
		{
			try
			{
				LogQueue.Add(message);
			}
			catch (Exception e)
			{
				Console.WriteLine(FormatMessage("[ERROR]", $"Failed to log message: \"{message}\". {e}"));
			}
		}

		/// <summary>
		/// Callback from the Timer to flush the StreamWriter.
		/// </summary>
		/// <param name="_">Unused.</param>
		private void FlushTimerCallback(object _)
		{
			lock (StreamWriterLock)
			{
				StreamWriter?.Flush();
			}
		}

		/// <summary>
		/// Method called via a Task to write enqueued messages to the StreamWriter.
		/// </summary>
		private void WriteQueue()
		{
			foreach (var message in LogQueue.GetConsumingEnumerable())
			{
				if (WriteToConsole)
				{
					Console.WriteLine(message);
				}
				if (StreamWriter != null)
				{
					lock (StreamWriterLock)
					{
						StreamWriter?.WriteLine(message);
					}
				}
			}
		}
	}
}
