using System;
using Leopotam.Ecs;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	class Monster : IStandardEntity
	{
		private Character character;

		private float aiTickRate;
		private float aiCooldown;
		
		private double idleEnd;

		private bool isMoving;
		private float moveDelay;
		
		private const float minIdleWaitTime = 3f;
		private const float maxIdleWaitTime = 6f;

		private bool hasTarget;
		private EcsEntity target;
		private Character targetCharacter => target.GetIfAlive<Character>();
		
		public void Reset()
		{
			character = null;
			aiTickRate = 0.1f;
			aiCooldown = GameRandom.NextFloat(0, aiTickRate);
			idleEnd = Time.ElapsedTime + GameRandom.NextDouble(minIdleWaitTime, maxIdleWaitTime);
			target = EcsEntity.Null;
		}

		public bool ChaseTargetIfPossible(ref EcsEntity e)
		{
			var target = targetCharacter;
			if (target == null)
				return false;

			var distance = character.Position.SquareDistance(target.Position);

			if (distance <= 1)
				return false;

			if (distance > 14)
				return false;

			var targetChar = targetCharacter;
			if (targetChar == null)
				return false;

			if (character.TryMove(ref e, targetChar.Position, 1))
			{
				moveDelay = 0;
				return true;
			}

			return false;
		}

		public bool ScanForTarget(ref EcsEntity e)
		{
			var list = EntityListPool.Get();

			character.Map.GatherPlayersInRange(character, 9, list);

			if (list.Count == 0)
			{
				EntityListPool.Return(list);
				return false;
			}

			target = list[0];
			hasTarget = true;

			EntityListPool.Return(list);

			var targetChar = targetCharacter;
			if (targetChar == null)
				return false;

			var distance = character.Position.SquareDistance(targetChar.Position);
			if (distance <= 1)
				return false;

			if (character.TryMove(ref e, targetChar.Position, 1))
			{
				moveDelay = 0;
				return true;
			}

			return false;
		}

		public void Update(ref EcsEntity e, Character ch, CombatEntity ce)
		{
			aiCooldown -= Time.DeltaTimeFloat;
			moveDelay -= Time.DeltaTimeFloat;
			if (aiCooldown > 0 && !hasTarget)
				return;

			if(aiCooldown <= 0)
				aiCooldown += aiTickRate;

			if (character == null)
				character = ch;
			
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
				}

				if (!hasTarget)
				{
					if (ScanForTarget(ref e))
						return;
				}

				if (hasTarget)
				{
					if (ChaseTargetIfPossible(ref e))
					{
						return;
					}
					else
					{
						target = EcsEntity.Null;
						hasTarget = false;
						aiCooldown += aiTickRate;
						return;
					}
				}

				if (moveDelay > 0 || ch.MoveSpeed <= 0)
				{
					return;
				}

				var moveArea = Area.CreateAroundPoint(ch.Position, 9).ClipArea(ch.Map.MapBounds);
				var newPos = Position.RandomPosition(moveArea);

				ch.TryMove(ref e, newPos, 0);
				
				
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
