using System.Diagnostics;
using System.Linq;
using RebuildData.Server.Config;
using RebuildData.Server.Logging;
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
