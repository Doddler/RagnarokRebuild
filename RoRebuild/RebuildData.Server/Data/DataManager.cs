using System;
using System.Collections.Generic;
using RebuildData.Server.Data;
using RebuildData.Server.Data.Types;
using RebuildData.Shared.Data;
using RebuildZoneServer.Data.Management.Types;

namespace RebuildZoneServer.Data.Management
{
	public static class DataManager
	{
		private static List<MonsterDatabaseInfo> monsterStats;
		private static Dictionary<int, MonsterDatabaseInfo> monsterIdLookup;
		private static Dictionary<string, MonsterDatabaseInfo> monsterCodeLookup;
		private static Dictionary<string, List<MapConnector>> mapConnectorLookup;

		private static MapSpawnDatabaseInfo mapSpawnInfo;

		private static List<MapEntry> mapList;
		public static List<MapEntry> Maps => mapList;

		public static bool HasMonsterWithId(int id)
		{
			return monsterIdLookup.ContainsKey(id);
		}

		public static List<MapConnector> GetMapConnectors(string mapName)
		{
			if (mapConnectorLookup.TryGetValue(mapName, out var list))
				return list;

			mapConnectorLookup.Add(mapName, new List<MapConnector>());
			return mapConnectorLookup[mapName];
		}

		public static int GetMonsterIdForCode(string code)
		{
#if DEBUG
			if (!monsterCodeLookup.ContainsKey(code))
				throw new Exception("Could not find monster in code lookup with with code: " + code);
#endif
			return monsterCodeLookup[code].Id;
		}

		public static MonsterDatabaseInfo GetMonsterById(int id)
		{
			return monsterIdLookup[id];
		}

		public static List<MapSpawnEntry> GetSpawnsForMap(string mapCode)
		{
			if (mapSpawnInfo.MapSpawnEntries.ContainsKey(mapCode))
				return mapSpawnInfo.MapSpawnEntries[mapCode];

			return null;
		}

		public static MapConnector GetConnector(string mapName, Position pos)
		{
			if (!mapConnectorLookup.ContainsKey(mapName))
				return null;

			var cons = mapConnectorLookup[mapName];
			for (var i = 0; i < mapConnectorLookup[mapName].Count; i++)
			{
				var entry = mapConnectorLookup[mapName][i];

				if (entry.SrcArea.Contains(pos))
					return entry;
			}

			return null;
		}

		public static void Initialize(string dataPath)
		{
			var loader = new DataLoader();

			mapList = loader.LoadMaps();
			mapConnectorLookup = loader.LoadConnectors(mapList);
			monsterStats = loader.LoadMonsterStats();
			mapSpawnInfo = loader.LoadSpawnInfo();
			
			monsterIdLookup = new Dictionary<int, MonsterDatabaseInfo>(monsterStats.Count);
			monsterCodeLookup = new Dictionary<string, MonsterDatabaseInfo>(monsterStats.Count);

			
			foreach (var m in monsterStats)
			{
				monsterIdLookup.Add(m.Id, m);
				monsterCodeLookup.Add(m.Code, m);
			}
		}
	}
}
