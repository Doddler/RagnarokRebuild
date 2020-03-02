using Leopotam.Ecs;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{


	class Player : IStandardEntity
	{
		public EcsEntity Entity;
		
		public NetworkConnection Connection;
		public float CurrentCooldown;
		public HeadFacing HeadFacing;
		public byte HeadId;
		public bool IsMale;

		public void Reset()
		{
			Entity = EcsEntity.Null;
			Connection = null;
			CurrentCooldown = 0f;
			HeadId = 0;
			HeadFacing = HeadFacing.Center;
			IsMale = true;
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
