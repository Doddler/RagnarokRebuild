using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
			if (nextMoveUpdate <= Time.ElapsedTimeFloat || monsterBase.MoveSpeed < 0)
				return true;

			return false;
		}

		private bool InReachedTarget()
		{
			if (Character.State != CharacterState.Moving)
				return true;

			return false;
		}

		private bool InEnemyOutOfSight()
		{
			if (!ValidateTarget())
				return true;

			if (targetCharacter.Position == Character.Position)
				return false;

			if (targetCharacter.Position.SquareDistance(Character.Position) > monsterBase.ChaseDist)
				return true;

			if (Character.MoveSpeed < 0 && InEnemyOutOfAttackRange())
				return true;

			if (Pathfinder.GetPath(Character.Map.WalkData, Character.Position, targetCharacter.Position, null, 1) == 0)
				return true;

			return false;
		}

		private bool InAttackRange()
		{
			if (Character.Map.PlayerCount == 0)
				return false;

			if (!ValidateTarget())
				target = EcsEntity.Null;

			if (target == EcsEntity.Null)
			{
				if (!FindRandomTargetInRange(monsterBase.Range, out var newTarget))
					return false;

				target = newTarget;
			}

			if (targetCharacter.Position.SquareDistance(Character.Position) <= monsterBase.Range)
				return true;

			return false;
		}

		private bool InAttackDelayEnd()
		{
			if (!ValidateTarget())
				return false;

			if (Character.AttackCooldown > Time.ElapsedTimeFloat)
				return false;

			return true;
		}

		private bool InAttacked()
		{
			if (Character.LastAttacked.IsNull())
				return false;
			if (!Character.LastAttacked.IsAlive())
				return false;

			target = Character.LastAttacked;

			return true;
		}

		private bool InEnemyOutOfAttackRange()
		{
			if (!ValidateTarget())
				return false;

			var targetChar = targetCharacter;
			if (targetCharacter == null)
				return false;

			if (targetCharacter.Position.SquareDistance(Character.Position) > monsterBase.Range)
				return true;

			return false;
		}

		private bool InTargetSearch()
		{
			if (Character.Map.PlayerCount == 0)
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
			nextMoveUpdate = Time.ElapsedTimeFloat + GameRandom.NextFloat(3f, 6f);

			return true;
		}

		private bool OutWaitForever()
		{
			nextAiUpdate += 10000f;
			return true;
		}

		private bool OutRandomMoveStart()
		{
			if (monsterBase.MoveSpeed < 0)
				return false;

			var moveArea = Area.CreateAroundPoint(Character.Position, 9).ClipArea(Character.Map.MapBounds);
			var newPos = Position.RandomPosition(moveArea);

			for (var i = 0; i < 20; i++)
			{
				if (Character.TryMove(ref Entity, newPos, 0))
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

			var distance = Character.Position.SquareDistance(targetChar.Position);
			if (distance <= monsterBase.Range)
			{
				hasTarget = true;
				return true;
			}

			if (Character.TryMove(ref Entity, targetChar.Position, 1))
			{

				nextMoveUpdate = 0;
				hasTarget = true;
				return true;
			}

			return false;
		}

		private bool OutPerformAttack()
		{
			nextAiUpdate += monsterBase.AttackTime;
			
			var targetEntity = targetCharacter.Entity.Get<CombatEntity>();
			CombatEntity.PerformMeleeAttack(targetEntity);

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
			Character.StopMovingImmediately();
			nextAiUpdate = Time.ElapsedTimeFloat;
			return true;
		}

		#endregion
	}
}
