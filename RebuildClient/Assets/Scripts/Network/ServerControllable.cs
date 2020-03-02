using System.Collections.Generic;
using Assets.Scripts.MapEditor;
using Assets.Scripts.Sprites;
using RebuildData.Shared.Enum;
using UnityEngine;

namespace Assets.Scripts.Network
{
	public class ServerControllable : MonoBehaviour
	{
		private RoWalkDataProvider walkProvider;

		public CharacterType CharacterType;
		public RoSpriteAnimator SpriteAnimator;
		public int Id;
		public Vector2Int Position;
		public Vector3 StartPos;
		public float ShadowSize;

		public ClientSpriteType SpriteMode;
		public GameObject EntityObject;

		private List<Vector2Int> movePath;
		private Vector2Int[] tempPath;
		private float moveSpeed = 0.2f;
		private float moveProgress;
		private bool isMoving;

		private Material shadowMaterial;

		public bool IsWalking => movePath != null && movePath.Count > 1;

		public void ConfigureEntity(int id, Vector2Int worldPos, FacingDirection direction)
		{
			if(walkProvider == null)
				walkProvider = RoWalkDataProvider.Instance;

			Id = id;
			
			var start = new Vector3(worldPos.x + 0.5f, 0f, worldPos.y + 0.5f);
			var position = new Vector3(start.x, walkProvider.GetHeightForPosition(start), start.z);

			transform.localPosition = position;

			if(SpriteMode == ClientSpriteType.Sprite)
				SpriteAnimator.Direction = direction;
		}
		
		private bool IsNeighbor(Vector2Int pos1, Vector2Int pos2)
		{
			var x = Mathf.Abs(pos1.x - pos2.x);
			var y = Mathf.Abs(pos1.y - pos2.y);

			if (x <= 1 && y <= 1)
				return true;
			return false;
		}

		private FacingDirection GetDirectionForOffset(Vector2Int offset)
		{

			if (offset.x == -1 && offset.y == -1) return FacingDirection.SouthWest;
			if (offset.x == -1 && offset.y == 0) return FacingDirection.West;
			if (offset.x == -1 && offset.y == 1) return FacingDirection.NorthWest;
			if (offset.x == 0 && offset.y == 1) return FacingDirection.North;
			if (offset.x == 1 && offset.y == 1) return FacingDirection.NorthEast;
			if (offset.x == 1 && offset.y == 0) return FacingDirection.East;
			if (offset.x == 1 && offset.y == -1) return FacingDirection.SouthEast;
			if (offset.x == 0 && offset.y == -1) return FacingDirection.South;

			return FacingDirection.South;
		}

		private bool IsDiagonal(FacingDirection dir)
		{
			if (dir == FacingDirection.NorthEast || dir == FacingDirection.NorthWest ||
			    dir == FacingDirection.SouthEast || dir == FacingDirection.SouthWest)
				return true;
			return false;
		}

		public void StartMove(float speed, float progress, int stepCount, int curStep, List<Vector2Int> steps)
		{
			//don't reset start pos if the next tile is the same
			if (movePath == null || movePath.Count <= 1 || movePath[1] != steps[1])
				StartPos = transform.position - new Vector3(0.5f, 0f, 0.5f);
			
			moveSpeed = speed;
			moveProgress = progress / speed; //hack please fix

			if (movePath == null)
				movePath = new List<Vector2Int>(20);
			else
				movePath.Clear();

			//var pathString = "";

			for (var i = curStep; i < stepCount; i++)
			{
				//pathString += steps[i] + " ";
				
				movePath.Add(steps[i]);
			}
			
			//Debug.Log(pathString);
			//Debug.Log(movePath.Count);
			
			isMoving = true;
		}
		
        private void UpdateMove()
		{
			if (movePath.Count == 0 || SpriteMode == ClientSpriteType.Prefab) return;

			if (movePath.Count > 1)
			{

				var offset = movePath[1] - movePath[0];
				if (IsDiagonal(GetDirectionForOffset(offset)))
					moveProgress -= Time.deltaTime / moveSpeed * 0.80f;
				else
					moveProgress -= Time.deltaTime / moveSpeed;
				SpriteAnimator.Direction = GetDirectionForOffset(offset);
			}

			while (moveProgress < 0f && movePath.Count > 1)
			{
				movePath.RemoveAt(0);
				StartPos = new Vector3(movePath[0].x, walkProvider.GetHeightForPosition(transform.position), movePath[0].y);
				moveProgress += 1f;
			}

			if (movePath.Count == 0)
				Debug.Log("WAAA");

			if (movePath.Count == 1)
			{
				var last = movePath[0];
				transform.position = new Vector3(last.x + 0.5f, walkProvider.GetHeightForPosition(transform.position), last.y + 0.5f);
				movePath.Clear();
				isMoving = false;
			}
			else
			{
				var xPos = Mathf.Lerp(StartPos.x, movePath[1].x, 1 - moveProgress);
				var yPos = Mathf.Lerp(StartPos.z, movePath[1].y, 1 - moveProgress);

				transform.position = new Vector3(xPos + 0.5f, walkProvider.GetHeightForPosition(transform.position), yPos + 0.5f);

				//var offset = movePath[1] - movePath[0];
			}
		}

        public void MovePosition(Vector2Int targetPosition)
        {
	        transform.position = new Vector3(targetPosition.x + 0.5f, walkProvider.GetHeightForPosition(transform.position), targetPosition.y + 0.5f);
		}

        public void StopWalking()
        {
			if(movePath.Count > 2)
				movePath.RemoveRange(2, movePath.Count - 2);
		}

        public void AttachShadow(Sprite spriteObj)
        {
	        if (gameObject == null)
		        return;
			var go = new GameObject("Shadow");
			go.layer = LayerMask.NameToLayer("Characters");
			go.transform.SetParent(transform, false);
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = new Vector3(ShadowSize, ShadowSize, ShadowSize);

			if (Mathf.Approximately(0, ShadowSize))
				ShadowSize = 0.5f;

			var sprite = go.AddComponent<SpriteRenderer>();
			sprite.sprite = spriteObj;

			var shader = Shader.Find("Unlit/TestSpriteShader");
			var mat = new Material(shader);
			mat.SetFloat("_Offset", 0.4f);
			mat.color = new Color(1f, 1f, 1f, 0.5f);
			sprite.material = mat;

			sprite.sortingOrder = -1;

			SpriteAnimator.Shadow = go;
			if(SpriteAnimator.State == SpriteState.Sit)
				go.SetActive(false);
        }

		private void Update()
		{
			if (SpriteMode == ClientSpriteType.Prefab)
				return;

			if (isMoving)
			{
				UpdateMove();

				if (SpriteAnimator.State != SpriteState.Walking)
				{
					SpriteAnimator.AnimSpeed = moveSpeed / 0.2f;
					SpriteAnimator.State = SpriteState.Walking;
					SpriteAnimator.ChangeMotion(SpriteMotion.Walk);
				}
			}
			else
			{
				if (SpriteAnimator.State == SpriteState.Walking)
				{
					SpriteAnimator.AnimSpeed = 1f;
					SpriteAnimator.State = SpriteState.Idle;
					SpriteAnimator.ChangeMotion(SpriteMotion.Idle);
				}
			}
		}

		private void OnDestroy()
		{
			if(shadowMaterial != null)
				Object.Destroy(shadowMaterial);
		}
	}
}
