using System.Collections.Generic;
using System.Linq;

namespace SubdivisionRenderer
{
	public class Face
	{
		public List<Point> Points; 

		public Face()
		{
			Points = new List<Point>();
		}

		public bool HasEdge(Point a, Point b)
		{
			return a.PositionIndex == Points[0].PositionIndex && b.PositionIndex == Points[1].PositionIndex || 
				   a.PositionIndex == Points[1].PositionIndex && b.PositionIndex == Points[0].PositionIndex ||
				   a.PositionIndex == Points[1].PositionIndex && b.PositionIndex == Points[2].PositionIndex || 
				   a.PositionIndex == Points[2].PositionIndex && b.PositionIndex == Points[1].PositionIndex ||
				   a.PositionIndex == Points[2].PositionIndex && b.PositionIndex == Points[3].PositionIndex || 
				   a.PositionIndex == Points[3].PositionIndex && b.PositionIndex == Points[2].PositionIndex ||
				   a.PositionIndex == Points[3].PositionIndex && b.PositionIndex == Points[0].PositionIndex || 
				   a.PositionIndex == Points[0].PositionIndex && b.PositionIndex == Points[3].PositionIndex;
		}

		public bool SharesPoints(Face f)
		{
			return f.Points.Any(p1 => Points.Any(p1.SamePosition));
		}

		public Point? SharedPoint(Face f)
		{
			return Points.FirstOrDefault(p => f.Points.Any(fp => fp.PositionIndex == p.PositionIndex));
		}
	}

	public class FaceComparer : IEqualityComparer<Face>
	{
		public bool Equals(Face x, Face y)
		{
			return x.Points[0].PositionIndex == y.Points[0].PositionIndex &&
				   x.Points[1].PositionIndex == y.Points[1].PositionIndex &&
				   x.Points[2].PositionIndex == y.Points[2].PositionIndex &&
				   x.Points[3].PositionIndex == y.Points[3].PositionIndex;
		}

		public int GetHashCode(Face f)
		{
			return f.Points[0].GetHashCode() ^ f.Points[1].GetHashCode() ^ 
				   f.Points[2].GetHashCode() ^ f.Points[3].GetHashCode();
		}
	}
}