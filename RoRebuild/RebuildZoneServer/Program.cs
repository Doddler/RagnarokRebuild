using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RebuildData.Server.Config;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Data;
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

			DataManager.Initialize(ServerConfig.DataPath);
			var world = new World();
			NetworkManager.Init(world);
			

			Time.Start();

			var dir = new Position(0, 5);
			var dir2 = new Position(0, -5);

			dir.GetDirection();
			dir2.GetDirection();

			dir = new Position(5, 0);
			dir = new Position(-5, 0);


			dir.GetDirection();
			dir2.GetDirection();


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


				//while (Time.MsSinceLastUpdate() < 5)
				//	Thread.Sleep(1);

				//Thread.Sleep(50);

				samples[spos] = Time.GetExactTime() - Time.ElapsedTime;
				spos++;
				if (spos == frameCount)
				{
					var avg = samples.Sum() / frameCount;
					var max = samples.Max() * 1000;
					var fps = 1 / avg;
					if (lastLog + 10 < Time.ElapsedTime)
					{
						ServerLogger.Log($"[Program] Average frame time: {avg:F3}ms ({fps:N0}fps)  Peak frame time: {max:N0}ms");
						lastLog = Time.ElapsedTime;
					}

					spos = 0;

					NetworkManager.ScanAndDisconnect();
				}
			}
		}
	}
}
