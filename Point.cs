using System.Collections.Generic;

namespace SubdivisionRenderer
{
	public struct Point
	{
		public int PositionIndex;
		public int NormalIndex;
		public int TextureIndex;

		public static bool operator ==(Point x, Point y)
		{
			return x.PositionIndex == y.PositionIndex
				&& x.NormalIndex == y.NormalIndex
				&& x.TextureIndex == y.TextureIndex;
		}

		public static bool operator !=(Point x, Point y)
		{
			return !(x == y);
		}

		public bool SamePosition(Point x)
		{
			return x.PositionIndex == PositionIndex;
		}
	}

	public class PointPositionComparer : IEqualityComparer<Point>
	{
		public bool Equals(Point x, Point y)
		{
			return x.PositionIndex == y.PositionIndex;
		}

		public int GetHashCode(Point obj)
		{
			return obj.PositionIndex;
		}
	}
}
