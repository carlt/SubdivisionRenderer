using SlimDX;

namespace SubdivisionRenderer
{
	class PhongParameters
	{
		public float Ambient { get; set; }
		public float Specular { get; set; }
		public float Diffuse { get; set; }
		public float Shininess { get; set; }

		public Vector4 AsVector()
		{
			return new Vector4(Ambient, Specular, Diffuse, Shininess);
		}
	}
}