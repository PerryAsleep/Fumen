using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

		public class LogMessage
		{
			public readonly string Message;
			public readonly LogLevel Level;
			public readonly DateTime Time;

			public LogMessage(string message, LogLevel level, DateTime time)
			{
				Message = message;
				Level = level;
				Time = time;
			}

			public override string ToString()
			{
				string levelStr = null;
				switch (Level)
				{
					case LogLevel.Info:
						levelStr = " [INFO]";
						break;
					case LogLevel.Warn:
						levelStr = " [WARN]";
						break;
					case LogLevel.Error:
						levelStr = " [ERROR]";
						break;
				}

				return $"{Time:yyyy-MM-dd HH:mm:ss.fff}{levelStr} {Message}";
			}
		}

		/// <summary>
		/// Configuration object for initializing the Logger.
		/// </summary>
		public class Config
		{
			public LogLevel Level = LogLevel.Info;

			public bool WriteToFile;
			public string LogFilePath;
			public int LogFileFlushIntervalSeconds;
			public int LogFileBufferSizeBytes;

			public bool WriteToConsole;

			public bool WriteToBuffer;
			public object BufferLock;
			public LinkedList<LogMessage> Buffer;
			public int BufferSize;
		}

		/// <summary>
		/// Queue of messages to write to the StreamWriter.
		/// </summary>
		private readonly BlockingCollection<LogMessage> LogQueue;

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

		private readonly object BufferLock;
		private readonly LinkedList<LogMessage> Buffer;
		private readonly int BufferSize;

		/// <summary>
		/// Start up the logger.
		/// </summary>
		/// <param name="config">Config object for configuring the logger.</param>
		public static void StartUp(Config config)
		{
			LogLevel = config.Level;

			Instance?.Dispose();
			Instance = new Logger(config);
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
			Instance?.Log(new LogMessage(message, LogLevel.Info, DateTime.Now));
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
			Instance?.Log(new LogMessage(message, LogLevel.Warn, DateTime.Now));
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
			Instance?.Log(new LogMessage(message, LogLevel.Error, DateTime.Now));
		}

		/// <summary>
		/// Private Constructor.
		/// </summary>
		/// <param name="config">Config object for configuring the logger.</param>
		private Logger(Config config)
		{
			if (config.WriteToFile)
			{
				FileStream = new FileStream(config.LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				StreamWriter = new StreamWriter(FileStream, Encoding.UTF8, config.LogFileBufferSizeBytes)
				{
					AutoFlush = false,
				};
			}

			WriteToConsole = config.WriteToConsole;
			LogQueue = new BlockingCollection<LogMessage>(QueueCapacity);

			if (config.WriteToBuffer)
			{
				Buffer = config.Buffer;
				BufferLock = config.BufferLock;
				BufferSize = config.BufferSize;
			}

			// Start a Task to write enqueued messages to the StreamWriter.
			WriteQueueTask = Task.Factory.StartNew(
				WriteQueue,
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);

			// Start a timer to flush the StreamWriter to disk periodically.
			if (config.WriteToFile)
			{
				if (config.LogFileFlushIntervalSeconds > 0)
				{
					var periodMillis = config.LogFileFlushIntervalSeconds * 1000;
					Timer = new Timer(FlushTimerCallback, null, periodMillis, periodMillis);
				}
			}
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
		private void Log(LogMessage message)
		{
			try
			{
				LogQueue.Add(message);
			}
			catch (Exception)
			{
				Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERROR] Failed to log message: \"{message}\"");
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
		/// Method called via a Task to write enqueued messages.
		/// </summary>
		private void WriteQueue()
		{
			foreach (var message in LogQueue.GetConsumingEnumerable())
			{
				// Write to the console, if configured to do so.
				if (WriteToConsole)
					Console.WriteLine(message);

				// Write to the StreamWriter for writing to a file, if configured to do so.
				if (StreamWriter != null)
				{
					lock (StreamWriterLock)
					{
						StreamWriter?.WriteLine(message);
					}
				}

				// Write to the provided Buffer, if configured to do so.
				if (Buffer != null)
				{
					lock (BufferLock)
					{
						while (Buffer.Count >= BufferSize)
							Buffer.RemoveLast();
						Buffer.AddFirst(message);
					}
				}
			}
		}
	}
}
