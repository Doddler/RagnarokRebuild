using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json;
using OfficeOpenXml;
using RebuildData.Shared.ClientTypes;

namespace DataToClientUtility
{
	class Program
	{
		static void Main(string[] args)
		{
			var path = @"..\..\..\..\RebuildZoneServer\Data\GameData.xlsx";
			var outPath = @"..\..\..\..\..\RebuildClient\Assets\Data\";


			File.Copy(path, ".\\GameData.xlsx", true);

			var xls = new ExcelPackage(new FileInfo("GameData.xlsx"));

			var sheet = xls.Workbook.Worksheets["Monsters"];

			int lastRow = sheet.Cells.Last(cell => !string.IsNullOrWhiteSpace(cell?.Value?.ToString())).End.Row;

			var mData = new List<MonsterClassData>();

			var table = sheet.Tables.First();

			var idColumn = table.Columns.First(c => c.Name == "Id").Position + 1;
			var nameColumn = table.Columns.First(c => c.Name == "Name").Position + 1;
			var spriteColumn = table.Columns.First(c => c.Name == "ClientSprite").Position + 1;
			var offsetColumn = table.Columns.First(c => c.Name == "ClientOffset").Position + 1;
			var offsetShadow = table.Columns.First(c => c.Name == "ClientShadow").Position + 1;
			var clientSize = table.Columns.First(c => c.Name == "ClientSize").Position + 1;


			for (var row = 2; row <= lastRow; row++)
			{
				var mc = new MonsterClassData()
				{
					Id = Convert.ToInt32((double) sheet.Cells[row, idColumn].Value),
					Name = sheet.Cells[row, nameColumn].Value as string,
					SpriteName = "Assets/Sprites/Monsters/" + (string)sheet.Cells[row, spriteColumn].Value,
					Offset = (float)(double)sheet.Cells[row, offsetColumn].Value,
					ShadowSize = (float)(double)sheet.Cells[row, offsetShadow].Value,
					Size = (float)(double)sheet.Cells[row, clientSize].Value
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

			xls.Dispose();

			File.Delete("GameData.xlsx");
		}
	}
}
