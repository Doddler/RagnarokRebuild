using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketEnterServer : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.EnterServer;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character != null)
				return;

			var playerEntity = State.World.CreatePlayer(connection, "prontera", Area.CreateAroundPoint(new Position(155, 57), 5));
			connection.Entity = playerEntity;
			connection.LastKeepAlive = Time.ElapsedTime;
			connection.Character = playerEntity.Get<Character>();
			connection.Character.IsActive = false;
			var networkPlayer = playerEntity.Get<Player>();
			networkPlayer.Connection = connection;

			ServerLogger.Log($"Player assigned entity {playerEntity}, creating entity at location {connection.Character.Position}.");

			CommandBuilder.InformEnterServer(connection.Character, networkPlayer);
		}
	}
}
