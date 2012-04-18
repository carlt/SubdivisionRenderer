using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SubdivisionRenderer
{
	static class AccConstantsTable
	{
		private static float[,,] _tanM;
		public static float[,,] TanM {
			get { if (_tanM == null) FillTables(); return _tanM; }
		}

		private static float[,] _ci;
		public static float[,] Ci
		{
			get { if (_ci == null) FillTables(); return _ci; }
		}

		private static void FillTables()
		{
			_tanM = new float[Model.MaxValence, 64, 4];
			_ci = new float[16, 4];

			foreach (var v in Enumerable.Range(0, Model.MaxValence))
			{
				var cosPiV = Math.Cos(Math.PI / v);
				var vSqrt = (v * Math.Sqrt(4d + cosPiV * cosPiV));

				foreach (var i in Enumerable.Range(0, 32))
				{
					// alpha i
					_tanM[v, i * 2, 0] = (float) (((1f / v) + cosPiV / vSqrt) * Math.Cos((2 * Math.PI * i) / v));
					
					// beta i
					_tanM[v, i * 2 + 1, 0] = (float) ((1f / vSqrt) * Math.Cos(2 * Math.PI * i + Math.PI));
				}

				_ci[v, 0] = (float) (Math.Cos(2d * Math.PI) / (v + 3f));
			}
		}

		public static int SizeInBytes()
		{
			return (Model.MaxValence*64 + 16) * Marshal.SizeOf(typeof (float)) * 4;
		}
	}
}
