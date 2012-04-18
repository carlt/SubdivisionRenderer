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

		public AccPatch(List<Point> points, List<int> valences, List<int> prefixes)
		{
			Points = points;
			Valences = valences;
			Prefixes = prefixes;
		}

		public static bool operator ==(AccPatch a, AccPatch b)
		{
			return a.Points.Select(p => p.PositionIndex).SequenceEqual(b.Points.Select(p => p.PositionIndex));
		}

		public static bool operator !=(AccPatch a, AccPatch b)
		{
			return !(a == b);
		}
	}
}
