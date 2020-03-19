using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Server.Config;
using RebuildData.Server.Logging;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketAttack : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.Attack;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			var id = msg.ReadInt32();
			
			var target = State.World.GetEntityById(id);

			if (target.IsNull() || !target.IsAlive())
				return;

			var targetCharacter = target.Get<Character>();
			if (targetCharacter == null)
				return;

			if (targetCharacter.Map != connection.Character.Map)
				return;

			if (connection.Character.Position.SquareDistance(targetCharacter.Position) > ServerConfig.MaxViewDistance)
				return;

			connection.Player.TargetForAttack(targetCharacter);
		}
	}
}
