using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildData.Server.Data.Monster
{
	public enum MonsterAiType : byte
	{
		AiEmpty,
		AiPassive,
		AiPassiveImmobile,
		AiAggressive,
		AiAggressiveImmobile,
		AiLooter,
		AiAssist,
		AiAssistLooter,
		AiAggressiveLooter,
		AiAngry
	}

	public enum MonsterAiState : byte
	{
		StateIdle,
		StateRandomMove,
		StateChase,
		StateAbnormal,
		StateSearch,
		StateAttacking,
		StateDead
	}

	public enum MonsterInputCheck : byte
	{
		InWaitEnd,
		InAttacked,
		InReachedTarget,
		InAttackRange,
		InChangeNormal,
		InTargetSearch,
		InEnemyOutOfSight,
		InEnemyOutOfRange,
		InAttackDelayEnd,
		InDeadTimeoutEnd
	}

	public enum MonsterOutputCheck : byte
	{
		OutRandomMoveStart,
		OutWaitStart,
		OutStartChase,
		OutStartAttacking,
		OutSearch,
		OutChangeNormal,
		OutPerformAttack,
		OutChangeTargets,
		OutTryRevival
	}
}
