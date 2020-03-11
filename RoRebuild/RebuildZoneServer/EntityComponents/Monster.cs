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
			var ch = targetCharacter;
			if (ch == null)
				return false;
			if (ch.Map != character.Map)
				return false;
			if (ch.SpawnImmunity > 0)
				return false;
			return true;
		}

		private bool FindRandomTargetInRange(int distance, out EcsEntity newTarget)
		{
			var list = EntityListPool.Get();
			
			character.Map.GatherPlayersInRange(character, distance, list, true);

			if (list.Count == 0)
			{
				EntityListPool.Return(list);
				newTarget = EcsEntity.Null;
				return false;
			}

			newTarget = list.Count == 1 ? list[0] : list[GameRandom.Next(0, list.Count - 1)];

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

			character.LastAttacked = EcsEntity.Null;

			if(aiCooldown < 0)
				aiCooldown += 0.1f;

			if (character.Map.PlayerCount == 0)
				aiCooldown += 2f;
		}

		public void Update(ref EcsEntity e, Character ch, CombatEntity ce)
		{
			aiCooldown -= Time.DeltaTimeFloat;
			
			randomMoveCooldown -= Time.DeltaTimeFloat;
			if (aiCooldown > 0)
				return;

			if (aiCooldown <= 0)
				aiCooldown += aiTickRate;

			if (character == null)
				character = ch;

			if (combatEntity == null)
				ce = combatEntity;

			AiStateMachineUpdate();
		}
	}
}

