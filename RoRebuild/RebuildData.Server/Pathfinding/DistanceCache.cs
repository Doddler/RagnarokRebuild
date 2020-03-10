using System;
using System.Collections.Generic;
using System.Text;
using RebuildData.Server.Config;
using RebuildData.Shared.Data;
using RebuildData.Shared.Enum;

namespace RebuildData.Server.Pathfinding
{
	public static class DistanceCache
	{
		private static Direction[] directions;
		private static float[] angles;
		private static float[] distances;

		private static int max;
		private static int width;
		private static int height;

		private const int center = ServerConfig.MaxViewDistance;
		
		public static void Init()
		{
			max = ServerConfig.MaxViewDistance;
			width = max * 2 + 1;
			height = max * 2 + 1;

			angles = new float[width * height];
			distances = new float[width * height];

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					distances[x + y * width] = CalcDistance(0, 0, x - max, y - max);
				}
			}
		}

		public static float Distance(Position p1, Position p2)
		{
			var offset = p1 - p2;
			if (offset.SquareDistance(Position.Zero) > max)
				return CalcDistance(offset.X, offset.Y, 0, 0);

			return distances[(offset.X + max) + (offset.Y + max) * width];
		}

		private static float CalcDistance(int x1, int y1, int x2, int y2)
		{
			var p1 = Math.Pow((x2 - x1), 2);
			var p2 = Math.Pow((y2 - y1), 2);
			return (float)Math.Sqrt(p1 + p2);
		}
	}
}
