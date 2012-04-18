using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using SlimDX;

namespace SubdivisionRenderer
{
	public class Model 
	{
		public const int MaxValence = 16;

		public List<Vector4> Vertices { get; private set; }
		public List<Vector3> Normals { get; private set; }
		public List<Vector2> Textures { get; private set; }
		public List<Face> Faces { get; private set; }

		public readonly List<AccPatch> AccPatches = new List<AccPatch>();

		public Model(string path)
		{
			Vertices = new List<Vector4>();
			Normals = new List<Vector3>();
			Textures = new List<Vector2>();
			Faces = new List<Face>();

			Load(path);
			SetupAccPatches();
		}

		private void Load(string path)
		{
			var numberRegex = new Regex(@"-?[\d]+(?:\.[\d]*)?(e[-|+]?\d+)?", RegexOptions.IgnoreCase);
			var faceRegex = new Regex(@"(\d+)/([\d]*)/([\d]*)", RegexOptions.IgnoreCase);

			Vertices.Clear();
			Normals.Clear();
			Textures.Clear();
			Faces.Clear();

			foreach (var line in File.ReadLines(path).Where(line => numberRegex.IsMatch(line)))
			{
				var matches = numberRegex.Matches(line);
				
				if (line.StartsWith("v "))
				{
					if (matches.Count != 3)
						throw new FileLoadException("Error parsing vertices.");

					Vertices.Add(
						new Vector4 {
							X = float.Parse(matches[0].Value),
							Y = float.Parse(matches[1].Value),
							Z = float.Parse(matches[2].Value),
							W = 1f
						});
				}
				else if (line.StartsWith("vt "))
				{
					if (matches.Count != 2)
						throw new FileLoadException("Error parsing textures.");

					Textures.Add(
						new Vector2 {
							X = float.Parse(matches[0].Value),
							Y = float.Parse(matches[1].Value)
						});
				} 
				else if (line.StartsWith("vn "))
				{
					if (matches.Count != 3)
						throw new FileLoadException("Error parsing normals.");

					Normals.Add(
						Vector3.Normalize(new Vector3 {
							X = float.Parse(matches[0].Value),
							Y = float.Parse(matches[1].Value),
							Z = float.Parse(matches[2].Value)
						}));
				}
				else if (line.StartsWith("f "))
				{
					matches = faceRegex.Matches(line);

					var face = new Face();
					foreach (var i in Enumerable.Range(0, 4))
					{
						face.Points.Add(
							new Point {
								PositionIndex = int.Parse(matches[i].Groups[1].Value) - 1,
								TextureIndex = matches[i].Groups[2].Value == String.Empty ? 0 : int.Parse(matches[i].Groups[2].Value) - 1,
								NormalIndex = int.Parse(matches[i].Groups[3].Value) - 1
							});
					}

					AccPatches.Add(
						new AccPatch {
							Points = new List<Point>(face.Points),
							Prefixes = new List<int>(),
							Valences = new List<int>()
						});

					Faces.Add(face);
				}
		   } 
		}

		private void SetupAccPatches()
		{
			foreach (var patch in AccPatches)
				ConditionPatch(patch);
		}
		
		private void ConditionPatch(AccPatch patch)
		{
			var neighborPoints = new List<Point>();
			var others = new Point[3];

			var v0 = patch.Points[0];
			var v1 = patch.Points[1];
			var v2 = patch.Points[2];
			var v3 = patch.Points[3];

			patch.Valences.Clear();
			patch.Prefixes.Clear();

			// v0
			others[0] = v1;
			others[1] = v2;
			others[2] = v3;

			patch.Valences.Add(ConditionPoint(v0, others, ref neighborPoints));
			patch.Prefixes.Add(neighborPoints.Count + 4);

			// v1
			others[0] = v2;
			others[1] = v3;
			others[2] = v0;
			patch.Valences.Add(ConditionPoint(v1, others, ref neighborPoints));
			patch.Prefixes.Add(neighborPoints.Count + 4);

			// v2
			others[0] = v3;
			others[1] = v0;
			others[2] = v1;
			patch.Valences.Add(ConditionPoint(v2, others, ref neighborPoints));
			patch.Prefixes.Add(neighborPoints.Count + 4);

			// v3
			others[0] = v0;
			others[1] = v1;
			others[2] = v2;
			patch.Valences.Add(ConditionPoint(v3, others, ref neighborPoints));
			patch.Prefixes.Add(neighborPoints.Count + 4);

			patch.Points.AddRange(neighborPoints);
		}

		private int ConditionPoint(Point p, IList<Point> others, ref List<Point> neighborPoints)
		{
			var startNeighborPoints = neighborPoints.Count;

			var currentQuad = FindQuadWithPointsAbNotC(p, others[2], others[0]);
			var endQuad = FindQuadWithPointsAbNotC(p, others[0], others[2]);

			var farEdgePoint = currentQuad.Points.FindIndex(point => point.PositionIndex == others[2].PositionIndex);
			var offEdgePoint = currentQuad.Points[(farEdgePoint + 1) % 4];
			var fanPoint     = currentQuad.Points[(farEdgePoint + 2) % 4];

			neighborPoints.Add(fanPoint);

			currentQuad = FindQuadWithPointsAbNotC(p, fanPoint, offEdgePoint);

			while (currentQuad != endQuad)
			{
				farEdgePoint = currentQuad.Points.FindIndex(point => point.PositionIndex == fanPoint.PositionIndex);
				offEdgePoint = currentQuad.Points[(farEdgePoint + 1) % 4];
				fanPoint	 = currentQuad.Points[(farEdgePoint + 2) % 4];

				neighborPoints.Add(offEdgePoint);
				neighborPoints.Add(fanPoint);

				currentQuad = FindQuadWithPointsAbNotC(p, fanPoint, offEdgePoint);
			}

			var endNeighborPoints = neighborPoints.Count - startNeighborPoints;
			return (endNeighborPoints + 5) / 2;
		}

		private AccPatch FindQuadWithPointsAbNotC(Point a, Point b, Point c)
		{
			return AccPatches.First(
				p => p.Points.Take(4).Any(pa => pa.PositionIndex == a.PositionIndex) &&
					 p.Points.Take(4).Any(pb => pb.PositionIndex == b.PositionIndex) &&
					!p.Points.Take(4).Any(pc => pc.PositionIndex == c.PositionIndex));
		}
	}
}
