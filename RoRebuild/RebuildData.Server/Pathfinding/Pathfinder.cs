using System;
using System.Collections.Generic;
using System.Text;
using RebuildData.Server.Logging;
using RebuildData.Shared.Data;

namespace RebuildData.Server.Pathfinding
{
	public class PathNode
	{
		public PathNode Parent;
		public Position Position;
		public int Steps;
		public int Distance;
		public int F;

		public void Set(PathNode parent, Position position, int distance)
		{
			Parent = parent;
			Position = position;
			if (Parent == null)
				Steps = 0;
			else
				Steps = Parent.Steps + 1;
			Distance = distance;
			F = Steps + Distance;
		}

		public PathNode(PathNode parent, Position position, int distance)
		{
			Set(parent, position, distance);
		}
	}

	public static class Pathfinder
	{
		private static PathNode[] nodeCache;
		private static int cachePos;
		private const int MaxDistance = 15;
		private const int MaxCacheSize = ((MaxDistance + 1) * 2) * ((MaxDistance + 1) * 2);

		private static List<PathNode> openList = new List<PathNode>(MaxCacheSize);

		private static HashSet<Position> openListPos = new HashSet<Position>();
		private static HashSet<Position> closedListPos = new HashSet<Position>();

		private static void BuildCache()
		{
			ServerLogger.Log("Build path cache");

			nodeCache = new PathNode[MaxCacheSize];
			for (var i = 0; i < MaxCacheSize; i++)
			{
				var n = new PathNode(null, Position.Zero, 0);
				nodeCache[i] = n;
			}

			cachePos = MaxCacheSize;

		}

		private static PathNode NextPathNode(PathNode parent, Position position, int distance)
		{
			var n = nodeCache[cachePos - 1];
			n.Set(parent, position, distance);
			cachePos--;
			return n;
		}

		private static int CalcDistance(Position pos, Position dest)
		{
			return Math.Abs(pos.X - dest.X) + Math.Abs(pos.Y - dest.Y);
		}

		private static bool HasPosition(List<PathNode> node, Position pos)
		{
			for (var i = 0; i < node.Count; i++)
			{
				if (node[i].Position == pos)
					return true;
			}

			return false;
		}

		private static PathNode BuildPath(MapWalkData walkData, Position start, Position target, int maxLength)
		{
			if (nodeCache == null)
				BuildCache();

			cachePos = MaxCacheSize;

			openList.Clear();
			openListPos.Clear();
			closedListPos.Clear();


			var current = NextPathNode(null, start, CalcDistance(start, target));

			openList.Add(current);


			while (openList.Count > 0 && !closedListPos.Contains(target))
			{
				current = openList[0];
				openList.RemoveAt(0);
				openListPos.Remove(current.Position);
				closedListPos.Add(current.Position);
				
				if (current.Steps > 15 || current.Steps + current.Distance / 2 > maxLength)
					continue;

				for (var x = -1; x <= 1; x++)
				{
					for (var y = -1; y <= 1; y++)
					{
						if (x == 0 && y == 0)
							continue;

						var np = current.Position;
						np.X += x;
						np.Y += y;

						if (np.X < 0 || np.Y < 0 || np.X >= walkData.Width || np.Y >= walkData.Height)
							continue;
						
						if (closedListPos.Contains(np) || openListPos.Contains(np))
							continue;

						if (!walkData.IsCellWalkable(np))
							continue;

						if (x == -1 && y == -1)
							if (!walkData.IsCellWalkable(current.Position.X - 1, current.Position.Y) ||
								!walkData.IsCellWalkable(current.Position.X, current.Position.Y - 1))
								continue;

						if (x == -1 && y == 1)
							if (!walkData.IsCellWalkable(current.Position.X - 1, current.Position.Y) ||
								!walkData.IsCellWalkable(current.Position.X, current.Position.Y + 1))
								continue;

						if (x == 1 && y == -1)
							if (!walkData.IsCellWalkable(current.Position.X + 1, current.Position.Y) ||
								!walkData.IsCellWalkable(current.Position.X, current.Position.Y - 1))
								continue;

						if (x == 1 && y == 1)
							if (!walkData.IsCellWalkable(current.Position.X + 1, current.Position.Y) ||
								!walkData.IsCellWalkable(current.Position.X, current.Position.Y + 1))
								continue;
						
						if (np == target)
						{
							return NextPathNode(current, np, 0);
						}

						openList.Add(NextPathNode(current, np, CalcDistance(np, target)));
						openListPos.Add(np);
						closedListPos.Add(np);

						openList.Sort((a, b) => a.F.CompareTo(b.F));
					}
				}

			}

			return null;
		}

		private static PathNode MakePath(MapWalkData walkData, Position start, Position target, int maxDistance)
		{
			if (!walkData.IsCellWalkable(target))
				return null;

			var path = BuildPath(walkData, start, target, maxDistance);

			openList.Clear();
			openListPos.Clear();
			closedListPos.Clear();

			if (path == null)
			{
				return null;
			}

			return path;
		}

		public static int GetPathWithInitialStep(MapWalkData walkData, Position start, Position initial, Position target, Position[] pathOut)
		{
			var path = MakePath(walkData, initial, target, MaxDistance - 1);
			if (path == null)
				return 0;

			var steps = path.Steps + 1;

			if (path.Steps >= pathOut.Length)
				ServerLogger.LogWarning($"Whoa! This isn't good. Steps is {path.Steps} but the array is {pathOut.Length}");

			while (path != null)
			{
				pathOut[path.Steps + 1] = path.Position;
				path = path.Parent;
			}

			pathOut[0] = start;

			return steps + 1; //add initial first step
		}

		public static int GetPath(MapWalkData walkData, Position start, Position target, Position[] pathOut)
		{
			var path = MakePath(walkData, start, target, MaxDistance);
			if (path == null)
				return 0;

			var steps = path.Steps + 1;

			if (path.Steps >= pathOut.Length)
				ServerLogger.LogWarning($"Whoa! This isn't good. Steps is {path.Steps} but the array is {pathOut.Length}");

			while (path != null)
			{
				pathOut[path.Steps] = path.Position;
				path = path.Parent;
			}

			return steps;
		}
	}
}
