using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using RebuildData.Shared.Networking;
using RebuildZoneServer.Config;
using RebuildZoneServer.Sim;

namespace RebuildZoneServer.Networking
{
	public class ServerState
	{
		public NetServer Server;
		public SocketPolicyServer PolicyServer;
		public NetPeerConfiguration Config;

		public Dictionary<NetConnection, NetworkConnection> ConnectionLookup = new Dictionary<NetConnection, NetworkConnection>(NetworkConfig.InitialConnectionCapacity);
		public List<NetworkConnection> Players = new List<NetworkConnection>();

		public List<NetworkConnection> DisconnectList = new List<NetworkConnection>(5);

		public Action<NetIncomingMessage>[] PacketHandlers;

		public World World;

		public PacketType LastPacketType;
	}
}
