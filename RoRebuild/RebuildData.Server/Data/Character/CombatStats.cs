using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildData.Server.Data.Character
{
	public class CombatStats
	{
		public int Hp, MaxHp, Sp, MaxSp;
		public short Str, Agi, Dex, Vit, Int, Luk;
		public short Atk, Atk2;

		public float MoveSpeed;
		public float AttackMotionTime, AttackDelayTime;
	}
}
