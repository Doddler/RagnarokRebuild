using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Shared.Networking;

namespace RebuildZoneServer.Networking
{
	public abstract class ClientPacketHandler
	{
		public ServerState State;
		public virtual PacketType PacketType => throw new Exception("Packet type not specified on client packet handler.");

		public virtual void HandlePacket(NetIncomingMessage msg)
		{
			throw new NotImplementedException(); //must be overridden
		}
	}
}
