using System;
using System.Collections.Generic;
using System.Text;
using Leopotam.Ecs;
using Lidgren.Network;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;
using RebuildZoneServer.Networking.Enum;

namespace RebuildZoneServer.Networking
{
	static class CommandBuilder
	{
		private static List<NetConnection> recipients = new List<NetConnection>(10);
		
		public static void AddRecipient(EcsEntity e)
		{
			if (!e.IsAlive())
				return;

			var player = e.Get<Player>();
			recipients.Add(player.Connection.ClientConnection);
		}

		public static void ClearRecipients()
		{
			recipients.Clear();
		}

		public static bool HasRecipients()
		{
			return recipients.Count > 0;
		}

		private static void WriteMoveData(Character c, NetOutgoingMessage packet)
		{
			packet.Write(c.MoveSpeed);
			packet.Write(c.MoveCooldown);
			packet.Write((byte)c.TotalMoveSteps);
			packet.Write((byte)c.MoveStep);
			for (var i = 0; i < c.TotalMoveSteps; i++)
				packet.Write(c.WalkPath[i]);
		}

		private static NetOutgoingMessage BuildCreateEntity(Character c, bool isSelf = false)
		{
			var type = isSelf ? PacketType.EnterServer : PacketType.CreateEntity;
			var packet = NetworkManager.StartPacket(type, 256);

			packet.Write(c.Id);
			packet.Write((byte)c.Type);
			packet.Write((short)c.ClassId);
			packet.Write(c.Position);
			packet.Write((byte)c.FacingDirection);
			packet.Write((byte)c.State);
			if (c.Type == CharacterType.Player)
			{
				var player = c.Entity.Get<Player>();
				packet.Write((byte)player.HeadFacing);
				packet.Write((byte)player.HeadId);
				packet.Write(player.IsMale);
			}
			if (c.State == CharacterState.Moving)
			{
				WriteMoveData(c, packet);
			}

			return packet;
		}

		public static void ChangeSittingMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.SitStand, 48);

			packet.Write(c.Id);
			packet.Write(c.State == CharacterState.Sitting);

			NetworkManager.SendMessageMulti(packet, recipients);
		}

		public static void ChangeFacingMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.LookTowards, 48);

			packet.Write(c.Id);
			packet.Write((byte)c.FacingDirection);
			if (c.Type == CharacterType.Player)
			{
				var player = c.Entity.Get<Player>();
				packet.Write((byte) player.HeadFacing);
			}

			NetworkManager.SendMessageMulti(packet, recipients);
		}


		public static void CharacterStopMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.StopAction, 32);

			packet.Write(c.Id);
			
			NetworkManager.SendMessageMulti(packet, recipients);
		}

		public static void SendMoveEntityMulti(Character c)
		{
			var packet = NetworkManager.StartPacket(PacketType.Move, 48);

			packet.Write(c.Id);
			packet.Write(c.Position);

			NetworkManager.SendMessageMulti(packet, recipients);
		}

		public static void SendStartMoveEntityMulti(Character c)
		{
			var packet = NetworkManager.StartPacket(PacketType.StartMove, 256);

			packet.Write(c.Id);
			WriteMoveData(c, packet);

			NetworkManager.SendMessageMulti(packet, recipients);
		}

		public static void InformEnterServer(Character c, Player p)
		{
			var packet = BuildCreateEntity(c, true);
			packet = NetworkManager.StartPacket(PacketType.EnterServer, 32);
			packet.Write(c.Id);
			packet.Write(c.Map.Name);
			NetworkManager.SendMessage(packet, p.Connection.ClientConnection);
		}

		public static void SendCreateEntityMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = BuildCreateEntity(c);
			NetworkManager.SendMessageMulti(packet, recipients);
		}

		public static void SendCreateEntity(Character c, Player player)
		{
			var packet = BuildCreateEntity(c);
			if (packet == null)
				return;

			NetworkManager.SendMessage(packet, player.Connection.ClientConnection);
		}
		
		public static void SendRemoveEntityMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.RemoveEntity, 32);
			packet.Write(c.Id);

			NetworkManager.SendMessageMulti(packet, recipients);
		}
		
		public static void SendRemoveEntity(Character c, Player player)
		{
			var packet = NetworkManager.StartPacket(PacketType.RemoveEntity, 32);
			packet.Write(c.Id);

			NetworkManager.SendMessage(packet, player.Connection.ClientConnection);
		}

		public static void SendRemoveAllEntities(Player player)
		{
			var packet = NetworkManager.StartPacket(PacketType.RemoveAllEntities, 8);

			NetworkManager.SendMessage(packet, player.Connection.ClientConnection);
		}

		public static void SendChangeMap(Character c, Player player)
		{
			var packet = NetworkManager.StartPacket(PacketType.ChangeMaps, 128);

			packet.Write(c.Map.Name);
			//packet.Write(c.Position);

			NetworkManager.SendMessage(packet, player.Connection.ClientConnection);
		}
	}
}
