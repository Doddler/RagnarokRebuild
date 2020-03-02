using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildData.Server.Logging
{
	public static class ServerLogger
	{
		public static void Log(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Info] {error}");
		public static void LogWarning(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Warning] {error}");
		public static void LogError(string error) => Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: [Error] {error}");
	}
}
