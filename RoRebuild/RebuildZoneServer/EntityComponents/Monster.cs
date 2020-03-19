using System;
using System.Collections.Generic;
using Leopotam.Ecs;
using RebuildData.Server.Data.Monster;
using RebuildData.Server.Data.Types;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	public partial class Monster : IStandardEntity
	{
		public EcsEntity Entity;
		public Character Character;
		public CombatEntity CombatEntity;

		private float aiTickRate;
		//private float aiCooldown;

		private float nextAiUpdate;
		private float nextMoveUpdate;

		//private float randomMoveCooldown;

		private const float minIdleWaitTime = 3f;
		private const float maxIdleWaitTime = 6f;

		private bool hasTarget;
		private EcsEntity target;
		private Character targetCharacter => target.GetIfAlive<Character>();

		public MonsterDatabaseInfo MonsterBase;
		public MapSpawnEntry SpawnEntry;
		public string SpawnMap;
		private MonsterAiType aiType;
		private List<MonsterAiEntry> aiEntries;

		private MonsterAiState currentState;

		private Character searchTarget;

		private float deadTimeout;

		public void Reset()
		{
			Entity = EcsEntity.Null;
			Character = null;
			aiEntries = null;
			SpawnEntry = null;
			CombatEntity = null;
			searchTarget = null;
			aiTickRate = 0.1f;
			nextAiUpdate = Time.ElapsedTimeFloat + GameRandom.NextFloat(0, aiTickRate);
			
			target = EcsEntity.Null;
		}

		public void Initialize(ref EcsEntity e, Character character, CombatEntity combat, MonsterDatabaseInfo monData, MonsterAiType type, MapSpawnEntry spawnEntry, string mapName)
		{
			Entity = e;
			Character = character;
			this.SpawnEntry = spawnEntry;
			CombatEntity = combat;
			MonsterBase = monData;
			aiType = type;
			SpawnMap = mapName;
			aiEntries = DataManager.GetAiStateMachine(aiType);
			nextAiUpdate = Time.ElapsedTimeFloat + 1f;
			
			UpdateStats();

			currentState = MonsterAiState.StateIdle;
		}

		private void UpdateStats()
		{
			var s = CombatEntity.Stats;

			s.Hp = MonsterBase.HP;
			s.MaxHp = MonsterBase.HP;
			s.Atk = (short)MonsterBase.AtkMin;
			s.Atk2 = (short)MonsterBase.AtkMax;
			s.MoveSpeed = MonsterBase.MoveSpeed;
			s.Range = MonsterBase.Range;
			s.SpriteAttackTiming = MonsterBase.SpriteAttackTiming;
			s.HitDelayTime = MonsterBase.HitTime;
			s.AttackMotionTime = MonsterBase.RechargeTime;
		}

		private bool ValidateTarget()
		{
			if (target.IsNull() || !target.IsAlive())
				return false;
			var ch = targetCharacter;
			if (ch == null)
				return false;
			if (ch.Map != Character.Map)
				return false;
			if (ch.SpawnImmunity > 0)
				return false;
			return true;
		}

		public void Die()
		{
			currentState = MonsterAiState.StateDead;

			Character.IsActive = false;
			Character.Map.RemoveEntity(ref Entity, CharacterRemovalReason.Dead);

			if(SpawnEntry == null)
				World.Instance.RemoveEntity(ref Entity);
			else
			{
				var min = SpawnEntry.SpawnTime;
				var max = SpawnEntry.SpawnTime + SpawnEntry.SpawnVariance;
				deadTimeout = GameRandom.NextFloat(min / 1000f, max / 1000f);
				if (deadTimeout < 0.4f)
					deadTimeout = 0.4f; //minimum respawn time
				nextAiUpdate = Time.ElapsedTimeFloat + deadTimeout + 0.1f;
				deadTimeout += Time.ElapsedTimeFloat;
			}
		}

		public void ResetDelay()
		{
			nextAiUpdate = 0f;
		}

		public void AddDelay(float delay)
		{
			//usually to stop a monster from acting after taking fatal damage, but before the damage is applied
			nextAiUpdate = Time.ElapsedTimeFloat + delay;
		}

		private bool FindRandomTargetInRange(int distance, out EcsEntity newTarget)
		{
			var list = EntityListPool.Get();
			
			Character.Map.GatherPlayersInRange(Character, distance, list, true);

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

			Character.LastAttacked = EcsEntity.Null;

			if(nextAiUpdate < Time.ElapsedTimeFloat)
				nextAiUpdate += aiTickRate;

			if (Character.Map.PlayerCount == 0)
				nextAiUpdate += 2f + GameRandom.NextFloat(0f, 1f);
		}

		public void Update()
		{
			if (nextAiUpdate > Time.ElapsedTimeFloat)
				return;

			AiStateMachineUpdate();
		}
	}
}

