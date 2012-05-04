using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SlimDX;

namespace SubdivisionRenderer
{
	static class  ExtensionMethods 
	{
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
