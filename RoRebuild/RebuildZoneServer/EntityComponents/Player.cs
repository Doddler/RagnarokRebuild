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

		public EcsEntity Target { get; set; }

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
			if (Target.IsNull() || !Target.IsAlive())
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
			if (!ValidateTarget())
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
				TargetForAttack(targetCharacter);
				return;
			}

			PerformAttack(targetCharacter);
		}

		public void PerformAttack(Character targetCharacter)
		{
			if (targetCharacter.Type == CharacterType.NPC)
			{
				Target = EcsEntity.Null;
				return;
			}

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

		public void UpdatePosition(Position nextPos)
		{
			var connector = DataManager.GetConnector(Character.Map.Name, nextPos);

			if (connector != null)
			{
				Character.State = CharacterState.Idle;

				if (connector.Map == connector.Target)
					Character.Map.MoveEntity(ref Entity, Character, connector.DstArea.RandomInArea());
				else
					Character.Map.World.MovePlayerMap(ref Entity, Character, connector.Target, connector.DstArea.RandomInArea());

				CombatEntity.ClearDamageQueue();

				return;
			}

			if (!ValidateTarget())
				return;

			var targetCharacter = Target.Get<Character>();

			if (Character.State == CharacterState.Moving)
			{
				if (Character.Position.SquareDistance(targetCharacter.Position) <= CombatEntity.Stats.Range)
					PerformAttack(targetCharacter);
			}
			
			if(Character.State == CharacterState.Idle)
			{
				TargetForAttack(targetCharacter);
			}
		}

		public void TargetForAttack(Character enemy)
		{
			if (Character.Position.SquareDistance(enemy.Position) <= CombatEntity.Stats.Range)
			{
				Target = enemy.Entity;
				var targetCharacter = Target.Get<Character>();

				PerformAttack(targetCharacter);
				return;
			}

			if (!Character.TryMove(ref Entity, enemy.Position, 0))
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
