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
		static void Main(string[] args)
		{
			var path = @"..\..\..\..\RebuildZoneServer\Data\";
			var outPath = @"..\..\..\..\..\RebuildClient\Assets\Data\";

			using var tr = new StreamReader(Path.Combine(path, "Monsters.csv")) as TextReader;
			using var csv = new CsvReader(tr, CultureInfo.CurrentCulture);

			var monsters = csv.GetRecords<CsvMonsterData>().ToList();
			var mData = new List<MonsterClassData>(monsters.Count);
			
			foreach(var monster in monsters)
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
