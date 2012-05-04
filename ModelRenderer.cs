using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;

namespace SubdivisionRenderer
{
	class ModelRenderer : IDisposable
	{
		private readonly Device _device;

		private Model _model;
		public Model Model {
			get { return _model; } 
			set { _model = value; SetupBuffers(); }
		}

		private Effect _shaderEffect;
		private EffectPass _shaderEffectPass;

		public RenderParameters RenderParameters { get; set; }
		
		private EffectMatrixVariable _world;
		private EffectMatrixVariable _worldViewProjection;
		
		private EffectScalarVariable _tessFactor;
		private EffectScalarVariable _enableTexture;
		private EffectScalarVariable _enableNormals;
		private EffectScalarVariable _flatShading;

		private EffectVectorVariable _phongParameters;
		private EffectVectorVariable _ambientLightColor;
		private EffectVectorVariable _lightColor;
		private EffectVectorVariable _lightDirection;
		private EffectVectorVariable _light2Color;
		private EffectVectorVariable _light2Direction;
		private EffectVectorVariable _cameraPosition;

		private EffectResourceVariable _textureMap;
		private EffectResourceVariable _valencePrefixResource;
		private ShaderResourceView _textureView;
		private ShaderResourceView _valencePrefixView;

		private Buffer _vertexBuffer;
		private Buffer _indexBuffer;
		private Buffer _accIndexBuffer;
		private Buffer _valencePrefixBuffer;
		

		public ModelRenderer(Device device, Model model)
		{
			_device = device;
			Model = model;

			RenderParameters = new RenderParameters();

			CompileShaders(Path.Combine("Textures", "Texture.dds"), Path.Combine("Shaders", "Tessellation.hlsl"));
		}

		public void Render()
		{
			SetRenderParameters();

			if (RenderParameters.ShaderMode == ShaderMode.Acc)
			{
				_device.ImmediateContext.InputAssembler.SetIndexBuffer(_accIndexBuffer, Format.R32_UInt, 0);
				_device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith32ControlPoints;
			}
			else
			{
				_device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
				_device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
			}

			var drawCount = RenderParameters.ShaderMode == ShaderMode.Acc ? Model.AccPatches.Count * 32 : Model.Faces.Count * 4;

			// Draw
			ChangeWireFrame(false);
			_shaderEffectPass.Apply(_device.ImmediateContext);
			_device.ImmediateContext.DrawIndexed(drawCount, 0, 0);

			if (!RenderParameters.WireFrame) return;
			
			ChangeWireFrame(true);
			_shaderEffect.GetTechniqueByIndex((int) RenderParameters.ShaderMode).GetPassByIndex(1).Apply(_device.ImmediateContext);
			_device.ImmediateContext.DrawIndexed(drawCount, 0, 0);
		}

		private void SetRenderParameters()
		{
			_shaderEffectPass = _shaderEffect.GetTechniqueByIndex((int) RenderParameters.ShaderMode).GetPassByIndex(0);

			// Update Camera
			_world.SetMatrix(RenderParameters.Camera.World());
			_worldViewProjection.SetMatrix(RenderParameters.Camera.WorldViewProjection());

			// Set Texture
			_textureMap.SetResource(_textureView);
			_enableTexture.Set(RenderParameters.Textured);

			// Set Tesselation
			_tessFactor.Set(RenderParameters.TessellationFactor);

			// Set Lighting
			_phongParameters.Set(RenderParameters.Lighting.PhongParameters.AsVector());
			_lightColor.Set(RenderParameters.Lighting.Lights[0].Color);
			_lightDirection.Set(RenderParameters.Lighting.Lights[0].Direction);
			_light2Color.Set(RenderParameters.Lighting.Lights[1].Color);
			_light2Direction.Set(RenderParameters.Lighting.Lights[1].Direction);
			_ambientLightColor.Set(RenderParameters.Lighting.AmbientLightColor);
			_cameraPosition.Set(RenderParameters.Camera.Eye);
			
			// Set Options
			_flatShading.Set(RenderParameters.FlatShading);
			_enableNormals.Set(RenderParameters.DisplayNormals);

			// Valences and Prefixes for ACC
			_valencePrefixResource.SetResource(_valencePrefixView);
		}

		private void CompileShaders(string texturePath, string shaderPath)
		{
			_shaderEffect = new Effect(_device, ShaderBytecode.CompileFromFile(shaderPath, "fx_5_0", ShaderFlags.None, EffectFlags.None));
			_shaderEffectPass = _shaderEffect.GetTechniqueByIndex((int) RenderParameters.ShaderMode).GetPassByIndex(0);
			_device.ImmediateContext.InputAssembler.InputLayout = new InputLayout(_device, _shaderEffectPass.Description.Signature, VertexShaderInput.InputLayout);

			// Setup Global Variables
			_world = _shaderEffect.GetVariableByName("World").AsMatrix();
			_worldViewProjection = _shaderEffect.GetVariableByName("WorldViewProj").AsMatrix();
			_tessFactor = _shaderEffect.GetVariableByName("TessFactor").AsScalar();
			_textureMap = _shaderEffect.GetVariableByName("Texture").AsResource();
			_enableTexture = _shaderEffect.GetVariableByName("EnableTexture").AsScalar();
			_enableNormals = _shaderEffect.GetVariableByName("Normals").AsScalar();
			_textureView = new ShaderResourceView(_device, Texture2D.FromFile(_device, texturePath));

			// Setup Lighting Variables
			_phongParameters = _shaderEffect.GetVariableByName("AmbSpecDiffShini").AsVector();
			_lightColor = _shaderEffect.GetVariableByName("LightColor").AsVector();
			_lightDirection = _shaderEffect.GetVariableByName("LightDirection").AsVector();
			_light2Color = _shaderEffect.GetVariableByName("Light2Color").AsVector();
			_light2Direction = _shaderEffect.GetVariableByName("Light2Direction").AsVector();
			_ambientLightColor = _shaderEffect.GetVariableByName("AmbientLight").AsVector();
			_cameraPosition = _shaderEffect.GetVariableByName("Eye").AsVector();
			_flatShading = _shaderEffect.GetVariableByName("FlatShading").AsScalar();

			// Valences and Prefixes for ACC
			_valencePrefixResource = _shaderEffect.GetVariableByName("ValencePrefixBuffer").AsResource();
		}


		private void SetupBuffers()
		{
			if (Model.Faces.Count == 0) return;

			var allPoints = Model.Faces.SelectMany(f => f.Points).ToList();

			var indexBufferContents = Model.Faces.SelectMany(face => face.Points).Select(point => Model.FindPointIndex(point)).ToList();
			var vertexBufferContents = allPoints.Select(
				point => new VertexShaderInput {
					Position = Model.Vertices[point.PositionIndex],
					Normal = Model.Normals[point.NormalIndex],
					TexCoord = Model.Textures.Count == 0 ? new Vector2(0.5f, 0.5f) : Model.Textures[point.TextureIndex]
				}).ToList();

			var vertexStream = new DataStream(vertexBufferContents.Count * VertexShaderInput.SizeInBytes, true, true);

			foreach (var vertexInfo in vertexBufferContents)
				vertexStream.Write(vertexInfo);

			vertexStream.Position = 0;

			_vertexBuffer = new Buffer(_device, vertexStream,
				new BufferDescription {
					BindFlags = BindFlags.VertexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = (int) vertexStream.Length,
					Usage = ResourceUsage.Default
				});

			vertexStream.Dispose();

			_device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, VertexShaderInput.SizeInBytes, 0));

			var indexStream = new DataStream(indexBufferContents.Count * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var indexInfo in indexBufferContents)
				indexStream.Write(indexInfo);

			indexStream.Position = 0;

			_indexBuffer = new Buffer(_device, indexStream,
				new BufferDescription {
					BindFlags = BindFlags.IndexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = (int) indexStream.Length,
					Usage = ResourceUsage.Default
				});

			indexStream.Dispose();

			SetupAccBuffers();
		}

		private void SetupAccBuffers()
		{
			var accIndexBuffer =
				Model.AccPatches
					.SelectMany(p => p.Points.Pad(32))
					.Select(p => Model.FindPointIndex(p))
					.ToList();

			var indexStream = new DataStream(accIndexBuffer.Count * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var indexInfo in accIndexBuffer)
				indexStream.Write(indexInfo);

			indexStream.Position = 0;

			_accIndexBuffer = new Buffer(_device, indexStream,
				new BufferDescription {
					BindFlags = BindFlags.IndexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = (int) indexStream.Length,
					Usage = ResourceUsage.Default
				});

			indexStream.Dispose();

			var valencePrefixStream = new DataStream(Model.AccPatches.Count * 8 * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var extraordinaryPatch in Model.AccPatches)
			{
				foreach (var valence in extraordinaryPatch.Valences)
					valencePrefixStream.Write(Convert.ToUInt32(valence));

				foreach (var prefix in extraordinaryPatch.Prefixes)
					valencePrefixStream.Write(Convert.ToUInt32(prefix));
			}

			valencePrefixStream.Position = 0;

			_valencePrefixBuffer = new Buffer(_device, valencePrefixStream,
				new BufferDescription {
					BindFlags = BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = (int) valencePrefixStream.Length,
					Usage = ResourceUsage.Default
				});

			valencePrefixStream.Dispose();

			_valencePrefixView = new ShaderResourceView(_device, _valencePrefixBuffer,
				new ShaderResourceViewDescription {
					Format = Format.R32G32B32A32_UInt,
					Dimension = ShaderResourceViewDimension.Buffer,
					ElementOffset = 0,
					FirstElement = 0,
					ElementWidth = Model.AccPatches.Count * 2,
					ElementCount = Model.AccPatches.Count * 2,
				});
		}

		private void ChangeWireFrame(bool enabled)
		{
			_device.ImmediateContext.Rasterizer.State = RasterizerState.FromDescription(_device, 
				new RasterizerStateDescription {
					CullMode = CullMode.Back,
					FillMode = enabled ? FillMode.Wireframe : FillMode.Solid,
					IsMultisampleEnabled = true
				});
		}

		public void Dispose()
		{
			if (_shaderEffect != null)
				_shaderEffect.Dispose();
			if (_textureView != null)
				_textureView.Dispose();
			
			if (_indexBuffer != null)
				_indexBuffer.Dispose();
			if (_vertexBuffer != null)
				_vertexBuffer.Dispose();
			if (_accIndexBuffer != null)
				_accIndexBuffer.Dispose();
			if (_valencePrefixBuffer != null)
				_valencePrefixBuffer.Dispose();

			if (_valencePrefixView != null)
				_valencePrefixView.Dispose();
		}
	}
}
