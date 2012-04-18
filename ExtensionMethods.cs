using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SlimDX;

namespace SubdivisionRenderer
{
	static class  ExtensionMethods 
	{
		public static Vector4 ToVector(this Color color)
		{
			return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}

		public static List<T> Pad<T>(this List<T> accPatchList, int finalSize)
		{
			while (accPatchList.Count < finalSize)
			{
				accPatchList.Add(accPatchList.Last());
			}

			return accPatchList;
		}
	}
}
