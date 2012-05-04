using System.Collections.Generic;
using System.Drawing;
using SlimDX;

namespace SubdivisionRenderer
{
	class Lighting
	{
		public PhongParameters PhongParameters { get; set; }
		public Color AmbientLightColor { get; set; }
		public List<Light> Lights { get; set; }

		public Lighting()
		{
			AmbientLightColor = Color.White;
			
			Lights = 
				new List<Light> {
					new Light { Color = Color.White, Direction = Vector3.Normalize(new Vector3(1.5f, 1.5f, 2f)) },
					new Light { Color = Color.Gray, Direction = Vector3.Normalize(new Vector3(-1.5f, -1.5f, 2f)) }
				};

			PhongParameters = 
				new PhongParameters {
					Ambient = 0.1f,
					Diffuse = 0.5f,
					Specular = 0.5f,
					Shininess = 40f
				};
		}
	}
}
