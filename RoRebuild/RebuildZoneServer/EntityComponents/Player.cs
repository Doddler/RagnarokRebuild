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
		
		public NetworkConnection Connection;
		public float CurrentCooldown;
		public HeadFacing HeadFacing;
		public byte HeadId;
		public bool IsMale;

		public EcsEntity Target;

		public void Reset()
		{
			Entity = EcsEntity.Null;
			Target = EcsEntity.Null;
			Connection = null;
			CurrentCooldown = 0f;
			HeadId = 0;
			HeadFacing = HeadFacing.Center;
			IsMale = true;
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

		public void PerformAttack(Character chara, Character targetCharacter)
		{
			chara.StopMovingImmediately();
			chara.SpawnImmunity = -1;

			chara.FacingDirection = DistanceCache.Direction(chara.Position, targetCharacter.Position);

			chara.Map.GatherPlayersForMultiCast(ref Entity, chara);
			CommandBuilder.AttackMulti(chara, targetCharacter);
			CommandBuilder.ClearRecipients();
			targetCharacter.LastAttacked = chara.Entity;
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

			if (chara.Position.SquareDistance(targetCharacter.Position) <= 2)
			{
				PerformAttack(chara, targetCharacter);
			}
		}

		public void TargetForAttack(Character chara, Character enemy)
		{
			if (chara.Position.SquareDistance(enemy.Position) <= 2)
			{
				Target = enemy.Entity;
				var targetCharacter = Target.Get<Character>();

				PerformAttack(chara, targetCharacter);
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
			CurrentCooldown -= Time.DeltaTimeFloat;
			if (CurrentCooldown < 0)
				CurrentCooldown = 0;
		}
	}
}
