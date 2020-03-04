using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Shared.Networking;

namespace RebuildZoneServer.Networking.PacketHandlers
{
	class PacketDisconnect : ClientPacketHandler
	{
		public override PacketType PacketType => PacketType.Disconnect;

		public override void HandlePacket(NetIncomingMessage msg)
		{
			if (!State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			NetworkManager.DisconnectPlayer(connection);
		}
	}
}
