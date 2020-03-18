using System;
using System.Collections.Generic;
using Assets.Scripts.MapEditor;
using Assets.Scripts.Network;
using Assets.Scripts.Sprites;
using Assets.Scripts.Utility;
using RebuildData.Shared.Config;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
	public class CameraFollower : MonoBehaviour
	{
		public GameObject ListenerProbe;

		public Texture2D NormalCursorTexture;
		public Texture2D AttackCursorTexture;

		private static CameraFollower _instance;
		private RoWalkDataProvider WalkProvider;

		public Canvas UiCanvas;
		public TextMeshProUGUI TargetUi;
		public CanvasScaler CanvasScaler;

		private Texture2D currentCursor;

		private Vector2Int[] tempPath;

		private int lastWidth;
		private int lastHeight;

		private Vector2Int lastTile;
		private bool lastPathValid;

#if DEBUG
		private const float MaxClickDistance = 500;
#else
        private const float MaxClickDistance = 150;
#endif

		public static CameraFollower Instance
		{
			get
			{
				if (_instance != null)
					return _instance;
				var cam = GameObject.FindObjectOfType<CameraFollower>();
				if (cam != null)
				{
					_instance = cam;
					return _instance;
				}

				var mc = Camera.main;
				if (mc == null)
					return null;

				cam = mc.GetComponent<CameraFollower>();
				if (cam != null)
				{
					_instance = cam;
					return _instance;
				}

				cam = mc.gameObject.AddComponent<CameraFollower>();
				_instance = cam;
				return _instance;
			}
		}

		public GameObject Target;
		public Camera Camera;
		public Vector3 CurLookAt;

		private ServerControllable controllable;

		public Vector3 MoveTo;

		public Vector3 TargetFollow;

		//private EntityWalkable targetWalkable;

		public List<Vector2Int> MovePath;
		public float MoveSpeed;
		public float MoveProgress;
		public Vector3 StartPos;

		public float TargetRotation;

		public float ClickDelay;

		public float Rotation;
		public float Distance;
		public float Height;

		public float TurnSpeed;

		public int MonSpawnCount = 1;

		public float LastRightClick;
		private bool isHolding;

		public void Awake()
		{
			//CurLookAt = Target.transform.position;
			TargetFollow = CurLookAt;
			Camera = GetComponent<Camera>();
			//MoveTo = Target.transform.position;

			WalkProvider = GameObject.FindObjectOfType<RoWalkDataProvider>();

			UpdateCameraSize();

			Physics.queriesHitBackfaces = true;

			//targetWalkable = Target.GetComponent<EntityWalkable>();
			//if (targetWalkable == null)
			//    targetWalkable = Target.AddComponent<EntityWalkable>();

			//DoMapSpawn();
		}

		private void UpdateCameraSize()
		{
			//if (Screen.width == 0)
			// return; //wut?
			//var scale = 1f / (1080f / Screen.height);
			//CanvasScaler.scaleFactor = scale;
			//lastWidth = Screen.width;
			//lastHeight = Screen.height;
		}


		private FacingDirection GetFacingForAngle(float angle)
		{
			if (angle > 157.5f) return FacingDirection.South;
			if (angle > 112.5f) return FacingDirection.SouthWest;
			if (angle > 67.5f) return FacingDirection.West;
			if (angle > 22.5f) return FacingDirection.NorthWest;
			if (angle > -22.5f) return FacingDirection.North;
			if (angle > -67.5f) return FacingDirection.NorthEast;
			if (angle > -112.5f) return FacingDirection.East;
			if (angle > -157.5f) return FacingDirection.SouthEast;
			return FacingDirection.South;
		}


		private Direction GetFacingForPoint(Vector2Int point)
		{
			if (point.y == 0)
			{
				if (point.x < 0)
					return Direction.West;
				else
					return Direction.East;
			}

			if (point.x == 0)
			{
				if (point.y < 0)
					return Direction.South;
				else
					return Direction.North;
			}

			if (point.x < 0)
			{
				if (point.y < 0)
					return Direction.SouthWest;
				else
					return Direction.NorthWest;
			}

			if (point.y < 0)
				return Direction.SouthEast;

			return Direction.NorthEast;

			//return FacingDirection.South;
		}

		public void ChangeFacing(Vector3 dest)
		{
			var srcPoint = WalkProvider.GetTilePositionForPoint(Target.transform.position);
			var destPoint = WalkProvider.GetTilePositionForPoint(dest);

			var curFacing = controllable.SpriteAnimator.Direction;
			var newFacing = GetFacingForPoint(destPoint - srcPoint);
			var newHead = HeadFacing.Center;

			if (curFacing == newFacing)
			{
				if (controllable.SpriteAnimator.HeadFacing != HeadFacing.Center)
					NetworkManager.Instance.ChangePlayerFacing(newFacing, HeadFacing.Center);

				return;
			}

			if (controllable.SpriteAnimator.State != SpriteState.Idle &&
				controllable.SpriteAnimator.State != SpriteState.Sit)
			{
				NetworkManager.Instance.ChangePlayerFacing(newFacing, HeadFacing.Center);
				return;
			}

			var diff = (int)curFacing - (int)newFacing;
			if (diff < 0)
				diff += 8;

			//var dontChange = false;

			if (diff != 4) //they're trying to turn around, let them without changing face
			{
				if ((diff == 1 && controllable.SpriteAnimator.HeadFacing == HeadFacing.Left)
					|| (diff == 7 && controllable.SpriteAnimator.HeadFacing == HeadFacing.Right))
				{
					//we go from head turn to fully turning in that direction.
					//not inverting the if statement just for clarity
				}
				else
				{
					var facing = (int)newFacing;
					if (diff < 4)
					{
						facing = facing + 1;
						if (facing > 7)
							facing = 0;
						newHead = HeadFacing.Left;
					}
					else
					{
						facing = facing - 1;
						if (facing < 0)
							facing = 7;
						newHead = HeadFacing.Right;
					}

					newFacing = (Direction)facing;
				}
			}

			//Debug.Log($"{curFacing} {newFacing} {newHead} {diff}");

			NetworkManager.Instance.ChangePlayerFacing(newFacing, newHead);
		}

		public void SnapLookAt()
		{
			CurLookAt = Target.transform.position;
			TargetFollow = Target.transform.position;
		}

		public void ChangeCursor(Texture2D cursorTexture)
		{
			if (currentCursor == cursorTexture)
				return;

			Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
			currentCursor = cursorTexture;
		}

		private RoSpriteAnimator GetHitAnimator(RaycastHit hit)
		{
			var target = hit.transform.gameObject.GetComponent<RoSpriteAnimator>();
			return target.Parent ? target.Parent : target;
		}

		private RoSpriteAnimator GetClosestOrEnemy(RaycastHit[] hits)
		{
			var closestAnim = GetHitAnimator(hits[0]);
			var closestHit = hits[0];

			//var log = $"{hits.Length} {closestAnim.Controllable.gameObject.name}{closestHit.distance}{closestAnim.Controllable.IsAlly}";

			if (hits.Length == 1)
			{
				if (closestAnim.State == SpriteState.Dead)
					return null;
				return closestAnim;
			}

			var isOk = closestAnim.State != SpriteState.Dead;
			var isAlly = closestAnim.Controllable.IsAlly;

			for (var i = 1; i < hits.Length; i++)
			{
				var hit = hits[i];
				var anim = GetHitAnimator(hit);

				if (anim.State == SpriteState.Dead)
					continue;
				
				if(!isOk)
				{
					closestAnim = anim;
					closestHit = hit;
					isAlly = anim.Controllable.IsAlly;
					isOk = true;
				}

				if (!isAlly && anim.Controllable.IsAlly)
					continue;

				//log += $" {anim.Controllable.gameObject.name}{hit.distance}{anim.Controllable.IsAlly}-{anim.State}";

				if ((hit.distance < closestHit.distance) || (isAlly && !anim.Controllable.IsAlly))
				{
					closestHit = hit;
					closestAnim = anim;
					isAlly = anim.Controllable.IsAlly;
				}
			}

			//log += $" : {closestAnim.Controllable.gameObject.name}{closestHit.distance}{closestAnim.Controllable.IsAlly} {isOk}";
			//Debug.Log(log);


			if (isOk)
				return closestAnim;

			return null;
		}

		private void DoScreenCast()
		{
			var ray = Camera.ScreenPointToRay(Input.mousePosition);

			var hasHitCharacter = false;

			var characterHits = Physics.RaycastAll(ray, MaxClickDistance, (1 << LayerMask.NameToLayer("Characters")));
			var hasHitMap = Physics.Raycast(ray, out var groundHit, MaxClickDistance, (1 << LayerMask.NameToLayer("WalkMap")));

			if (characterHits.Length > 0)
				hasHitCharacter = true;

			if (isHolding)
				hasHitCharacter = false;

			if (hasHitCharacter)
			{
				//var anim = charHit.transform.gameObject.GetComponent<RoSpriteAnimator>();
				var anim = GetClosestOrEnemy(characterHits);
				if (anim == null)
					hasHitCharacter = false; //back out if our hit is a false positive (the object is dead or dying for example)

				if (hasHitCharacter)
				{
					var screenPos = Camera.main.WorldToScreenPoint(anim.Controllable.gameObject.transform.position);
					var color = "";
					if (!anim.Controllable.IsAlly)
					{
						ChangeCursor(AttackCursorTexture);
						color = "<color=#FFAAAA>";
						hasHitMap = false;
					}
					else
					{
						ChangeCursor(NormalCursorTexture);
					}

					var reverseScale = 1f / CanvasScaler.scaleFactor;

					TargetUi.rectTransform.anchoredPosition = new Vector2(screenPos.x * reverseScale,
						((screenPos.y - UiCanvas.pixelRect.height) - 30) * reverseScale);
					TargetUi.text = color + anim.Controllable.gameObject.name;

					if (anim.Controllable.CharacterType == CharacterType.Monster)
					{
						if (Input.GetMouseButtonDown(0))
						{
							NetworkManager.Instance.SendAttack(anim.Controllable.Id);
						}
					}
				}
			}
			else
				ChangeCursor(NormalCursorTexture);

			ClickDelay -= Time.deltaTime;

			if (Input.GetMouseButtonUp(0))
				ClickDelay = 0;

			if (!Input.GetMouseButton(0))
				isHolding = false;

			if (!hasHitMap)
			{
				WalkProvider.DisableRenderer();
				return;
			}

			//we hit the map! Do map things

			var hasGroundPos = WalkProvider.GetMapPositionForWorldPosition(groundHit.point, out var mapPosition);
			var hasSrcPos = WalkProvider.GetMapPositionForWorldPosition(Target.transform.position, out var srcPosition);
			var okPath = true;

			if (hasGroundPos && hasSrcPos)
			{
				if (mapPosition != lastTile)
				{
					if (tempPath == null)
						tempPath = new Vector2Int[SharedConfig.MaxPathLength + 2];

					if (!WalkProvider.IsCellWalkable(mapPosition))
						okPath = false;

					if ((mapPosition - srcPosition).SquareDistance() > SharedConfig.MaxPathLength)
						okPath = false;

					if (okPath)
					{
						//Debug.Log("Performing path check");

						var steps = Pathfinder.GetPath(WalkProvider.WalkData, mapPosition, srcPosition, tempPath);
						if (steps == 0)
							okPath = false;
					}
				}
				else
					okPath = lastPathValid;

				WalkProvider.UpdateCursorPosition(Target.transform.position, groundHit.point, okPath);

				lastPathValid = okPath;
				lastTile = mapPosition;
			}

			if (!hasHitCharacter)
				TargetUi.text = "";


			if (Input.GetMouseButton(0) && ClickDelay <= 0)
			{
				var srcPos = controllable.Position;
				var hasDest = WalkProvider.GetClosestTileTopToPoint(groundHit.point, out var destPos);

				if (hasDest)
				{
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
						controllable.SpriteAnimator.State == SpriteState.Sit)
					{
						if (Input.GetMouseButtonDown(0)) //only do this when mouse is down the first time. Yeah the second check is dumb...
						{
							ClickDelay = 0.1f;
							ChangeFacing(WalkProvider.GetWorldPositionForTile(destPos));
						}
					}
					else
					{
						var dist = (srcPos - destPos).SquareDistance();
						if (WalkProvider.IsCellWalkable(destPos) && dist < SharedConfig.MaxPathLength)
						{
							NetworkManager.Instance.MovePlayer(destPos);
							ClickDelay = 0.5f;
							isHolding = true;
						}
						else
						{
							if (WalkProvider.GetNextWalkableTileForClick(srcPos, destPos, out var dest2))
							{
								NetworkManager.Instance.MovePlayer(dest2);
								ClickDelay = 0.5f;
								isHolding = true;
							}
						}
					}
				}
			}
		}

		public void Update()
		{
			if (Target == null)
				return;

			if (controllable == null)
				controllable = Target.GetComponent<ServerControllable>();

			if (ListenerProbe != null)
			{
				var forward = Camera.main.transform.forward;
				var dist = Vector3.Distance(Camera.main.transform.position, Target.transform.position);
				ListenerProbe.transform.localPosition = new Vector3(0f, 0f, dist - 10f);
			}

			if (WalkProvider == null)
				WalkProvider = GameObject.FindObjectOfType<RoWalkDataProvider>();

			if (Screen.height != lastHeight)
				UpdateCameraSize();

			if (Input.GetKeyDown(KeyCode.Insert))
			{
				if (controllable.SpriteAnimator.State == SpriteState.Idle || controllable.SpriteAnimator.State == SpriteState.Standby)
					NetworkManager.Instance.ChangePlayerSitStand(true);
				if (controllable.SpriteAnimator.State == SpriteState.Sit)
					NetworkManager.Instance.ChangePlayerSitStand(false);
			}

			if (Input.GetKeyDown(KeyCode.S))
				controllable.SpriteAnimator.Standby = true;

			if (Input.GetKeyDown(KeyCode.Space))
			{
				//Debug.Log(controllable.IsWalking);
				//if(controllable.IsWalking)
				NetworkManager.Instance.StopPlayer();
			}

			if (Input.GetMouseButtonDown(1))
			{
				if (Time.timeSinceLevelLoad - LastRightClick < 0.3f)
				{
					if (Input.GetKey(KeyCode.LeftShift))
						Height = 50;
					else
						TargetRotation = 0f;
				}

				LastRightClick = Time.timeSinceLevelLoad;
			}

			if (Input.GetMouseButton(1))
			{

				if (Input.GetMouseButton(1) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
				{
					Height -= Input.GetAxis("Mouse Y") / 4;

					Height = Mathf.Clamp(Height, 0f, 90f);
				}
				else
				{
					var turnSpeed = 200;
					TargetRotation += Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime;
				}
			}

#if !DEBUG
            if (Height > 70)
	            Height = 70;
            if (Height < 35)
	            Height = 35;
#endif

			DoScreenCast();

			//Rotation += Time.deltaTime * 360;

			if (TargetRotation > 360)
				TargetRotation -= 360;
			if (TargetRotation < 0)
				TargetRotation += 360;

			if (Rotation > 360)
				Rotation -= 360;
			if (Rotation < 0)
				Rotation += 360;

			Rotation = Mathf.LerpAngle(Rotation, TargetRotation, 7.5f * Time.deltaTime);

			var ctrlKey = Input.GetKey(KeyCode.LeftControl) ? 10 : 1;

			Distance += Input.GetAxis("Mouse ScrollWheel") * 20 * ctrlKey;

#if !DEBUG
            if (Distance > 80)
	            Distance = 80;
            if (Distance < 30)
	            Distance = 30;
#endif

			TargetFollow = Vector3.Lerp(TargetFollow, Target.transform.position, Time.deltaTime * 5f);
			CurLookAt = TargetFollow;

			var targetHeight = Mathf.Lerp(Target.transform.position.y, WalkProvider.GetHeightForPosition(Target.transform.position), Time.deltaTime * 20f);

			Target.transform.position = new Vector3(Target.transform.position.x, targetHeight, Target.transform.position.z);

			var pos = Quaternion.Euler(Height, Rotation, 0) * Vector3.back * Distance;

			transform.position = CurLookAt + pos;
			transform.LookAt(CurLookAt, Vector3.up);

			if (Input.GetKeyDown(KeyCode.F5))
				NetworkManager.Instance.RandomTeleport();

			if (Input.GetKeyDown(KeyCode.M))
			{
				AudioListener.volume = 1 - AudioListener.volume;
			}

			if (Input.GetKeyDown(KeyCode.Tab))
			{
				var chunks = GameObject.FindObjectsOfType<RoMapChunk>();
				foreach (var c in chunks)
				{
					if (c.gameObject.layer == LayerMask.NameToLayer("WalkMap"))
					{
						var r = c.gameObject.GetComponent<MeshRenderer>();
						r.enabled = !r.enabled;
					}
				}
			}
		}
	}
}
