using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Networking;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketEnterServer : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.EnterServer;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			connection.Character.IsActive = true;
			connection.Character.Map.SendAllEntitiesToPlayer(ref connection.Entity);

			connection.Character.Map.SendAddEntityAroundCharacter(ref connection.Entity, connection.Character);

			ServerLogger.Debug($"Player {connection.Entity} finished loading, spawning him on {connection.Character.Map.Name} at position {connection.Character.Position}.");
		}
	}
}
