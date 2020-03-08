using RebuildData.Server.Data.Monster;

namespace RebuildData.Server.Data.Types
{
	public class MonsterDatabaseInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public MonsterAiType AiType { get; set; }
		public float MoveSpeed { get; set; }
	}
}
