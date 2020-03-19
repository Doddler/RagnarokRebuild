using System;
using System.Collections.Generic;
using Leopotam.Ecs;
using RebuildData.Server.Data.Character;
using RebuildData.Server.Pathfinding;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	public class CombatEntity : IStandardEntity
	{
		public Character Character;

		public EcsEntity Entity;

		public BaseStats BaseStats; //base stats before being modified
		public CombatStats Stats; //modified stats

		[EcsIgnoreNullCheck]
		public List<DamageInfo> DamageQueue;


		public void Reset()
		{
			Character = null;
			Entity = EcsEntity.Null;
			BaseStats = null;
			Stats = null;
			
			if(DamageQueue == null)
				DamageQueue = new List<DamageInfo>(10);

			//bad! This will cause gen2 garbage
			BaseStats = null;
			Stats = null;
		}

		public void QueueDamage(DamageInfo info)
		{
			DamageQueue.Add(info);
			if (DamageQueue.Count > 1)
				DamageQueue.Sort((a, b) => a.Time.CompareTo(b.Time));
		}
		
		public void PerformMeleeAttack(CombatEntity target)
		{
			if (Character.AttackCooldown + Time.DeltaTimeFloat + 0.005f < Time.ElapsedTimeFloat)
				Character.AttackCooldown = Time.ElapsedTimeFloat + Stats.AttackMotionTime; //they are consecutively attacking
			else
				Character.AttackCooldown += Stats.AttackMotionTime;

			Character.FacingDirection = DistanceCache.Direction(Character.Position, target.Character.Position);

			var atk1 = Stats.Atk;
			var atk2 = Stats.Atk2;
			if (atk1 <= 0)
				atk1 = 1;
			if (atk2 < atk1)
				atk2 = atk1;

			var damage = (short)GameRandom.Next(Stats.Atk, Stats.Atk2);

			var di = new DamageInfo()
			{
				Damage = damage,
				HitCount = 1,
				KnockBack = 0,
				Source = Entity,
				Target = target.Entity,
				Time = Time.ElapsedTimeFloat + Stats.SpriteAttackTiming
			};

			//ServerLogger.Log($"{aiCooldown} {character.AttackCooldown} {angle} {dir}");

			Character.Map.GatherPlayersForMultiCast(ref Entity, Character);
			CommandBuilder.AttackMulti(Character, target.Character, di);
			CommandBuilder.ClearRecipients();
			
			target.QueueDamage(di);
			
			if (target.Character.Type == CharacterType.Monster && di.Damage > target.Stats.Hp)
			{
				var mon = target.Entity.Get<Monster>();
				mon.AddDelay(Stats.SpriteAttackTiming); //make sure it stops acting until it dies
			}

			if (Character.Type == CharacterType.Player && di.Damage > target.Stats.Hp)
			{
				var player = Entity.Get<Player>();
				player.ClearTarget();
				var bonus = (int)Math.Round(target.Stats.MaxHp * 0.02f);
				if (bonus < 1)
					bonus = 1;
				if (bonus > 5)
					bonus = 5 + (bonus - 5) / 2;
				if (bonus > 25)
					bonus = 25 + (bonus - 25) / 2;

				if ((int) Stats.Atk2 + bonus > short.MaxValue)
				{
					Stats.Atk = (short)(short.MaxValue * 0.8f);
					Stats.Atk2 = short.MaxValue;
				}
				else
				{
					Stats.Atk += (short)bonus;
					Stats.Atk2 += (short)(bonus * 1.2f);
				}
			}
		}

		public void Init(ref EcsEntity e, Character ch)
		{
			Entity = e;
			Character = ch;
			BaseStats = new BaseStats();
			Stats = new CombatStats();

			Stats.Range = 2;
		}

		private void AttackUpdate()
		{
			while (DamageQueue.Count > 0 && DamageQueue[0].Time < Time.ElapsedTimeFloat)
			{
				var di = DamageQueue[0];
				DamageQueue.RemoveAt(0);
				if (di.Target.IsNull() || !di.Target.IsAlive())
					continue;
				var enemy = di.Target.Get<Character>();
				if (enemy == null)
					continue;
				if (!enemy.IsActive || enemy.Map != Character.Map)
					continue;

				if (Character.AddMoveDelay(Stats.HitDelayTime))
				{
					Character.Map.GatherPlayersForMultiCast(ref Entity, Character);
					CommandBuilder.SendHitMulti(Character, Stats.HitDelayTime);
					CommandBuilder.ClearRecipients();
				}

				if(!di.Target.IsNull() && di.Source.IsAlive())
					Character.LastAttacked = di.Source;

				var ec = di.Target.Get<CombatEntity>();
				ec.Stats.Hp -= di.Damage;
				
				if (Character.Type == CharacterType.Monster && ec.Stats.Hp <= 0)
				{
					Character.ResetState();
					var monster = Entity.Get<Monster>();
					monster.Die();
					DamageQueue.Clear();
					return;
				}
			}
		}

		public void Update()
		{
			if (DamageQueue.Count > 0)
				AttackUpdate();
		}
	}
}
