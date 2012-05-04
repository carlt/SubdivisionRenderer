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
	}
}
