using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Sprites
{
	public static class SpriteMeshCache
	{
		private static Dictionary<string, Dictionary<int, Mesh>> spriteMeshCache;

		public static Dictionary<int, Mesh> GetMeshCacheForSprite(string spriteName)
		{
			if(spriteMeshCache == null)
				spriteMeshCache = new Dictionary<string, Dictionary<int, Mesh>>();

			if (spriteMeshCache.TryGetValue(spriteName, out var meshCache)) 
				return meshCache;

			//Debug.Log("Making new mesh cache");

			var newCache = new Dictionary<int, Mesh>();
			spriteMeshCache.Add(spriteName, newCache);
			return newCache;
		}
	}
}