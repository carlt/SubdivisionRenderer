using System.Collections.Generic;
using System.Linq;

namespace SubdivisionRenderer
{
	public struct AccPatch
	{
		private const int MaxPoints = 32;
		private const int MaxValence = 16;

		public List<Point> Points;
		public List<int> Valences;
		public List<int> Prefixes;

		public static bool operator ==(AccPatch a, AccPatch b)
		{
			return a.Points[0].PositionIndex == b.Points[0].PositionIndex &&
				   a.Points[1].PositionIndex == b.Points[1].PositionIndex &&
				   a.Points[2].PositionIndex == b.Points[2].PositionIndex &&
				   a.Points[3].PositionIndex == b.Points[3].PositionIndex;
		}

		public static bool operator !=(AccPatch a, AccPatch b)
		{
			return !(a == b);
		}
	}
}
