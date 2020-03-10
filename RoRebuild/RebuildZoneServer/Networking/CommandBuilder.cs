using System.Collections.Generic;
using Leopotam.Ecs;
using Lidgren.Network;
using RebuildData.Shared.Enum;
using RebuildData.Shared.Networking;
using RebuildZoneServer.EntityComponents;

namespace RebuildZoneServer.Networking
{
	public static class CommandBuilder
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
			if (c.TotalMoveSteps > 0)
			{
				packet.Write(c.WalkPath[0]);

				var i = 1;

				//pack directions into 2 steps per byte
				while (i < c.TotalMoveSteps)
				{
					var b = (byte)((byte)(c.WalkPath[i] - c.WalkPath[i - 1]).GetDirectionForOffset() << 4);
					i++;
					if (i < c.TotalMoveSteps)
						b |= (byte)(c.WalkPath[i] - c.WalkPath[i - 1]).GetDirectionForOffset();
					i++;
					packet.Write(b);
				}
			}
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
				packet.Write(player.HeadId);
				packet.Write(player.IsMale);
			}
			if (c.State == CharacterState.Moving)
			{
				WriteMoveData(c, packet);
			}

			return packet;
		}

		public static void AttackMulti(Character attacker, Character target)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.Attack, 48);

			packet.Write(attacker.Id);
			packet.Write(target.Id);
			packet.Write((byte)attacker.FacingDirection);

			NetworkManager.SendMessageMulti(packet, recipients);
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
				packet.Write((byte)player.HeadFacing);
			}

			NetworkManager.SendMessageMulti(packet, recipients);
		}


		public static void CharacterStopImmediateMulti(Character c)
		{
			if (recipients.Count <= 0)
				return;

			var packet = NetworkManager.StartPacket(PacketType.StopImmediate, 32);

			packet.Write(c.Id);
			packet.Write(c.Position);

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
