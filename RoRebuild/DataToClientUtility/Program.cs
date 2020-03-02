using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using Dahomey.Json;
using RebuildData.Server.Data.CsvDataTypes;
using RebuildData.Shared.ClientTypes;

namespace DataToClientUtility
{
	class Program
	{
		private const string path = @"..\..\..\..\RebuildZoneServer\Data\";
		private const string outPath = @"..\..\..\..\..\RebuildClient\Assets\Data\";

		static void Main(string[] args)
		{
			WriteMonsterData();
			WriteServerConfig();
		}


		private static void WriteServerConfig()
		{
			using var tr = new StreamReader(Path.Combine(path, @"ServerSettings.csv")) as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var entries = csv.GetRecords<CsvServerConfig>().ToList();

			var ip = entries.FirstOrDefault(e => e.Key == "IP").Value;
			var port = entries.FirstOrDefault(e => e.Key == "Port").Value;

			var configPath = Path.Combine(outPath, "serverconfig.txt");

			File.WriteAllText(configPath, $"{ip}:{port}");
		}


		private static void WriteMonsterData()
		{
			using var tr = new StreamReader(Path.Combine(path, "Monsters.csv")) as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var monsters = csv.GetRecords<CsvMonsterData>().ToList();
			var mData = new List<MonsterClassData>(monsters.Count);

			foreach (var monster in monsters)
			{
				var mc = new MonsterClassData()
				{
					Id = monster.Id,
					Name = monster.Name,
					SpriteName = monster.ClientSprite,
					Offset = monster.ClientOffset,
					ShadowSize = monster.ClientShadow,
					Size = monster.ClientSize
				};

				mData.Add(mc);
			}

			var dbTable = new DatabaseMonsterClassData();
			dbTable.MonsterClassData = mData;

			JsonSerializerOptions options = new JsonSerializerOptions();
			options.SetupExtensions();

			var json = JsonSerializer.Serialize(dbTable, options);

			var monsterDir = Path.Combine(outPath, "monsterclass.json");

			File.WriteAllText(monsterDir, json);
		}
	}
}
