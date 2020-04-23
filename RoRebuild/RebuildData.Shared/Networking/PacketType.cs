namespace RebuildData.Shared.Networking
{
	public enum PacketType : byte
	{
		PlayerReady,
		EnterServer,
		EnterServerSpecificMap,
		Ping,
		CreateEntity,
		StartMove,
		Move,
		Attack,
		LookTowards,
		SitStand,
		RemoveEntity,
		RemoveAllEntities,
		Disconnect,
		ChangeMaps,
		StopAction,
		StopImmediate,
		RandomTeleport,
		UnhandledPacket,
		HitTarget,
		Skill,
	}
}
