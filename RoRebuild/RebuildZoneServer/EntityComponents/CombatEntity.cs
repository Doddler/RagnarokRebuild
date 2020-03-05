using Leopotam.Ecs;
using RebuildData.Server.Data.Character;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	class CombatEntity : IStandardEntity
	{
		public BaseStats BaseStats; //base stats before being modified
		public CombatStats Stats; //modified stats

		public float AttackCooldown;

		public void Reset()
		{
		}
	}
}
