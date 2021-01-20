using System;

namespace Fumen
{
	public enum LogLevel
	{
		Info,
		Warn,
		Error,
		None
	}

	public class Logger
	{
		public static LogLevel LogLevel = LogLevel.Info;

		public static void Info(string message)
		{
			if (LogLevel > LogLevel.Info)
				return;
			Console.WriteLine($"[INFO] {message}");
		}
		public static void Warn(string message)
		{
			if (LogLevel > LogLevel.Warn)
				return;
			Console.WriteLine($"[WARNING] {message}");
		}
		public static void Error(string message)
		{
			if (LogLevel > LogLevel.Error)
				return;
			Console.WriteLine($"[ERROR] {message}");
		}
	}

	public interface ILogger
	{
		void Info(string message);
		void Warn(string message);
		void Error(string message);
	}
}
