using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using Lidgren.Network;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildData.Shared.Networking;
using RebuildZoneServer.Config;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Networking.Enum;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.Networking
{
	public static class NetworkManager
	{
		private static NetServer server;
		private static SocketPolicyServer policyServer;
		private static NetPeerConfiguration config;

		private static Dictionary<NetConnection, NetworkConnection> connectionLookup = new Dictionary<NetConnection, NetworkConnection>(NetworkConfig.InitialConnectionCapacity);
		private static List<NetworkConnection> players = new List<NetworkConnection>();

		private static List<NetworkConnection> disconnectList = new List<NetworkConnection>(5);

		private static World world;

		public static void Init(World gameWorld)
		{
			world = gameWorld;

			const string allPolicy =
				@"<?xml version='1.0'?>
<cross-domain-policy>
        <allow-access-from domain=""*"" to-ports=""*"" />
</cross-domain-policy>";

			policyServer = new SocketPolicyServer(allPolicy);
			int ret = policyServer.Start();
			if (ret != 0)
				ServerLogger.Log("Failed to start policy server.");
			else
				ServerLogger.Log("Policy service started.");

			//Alright, now onto the regular server.
			config = new NetPeerConfiguration("RebuildZoneServer");
			config.Port = 14248;
			config.MaximumConnections = 100;
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

			server = new NetServer(config);
			server.Start();

			ServerLogger.Log("Server started.");
		}

		public static void ScanAndDisconnect()
		{
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
			while (server.ReadMessage(out var msg))
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

							var msgOut = server.CreateMessage(64);
							msgOut.Write((byte) 0x90);

							msg.SenderConnection.Approve(msgOut);

							connectionLookup.Add(playerConnection.ClientConnection, playerConnection);
							players.Add(playerConnection);
						}
							break;
						case NetIncomingMessageType.Data:
							HandleMessage(msg);
							break;
						case NetIncomingMessageType.StatusChanged:
							ServerLogger.Log("Client status changed: " + System.Enum.GetName(typeof(NetConnectionStatus), msg.SenderConnection.Status));
							break;
						case NetIncomingMessageType.WarningMessage:
						{
							Console.WriteLine($"[Network] Warning: {msg.ReadString()}");
						}
							break;
						default:
							Console.WriteLine(
								$"[Network] We encountered a packet type we didn't handle: {msg.MessageType}");
							break;
					}
				}
				catch (Exception e)
				{
					ServerLogger.LogWarning("Received invalid packet which generated an exception. Error: " +
					                        e.Message);
				}
				finally
				{
					server.Recycle(msg);
				}
			}
		}

		private static void IncomingMessageClientReady(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			connection.Character.IsActive = true;
			connection.Character.Map.SendAllEntitiesToPlayer(ref connection.Entity);

			connection.Character.Map.SendAddEntityAroundCharacter(ref connection.Entity, connection.Character);

			ServerLogger.Log($"Player {connection.Entity} finished loading, spawning him on {connection.Character.Map.Name} at position {connection.Character.Position}.");
		}

		private static void IncomingMessageEnterServer(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character != null)
				return;

			var playerEntity = world.CreatePlayer(connection, "prontera", Area.CreateAroundPoint(new Position(155, 57), 5));
			connection.Entity = playerEntity;
			connection.LastKeepAlive = Time.ElapsedTime;
			connection.Character = playerEntity.Get<Character>();
			connection.Character.IsActive = false;
			var networkPlayer = playerEntity.Get<Player>();
			networkPlayer.Connection = connection;

			ServerLogger.Log($"Player assigned entity {playerEntity}, creating entity at location {connection.Character.Position}.");

			CommandBuilder.InformEnterServer(connection.Character, networkPlayer);
		}


		private static void IncomingMessageEnterServerSpecificMap(NetIncomingMessage msg)
		{
#if  DEBUG
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character != null)
				return;
			


			var mapName = msg.ReadString();
			var hasPosition = msg.ReadBoolean();
			var area = Area.Zero;

			if (hasPosition)
			{
				var x = msg.ReadInt16();
				var y = msg.ReadInt16();

				var target = new Position(x, y);
				ServerLogger.Log($"Player chose to spawn at specific point: {x},{y}");

				area = Area.CreateAroundPoint(target, 0);
			}

			var playerEntity = world.CreatePlayer(connection, mapName, area);
			connection.Entity = playerEntity;
			connection.LastKeepAlive = Time.ElapsedTime;
			connection.Character = playerEntity.Get<Character>();
			connection.Character.IsActive = false;
			var networkPlayer = playerEntity.Get<Player>();
			networkPlayer.Connection = connection;


			ServerLogger.Log($"Player assigned entity {playerEntity}, creating entity at location {connection.Character.Position}.");

			CommandBuilder.InformEnterServer(connection.Character, networkPlayer);
#else
			IncomingMessageDisconnect(msg); //yeah no
#endif
		}

		private static void IncomingMessageDisconnect(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			DisconnectPlayer(connection);
		}

		private static void IncomingMessageStartMove(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return; //we don't accept the keep-alive packet if they haven't entered the world yet

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Log("Player click ignored due to cooldown.");
				return;
			}

			player.AddActionDelay(CooldownActionType.Click);

			var x = msg.ReadInt16();
			var y = msg.ReadInt16();

			var target = new Position(x, y);

			connection.Character.TryMove(ref connection.Entity, target);
		}

		private static void IncomingMessageLookTowards(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;
			
			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Log("Player look action ignored due to cooldown.");
				return;
			}
			player.AddActionDelay(CooldownActionType.FaceDirection);

			var dir = (Direction) msg.ReadByte();
			var head = (HeadFacing) msg.ReadByte();
			connection.Character.ChangeLookDirection(ref connection.Entity, dir, head);
		}

		private static void IncomingMessageRandomTeleport(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Log("Player stop action ignored due to cooldown.");
				return;
			}

			var ch = connection.Character;
			var map = ch.Map;

			var p = new Position();

			do
			{
				p = new Position(GameRandom.Next(0, map.Width-1), GameRandom.Next(0, map.Height-1));
			} while (!map.WalkData.IsCellWalkable(p));


			player.AddActionDelay(1.1f); //add 1s to the player's cooldown times. Should lock out immediate re-use.
			ch.ResetState();
			map.MoveEntity(ref connection.Entity, ch, p);
		}

		private static void IncomingMessageStopAction(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Log("Player stop action ignored due to cooldown.");
				return;
			}
			player.AddActionDelay(CooldownActionType.StopAction);

			connection.Character.StopAction();
		}

		private static void IncomingMessageSitStand(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null)
				return;

			var player = connection.Entity.Get<Player>();
			if (player.InActionCooldown())
			{
				ServerLogger.Log("Player sit/stand action ignored due to cooldown.");
				return;
			}
			player.AddActionDelay(CooldownActionType.SitStand);

			var isSitting = msg.ReadBoolean();
			connection.Character.SitStand(ref connection.Entity, isSitting);
		}
		
		private static void IncomingMessagePing(NetIncomingMessage msg)
		{
			if (!connectionLookup.TryGetValue(msg.SenderConnection, out var connection))
				return;

			if (connection.Character == null || !connection.Character.IsActive)
			{
				ServerLogger.Log("Ignored player ping packet as the player isn't alive in the world yet.");
				return; //we don't accept the keep-alive packet if they haven't entered the world yet
			}

			connection.LastKeepAlive = Time.ElapsedTime;
		}

		private static void DisconnectPlayer(NetworkConnection connection)
		{
			if (connection.Entity.IsAlive())
			{
				var player = connection.Entity.Get<Player>();
				var combatEntity = connection.Entity.Get<CombatEntity>();

				connection.Character.Map?.RemoveEntity(ref connection.Entity);
				connection.ClientConnection.Disconnect("Thanks for playing!");

				world.RemoveEntity(ref connection.Entity);
			}

			if(connectionLookup.ContainsKey(connection.ClientConnection))
				connectionLookup.Remove(connection.ClientConnection);

			if(players.Contains(connection))
				players.Remove(connection);
		}

		public static void HandleMessage(NetIncomingMessage msg)
		{
			var type = (PacketType) msg.ReadByte();
#if DEBUG
			if(connectionLookup.TryGetValue(msg.SenderConnection, out var connection) && connection.Entity.IsAlive())
				ServerLogger.Log($"Received message of type: {System.Enum.GetName(typeof(PacketType), type)} from entity {connection.Entity}.");
			else
				ServerLogger.Log($"Received message of type: {System.Enum.GetName(typeof(PacketType), type)} from entity-less connection.");
#endif
			switch (type)
			{
				case PacketType.PlayerReady:
					IncomingMessageClientReady(msg);
					break;
				case PacketType.EnterServer:
					IncomingMessageEnterServer(msg);
					break;
				case PacketType.EnterServerSpecificMap:
					IncomingMessageEnterServerSpecificMap(msg);
					break;
				case PacketType.Disconnect:
					IncomingMessageDisconnect(msg);
					break;
				case PacketType.StartMove:
					IncomingMessageStartMove(msg);
					break;
				case PacketType.LookTowards:
					IncomingMessageLookTowards(msg);
					break;
				case PacketType.SitStand:
					IncomingMessageSitStand(msg);
					break;
				case PacketType.Ping:
					IncomingMessagePing(msg);
					break;
				case PacketType.StopAction:
					IncomingMessageStopAction(msg);
					break;
				case PacketType.RandomTeleport:
					IncomingMessageRandomTeleport(msg);
					break;
				default:
					ServerLogger.LogWarning("Did not handle packet of type " + System.Enum.GetName(typeof(PacketType), type));
					break;
			}
		}

		public static void SendMessage(NetOutgoingMessage message, NetConnection connection,
			NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channel = 1)
		{
			server.SendMessage(message, connection, method, channel);
		}

		public static void SendMessageMulti(NetOutgoingMessage message, List<NetConnection> connections, 
			NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channel = 1)
		{
			server.SendMessage(message, connections, method, channel);
		}

		public static NetOutgoingMessage StartPacket(PacketType type, int capacity = 0)
		{
			NetOutgoingMessage msg;

			if (capacity == 0)
				msg = server.CreateMessage();
			else
				msg = server.CreateMessage(capacity);

			msg.Write((byte)type);

			return msg;
		}
	}
}
