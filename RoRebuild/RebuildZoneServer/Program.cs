using System.Diagnostics;
using System.Linq;
using RebuildData.Server.Config;
using RebuildData.Server.Logging;
using RebuildData.Server.Pathfinding;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer
{
	class Program
	{
		static void Main(string[] args)
		{
			ServerLogger.Log("Ragnarok Rebuild Zone Server, starting up!");

			DistanceCache.Init();
			DataManager.Initialize();
			var world = new World();
			NetworkManager.Init(world);
			
			Time.Start();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var frameCount = 200;

			var samples = new double[frameCount];
			var spos = 0;
			var lastLog = Time.ElapsedTime;

			while (true)
			{
				Time.Update();

				NetworkManager.ProcessIncomingMessages();

				world.Update();
				
				samples[spos] = Time.GetExactTime() - Time.ElapsedTime;
				spos++;
				if (spos == frameCount)
				{
					var avg = (samples.Sum() / frameCount);
					var max = samples.Max() * 1000;
					var fps = 1 / avg;
					if (lastLog + 10 < Time.ElapsedTime)
					{
#if DEBUG
						var server = NetworkManager.State.Server;
						ServerLogger.Log(
							$"[Program] Average frame time: {avg:F3}ms ({fps:N0}fps), Peak frame time: {max:F3}ms, " +
							$"Sent {server.Statistics.SentBytes} bytes, {server.Statistics.SentMessages} messages, {server.Statistics.SentPackets} packets");
#else
						ServerLogger.Log(
							$"[Program] Average frame time: {avg*1000:F3}ms ({fps:N0}fps), Peak frame time: {max:F3}ms");
#endif
						lastLog = Time.ElapsedTime;
					}

					spos = 0;

					NetworkManager.ScanAndDisconnect();
				}
			}
		}
	}
}
