using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
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

			Profiler.Init(0.005f); //logs events for frames that take longer than 5ms

			Time.Start();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var total = 0d;
			var max = 0d;
			var spos = 0;
			var lastLog = Time.ElapsedTime;

			var totalNetwork = 0d;
			var totalEcs = 0d;
			var totalWorld = 0d;
			var maxNetwork = 0d;
			var maxEcs = 0d;
			var maxWorld = 0d;


			GC.Collect();
			GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

#if DEBUG
			var noticeTime = 5f;
#else
			var noticeTime = 5f;
#endif

			while (true)
			{
				Time.Update();

				var startTime = Time.GetExactTime();

				NetworkManager.ProcessIncomingMessages();

				var networkTime = Time.GetExactTime();

				world.RunEcs();

				var ecsTime = Time.GetExactTime();

				world.Update();

				var worldTime = Time.GetExactTime();

				var elapsed = Time.GetExactTime() - startTime;
				//Console.WriteLine(elapsed);
				total += elapsed;

				Profiler.FinishFrame((float)elapsed);

				var nt = networkTime - startTime;
				var et = ecsTime - networkTime;
				var wt = worldTime - ecsTime;

				totalNetwork += nt;
				totalEcs += et;
				totalWorld += wt;

				if (max < elapsed)
					max = elapsed;

				if (maxNetwork < nt)
					maxNetwork = nt;
				if (maxEcs < et)
					maxEcs = et;
				if (maxWorld < wt)
					maxWorld = wt;

				spos++;


				var ms = (int) (elapsed * 1000) + 1;

				if(ms < 10)
					Thread.Sleep(10 - ms);
				//Thread.Sleep(1000);

				if (lastLog + noticeTime < Time.ElapsedTime)
				{
					var avg = (total / spos);
					//var fps = 1 / avg;
					var players = NetworkManager.PlayerCount;

					avg *= 1000d;

					//var avgNetwork = (totalNetwork / spos) * 1000d;
					//var avgEcs = (totalEcs / spos) * 1000d;
					//var avgWorld = (totalWorld / spos) * 1000d;

#if DEBUG
					var server = NetworkManager.State.Server;
					ServerLogger.Log(
						$"[Program] {players} players. Avg {avg:F2}ms / Peak {max * 1000:F2}ms "
						+ $"(Net/ECS/World: {maxNetwork * 1000:F2}/{maxEcs * 1000:F2}/{maxWorld:F2}) "
						+ $"Sent {server.Statistics.SentBytes}bytes/{server.Statistics.SentMessages}msg/{server.Statistics.SentPackets}packets");
#else
					ServerLogger.Log(
						$"[Program] {players} players. Avg {avg:F2}ms / Peak {max * 1000:F2}ms "
						+ $"(Net/ECS/World: {maxNetwork * 1000:F2}/{maxEcs * 1000:F2}/{maxWorld:F2})");
#endif
					lastLog = Time.ElapsedTime;

					total = 0;
					max = 0;
					spos = 0;

					totalNetwork = 0;
					totalWorld = 0;
					totalEcs = 0;
					maxNetwork = 0;
					maxWorld = 0;
					maxEcs = 0;

					NetworkManager.ScanAndDisconnect();
				}
			}
		}
	}
}
