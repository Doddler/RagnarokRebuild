using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using RebuildData.Server.Data.Monster;
using RebuildData.Server.Logging;
using RebuildData.Server.Pathfinding;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Networking;
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
				case MonsterInputCheck.InAttackDelayEnd: return InAttackDelayEnd();
				case MonsterInputCheck.InAttacked: return InAttacked();
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

			if (targetCharacter.Position == character.Position)
				return false;

			if (targetCharacter.Position.SquareDistance(character.Position) > monsterBase.ChaseDist)
				return true;

			if (character.MoveSpeed < 0 && InEnemyOutOfAttackRange())
				return true;

			if (Pathfinder.GetPath(character.Map.WalkData, character.Position, targetCharacter.Position, null, 1) == 0)
				return true;

			return false;
		}

		private bool InAttackRange()
		{
			if (character.Map.PlayerCount == 0)
				return false;

			if (!ValidateTarget())
				target = EcsEntity.Null;

			if (target == EcsEntity.Null)
			{
				if (!FindRandomTargetInRange(monsterBase.Range, out var newTarget))
					return false;

				target = newTarget;
			}

			if (targetCharacter.Position.SquareDistance(character.Position) <= monsterBase.Range)
				return true;

			return false;
		}

		private bool InAttackDelayEnd()
		{
			if (!ValidateTarget())
				return false;

			if (character.AttackCooldown > 0)
				return false;

			return true;
		}

		private bool InAttacked()
		{
			if (character.LastAttacked.IsNull())
				return false;
			if (!character.LastAttacked.IsAlive())
				return false;

			target = character.LastAttacked;

			return true;
		}

		private bool InEnemyOutOfAttackRange()
		{
			if (!ValidateTarget())
				return false;

			var targetChar = targetCharacter;
			if (targetCharacter == null)
				return false;

			if (targetCharacter.Position.SquareDistance(character.Position) > monsterBase.Range)
				return true;

			return false;
		}

		private bool InTargetSearch()
		{
			if (character.Map.PlayerCount == 0)
				return false;

			if (!FindRandomTargetInRange(monsterBase.ScanDist, out var newTarget))
				return false;

			target = newTarget;
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
				case MonsterOutputCheck.OutTryAttacking: return OutTryAttacking();
				case MonsterOutputCheck.OutStartAttacking: return OutStartAttacking();
				case MonsterOutputCheck.OutPerformAttack: return OutPerformAttack();
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

		private bool OutPerformAttack()
		{
			aiCooldown += monsterBase.AttackTime;
			character.AttackCooldown += monsterBase.RechargeTime;
			
			character.FacingDirection = DistanceCache.Direction(character.Position, targetCharacter.Position);

			//ServerLogger.Log($"{aiCooldown} {character.AttackCooldown} {angle} {dir}");

			character.Map.GatherPlayersForMultiCast(ref entity, character);
			CommandBuilder.AttackMulti(character, targetCharacter);
			CommandBuilder.ClearRecipients();

			return true;
		}

		private bool OutTryAttacking()
		{
			if (!InAttackRange())
				return false;

			return OutStartAttacking();
		}

		private bool OutStartAttacking()
		{
			character.StopMovingImmediately();
			if(character.AttackCooldown < 0)
				character.AttackCooldown = 0;
			aiCooldown = 0.001f;
			return true;
		}

		#endregion
	}
}
