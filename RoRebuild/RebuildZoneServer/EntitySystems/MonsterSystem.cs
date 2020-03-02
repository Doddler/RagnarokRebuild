using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using RebuildData.Shared.Data;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntitySystems
{
	class MonsterSystem : IEcsRunSystem
	{
		private EcsWorld world = null;
		private EcsFilter<Monster, Character, CombatEntity> monsterFilter = null;
		public void Run()
		{
			foreach (var mId in monsterFilter)
			{
				var m = monsterFilter.Get1[mId];
				var ch = monsterFilter.Get2[mId];
				var ce = monsterFilter.Get3[mId];

				m.Update(ref monsterFilter.Entities[mId], ch, ce);

				//m.UpdateTime -= Time.DeltaTimeFloat;
				//if (m.UpdateTime < 0f)
				//{
				//	m.UpdateTime += 5f;

				//	var newPos = new Position() { X = GameRandom.Next(0, 255), Y = GameRandom.Next(0, 255) };

				//	ch.Map.MoveEntity(ref e, ref ch, newPos);
				//}
			}
		}
	}
}
