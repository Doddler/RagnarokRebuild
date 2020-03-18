using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketStopAction : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.StopAction;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Debug("Player stop action ignored due to cooldown.");
				return;
			}
			player.AddActionDelay(CooldownActionType.StopAction);
			
			connection.Character.StopAction();

			connection.Player.ClearTarget();
		}
	}
}
