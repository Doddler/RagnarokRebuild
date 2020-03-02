using System;
using Leopotam.Ecs;
using RebuildData.Server.Logging;
using RebuildData.Server.Pathfinding;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;
using RebuildZoneServer.Data.Management;
using RebuildZoneServer.Data.Management.Types;
using RebuildZoneServer.Networking;
using RebuildZoneServer.Sim;
using RebuildZoneServer.Util;

namespace RebuildZoneServer.EntityComponents
{
	public class Character : IStandardEntity
	{
		public int Id;
		public EcsEntity Entity;
		public bool IsActive;
		public int ClassId;
		public Direction FacingDirection;
		public CharacterState State;
		public CharacterType Type;
		public Position Position;
		public Position TargetPosition;

		public Position[] WalkPath;

		public float MoveSpeed;
		public float MoveCooldown;
		public int MoveStep;
		public int TotalMoveSteps;

		public Map Map;

		public void Reset()
		{
			Id = -1;
			Entity = EcsEntity.Null;
			IsActive = true;
			Map = null;
			State = CharacterState.Idle;
			MoveCooldown = 0;
			MoveSpeed = 0.15f;
			MoveStep = 0;
			Position = new Position();
			TargetPosition = new Position();
			FacingDirection = Direction.South;
			WalkPath = null;
		}

		public void ResetState()
		{
			MoveCooldown = 0;
			State = CharacterState.Idle;
		}

		public void SitStand(ref EcsEntity entity, bool isSitting)
		{
			if (Type != CharacterType.Player)
				return;

			if (State == CharacterState.Moving || State == CharacterState.Dead)
				return;

			if (isSitting)
				State = CharacterState.Sitting;
			else
				State = CharacterState.Idle;
			
			Map.GatherPlayersForMultiCast(ref entity, this);
			CommandBuilder.ChangeSittingMulti(this);
			CommandBuilder.ClearRecipients();
		}

		public void ChangeLookDirection(ref EcsEntity entity, Direction direction, HeadFacing facing)
		{
			if (State == CharacterState.Moving || State == CharacterState.Dead)
				return;

			FacingDirection = direction;

			var player = entity.Get<Player>();
			if(player != null)
				player.HeadFacing = facing;

			Map.GatherPlayersForMultiCast(ref entity, this);
			CommandBuilder.ChangeFacingMulti(this);
			CommandBuilder.ClearRecipients();
		}

		private void ChangeToActionState()
		{
			if (Type != CharacterType.Player)
				return;

			var player = Entity.Get<Player>();
			player.HeadFacing = HeadFacing.Center; //don't need to send this to client, they will assume it resets
		}

		public bool TryMove(ref EcsEntity entity, Position target)
		{
			if (State == CharacterState.Sitting || State == CharacterState.Dead)
				return false;

			if (!Map.WalkData.IsCellWalkable(target))
				return false;

			if(WalkPath == null)
				WalkPath = new Position[17];

			var hasOld = false;
			var oldNext = new Position();
			var oldCooldown = MoveCooldown;
			
			if (MoveStep + 1 < TotalMoveSteps && State == CharacterState.Moving)
			{
				oldNext = WalkPath[MoveStep + 1];
				//if(Type == CharacterType.Player)
				//	ServerLogger.Log($"Old move: {MoveStep} Pos: {oldNext} Cooldown: {MoveCooldown}");
				hasOld = true;
			}
			
			var len = Pathfinder.GetPath(Map.WalkData, Position, target, WalkPath);
			if (len == 0)
				return false;

			TargetPosition = target;
			MoveCooldown = MoveSpeed;
			MoveStep = 0;
			TotalMoveSteps = len;
			FacingDirection = (WalkPath[1] - WalkPath[0]).GetDirectionForOffset();
			
			State = CharacterState.Moving;

			//ServerLogger.Log($"Doing move action: {MoveStep} {TotalMoveSteps}");

			if (hasOld && WalkPath[MoveStep+1] == oldNext)
				MoveCooldown = oldCooldown;

			if (hasOld && WalkPath[MoveStep + 1].SquareDistance(oldNext) == 1)
				MoveCooldown = MoveSpeed - ((MoveSpeed - oldCooldown) * 0.4f);
			
			Map.StartMove(ref entity, this);
			ChangeToActionState();

			return true;
		}

		public void StopAction()
		{
			var needsStop = false;

			//if it's not MoveStep + 2, that means the next step is already the last step.
			if (State == CharacterState.Moving && MoveStep + 2 < TotalMoveSteps)
			{
				TotalMoveSteps = MoveStep + 2;
				TargetPosition = WalkPath[TotalMoveSteps-1];

				//ServerLogger.Log("Stopping player at: " + TargetPosition);
				needsStop = true;
			}

			if (!needsStop)
				return;

			Map.StartMove(ref Entity, this);
		}

		private MapConnector GetConnectorForPosition(Position nextPos)
		{
			if (Type != CharacterType.Player)
				return null;

			return DataManager.GetConnector(Map.Name, nextPos);;
		}
		
		public void Update(ref EcsEntity e)
		{
			if (State == CharacterState.Idle)
				return;

			if (State == CharacterState.Moving)
			{
				if (FacingDirection.IsDiagonal())
					MoveCooldown -= Time.DeltaTimeFloat * 0.8f;
				else
					MoveCooldown -= Time.DeltaTimeFloat;

				if (MoveCooldown <= 0f)
				{
					FacingDirection = (WalkPath[MoveStep + 1] - WalkPath[MoveStep]).GetDirectionForOffset();

					MoveStep++;
					var nextPos = WalkPath[MoveStep];

					var connector = GetConnectorForPosition(nextPos);
					
					if (connector != null)
					{
						State = CharacterState.Idle;

						if(connector.Map == connector.Target)
							Map.MoveEntity(ref e, this, connector.DstArea.RandomInArea());
						else
							Map.World.MovePlayerMap(ref e, this, connector.Target, connector.DstArea.RandomInArea());
					}
					else
					{
						Map.MoveEntity(ref e, this, nextPos, true);

						if (nextPos == TargetPosition)
						{
							State = CharacterState.Idle;
							//if(Type == CharacterType.Player)
							//	ServerLogger.Log($"Entity {Id} reached {TargetPosition}");
						}
						else
						{
							FacingDirection = (WalkPath[MoveStep + 1] - WalkPath[MoveStep]).GetDirectionForOffset();
							//if(Type == CharacterType.Player)
							//	ServerLogger.Log("Set facing direction to: " + FacingDirection);
							MoveCooldown += MoveSpeed;
						}
					}
				}
			}
		}
	}
}
