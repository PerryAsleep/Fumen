using System;
using System.Collections.Generic;
using System.Text;

namespace Fumen
{
	public class Logger
	{
		public static void Info(string message)
		{
			Console.WriteLine($"[INFO] {message}");
		}
		public static void Warn(string message)
		{
			Console.WriteLine($"[WARNING] {message}");
		}
		public static void Error(string message)
		{
			Console.WriteLine($"[ERROR] {message}");
		}
	}
}
