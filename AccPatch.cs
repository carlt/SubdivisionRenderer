using System.Collections.Generic;
using System.Linq;

namespace SubdivisionRenderer
{
	public class AccPatch
	{
		public List<Point> Points { get; set; }
		public List<int> Valences { get; set; }
		public List<int> Prefixes { get; set; }

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
