using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using RebuildData.Server.Data.Monster;
using RebuildData.Server.Pathfinding;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	partial class Monster
	{
		#region InputStateChecks

		private bool InputStateCheck(MonsterInputCheck inCheckType)
		{
			switch (inCheckType)
			{
				case MonsterInputCheck.InWaitEnd: return InWaitEnd();
				case MonsterInputCheck.InEnemyOutOfSight: return InEnemyOutOfSight();
				case MonsterInputCheck.InEnemyOutOfAttackRange: return InEnemyOutOfAttackRange();
				case MonsterInputCheck.InReachedTarget: return InReachedTarget();
				case MonsterInputCheck.InTargetSearch: return InTargetSearch();
				case MonsterInputCheck.InAttackRange: return InAttackRange();
			}

			return false;
		}
		
		private bool InWaitEnd()
		{
			if (randomMoveCooldown <= 0 || monsterBase.MoveSpeed < 0)
				return true;

			return false;
		}

		private bool InReachedTarget()
		{
			if (character.State != CharacterState.Moving)
				return true;

			return false;
		}

		private bool InEnemyOutOfSight()
		{
			if (!ValidateTarget())
				return true;

			if (targetCharacter.Position.SquareDistance(character.Position) >= monsterBase.ChaseDist)
				return true;

			if (Pathfinder.GetPath(character.Map.WalkData, character.Position, targetCharacter.Position, null, 1) == 0)
				return true;

			return false;
		}

		private bool InAttackRange()
		{
			if (!ValidateTarget())
				return false;

			if (targetCharacter.Position.SquareDistance(character.Position) <= monsterBase.Range)
				return true;

			return false;
		}

		private bool InEnemyOutOfAttackRange()
		{
			if (!ValidateTarget())
				return false;

			var targetChar = targetCharacter;
			if (targetCharacter == null)
				return false;

			if (targetCharacter.Position.SquareDistance(character.Position) <= monsterBase.Range)
				return false;

			return true;
		}

		private bool InTargetSearch()
		{
			var list = EntityListPool.Get();

			target = EcsEntity.Null;

			character.Map.GatherPlayersInRange(character, monsterBase.ScanDist, list);

			if (list.Count == 0)
			{
				EntityListPool.Return(list);
				return false;
			}

			target = list.Count == 1 ? list[0] : list[GameRandom.Next(0, list.Count - 1)];

			EntityListPool.Return(list);

			return true;
		}

		#endregion

		#region OutputStateChecks

		private bool OutputStateCheck(MonsterOutputCheck outCheckType)
		{
			switch (outCheckType)
			{
				case MonsterOutputCheck.OutRandomMoveStart: return OutRandomMoveStart();
				case MonsterOutputCheck.OutWaitStart: return OutWaitStart();
				case MonsterOutputCheck.OutSearch: return OutSearch();
				case MonsterOutputCheck.OutStartChase: return OutStartChase();
				case MonsterOutputCheck.OutStartAttacking: return OutStartAttacking();
			}

			return false;
		}

		private bool OutWaitStart()
		{
			randomMoveCooldown = GameRandom.NextFloat(3f, 6f);

			return true;
		}

		private bool OutRandomMoveStart()
		{
			if (monsterBase.MoveSpeed < 0)
				return false;

			var moveArea = Area.CreateAroundPoint(character.Position, 9).ClipArea(character.Map.MapBounds);
			var newPos = Position.RandomPosition(moveArea);

			for (var i = 0; i < 20; i++)
			{
				if (character.TryMove(ref entity, newPos, 0))
					return true;
			}
			
			return false;
		}

		private bool OutSearch()
		{
			return true;
		}

		private bool OutStartChase()
		{
			var targetChar = targetCharacter;
			if (targetChar == null)
				return false;

			var distance = character.Position.SquareDistance(targetChar.Position);
			if (distance <= monsterBase.Range)
			{
				hasTarget = true;
				return true;
			}

			if (character.TryMove(ref entity, targetChar.Position, 1))
			{
				randomMoveCooldown = 0;
				hasTarget = true;
				return true;
			}

			return false;
		}

		private bool OutStartAttacking()
		{
			character.StopMovingImmediately();
			return true;
		}

		#endregion
	}
}
