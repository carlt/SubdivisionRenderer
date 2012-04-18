using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace SubdivisionRenderer
{
	[StructLayout(LayoutKind.Sequential)]
	struct VertexShaderInput
	{
		public Vector4 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;

		public static readonly int SizeInBytes = Marshal.SizeOf(typeof(VertexShaderInput));

		public static readonly InputElement[] InputLayout = {
			new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
			new InputElement("NORMAL", 0, Format.R32G32B32_Float, Vector4.SizeInBytes, 0),
			new InputElement("TEXCOORD", 0, Format.R32G32_Float, Vector4.SizeInBytes + Vector3.SizeInBytes, 0) 
		};
	}
}
