using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using RebuildData.Server.Data.CsvDataTypes;
using RebuildData.Server.Data.Types;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildZoneServer.Data.Management.Types;

namespace RebuildData.Server.Data
{
	class DataLoader
	{
		public Dictionary<string, List<MapConnector>> LoadConnectors(List<MapEntry> maps)
		{
			var connectors = new Dictionary<string, List<MapConnector>>();

			using var tr = new StreamReader(@"Data\Connectors.csv") as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var connections = csv.GetRecords<CsvMapConnector>();
			var entryCount = 0;

			foreach (var connector in connections)
			{
				if (string.IsNullOrWhiteSpace(connector.Source) || connector.Source.StartsWith("//") ||
					connector.Source.StartsWith("#"))
					continue;

				var con = new MapConnector()
				{
					Map = connector.Source,
					SrcArea = Area.CreateAroundPoint(new Position(connector.X, connector.Y), connector.Width, connector.Height),
					Target = connector.Target,
					DstArea = Area.CreateAroundPoint(new Position(connector.TargetX, connector.TargetY), connector.TargetWidth, connector.TargetHeight)
				};

				if (con.Map == null)
					continue;

				if (!maps.Any(m => m.Code == con.Target))
				{
					ServerLogger.LogWarning($"Connection on map {con.Map} goes to invalid map {con.Target}");
					continue;
				}

				if (!connectors.ContainsKey(con.Map))
					connectors.Add(con.Map, new List<MapConnector>());

				connectors[con.Map].Add(con);
				entryCount++;
			}

			ServerLogger.Log($"Loading connectors: {entryCount}");

			return connectors;
		}

		public List<MapEntry> LoadMaps()
		{
			using var tr = new StreamReader(@"Data\Maps.csv") as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var maps = csv.GetRecords<MapEntry>().ToList();

			ServerLogger.Log($"Loading maps: {maps.Count}");

			return maps;
		}

		public List<MonsterDatabaseInfo> LoadMonsterStats()
		{
			using var tr = new StreamReader(@"Data\Monsters.csv") as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var monsters = csv.GetRecords<CsvMonsterData>().ToList();

			var obj = new List<MonsterDatabaseInfo>(monsters.Count);

			foreach (var monster in monsters)
			{
				if (monster.Id <= 0)
					continue;

				obj.Add(new MonsterDatabaseInfo()
				{
					Id = monster.Id,
					Code = monster.Code,
					MoveSpeed = monster.MoveSpeed/1000f,
					Name = monster.Name
				});
			}

			ServerLogger.Log($"Loading monsters: {obj.Count}");

			return obj;
		}

		public MapSpawnDatabaseInfo LoadSpawnInfo()
		{
			var mapSpawns = new MapSpawnDatabaseInfo();
			mapSpawns.MapSpawnEntries = new Dictionary<string, List<MapSpawnEntry>>();

			using var tr = new StreamReader(@"Data\MapSpawns.csv") as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var spawns = csv.GetRecords<CsvMapSpawnEntry>().ToList();
			
			var entryCount = 0;

			foreach (var spawn in spawns)
			{
				if (string.IsNullOrWhiteSpace(spawn.Map) || spawn.Map.StartsWith("//") || spawn.Map.StartsWith("#"))
					continue;


				var entry = new MapSpawnEntry()
				{
					X = spawn.X,
					Y = spawn.Y,
					Width = spawn.Width,
					Height = spawn.Height,
					Count = spawn.Count,
					Class = spawn.Class,
					SpawnTime = spawn.SpawnTime,
					SpawnVariance = spawn.Variance
				};


				if (!mapSpawns.MapSpawnEntries.ContainsKey(spawn.Map))
					mapSpawns.MapSpawnEntries.Add(spawn.Map, new List<MapSpawnEntry>());

				mapSpawns.MapSpawnEntries[spawn.Map].Add(entry);
				entryCount++;
			}

			ServerLogger.Log($"Loading map spawn sets: {entryCount}");

			return mapSpawns;
		}
	}
}
