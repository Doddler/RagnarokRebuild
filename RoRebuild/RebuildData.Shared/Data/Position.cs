using System;
using RebuildData.Shared.Enum;

namespace RebuildData.Shared.Data
{
	public struct Position : IEquatable<Position>
	{
		public int X;
		public int Y;

		public int Width => X;
		public int Height => Y;

		public static Position Zero => new Position(0, 0);

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}
		
		public Position(Position src)
		{
			X = src.X;
			Y = src.Y;
		}
		
		public Position StepTowards(Position dest)
		{
			var pos = new Position(this);

			if (pos.X < dest.X)
				pos.X++;
			if (pos.X > dest.X)
				pos.X--;
			if (pos.Y < dest.Y)
				pos.Y++;
			if (pos.Y > dest.Y)
				pos.Y--;

			return pos;
		}

		public int SquareDistance(Position dest)
		{
			var xdist = Math.Abs(X - dest.X);
			var ydist = Math.Abs(Y - dest.Y);
			return Math.Max(xdist, ydist);
		}

		public bool InRange(Position target, int distance)
		{
			return target.X >= X - distance && target.X <= X + distance && target.Y >= Y - distance && target.Y <= Y + distance;
		}

		public static Position RandomPosition(Area area)
		{
			return RandomPosition(area.MinX, area.MinY, area.MaxX, area.MaxY);
		}

		public static Position RandomPosition(int maxx, int maxy)
		{
			var x = GameRandom.Next(0, maxx);
			var y = GameRandom.Next(0, maxy);
			return new Position(x, y);
		}

		public static Position RandomPosition(int minx, int miny, int maxx, int maxy)
		{
			var x = GameRandom.Next(minx, maxx);
			var y = GameRandom.Next(miny, maxy);
			return new Position(x, y);
		}


		public float GetDirection()
		{
			var rad = Math.Atan2(X, Y);
			
			var deg = rad * (180 / Math.PI);
			return (float)deg;

			//return Direction.South;
		}

		public Direction GetDirectionForOffset()
		{
#if DEBUG
			//sanity check
			if(X < -1 || X > 1 || Y < -1 || Y > 1)
				throw new Exception("Get Direction provided invalid inputs!");
#endif

			if (X == -1 && Y == -1) return Direction.SouthWest;
			if (X == -1 && Y == 0) return Direction.West;
			if (X == -1 && Y == 1) return Direction.NorthWest;
			if (X == 0 && Y == 1) return Direction.North;
			if (X == 1 && Y == 1) return Direction.NorthEast;
			if (X == 1 && Y == 0) return Direction.East;
			if (X == 1 && Y == -1) return Direction.SouthEast;
			if (X == 0 && Y == -1) return Direction.South;

			return Direction.South;
		}

		public static bool operator ==(Position src, Position dest)
		{
			return src.X == dest.X && src.Y == dest.Y;
		}

		public static bool operator !=(Position src, Position dest)
		{
			return src.X != dest.X || src.Y != dest.Y;
		}

		public bool Equals(Position other)
		{
			return X == other.X && Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			return obj is Position other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (X * 397) ^ Y;
			}
		}

		public override string ToString()
		{
			return $"{X.ToString()},{Y.ToString()}";
		}

		public static Position operator - (Position left, Position right) => new Position(left.X - right.X, left.Y - right.Y);
		public static Position operator + (Position left, Position right) => new Position(left.X + right.X, left.Y + right.Y);
		public static Position operator / (Position left, int right) => new Position(left.X / right, left.Y / right);
	}
}
