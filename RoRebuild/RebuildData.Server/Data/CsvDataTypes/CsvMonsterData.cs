using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildData.Server.Data.CsvDataTypes
{
	public class CsvMonsterData
	{
		public int Id { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public int MoveSpeed { get; set; }
		public string ClientSprite { get; set; }
		public float ClientOffset { get; set; }
		public float ClientShadow { get; set; }
		public float ClientSize { get; set; }
	}
}
