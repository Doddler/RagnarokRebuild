using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using RebuildData.Shared.Data;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Sim;

namespace RebuildZoneServer.EntitySystems
{
	class PlayerSystem : IEcsRunSystem
	{
		private World gameWorld = null;
		private EcsWorld world = null;
		private EcsFilter<Character, Player> playerFilter = null;

		public void Run()
		{
			foreach (var pId in playerFilter)
			{
				var c = playerFilter.Get1[pId];
				var p = playerFilter.Get2[pId];

				if (!c.IsActive)
					continue;

				p.Update();

				//if(c.Map.Name == "prontera" && c.Position.Y <= 25)
				//	gameWorld.MovePlayerMap(ref p.Entity, c, "prt_fild08", new Position(169 + GameRandom.Next(-1, 1), 371 + GameRandom.Next(-1, 1)));

				//if (c.Map.Name == "prt_fild08" && c.Position.Y > 375 && c.Position.X <= 171 && c.Position.X >= 168)
				//	gameWorld.MovePlayerMap(ref p.Entity, c, "prontera", new Position(156 + GameRandom.Next(-1, 1), 29 + GameRandom.Next(-1, 1)));
			}
		}
	}
}
