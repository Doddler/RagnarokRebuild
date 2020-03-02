using System;
using RebuildData.Shared.Enum;

namespace RebuildData.Shared.Data
{
	public static class Directions
	{
		public static Direction GetDirectionFromCoordinates(int x, int y)
		{
			if (x < 0 && y < 0) return Direction.SouthWest;
			if (x < 0 && y == 0) return Direction.West;
			if (x < 0 && y > 0) return Direction.NorthWest;
			if (x == 0 && y < 0) return Direction.South;
			//if (x == 0 && y == 0) return Direction.None;
			if (x == 0 && y > 0) return Direction.North;
			if (x > 0 && y < 0) return Direction.SouthEast;
			if (x > 0 && y == 0) return Direction.East;
			if (x > 0 && y > 0) return Direction.NorthEast;
			return Direction.None;
		}

		public static Direction GetReverseDirection(Direction d)
		{
			switch (d)
			{
				case Direction.NorthWest: return Direction.SouthEast;
				case Direction.North: return Direction.South;
				case Direction.NorthEast: return Direction.SouthWest;
				case Direction.West: return Direction.East;
				case Direction.East: return Direction.West;
				case Direction.SouthWest: return Direction.NorthEast;
				case Direction.South: return Direction.North;
				case Direction.SouthEast: return Direction.NorthWest;
				case Direction.None: return Direction.None;
			}

			throw new Exception("Invalid Direction: " + ((int)d).ToString());
		}
	}
}
