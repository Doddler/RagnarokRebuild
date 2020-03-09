using System;
using System.Collections.Generic;
using Leopotam.Ecs;
using RebuildData.Server.Data.Monster;
using RebuildData.Server.Data.Types;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	partial class Monster : IStandardEntity
	{
		private EcsEntity entity;
		private Character character;
		private CombatEntity combatEntity;

		private float aiTickRate;
		private float aiCooldown;

		private float randomMoveCooldown;

		private const float minIdleWaitTime = 3f;
		private const float maxIdleWaitTime = 6f;

		private bool hasTarget;
		private EcsEntity target;
		private Character targetCharacter => target.GetIfAlive<Character>();

		private MonsterAiType aiType;
		private MonsterDatabaseInfo monsterBase;
		private MapSpawnEntry spawnEntry;
		private List<MonsterAiEntry> aiEntries;

		private MonsterAiState currentState;

		private Character searchTarget;

		public void Reset()
		{
			entity = EcsEntity.Null;
			character = null;
			aiEntries = null;
			spawnEntry = null;
			combatEntity = null;
			searchTarget = null;
			aiTickRate = 0.1f;
			aiCooldown = GameRandom.NextFloat(0, aiTickRate);
			target = EcsEntity.Null;
		}

		public void Initialize(ref EcsEntity e, Character character, CombatEntity combat, MonsterDatabaseInfo monData, MonsterAiType type, MapSpawnEntry spawnEntry)
		{
			entity = e;
			this.character = character;
			this.spawnEntry = spawnEntry;
			combatEntity = combat;
			monsterBase = monData;
			aiType = type;
			aiEntries = DataManager.GetAiStateMachine(aiType);

			currentState = MonsterAiState.StateIdle;
		}

		private bool ValidateTarget()
		{
			if (target.IsNull() || !target.IsAlive())
				return false;
			return true;
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
				randomMoveCooldown = 0;
				return true;
			}

			return false;
		}

		public bool ScanForTarget(ref EcsEntity e)
		{
			var list = EntityListPool.Get();

			target = EcsEntity.Null;

			character.Map.GatherPlayersInRange(character, 9, list);

			if (list.Count == 0)
			{
				EntityListPool.Return(list);
				return false;
			}

			target = list[0];
			hasTarget = true;

			EntityListPool.Return(list);
			
			return true;
		}

		public void AiStateMachineUpdate()
		{
			for (var i = 0; i < aiEntries.Count; i++)
			{
				var entry = aiEntries[i];

				if (entry.InputState != currentState)
					continue;

				if (!InputStateCheck(entry.InputCheck))
					continue;

				if (!OutputStateCheck(entry.OutputCheck))
					continue;

				//ServerLogger.Log($"Monster from {entry.InputState} to state {entry.OutputState} (via {entry.InputCheck}/{entry.OutputCheck})");

				currentState = entry.OutputState;
			}

			aiCooldown += 0.1f;
		}

		public void Update(ref EcsEntity e, Character ch, CombatEntity ce)
		{
			aiCooldown -= Time.DeltaTimeFloat;
			
			randomMoveCooldown -= Time.DeltaTimeFloat;
			if (aiCooldown > 0 && !hasTarget)
				return;

			if (aiCooldown <= 0)
				aiCooldown += aiTickRate;

			if (character == null)
				character = ch;

			if (combatEntity == null)
				ce = combatEntity;

			AiStateMachineUpdate();


			//if (ch.State == CharacterState.Moving && !isMoving)
			//{
			//	isMoving = true;
			//}

			//if (ch.State == CharacterState.Idle)
			//{
			//	if (isMoving)
			//	{
			//		moveDelay = GameRandom.NextFloat(minIdleWaitTime, maxIdleWaitTime);
			//		isMoving = false;
			//	}

			//	if (!hasTarget)
			//	{
			//		if (ScanForTarget(ref e))
			//			return;
			//	}

			//	if (hasTarget)
			//	{
			//		if (ChaseTargetIfPossible(ref e))
			//		{
			//			return;
			//		}
			//		else
			//		{
			//			target = EcsEntity.Null;
			//			hasTarget = false;
			//			aiCooldown += aiTickRate;
			//			return;
			//		}
			//	}

			//	if (moveDelay > 0 || ch.MoveSpeed <= 0)
			//	{
			//		return;
			//	}

			//	var moveArea = Area.CreateAroundPoint(ch.Position, 9).ClipArea(ch.Map.MapBounds);
			//	var newPos = Position.RandomPosition(moveArea);

			//	ch.TryMove(ref e, newPos, 0);


			//ch.Map.MoveEntity(ref e, ch, newPos);

			//if (Time.ElapsedTime > idleEnd)
			//{
			//	var moveArea = Area.CreateAroundPoint(ch.Position, 16).ClipArea(ch.Map.MapBounds);
			//	var targetPos = Position.RandomPosition(moveArea);


			//}
		}
	}
}

