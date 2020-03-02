using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Editor
{
	public class StrAnimationFile
	{
		public int FrameRate;
		public int MaxKey;
		public int LayerCount;
		public List<StrLayer> Layers;
	}

	public class StrLayer
	{
		public int TextureCount;
		public List<string> Textures;
		public int AnimationCount;
		public List<StrAnimationEntry> Animations;
	}

	public class StrAnimationEntry
	{
		public int Frame;
		public int Type;
		public Vector2 Position;
		public Vector2[] UVs;
		public Vector2[] XY;
		public float Aniframe;
		public int Anitype;
		public float Delay;
		public float Angle;
		public Color Color;
		public float SrcAlpha;
		public float DstAlpha;
		public float MTPreset;
	}

    class RagnarokEffectLoader
    {
    }
}
