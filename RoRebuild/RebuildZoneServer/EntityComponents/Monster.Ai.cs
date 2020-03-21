﻿using System;
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
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	public partial class Monster
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
				case MonsterInputCheck.InDeadTimeoutEnd: return InDeadTimeoutEnd();
			}

			return false;
		}
		
		private bool InWaitEnd()
		{
			if (nextMoveUpdate <= Time.ElapsedTimeFloat || MonsterBase.MoveSpeed < 0)
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

			if (targetCharacter.Position.SquareDistance(Character.Position) > MonsterBase.ChaseDist)
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

			//do we have a target? If we do and it's not valid, remove it.
			if (!ValidateTarget())
				target = EcsEntity.Null;

			//if we have a character still, check if we're in range.
			if (target != EcsEntity.Null)
				return targetCharacter.Position.InRange(Character.Position, MonsterBase.Range);
			
			//if we don't have a target, check if we can acquire one quickly
			if (!FindRandomTargetInRange(MonsterBase.Range, out var newTarget))
				return false;

			//we have a character. Check if it's in range, and if so, assign it as our current target
			var targetChar = newTarget.Get<Character>();
			if (targetChar.Position.SquareDistance(Character.Position) <= MonsterBase.Range)
			{
				target = newTarget;
				return true;
			}

			//failed. Return false.
			return false;
		}

		private bool InAttackDelayEnd()
		{
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

			if (targetCharacter.Position.SquareDistance(Character.Position) > MonsterBase.Range)
				return true;

			return false;
		}

		private bool InTargetSearch()
		{
			if (Character.Map.PlayerCount == 0)
				return false;

			if (!FindRandomTargetInRange(MonsterBase.ScanDist, out var newTarget))
				return false;

			target = newTarget;
			return true;
		}

		private bool InDeadTimeoutEnd()
		{
			if (deadTimeout > Time.ElapsedTimeFloat)
			{
				nextAiUpdate = deadTimeout + 0.01f;
				return false;
			}

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
				case MonsterOutputCheck.OutChangeTargets: return OutChangeTargets();
				case MonsterOutputCheck.OutTryAttacking: return OutTryAttacking();
				case MonsterOutputCheck.OutStartAttacking: return OutStartAttacking();
				case MonsterOutputCheck.OutPerformAttack: return OutPerformAttack();
				case MonsterOutputCheck.OutTryRevival: return OutTryRevival();
			}

			return false;
		}

		private bool OutWaitStart()
		{
			target = EcsEntity.Null;
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
			if (MonsterBase.MoveSpeed < 0)
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
			if (distance <= MonsterBase.Range)
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

		private bool OutChangeTargets()
		{
			if (Character.LastAttacked.IsNull() || !Character.LastAttacked.IsAlive())
				return false;

			var targetChar = Character.LastAttacked.Get<Character>();
			if (!targetChar.IsActive)
				return false;

			var distance = Character.Position.SquareDistance(targetChar.Position);
			if (distance <= MonsterBase.Range)
			{
				hasTarget = true;
				target = Character.LastAttacked;
				return true;
			}

			if (Character.TryMove(ref Entity, targetChar.Position, 1))
			{

				nextMoveUpdate = 0;
				target = Character.LastAttacked;
				hasTarget = true;
				return true;
			}

			return false;
		}

		private bool OutPerformAttack()
		{
			var targetEntity = targetCharacter.Entity.Get<CombatEntity>();
			if (!targetEntity.IsValidTarget(CombatEntity))
				return false;

			CombatEntity.PerformMeleeAttack(targetEntity);

			nextAiUpdate += MonsterBase.AttackTime;

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

		private bool OutTryRevival()
		{
			return World.Instance.RespawnMonster(this);
		}

		#endregion
	}
}
