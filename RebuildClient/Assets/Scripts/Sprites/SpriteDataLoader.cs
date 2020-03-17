using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Network;
using Assets.Scripts.Utility;
using RebuildData.Shared.ClientTypes;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Sprites
{
	public struct PlayerSpawnParameters
	{
		public int ServerId;
		public int ClassId;
		public int HeadId;
		public HeadFacing HeadFacing;
		public Direction Facing;
		public CharacterState State;
		public Vector2Int Position;
		public bool IsMale;
	}

	public struct MonsterSpawnParameters
	{
		public int ServerId;
		public int ClassId;
		public Direction Facing;
		public CharacterState State;
		public Vector2Int Position;
	}

	public class SpriteDataLoader : MonoBehaviour
	{
		public static SpriteDataLoader Instance;

		public TextAsset MonsterClassData;
		public TextAsset PlayerClassData;
		public TextAsset PlayerHeadData;

		private Dictionary<int, MonsterClassData> monsterClassLookup = new Dictionary<int, MonsterClassData>();
		private Dictionary<int, PlayerHeadData> playerHeadLookup = new Dictionary<int, PlayerHeadData>();
		private Dictionary<int, PlayerClassData> playerClassLookup = new Dictionary<int, PlayerClassData>();

		private bool isInitialized;

		private void Awake()
		{
			Initialize();
		}

		private void Initialize()
		{
			Instance = this;
			var entityData = JsonUtility.FromJson<DatabaseMonsterClassData>(MonsterClassData.text);
			foreach (var m in entityData.MonsterClassData)
			{
				monsterClassLookup.Add(m.Id, m);
			}

			var headData = JsonUtility.FromJson<DatabasePlayerHeadData>(PlayerHeadData.text);
			foreach (var h in headData.PlayerHeadData)
			{
				playerHeadLookup.Add(h.Id, h);
			}
			
			var playerData = JsonUtility.FromJson<DatabasePlayerClassData>(PlayerClassData.text);
			foreach (var p in playerData.PlayerClassData)
			{
				playerClassLookup.Add(p.Id, p);
			}

			isInitialized = true;
		}

		public ServerControllable InstantiatePlayer(ref PlayerSpawnParameters param)
		{
			if (!isInitialized)
				Initialize();

			var pData = playerClassLookup[0]; //novice
			if (playerClassLookup.TryGetValue(param.ClassId, out var lookupData))
				pData = lookupData;
			else
				Debug.LogWarning("Failed to find player with id of " + param.ClassId);

			var hData = playerHeadLookup[0]; //default;
			if (playerHeadLookup.TryGetValue(param.HeadId, out var lookupData2))
				hData = lookupData2;
			else
				Debug.LogWarning("Failed to find player head with id of " + param.ClassId);


			var go = new GameObject(pData.Name);
			go.layer = LayerMask.NameToLayer("Characters");
			go.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
			var control = go.AddComponent<ServerControllable>();
			go.AddComponent<Billboard>();

			var body = new GameObject("Sprite");
			body.layer = LayerMask.NameToLayer("Characters");
			body.transform.SetParent(go.transform, false);
			body.transform.localPosition = Vector3.zero;
			body.AddComponent<SortingGroup>();

			var head = new GameObject("Head");
			head.layer = LayerMask.NameToLayer("Characters");
			head.transform.SetParent(body.transform, false);
			head.transform.localPosition = Vector3.zero;
			
			var bodySprite = body.AddComponent<RoSpriteAnimator>();
			var headSprite = head.AddComponent<RoSpriteAnimator>();

			control.SpriteAnimator = bodySprite;
			control.CharacterType = CharacterType.Player;
			control.SpriteMode = ClientSpriteType.Sprite;

			bodySprite.Controllable = control;
			if(param.State == CharacterState.Moving)
				bodySprite.ChangeMotion(SpriteMotion.Walk);
			bodySprite.ChildrenSprites.Add(headSprite);
			bodySprite.SpriteOffset = 0.5f;
			bodySprite.HeadFacing = param.HeadFacing;

			if (param.State == CharacterState.Sitting)
				bodySprite.State = SpriteState.Sit;
			if (param.State == CharacterState.Moving)
				bodySprite.State = SpriteState.Walking;

			headSprite.Parent = bodySprite;
			headSprite.SpriteOrder = 1;

			control.ShadowSize = 0.5f;
			
			var bodySpriteName = param.IsMale ? pData.SpriteMale : pData.SpriteFemale;
			var headSpriteName = param.IsMale ? hData.SpriteMale : hData.SpriteFemale;

			control.ConfigureEntity(param.ServerId, param.Position, param.Facing);

			AddressableUtility.LoadRoSpriteData(go, bodySpriteName, bodySprite.OnSpriteDataLoad);
			AddressableUtility.LoadRoSpriteData(go, headSpriteName, headSprite.OnSpriteDataLoad);
			AddressableUtility.LoadSprite(go, "shadow", control.AttachShadow);

			return control;
		}

		private ServerControllable PrefabMonster(MonsterClassData mData, ref MonsterSpawnParameters param)
		{
			var prefabName = mData.SpriteName.Replace(".prefab", "");
			var split = prefabName.Split('/');
			prefabName = split.Last();
			//Debug.Log(prefabName);
			var obj = GameObject.Instantiate(Resources.Load<GameObject>(prefabName));
			var control = obj.AddComponent<ServerControllable>();
			control.CharacterType = CharacterType.NPC;
			control.SpriteMode = ClientSpriteType.Prefab;
			control.EntityObject = obj;

			control.ConfigureEntity(param.ServerId, param.Position, param.Facing);

			return control;
		}

		public ServerControllable InstantiateMonster(ref MonsterSpawnParameters param)
		{
			if(!isInitialized)
				Initialize();

			var mData = monsterClassLookup[4000]; //poring
			if (monsterClassLookup.TryGetValue(param.ClassId, out var lookupData))
				mData = lookupData;
			else
				Debug.LogWarning("Failed to find monster with id of " + param.ClassId);

			if (mData.SpriteName.Contains(".prefab"))
				return PrefabMonster(mData, ref param);

			var go = new GameObject(mData.Name);
			go.layer = LayerMask.NameToLayer("Characters");
			go.transform.localScale = new Vector3(1.5f * mData.Size, 1.5f * mData.Size, 1.5f * mData.Size);
			var control = go.AddComponent<ServerControllable>();
			control.CharacterType = CharacterType.Monster;
			control.SpriteMode = ClientSpriteType.Sprite;
			go.AddComponent<Billboard>();

			var child = new GameObject("Sprite");
			child.layer = LayerMask.NameToLayer("Characters");
			child.transform.SetParent(go.transform, false);
			child.transform.localPosition = Vector3.zero;

			var sprite = child.AddComponent<RoSpriteAnimator>();
			sprite.Controllable = control;
			
			control.SpriteAnimator = sprite;
			control.SpriteAnimator.SpriteOffset = mData.Offset;
			control.ShadowSize = mData.ShadowSize;

			control.ConfigureEntity(param.ServerId, param.Position, param.Facing);
			
			AddressableUtility.LoadRoSpriteData(go, "Assets/Sprites/Monsters/" + mData.SpriteName, control.SpriteAnimator.OnSpriteDataLoad);
			if (mData.ShadowSize > 0)
				AddressableUtility.LoadSprite(go, "shadow", control.AttachShadow);

			return control;
		}
	}
}