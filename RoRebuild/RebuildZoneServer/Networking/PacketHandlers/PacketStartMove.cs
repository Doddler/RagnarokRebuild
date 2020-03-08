using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketStartMove : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.StartMove;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return; //we don't accept the keep-alive packet if they haven't entered the world yet

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Debug("Player click ignored due to cooldown.");
				return;
			}

			player.AddActionDelay(CooldownActionType.Click);

			var x = msg.ReadInt16();
			var y = msg.ReadInt16();

			var target = new Position(x, y);

			connection.Character.TryMove(ref connection.Entity, target, 0);
		}
	}
}
