using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.MapEditor;
using Assets.Scripts.Sprites;
using Assets.Scripts.Utility;
using Lidgren.Network;
using RebuildData.Shared.ClientTypes;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildData.Shared.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Network
{
	class NetworkManager : MonoBehaviour
	{
		public static NetworkManager Instance;

		public CameraFollower CameraFollower;
		public GameObject DamagePrefab;
		public Dictionary<int, ServerControllable> entityList = new Dictionary<int, ServerControllable>();
		public int PlayerId;

		private static NetClient client;

		private float lastPing = 0;
		private bool isConnected = false;
		private bool isLoggedIn = false;

		public Color FakeAmbient = Color.white;

		private string currentMap = "";

		private List<Vector2Int> pathData = new List<Vector2Int>(20);

		private Scene currentScene;
		

#if DEBUG
		public static string SpawnMap = "";
#endif

		private void Start()
		{
			Debug.Log("Starting Network Manager");

			Instance = this;

			NetPeerConfiguration config = new NetPeerConfiguration("RebuildZoneServer");

#if DEBUG
			UnloadOldScenes();

			//config.SimulatedMinimumLatency = 0.1f;
			//config.SimulatedLoss = 0.05f;
#endif

			LeanTween.init(4000);

			client = new NetClient(config);
			client.Start();

			NetOutgoingMessage outMsg = client.CreateMessage();
			outMsg.Write("A Client");

			var target = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "serverconfig.txt"));
			var s = target.Split(':');

			client.Connect(s[0], int.Parse(s[1]), outMsg);

			Debug.Log($"Connecting to server at target {target}...");

			lastPing = Time.time;
		}

		private void UnloadOldScenes()
		{
			var sceneCount = SceneManager.sceneCount;
			for (var i = 0; i < sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.name != "MainScene")
					SceneManager.UnloadSceneAsync(scene);
			}
			
			CameraFollower.Target = null;

			entityList.Clear();
		}

		private Vector2Int ReadPosition(NetIncomingMessage msg)
		{
			var x = msg.ReadInt16();
			var y = msg.ReadInt16();
			return new Vector2Int(x, y);
		}

		private void LoadMoveData(NetIncomingMessage msg, ServerControllable ctrl)
		{
			var moveSpeed = msg.ReadFloat();
			var moveCooldown = msg.ReadFloat();
			var totalSteps = (int)msg.ReadByte();
			var curStep = (int) msg.ReadByte();

			pathData.Clear();
			if (totalSteps > 0) //should always be true but whatever
			{
				pathData.Add(ReadPosition(msg));
				var i = 1;
				while (i < totalSteps)
				{
					var b = msg.ReadByte();
					pathData.Add(pathData[i - 1].AddDirection((Direction)(b >> 4)));
					i++;
					if (i < totalSteps)
					{
						pathData.Add(pathData[i - 1].AddDirection((Direction) (b & 0xF)));
						i++;
					}
				}
			}
			//for (var i = 0; i < totalSteps; i++)
			//	pathData.Add(ReadPosition(msg));

			//if(ctrl.Id == PlayerId)
			//	Debug.Log("Doing move for player!");

			ctrl.StartMove(moveSpeed, moveCooldown, totalSteps, curStep, pathData);
		}

		private ServerControllable SpawnEntity(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();
			var type = (CharacterType)msg.ReadByte();
			var classId = msg.ReadInt16();
			var pos = ReadPosition(msg);
			var facing = (Direction)msg.ReadByte();
			var state = (CharacterState)msg.ReadByte();
			
			ServerControllable controllable;
			if (type == CharacterType.Player)
			{
				var headFacing = (HeadFacing)msg.ReadByte();
				var headId = msg.ReadByte();
				var isMale = msg.ReadBoolean();

				var playerData = new PlayerSpawnParameters()
				{
					ServerId = id,
					ClassId = classId,
					Facing = facing,
					Position = pos,
					State = state,
					HeadFacing = headFacing,
					HeadId = headId,
					IsMale = isMale
				};

				controllable = SpriteDataLoader.Instance.InstantiatePlayer(ref playerData);
			}
			else
			{
				var monData = new MonsterSpawnParameters()
				{
					ServerId = id,
					ClassId = classId,
					Facing = facing,
					Position = pos,
					State = state
				};
				controllable = SpriteDataLoader.Instance.InstantiateMonster(ref monData);
			}
			
			entityList.Add(id, controllable);

			if (controllable.SpriteMode == ClientSpriteType.Prefab)
				return controllable;

			if (state == CharacterState.Moving)
				LoadMoveData(msg, controllable);
			if (state == CharacterState.Sitting)
			{
				controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Sit);
				controllable.SpriteAnimator.State = SpriteState.Sit;
			}

			if (PlayerId == controllable.Id)
			{
				CameraFollower.Target = controllable.gameObject;
				//Debug.Log($"Player entity sent, we're at position {pos}");

				SceneTransitioner.Instance.FadeIn();
				CameraFollower.Instance.SnapLookAt();
			}

			return controllable;
		}


		private void OnMessageChangeSitStand(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();
			var isSitting = msg.ReadBoolean();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to move entity " + id + ", but it does not exist in scene!");
				return;
			}

			if (isSitting)
			{
				controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Sit);
				controllable.SpriteAnimator.State = SpriteState.Sit;
				return;
			}

			if (controllable.SpriteAnimator.State == SpriteState.Sit)
			{
				controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Idle);
				controllable.SpriteAnimator.State = SpriteState.Idle;
			}
		}

		private void OnMessageChangeFacing(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();
			var facing = (Direction)msg.ReadByte();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to move entity " + id + ", but it does not exist in scene!");
				return;
			}

			controllable.SpriteAnimator.Direction = facing;
			if (controllable.SpriteAnimator.Type == SpriteType.Player)
				controllable.SpriteAnimator.SetHeadFacing((HeadFacing)msg.ReadByte());
		}

		private void OnMessageCreateEntity(NetIncomingMessage msg)
		{
			SpawnEntity(msg);
		}

		private void OnMessageMove(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to move entity " + id + ", but it does not exist in scene!");
				return;
			}

			controllable.MovePosition(ReadPosition(msg));
		}

		private void OnMessageStartMove(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to move entity " + id + ", but it does not exist in scene!");
				return;
			}
			
			LoadMoveData(msg, controllable);
		}

		private void OnMessageChangeMaps(NetIncomingMessage msg)
		{
			var mapName = msg.ReadString();

			entityList.Clear();
			
			currentMap = mapName;
			//var mapLoad = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
			//mapLoad.completed += OnMapLoad;

			SceneTransitioner.Instance.DoTransitionToScene(currentScene, currentMap, OnMapLoad);
			
			//SceneManager.UnloadSceneAsync(currentScene);
		}

		private void OnMessageEnterServer(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();
			var mapName = msg.ReadString();

			Debug.Log($"We're id {id} on map {mapName}");

			currentMap = mapName;

			//var mapLoad = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
			//mapLoad.completed += OnMapLoad;

			PlayerId = id;

			SceneTransitioner.Instance.LoadScene(currentMap, OnMapLoad);
		}

		private void OnMessageRemoveEntity(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();
			var reason = (CharacterRemovalReason) msg.ReadByte();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to remove entity " + id + ", but it does not exist in scene!");
				return;
			}

			entityList.Remove(id);

			if (id == PlayerId)
			{
				Debug.LogWarning("Whoa! Trying to delete player object. Is that right...?");
				CameraFollower.Instance.Target = null;
			}

			if (reason == CharacterRemovalReason.Dead)
			{
                if (controllable.SpriteAnimator.Type != SpriteType.Player)
                {
					controllable.MonsterDie(1);
					//               var vDir = -controllable.CounterHitDir;
					//Debug.Log(vDir);
					//var newDir = new Vector3(vDir.x, 0, vDir.z).normalized * 8f;
					//               newDir.y = 8f;
					//controllable.BlastOff(newDir * 5f);
				}
                else
					controllable.FadeOutAndVanish(0.1f);
			}
			else
				controllable.FadeOutAndVanish(0.1f);
			//GameObject.Destroy(controllable.gameObject);
		}

		private void OnMessageRemoveAllEntities(NetIncomingMessage msg)
		{
			foreach (var entity in entityList)
			{
				GameObject.Destroy(entity.Value.gameObject);
			}

			entityList.Clear();
		}


		private void OnMessageStopImmediate(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to stop entity " + id + ", but it does not exist in scene!");
				return;
			}

			var pos = ReadPosition(msg);

			controllable.StopImmediate(pos);
		}

		private void OnMessageStopPlayer(NetIncomingMessage msg)
		{
			var id = msg.ReadInt32();

			if (!entityList.TryGetValue(id, out var controllable))
			{
				Debug.LogWarning("Trying to stop entity " + id + ", but it does not exist in scene!");
				return;
			}

			controllable.StopWalking();
		}

		private void OnMessageAttack(NetIncomingMessage msg)
		{
			var id1 = msg.ReadInt32();
			var id2 = msg.ReadInt32();

			if (!entityList.TryGetValue(id1, out var controllable))
			{
				Debug.LogWarning("Trying to attack entity " + id1 + ", but it does not exist in scene!");
				return;
			}

			var hasTarget = entityList.TryGetValue(id2, out var controllable2);
			
			var dir = (Direction)msg.ReadByte();
			var pos = ReadPosition(msg);
			var dmg = msg.ReadInt16();

            if (hasTarget)
            {
                var cd = controllable.transform.localPosition - controllable2.transform.localPosition;
                cd.y = 0;
                controllable2.CounterHitDir = cd.normalized;
				//Debug.Log("Counter hit: " + cd);
            }
            else
            {
                var v = dir.GetVectorValue();
                controllable.CounterHitDir = new Vector3(v.x, 0, v.y);
            }

            controllable.StopImmediate(pos);
			controllable.SpriteAnimator.Direction = dir;
			controllable.SpriteAnimator.State = SpriteState.Idle;
			controllable.SpriteAnimator.AnimSpeed = 1f;
			if (controllable.SpriteAnimator.Type == SpriteType.Player)
			{
				if(controllable.IsMale)
					controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Attack2, true);
				else
					controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Attack3, true);
			}
			else
				controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Attack1, true);

			//controllable2.SpriteAnimator.ChangeMotion(SpriteMotion.Hit);

			if (hasTarget && controllable.SpriteAnimator.IsInitialized)
			{
				if (controllable.SpriteAnimator.SpriteData == null)
				{
					throw new Exception("AAA? " + controllable.gameObject.name + " " + controllable.gameObject);
				}

				var damageTiming = controllable.SpriteAnimator.SpriteData.AttackFrameTime / 1000f;
				if (controllable.SpriteAnimator.Type == SpriteType.Player)
					damageTiming = 0.5f;

				StartCoroutine(DamageEvent(dmg, damageTiming, 1, controllable2));
			}
		}

		private IEnumerator DamageEvent(int damage, float delay, int hitCount, ServerControllable target)
		{
			yield return new WaitForSeconds(delay);
			if (target != null && target.SpriteAnimator.IsInitialized)
			{
                for (var i = 0; i < hitCount; i++)
                {
                    var go = GameObject.Instantiate(DamagePrefab, target.transform.localPosition, Quaternion.identity);
                    var di = go.GetComponent<DamageIndicator>();
                    var red = target.SpriteAnimator.Type == SpriteType.Player;
                    var height = target.SpriteAnimator.SpriteData.Size / 50f;
                    di.DoDamage(damage, target.gameObject.transform.localPosition, height,
                        target.SpriteAnimator.Direction, red, false);
					yield return new WaitForSeconds(0.2f);
                }
            }
		}

		private void OnMessageHit(NetIncomingMessage msg)
		{
			var id1 = msg.ReadInt32();
			var delay = msg.ReadFloat();

			if (!entityList.TryGetValue(id1, out var controllable))
			{
				//Debug.LogWarning("Trying to do hit entity " + id1 + ", but it does not exist in scene!");
				return;
			}

			//Debug.Log("Move delay is " + delay);
			controllable.SetHitDelay(delay);

			if (controllable.SpriteAnimator.Type == SpriteType.Player)
				controllable.SpriteAnimator.State = SpriteState.Standby;

			if(controllable.SpriteAnimator.CurrentMotion != SpriteMotion.Dead && !controllable.SpriteAnimator.IsAttackMotion)
				controllable.SpriteAnimator.ChangeMotion(SpriteMotion.Hit);
		}

		void HandleDataPacket(NetIncomingMessage msg)
		{
			var type = (PacketType)msg.ReadByte();

			//Debug.Log(type);

			switch (type)
			{
				case PacketType.StartMove:
					OnMessageStartMove(msg);
					break;
				case PacketType.RemoveAllEntities:
					OnMessageRemoveAllEntities(msg);
					break;
				case PacketType.RemoveEntity:
					OnMessageRemoveEntity(msg);
					break;
				case PacketType.CreateEntity:
					OnMessageCreateEntity(msg);
					break;
				case PacketType.EnterServer:
					OnMessageEnterServer(msg);
					break;
				case PacketType.LookTowards:
					OnMessageChangeFacing(msg);
					break;
				case PacketType.SitStand:
					OnMessageChangeSitStand(msg);
					break;
				case PacketType.ChangeMaps:
					OnMessageChangeMaps(msg);
					break;
				case PacketType.StopAction:
					OnMessageStopPlayer(msg);
					break;
				case PacketType.StopImmediate:
					OnMessageStopImmediate(msg);
					break;
				case PacketType.Move:
					OnMessageMove(msg);
					break;
				case PacketType.Attack:
					OnMessageAttack(msg);
					break;
				case PacketType.HitTarget:
					OnMessageHit(msg);
					break;
				default:
					Debug.LogWarning($"Failed to handle packet type: {type}");
					break;
			}
		}

		private void DoPacketHandling()
		{
			while (client.ReadMessage(out var msg))
			{
				switch (msg.MessageType)
				{
					case NetIncomingMessageType.Data:
						HandleDataPacket(msg);
						break;
					case NetIncomingMessageType.DebugMessage:
					case NetIncomingMessageType.VerboseDebugMessage:
					case NetIncomingMessageType.WarningMessage:
					case NetIncomingMessageType.ErrorMessage:
						Debug.Log(msg.MessageType + ": " + msg.ReadString());
						break;
					case NetIncomingMessageType.StatusChanged:
						Debug.Log("Status changed: " + client.Status);
						break;
					default:
						Debug.Log("We received a packet type we didn't handle: " + msg.MessageType);
						break;
				}
			}
		}
		
		private void OnMapLoad()
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.PlayerReady);

			currentScene = SceneManager.GetSceneByName(currentMap);
			SceneManager.SetActiveScene(currentScene);
			
			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}
		
		public void ChangePlayerSitStand(bool isChangingToSitting)
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.SitStand);
			msg.Write(isChangingToSitting);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		public void ChangePlayerFacing(Direction direction, HeadFacing headFacing)
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.LookTowards);
			msg.Write((byte)direction);
			msg.Write((byte)headFacing);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		public void RandomTeleport()
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.RandomTeleport);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		public void MovePlayer(Vector2Int position)
		{
			var msg = client.CreateMessage();
			//Debug.Log(position);
			msg.Write((byte)PacketType.StartMove);
			msg.Write((short)position.x);
			msg.Write((short)position.y);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

        public void SkillAttack()
        {
            var msg = client.CreateMessage();

			msg.Write((byte)PacketType.Skill);

            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

		public void StopPlayer()
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.StopAction);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		public void SendAttack(int target)
		{
			var msg = client.CreateMessage();

			msg.Write((byte)PacketType.Attack);
			msg.Write(target);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		private void SendPing()
		{
			var msg = client.CreateMessage();
			msg.Write((byte)PacketType.Ping);

			client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
		}

		private void Update()
		{
			Shader.SetGlobalColor("_FakeAmbient", FakeAmbient);

			if (client.Status == NetPeerStatus.Running)
				DoPacketHandling();
			
			if (client.ConnectionStatus == NetConnectionStatus.Connected && !isConnected)
			{
				SendPing();
				var msg = client.CreateMessage();
#if DEBUG
				if (!string.IsNullOrWhiteSpace(SpawnMap))
				{
					msg.Write((byte)PacketType.EnterServerSpecificMap);
					msg.Write(SpawnMap);

					var prefx = PlayerPrefs.GetInt("DebugStartX", -1);
					var prefy = PlayerPrefs.GetInt("DebugStartY", -1);

					//Debug.Log(prefx + " : " + prefy);

					if (prefx > 0 && prefy > 0)
					{
						msg.Write(true);
						msg.Write((short)prefx);
						msg.Write((short)prefy);
						PlayerPrefs.DeleteKey("DebugStartX");
						PlayerPrefs.DeleteKey("DebugStartY");

					}
					msg.Write(false);
				}
				else
					msg.Write((byte)PacketType.EnterServer);
#else
				msg.Write((byte)PacketType.EnterServer);
#endif
				client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
				isConnected = true;
			}

			if (isConnected && client.ServerConnection == null)
			{
				isConnected = false;
				Debug.LogWarning("Client is now disconnected!");
			}

			if (!isConnected)
				return;


			if (client.ConnectionStatus == NetConnectionStatus.Disconnected)
			{
				Console.WriteLine("Disconnected!");
				return;
			}

			if (lastPing + 5 < Time.time)
			{
				SendPing();
				//Debug.Log("Sending keep alive packet.");

				lastPing = Time.time;
			}
		}
		
		public void OnApplicationQuit()
		{
			if (client == null)
				return;
			NetOutgoingMessage outmsg = client.CreateMessage();
			outmsg.Write((byte)PacketType.Disconnect);
			client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);
		}
	}
}
