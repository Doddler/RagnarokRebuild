using System;
using Leopotam.Ecs;
using RebuildData.Server.Logging;
using RebuildData.Server.Pathfinding;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.Data.Management.Types;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	public class Player : IStandardEntity
	{
		public EcsEntity Entity;
		public Character Character;
		public CombatEntity CombatEntity;
		
		public NetworkConnection Connection;
		public float CurrentCooldown;
		public HeadFacing HeadFacing;
		public byte HeadId;
		public bool IsMale;

		public EcsEntity Target;

		public bool QueueAttack;

		public void Reset()
		{
			Entity = EcsEntity.Null;
			Target = EcsEntity.Null;
			Character = null;
			CombatEntity = null;
			Connection = null;
			CurrentCooldown = 0f;
			HeadId = 0;
			HeadFacing = HeadFacing.Center;
			IsMale = true;
			QueueAttack = false;
		}

		public void Init()
		{
			UpdateStats();
		}

		private void UpdateStats()
		{
			var s = CombatEntity.Stats;

			s.AttackMotionTime = 0.9f;
			s.HitDelayTime = 0.4f;
			s.SpriteAttackTiming = 0.6f;
			s.Range = 2;
			s.Atk = 10;
			s.Atk2 = 12;
		}

		private bool ValidateTarget()
		{
			if (Target.IsNull())
				return false;
			if (!Target.IsAlive())
			{
				Target = EcsEntity.Null;
				return false;
			}

			return true;
		}

		public void ClearTarget()
		{
			QueueAttack = false;
			Target = EcsEntity.Null;
			
		}

		public void PerformQueuedAttack()
		{
			//QueueAttack = false;
			if (Target.IsNull() || !Target.IsAlive())
			{
				QueueAttack = false;
				return;
			}

			var targetCharacter = Target.Get<Character>();
			if (!targetCharacter.IsActive)
			{
				QueueAttack = false;
				return;
			}

			if (targetCharacter.Map != Character.Map)
			{
				QueueAttack = false;
				return;
			}

			if (Character.Position.SquareDistance(targetCharacter.Position) > CombatEntity.Stats.Range)
			{
				QueueAttack = false;
				return;
			}

			PerformAttack(targetCharacter);
		}

		public void PerformAttack(Character targetCharacter)
		{
			Character.StopMovingImmediately();

			if (Character.AttackCooldown > Time.ElapsedTimeFloat)
			{
				QueueAttack = true;
				Target = targetCharacter.Entity;
				return;
			}

			Character.SpawnImmunity = -1;

			var targetEntity = targetCharacter.Entity.Get<CombatEntity>();
			CombatEntity.PerformMeleeAttack(targetEntity);

			QueueAttack = true;

			Character.AttackCooldown = Time.ElapsedTimeFloat + CombatEntity.Stats.AttackMotionTime;
		}

		public void UpdatePosition(Character chara, Position nextPos)
		{
			var connector = DataManager.GetConnector(chara.Map.Name, nextPos);

			if (connector != null)
			{
				chara.State = CharacterState.Idle;

				if (connector.Map == connector.Target)
					chara.Map.MoveEntity(ref Entity, chara, connector.DstArea.RandomInArea());
				else
					chara.Map.World.MovePlayerMap(ref Entity, chara, connector.Target, connector.DstArea.RandomInArea());

				return;
			}

			if (!ValidateTarget())
				return;

			var targetCharacter = Target.Get<Character>();

			if (chara.Position.SquareDistance(targetCharacter.Position) <= CombatEntity.Stats.Range)
			{
				PerformAttack(targetCharacter);
			}
		}

		public void TargetForAttack(Character chara, Character enemy)
		{
			if (chara.Position.SquareDistance(enemy.Position) <= CombatEntity.Stats.Range)
			{
				Target = enemy.Entity;
				var targetCharacter = Target.Get<Character>();

				PerformAttack(targetCharacter);
				return;
			}

			if (!chara.TryMove(ref Entity, enemy.Position, 0))
				return;

			Target = enemy.Entity;
		}

		public bool InActionCooldown() => CurrentCooldown > 1f;
		public void AddActionDelay(CooldownActionType type) => CurrentCooldown += ActionDelay.CooldownTime(type);
		public void AddActionDelay(float time) => CurrentCooldown += CurrentCooldown;
		
		public void Update()
		{
			if (QueueAttack)
			{
				if(Character.AttackCooldown < Time.ElapsedTimeFloat)
					PerformQueuedAttack();
			}

			CurrentCooldown -= Time.DeltaTimeFloat;
			if (CurrentCooldown < 0)
				CurrentCooldown = 0;
		}
	}
}
