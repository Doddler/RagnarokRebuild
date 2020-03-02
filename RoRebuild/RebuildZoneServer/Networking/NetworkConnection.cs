using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using Lidgren.Network;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Networking.Enum;

namespace RebuildZoneServer.Networking
{
	public class NetworkConnection
	{
		public NetConnection ClientConnection;
		public ConnectionStatus Status;
		public EcsEntity Entity;
		public Character Character;
		public double LastKeepAlive;
		
		public NetworkConnection(NetConnection connection)
		{
			ClientConnection = connection;
		}
	}
}
