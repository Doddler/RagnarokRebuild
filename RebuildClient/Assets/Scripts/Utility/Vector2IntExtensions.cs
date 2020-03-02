using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utility
{
	public static class Vector2IntExtensions
	{
		public static int SquareDistance(this Vector2Int v)
		{
			return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y));
		}
	}
}
