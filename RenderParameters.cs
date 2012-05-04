namespace SubdivisionRenderer
{
	class RenderParameters
	{
		public bool WireFrame { get; set; }
		public float TessellationFactor { get; set; }
		public bool Textured { get; set; }
		public bool FlatShading { get; set; }
		public bool DisplayNormals { get; set; }

		public ShaderMode ShaderMode { get; set; }
		public Camera Camera { get; set; }
		public Lighting Lighting { get; set; }

		public RenderParameters()
		{
			WireFrame = false;
			TessellationFactor = 1f;
			Textured = false;
			FlatShading = false;
			DisplayNormals = false;
			Camera = new Camera();
			Lighting = new Lighting();
		}
	}
}