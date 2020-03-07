using System;
using Leopotam.Ecs;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	class Monster : IStandardEntity
	{
		private float aiTickRate;
		private float aiCooldown;
		
		private double idleEnd;

		private bool isMoving;
		private float moveDelay;
		
		private const float minIdleWaitTime = 3f;
		private const float maxIdleWaitTime = 6f;

		public void Reset()
		{
			aiTickRate = 0.1f;
			aiCooldown = GameRandom.NextFloat(0, aiTickRate);
			idleEnd = Time.ElapsedTime + GameRandom.NextDouble(minIdleWaitTime, maxIdleWaitTime);
		}

		public void Update(ref EcsEntity e, Character ch, CombatEntity ce)
		{
			aiCooldown -= Time.DeltaTimeFloat;
			moveDelay -= Time.DeltaTimeFloat;
			if (aiCooldown > 0)
				return;

			aiCooldown += aiTickRate;

			if (ch.State == CharacterState.Moving && !isMoving)
			{
				isMoving = true;
			}

			if (ch.State == CharacterState.Idle)
			{
				if (isMoving)
				{
					moveDelay = GameRandom.NextFloat(minIdleWaitTime, maxIdleWaitTime);
					isMoving = false;
					return;
				}

				if (moveDelay > 0 || ch.MoveSpeed <= 0)
				{
					return;
				}

				var moveArea = Area.CreateAroundPoint(ch.Position, 9).ClipArea(ch.Map.MapBounds);
				var newPos = Position.RandomPosition(moveArea);

				ch.TryMove(ref e, newPos);
				
				
				//ch.Map.MoveEntity(ref e, ch, newPos);

				//if (Time.ElapsedTime > idleEnd)
				//{
				//	var moveArea = Area.CreateAroundPoint(ch.Position, 16).ClipArea(ch.Map.MapBounds);
				//	var targetPos = Position.RandomPosition(moveArea);


				//}
			}
		}
	}
}
