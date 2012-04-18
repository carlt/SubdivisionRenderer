using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace SubdivisionRenderer
{
	static class Lighting
	{
		public static PhongParameters PhongParameters =
			new PhongParameters {
					Ambient = 0.1f,
					Diffuse = 0.5f,
					Specular = 0.5f,
					Shininess = 30f
				};

		public static Vector4 AmbientLightColor = Color.White.ToVector();

		public static Vector4 DirectionalLightColor = Color.White.ToVector();
		public static Vector3 DirectionalLightDirection = Vector3.Normalize(new Vector3(1.5f, 1.5f, 2f));
		
		public static Vector4 DirectionalLight2Color = Color.Gray.ToVector();
		public static Vector3 DirectionalLight2Direction = Vector3.Normalize(new Vector3(-1.5f, -1.5f, 2f));
	}

	[StructLayout(LayoutKind.Sequential)]
	struct PhongParameters
	{
		public float Ambient;
		public float Specular;
		public float Diffuse;
		public float Shininess;

		public Vector4 AsVector()
		{
			return new Vector4(Ambient, Specular, Diffuse, Shininess);
		}
	}
}
