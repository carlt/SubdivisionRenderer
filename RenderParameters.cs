namespace SubdivisionRenderer
{
	class RenderParameters
	{
		public bool WireFrame { get; set; }
		public float TessellationFactor { get; set; }
		public float TessellationStep { get; set; }
		public bool Textured { get; set; }
		public bool FlatShading { get; set; }
		public bool DisplayNormals { get; set; }
		public long TicksLastFrame { get; set; }
		public float FrameRate { get; set; }
		public ShaderMode ShaderMode { get; set; }
		public Camera Camera { get; set; }
		public Lighting Lighting { get; set; }

		public RenderParameters()
		{
			WireFrame = false;
			TessellationFactor = 1f;
			TessellationStep = 0.25f;
			Textured = false;
			FlatShading = false;
			DisplayNormals = false;
			TicksLastFrame = 200;
			FrameRate = 100f;
			Camera = new Camera();
			Lighting = new Lighting();
		}
	}
}