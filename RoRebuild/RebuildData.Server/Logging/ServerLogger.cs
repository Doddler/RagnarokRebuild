using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildData.Server.Logging
{
	public static class ServerLogger
	{
		public static void Debug(string message)
		{
#if DEBUG
			Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Debug] {message}");
#endif
		}

		public static void Log(string message) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Info] {message}");
		public static void LogWarning(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Warning] {error}");
		public static void LogError(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Error] {error}");
	}
}
