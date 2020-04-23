using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RebuildData.Server.Logging
{
	public static class ServerLogger
    {
        private static ILogger logger;

        public static void RegisterLogger(ILogger log) => logger = log;

        [Conditional("DEBUG")]
		public static void Debug(string message) => logger.LogDebug(message);
		public static void Log(string message) => logger.LogInformation(message);
		public static void LogWarning(string error) => logger.LogWarning(error);
		public static void LogError(string error) => logger.LogError(error);
	}
}
