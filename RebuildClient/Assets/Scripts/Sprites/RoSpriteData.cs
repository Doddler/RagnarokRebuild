﻿using System;
using UnityEngine;

namespace Assets.Scripts
{

    [Serializable]
    public class RoLayer
    {
        public Vector2 Position;
        public int Index;
        public bool IsMirror;
        public Vector2 Scale;
        public Color Color;
        public int Angle;
        public int Type;
        public int Width;
        public int Height;
    }

    [Serializable]
    public class RoPos
    {
        public Vector2 Position;
        public int Unknown1;
        public int Unknown2;
    }

    [Serializable]
    public class RoFrame
    {
        public RoLayer[] Layers;
        public RoPos[] Pos;
        public int Sound;
    }

    [Serializable]
    public class RoAction
    {
        public int Delay;
        public RoFrame[] Frames;
    }

    public enum FacingDirection
    {
        South,
        SouthWest,
        West,
        NorthWest,
        North,
        NorthEast,
        East,
        SouthEast,
    }

    public enum SpriteState
    {
        Idle,
        Walking,
        Standby,
        Sit,
        Dead
    }

    public enum SpriteType
    {
        Player,
        Head,
        Headgear,
        Monster,
        Monster2,
        Npc,
        ActionNpc,
        Pet
    }

    public enum SpriteMotion
    {
        Idle,
        Walk,
        Sit,
        PickUp,
        Attack1,
        Attack2,
        Attack3,
        Standby,
        Hit,
        Freeze1,
        Freeze2,
        Dead,
        Casting,
        Special,
        Performance1,
        Performance2,
        Performance3,
    }

    public static class RoAnimationHelper
    {
        private static float AngleDir(Vector2 targetDir, Vector2 up)
        {
            var dir = targetDir - up;
            var angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            if (angle < 0)
                angle += 360;
            return angle;
        }

        public static Vector2 FacingDirectionToVector(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.South: return new Vector2(0, -1);
                case FacingDirection.SouthWest: return new Vector2(-1, -1);
                case FacingDirection.West: return new Vector2(-1, 0);
                case FacingDirection.NorthWest: return new Vector2(-1, 1);
                case FacingDirection.North: return new Vector2(0, 1);
                case FacingDirection.NorthEast: return new Vector2(1, 1);
                case FacingDirection.East: return new Vector2(1, 0);
                case FacingDirection.SouthEast: return new Vector2(1, -1);
            }

            return Vector2.zero;
        }

        public static int GetSpriteIndexForAngle(FacingDirection facing, float cameraRotation)
        {
            cameraRotation += 45f * (int)facing + (45f / 2f);
            if (cameraRotation > 360)
                cameraRotation -= 360;
            if (cameraRotation < 0)
                cameraRotation += 360;

            var index = Mathf.FloorToInt(cameraRotation / 45f);

            //Debug.Log($"a: {angle} i: {index}");


            return index;
        }

        public static int GetSpriteIndexForAngle(FacingDirection facing, Vector3 position, Vector3 cameraPosition)
        {
            var targetDir = new Vector2(position.x, position.z) - new Vector2(cameraPosition.x, cameraPosition.z);
            var angle = -AngleDir(targetDir, Vector2.down);

            angle += 45f * (int) facing + (45f / 2f);
            if (angle > 360)
                angle -= 360;
            if (angle < 0)
                angle += 360;

            var index = Mathf.FloorToInt(angle / 45f);

            //Debug.Log($"a: {angle} i: {index}");
            

            return index;
        }

        public static int GetMotionIdForSprite(SpriteType type, SpriteMotion motion)
        {
            if (motion == SpriteMotion.Idle)
                return 0;

            if (type == SpriteType.ActionNpc)
            {
                switch (motion)
                {
                    case SpriteMotion.Walk: return 1 * 8;
                    case SpriteMotion.Hit: return 2 * 8;
                    case SpriteMotion.Attack1: return 3 * 8;
                }
            }

            if (type == SpriteType.Monster2)
            {
                if (motion == SpriteMotion.Attack2)
                    return 5 * 8;
            }

            if (type == SpriteType.Monster || type == SpriteType.Monster2 || type == SpriteType.Pet)
            {
                switch (motion)
                {
                    case SpriteMotion.Walk: return 1 * 8;
                    case SpriteMotion.Attack1: return 2 * 8;
                    case SpriteMotion.Attack2: return 2 * 8;
                    case SpriteMotion.Attack3: return 2 * 8;
                    case SpriteMotion.Hit: return 3 * 8;
                    case SpriteMotion.Dead: return 4 * 8;
                }
            }
            
            if (type == SpriteType.Pet)
            {
                switch (motion)
                {
                    case SpriteMotion.Special: return 5 * 8;
                    case SpriteMotion.Performance1: return 6 * 8;
                    case SpriteMotion.Performance2: return 7 * 8;
                    case SpriteMotion.Performance3: return 8 * 8;
                }
            }

            if (type == SpriteType.Player)
            {
                switch (motion)
                {
                    case SpriteMotion.Walk: return 1 * 8;
                    case SpriteMotion.Sit: return 2 * 8;
                    case SpriteMotion.PickUp: return 3 * 8;
                    case SpriteMotion.Standby: return 4 * 8;
                    case SpriteMotion.Attack1: return 11 * 8;
                    case SpriteMotion.Hit: return 6 * 8;
                    case SpriteMotion.Freeze1: return 7 * 8;
                    case SpriteMotion.Dead: return 8 * 8;
                    case SpriteMotion.Freeze2: return 9 * 8;
                    case SpriteMotion.Attack2: return 10 * 8;
                    case SpriteMotion.Attack3: return 11 * 8;
                    case SpriteMotion.Casting: return 12 * 8;
                }
            }

            return -1;
        }

        public static SpriteMotion GetMotionForState(SpriteState state)
        {
            switch (state)
            {
                case SpriteState.Idle:
                    return SpriteMotion.Idle;
                case SpriteState.Standby:
                    return SpriteMotion.Standby;
                case SpriteState.Walking:
                    return SpriteMotion.Walk;
                case SpriteState.Dead:
                    return SpriteMotion.Dead;
            }

            return SpriteMotion.Idle;
        }

        public static bool IsLoopingMotion(SpriteMotion motion)
        {
            switch (motion)
            {
                case SpriteMotion.Idle:
                case SpriteMotion.Sit:
                case SpriteMotion.Walk:
                case SpriteMotion.Casting:
                case SpriteMotion.Freeze1:
                case SpriteMotion.Freeze2:
                case SpriteMotion.Dead:
                    return true;
                case SpriteMotion.Attack1:
                case SpriteMotion.Attack2:
                case SpriteMotion.Attack3:
                case SpriteMotion.Hit:
                case SpriteMotion.PickUp:
                case SpriteMotion.Special:
                case SpriteMotion.Performance1:
                case SpriteMotion.Performance2:
                case SpriteMotion.Performance3:
                    return false;
            }

            return false;
        }
    }

    public class RoSpriteData : ScriptableObject
    {
        public string Name;
        public SpriteType Type;
        public RoAction[] Actions;
        public Sprite[] Sprites;
        public Vector2Int[] SpriteSizes;
        public Texture2D Atlas;
        public int Size;
        public AudioClip[] Sounds;
    }
}
