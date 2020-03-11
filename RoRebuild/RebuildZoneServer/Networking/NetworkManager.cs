using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Networking;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.Networking
{
	public static class NetworkManager
	{
		public static ServerState State;

		public static void Init(World gameWorld)
		{
			State = new ServerState();
			State.World = gameWorld;

			//policy server is required for web build, but since webGL doesn't support lidgren, it's disabled
			//StartPolicyServer();

			if(!DataManager.TryGetConfigInt("Port", out var port))
				throw new Exception("Configuration does not have value for port!");
			if(!DataManager.TryGetConfigInt("MaxConnections", out var maxConnections))
				throw new Exception("Configuration does not have value for max connections!");

			if (DataManager.TryGetConfigInt("Debug", out var debug))
				State.DebugMode = debug == 1;

#if DEBUG
			State.DebugMode = true;
#else
			ServerLogger.LogWarning("Server is started using debug mode config flag! Be sure this is what you want.");
#endif

			ServerLogger.Log($"Starting server listening on port {port}, with a maximum of {maxConnections} connections.");

			//Alright, now onto the regular server.
			State.Config = new NetPeerConfiguration("RebuildZoneServer");
			State.Config.Port = port;
			State.Config.MaximumConnections = maxConnections;
			State.Config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			
			State.Server = new NetServer(State.Config);
			State.Server.Start();
			
			var handlerCount = System.Enum.GetNames(typeof(PacketType)).Length;
			State.PacketHandlers = new Action<NetIncomingMessage>[handlerCount];

			foreach (var type in Assembly.GetAssembly(typeof(NetworkManager)).GetTypes()
				.Where(t => t.IsClass && t.IsSubclassOf(typeof(ClientPacketHandler))))
			{
				var handler = (ClientPacketHandler) Activator.CreateInstance(type);
				var packetType = handler.PacketType;
				handler.State = State;

				if(State.PacketHandlers[(int)packetType] != null)
					throw new Exception($"Duplicate packet handler exists for type {packetType}!");

				State.PacketHandlers[(int) packetType] = handler.HandlePacket;
			}

			for (var i = 0; i < handlerCount; i++)
			{
				if (State.PacketHandlers[i] == null)
				{

					ServerLogger.Debug($"No packet handler for packet type PacketType.{(PacketType) i} exists.");
					State.PacketHandlers[i] = State.PacketHandlers[(int) PacketType.UnhandledPacket];
				}
			}

			ServerLogger.Log("Server started.");
		}

		public static void StartPolicyServer()
		{
			//policy server is required for web build to connect
			const string allPolicy =
				@"<?xml version='1.0'?>
<cross-domain-policy>
        <allow-access-from domain=""*"" to-ports=""*"" />
</cross-domain-policy>";

			State.PolicyServer = new SocketPolicyServer(allPolicy);
			int ret = State.PolicyServer.Start();
			if (ret != 0)
				ServerLogger.Log("Failed to start policy server.");
			else
				ServerLogger.Log("Policy service started.");
		}

		public static void ScanAndDisconnect()
		{
			var players = State.Players;
			var disconnectList = State.DisconnectList;

			for (var i = 0; i < players.Count; i++)
			{
				if(players[i].ClientConnection.Status == NetConnectionStatus.Disconnected 
				   || players[i].ClientConnection.Status == NetConnectionStatus.Disconnecting)
					disconnectList.Add(players[i]);
				else
				{
					if (players[i].Character == null)
					{
						if(players[i].LastKeepAlive + 20 < Time.ElapsedTime)
							disconnectList.Add(players[i]);
					}
					else
					{
						if(players[i].Character.IsActive && players[i].LastKeepAlive + 20 < Time.ElapsedTime)
							disconnectList.Add(players[i]);
						if (!players[i].Character.IsActive && players[i].LastKeepAlive + 120 < Time.ElapsedTime)
							disconnectList.Add(players[i]);
					}
				}
			}

			for (var i = 0; i < disconnectList.Count; i++)
			{
				var player = disconnectList[i];
				ServerLogger.Log($"[Network] Player {player.Entity} has disconnected, removing from world.");
				DisconnectPlayer(player);
			}

			disconnectList.Clear();
		}

		public static void ProcessIncomingMessages()
		{
			while (State.Server.ReadMessage(out var msg))
			{
				try
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.ConnectionApproval:
						{
							ServerLogger.Log("[Network] Incoming connection request: " + msg.SenderConnection);
							var playerConnection = new NetworkConnection(msg.SenderConnection);
							playerConnection.LastKeepAlive = Time.ElapsedTime + 20; //you get an extra 20 seconds on first load before we kick you

							var msgOut = State.Server.CreateMessage(64);
							msgOut.Write((byte) 0x90);

							msg.SenderConnection.Approve(msgOut);

							State.ConnectionLookup.Add(playerConnection.ClientConnection, playerConnection);
							State.Players.Add(playerConnection);
						}
							break;
						case NetIncomingMessageType.Data:
							HandleMessage(msg);
							break;
						case NetIncomingMessageType.StatusChanged:
							ServerLogger.Log("Client status changed: " + System.Enum.GetName(typeof(NetConnectionStatus), msg.SenderConnection.Status));
							break;
						case NetIncomingMessageType.DebugMessage:
							ServerLogger.Log($"[Network] DebugMessage: {msg.ReadString()}");
							break;
						case NetIncomingMessageType.WarningMessage:
							ServerLogger.LogWarning($"[Network] Warning: {msg.ReadString()}");
							break;
						default:
							Console.WriteLine(
								$"[Network] We encountered a packet type we didn't handle: {msg.MessageType}");
							break;
					}
				}
#if !DEBUG
				catch (Exception e)
				{
					ServerLogger.LogWarning("Received invalid packet which generated an exception. Error: " +
					                        e.Message);

				}
#endif
				finally
				{
					State.Server.Recycle(msg);
				}
			}
		}
		
		
		public static void DisconnectPlayer(NetworkConnection connection)
		{
			if (connection.Entity.IsAlive())
			{
				var player = connection.Entity.Get<Player>();
				var combatEntity = connection.Entity.Get<CombatEntity>();

				connection.Character.Map?.RemoveEntity(ref connection.Entity);
				connection.ClientConnection.Disconnect("Thanks for playing!");

				State.World.RemoveEntity(ref connection.Entity);
			}

			if(State.ConnectionLookup.ContainsKey(connection.ClientConnection))
				State.ConnectionLookup.Remove(connection.ClientConnection);

			if(State.Players.Contains(connection))
				State.Players.Remove(connection);
		}

		public static void HandleMessage(NetIncomingMessage msg)
		{
			var type = (PacketType) msg.ReadByte();
#if DEBUG
			if(State.ConnectionLookup.TryGetValue(msg.SenderConnection, out var connection) && connection.Entity.IsAlive())
				ServerLogger.Debug($"Received message of type: {System.Enum.GetName(typeof(PacketType), type)} from entity {connection.Entity}.");
			else
				ServerLogger.Debug($"Received message of type: {System.Enum.GetName(typeof(PacketType), type)} from entity-less connection.");

			State.LastPacketType = type;
			State.PacketHandlers[(int)type](msg);
#endif
#if !DEBUG
			try
			{
				State.LastPacketType = type;
				State.PacketHandlers[(int) type](msg);
			}
			catch (Exception)
			{
				ServerLogger.LogError($"Error executing packet handler for packet type {type}");
				throw;
			}
#endif
		}

		public static void SendMessage(NetOutgoingMessage message, NetConnection connection,
			NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channel = 1)
		{
			State.Server.SendMessage(message, connection, method, channel);
		}

		public static void SendMessageMulti(NetOutgoingMessage message, List<NetConnection> connections, 
			NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channel = 1)
		{
			State.Server.SendMessage(message, connections, method, channel);
		}

		public static NetOutgoingMessage StartPacket(PacketType type, int capacity = 0)
		{
			NetOutgoingMessage msg;

			if (capacity == 0)
				msg = State.Server.CreateMessage();
			else
				msg = State.Server.CreateMessage(capacity);

			msg.Write((byte)type);

			return msg;
		}
	}
}
