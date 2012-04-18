using System.Collections.Generic;

namespace SubdivisionRenderer
{
	public class Face
	{
		public readonly List<Point> Points; 

		public Face()
		{
			Points = new List<Point>();
		}
	}
}