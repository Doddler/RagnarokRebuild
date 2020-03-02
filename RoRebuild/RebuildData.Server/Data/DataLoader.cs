using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using RebuildData.Server.Data.Types;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildZoneServer.Data.Management.Types;

namespace RebuildData.Server.Data
{
	class DataLoader : IDisposable
	{
		private readonly ExcelPackage xlsFile;

		public DataLoader(string path)
		{
			xlsFile = new ExcelPackage(new FileInfo(path));
		}

		public Dictionary<string,List<MapConnector>> LoadConnectors(List<MapEntry> maps)
		{
			var connectors = new Dictionary<string, List<MapConnector>>();

			var sheet = xlsFile.Workbook.Worksheets["Connectors"];
			var table = sheet.Tables.First();
			var rowCount = table.Address.Rows;
			var obj = new List<MonsterDatabaseInfo>(rowCount);

			var entryCount = 0;

			for (var row = 2; row <= rowCount; row++)
			{
				var cellOne = sheet.Cells[row, 1].Value;
				if (cellOne == null)
					continue;

				var enabled = (bool)cellOne;
				if(!enabled)
					continue;
				
				var conCell = sheet.Cells[row, 2].Value as string;

				if (conCell == null)
					continue;

				var x = Convert.ToInt32((double)sheet.Cells[row, 3].Value);
				var y = Convert.ToInt32((double)sheet.Cells[row, 4].Value);
				var width = Convert.ToInt32((double)sheet.Cells[row, 5].Value);
				var height = Convert.ToInt32((double)sheet.Cells[row, 6].Value);
				var targetX = Convert.ToInt32((double)sheet.Cells[row, 8].Value);
				var targetY = Convert.ToInt32((double)sheet.Cells[row, 9].Value);
				var targetWidth = Convert.ToInt32((double)sheet.Cells[row, 10].Value);
				var targetHeight = Convert.ToInt32((double)sheet.Cells[row, 11].Value);

				var con = new MapConnector()
				{
					Map = conCell,
					SrcArea = new Area(x - width, y - height, x + width, y + height),
					Target = sheet.Cells[row, 7].Value as string,
					DstArea = new Area(targetX - targetWidth, targetY - targetHeight, targetX + targetWidth, targetY + targetHeight)
				};

				if (con.Map == null)
					continue;

				if (!maps.Any(m => m.Code == con.Target))
				{
					ServerLogger.LogWarning($"Connection on map {con.Map} goes to invalid map {con.Target}");
					continue;
				}

				if(!connectors.ContainsKey(con.Map))
					connectors.Add(con.Map, new List<MapConnector>());

				connectors[con.Map].Add(con);
				entryCount++;
			}

			ServerLogger.Log($"Loading connectors: {entryCount}");

			return connectors;
		}

		public List<MapEntry> LoadMaps()
		{
			var maps = new List<MapEntry>();

			var sheet = xlsFile.Workbook.Worksheets["Maps"];
			var table = sheet.Tables.First();
			var rowCount = table.Address.Rows;
			var obj = new List<MonsterDatabaseInfo>(rowCount);

			for (var row = 2; row <= rowCount; row++)
			{
				var name = sheet.Cells[row, 1].Value as string;

				if (name == null)
					continue;

				var m = new MapEntry()
				{
					Name = name,
					Code = sheet.Cells[row, 2].Value as string,
					WalkData = sheet.Cells[row, 3].Value as string,
				};

				maps.Add(m);
			}

			ServerLogger.Log($"Loading maps: {maps.Count}");

			return maps;
		}

		public List<MonsterDatabaseInfo> LoadMonsterStats()
		{
			var sheet = xlsFile.Workbook.Worksheets["Monsters"];
			var table = sheet.Tables.First();
			var rowCount = table.Address.Rows;
			var obj = new List<MonsterDatabaseInfo>(rowCount);
			
			var idColumn = table.Columns.First(c => c.Name == "Id").Position + 1;
			var nameColumn = table.Columns.First(c => c.Name == "Name").Position + 1;
			var codeColumn = table.Columns.First(c => c.Name == "Code").Position + 1;
			var moveSpeedColumn = table.Columns.First(c => c.Name == "MoveSpeed").Position + 1;

			for (var row = 2; row <= rowCount; row++)
			{
				if (sheet.Cells[row, idColumn].Value == null)
					continue;

				obj.Add(new MonsterDatabaseInfo()
				{
					Id = Convert.ToInt32((double)sheet.Cells[row, idColumn].Value),
					Name = sheet.Cells[row, nameColumn].Value as string,
					Code = sheet.Cells[row, codeColumn].Value as string,
					MoveSpeed = ((float)(double)sheet.Cells[row, moveSpeedColumn].Value)/1000f
				});
			}


			ServerLogger.Log($"Loading monsters: {obj.Count}");

			return obj;
		}

		public MapSpawnDatabaseInfo LoadSpawnInfo()
		{
			var mapSpawns = new MapSpawnDatabaseInfo();
			mapSpawns.MapSpawnEntries = new Dictionary<string, List<MapSpawnEntry>>();

			var sheet = xlsFile.Workbook.Worksheets["MapSpawns"];
			var table = sheet.Tables.First();
			var rowCount = table.Address.Rows;

			var entryCount = 0;

			for (var row = 2; row <= rowCount; row++)
			{
				var map = sheet.Cells[row, 1].Value as string;

				if (map == null)
					continue;

				var entry = new MapSpawnEntry()
				{
					X = Convert.ToInt32((double)sheet.Cells[row, 2].Value),
					Y = Convert.ToInt32((double)sheet.Cells[row, 3].Value),
					Width = Convert.ToInt32((double)sheet.Cells[row, 4].Value),
					Height = Convert.ToInt32((double)sheet.Cells[row, 5].Value),
					Count = Convert.ToInt32((double)sheet.Cells[row, 6].Value),
					Class = sheet.Cells[row, 7].Value as string,
					SpawnTime = ((float)(double)sheet.Cells[row, 8].Value) / 1000f,
					SpawnVariance = ((float)(double)sheet.Cells[row, 9].Value) / 1000f,
				};

				if(!mapSpawns.MapSpawnEntries.ContainsKey(map))
					mapSpawns.MapSpawnEntries.Add(map, new List<MapSpawnEntry>());

				mapSpawns.MapSpawnEntries[map].Add(entry);
				entryCount++;
			}
			
			ServerLogger.Log($"Loading map spawn sets: {entryCount}");

			return mapSpawns;
		}

		public void Dispose()
		{
			xlsFile?.Dispose();
		}
	}
}
