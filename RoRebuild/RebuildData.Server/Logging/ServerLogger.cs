using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RebuildData.Server.Logging
{
	public static class ServerLogger
	{
		[Conditional("DEBUG")]
		public static void Debug(string message) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Debug] {message}");
		public static void Log(string message) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Info] {message}");
		public static void LogWarning(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Warning] {error}");
		public static void LogError(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Error] {error}");
	}
}
