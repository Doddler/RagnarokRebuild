using RebuildData.Server.Data.Monster;

namespace RebuildData.Server.Data.Types
{
	public class MonsterDatabaseInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public int ScanDist { get; set; }
		public int ChaseDist { get; set; }
		public float RechargeTime { get; set; }
		public float AttackTime { get; set; }
		public float HitTime { get; set; }
		public int Range { get; set; }
		public MonsterAiType AiType { get; set; }
		public float MoveSpeed { get; set; }
	}
}
