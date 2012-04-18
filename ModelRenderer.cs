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

		private InputLayout _inputLayout;
		private Effect _shaderEffect;
		private EffectPass _shaderEffectPass;
		private ShaderMode _currentShader;

		private EffectMatrixVariable _world;
		private EffectMatrixVariable _worldViewProjection;
		private EffectScalarVariable _tessFactor;
		private EffectResourceVariable _textureMap;
		private EffectScalarVariable _enableTexture;
		private EffectScalarVariable _enableWireframe;
		private ShaderResourceView _textureView;

		private EffectVectorVariable _phongParameters;
		private EffectVectorVariable _ambientLightColor;
		private EffectVectorVariable _directionalLightColor;
		private EffectVectorVariable _directionalLightDirection;
		private EffectVectorVariable _directionalLight2Color;
		private EffectVectorVariable _directionalLight2Direction;
		private EffectVectorVariable _cameraPosition;
		private EffectScalarVariable _flatShading;

		private EffectResourceVariable _valencePrefixResource;
		private ShaderResourceView _valencePrefixView;

		private Buffer _vertexBuffer;
		private Buffer _indexBuffer;
		private Buffer _accIndexBuffer;
		private Buffer _valencePrefixBuffer;

		public ModelRenderer(Device device, Model model)
		{
			_device = device;
			Model = model;
			_currentShader = ShaderMode.Flat;
			CompileShaders(Path.Combine("Textures", "Texture.dds"), Path.Combine("Shaders", "Tessellation.hlsl"));
		}

		public void ChangeShader(ShaderMode shader)
		{
			_currentShader = shader;
			_shaderEffectPass = _shaderEffect.GetTechniqueByIndex((int) shader).GetPassByIndex(0);		
		}

		private void CompileShaders(string texturePath, string shaderPath)
		{
			_shaderEffect = new Effect(_device, ShaderBytecode.CompileFromFile(shaderPath, "fx_5_0", ShaderFlags.None, EffectFlags.None));
			_shaderEffectPass = _shaderEffect.GetTechniqueByIndex((int) _currentShader).GetPassByIndex(0);
			_inputLayout = new InputLayout(_device, _shaderEffectPass.Description.Signature, VertexShaderInput.InputLayout);

			// Setup Global Variables
			_world = _shaderEffect.GetVariableByName("World").AsMatrix();
			_worldViewProjection = _shaderEffect.GetVariableByName("WorldViewProj").AsMatrix();
			_tessFactor = _shaderEffect.GetVariableByName("TessFactor").AsScalar();
			_textureMap = _shaderEffect.GetVariableByName("Texture").AsResource();
			_enableTexture = _shaderEffect.GetVariableByName("EnableTexture").AsScalar();
			_enableWireframe = _shaderEffect.GetVariableByName("WireFrame").AsScalar();
			_textureView = new ShaderResourceView(_device, Texture2D.FromFile(_device, texturePath));

			// Setup Lighting Variables
			_phongParameters = _shaderEffect.GetVariableByName("AmbSpecDiffShini").AsVector();
			_directionalLightColor = _shaderEffect.GetVariableByName("LightColor").AsVector();
			_directionalLightDirection = _shaderEffect.GetVariableByName("LightDirection").AsVector();
			_directionalLight2Color = _shaderEffect.GetVariableByName("Light2Color").AsVector();
			_directionalLight2Direction = _shaderEffect.GetVariableByName("Light2Direction").AsVector();
			_ambientLightColor = _shaderEffect.GetVariableByName("AmbientLight").AsVector();
			_cameraPosition = _shaderEffect.GetVariableByName("Eye").AsVector();
			_flatShading = _shaderEffect.GetVariableByName("FlatShading").AsScalar();

			_valencePrefixResource = _shaderEffect.GetVariableByName("ValencePrefixBuffer").AsResource();
		}

		public void Render(RenderParameters parameters)
		{
			_device.ImmediateContext.InputAssembler.InputLayout = _inputLayout;
			_device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, VertexShaderInput.SizeInBytes, 0));

			if (_currentShader != ShaderMode.Acc)
			{
				_device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
				_device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;

				SetRenderParameters(parameters);

				// Draw
				_shaderEffectPass.Apply(_device.ImmediateContext);
				_device.ImmediateContext.DrawIndexed(Model.Faces.Count * 4, 0, 0);
			}
			else
			{
				_device.ImmediateContext.InputAssembler.SetIndexBuffer(_accIndexBuffer, Format.R32_UInt, 0);
				_device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith32ControlPoints;

				_valencePrefixResource.SetResource(_valencePrefixView);
				SetRenderParameters(parameters);

				// Draw
				_shaderEffectPass.Apply(_device.ImmediateContext);
				_device.ImmediateContext.DrawIndexed(Model.AccPatches.Count * 32, 0, 0);
			}
		}

		private void SetRenderParameters(RenderParameters parameters)
		{
			// Update Camera
			_world.SetMatrix(Camera.World());
			_worldViewProjection.SetMatrix(Camera.WorldViewProjection());

			// Set Texture
			_textureMap.SetResource(_textureView);
			_enableTexture.Set(parameters.Textured);

			// Set Tesselation
			_tessFactor.Set(parameters.TessellationFactor);

			// Set Lighting
			_phongParameters.Set(Lighting.PhongParameters.AsVector());
			_directionalLightColor.Set(Lighting.DirectionalLightColor);
			_directionalLightDirection.Set(Lighting.DirectionalLightDirection);
			_directionalLight2Color.Set(Lighting.DirectionalLight2Color);
			_directionalLight2Direction.Set(Lighting.DirectionalLight2Direction);
			_ambientLightColor.Set(Lighting.AmbientLightColor);
			_cameraPosition.Set(Camera.Eye);
			
			// Set Options
			_flatShading.Set(parameters.FlatShading);
			_enableWireframe.Set(parameters.WireFrame);
		}

		public string GetSubdivisionMode()
		{
			switch (_currentShader)
			{
				case ShaderMode.Flat:
					return "Basic";
				case ShaderMode.Phong:
					return "Phong";
				case ShaderMode.PnQuads:
					return "PN Quads";
				case ShaderMode.Acc:
					return "ACC";
				default:
					return "Unknown";
			}
		}

		private void SetupBuffers()
		{
			var allPoints = Model.Faces.SelectMany(f => f.Points).ToList();

			var indexBufferContents = Model.Faces.SelectMany(face => face.Points).Select(point => Convert.ToUInt32(allPoints.FindIndex(p => p == point))).ToList();
			var vertexBufferContents = allPoints.Select(
				point => new VertexShaderInput {
					Position = Model.Vertices[point.PositionIndex],
					Normal = Model.Normals[point.NormalIndex],
					TexCoord = Model.Textures.Count == 0 ? new Vector2(0.5f, 0.5f) : Model.Textures[point.TextureIndex]
				}).ToList();

			// Create normal VertexBuffer
			var stream = new DataStream(vertexBufferContents.Count * VertexShaderInput.SizeInBytes, true, true);

			foreach (var vertexInfo in vertexBufferContents)
				stream.Write(vertexInfo);

			stream.Position = 0;

			_vertexBuffer = new Buffer(_device, stream,
				new BufferDescription {
					BindFlags = BindFlags.VertexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = vertexBufferContents.Count * VertexShaderInput.SizeInBytes,
					Usage = ResourceUsage.Default
				});

			stream.Dispose();

			// Create normal IndexBuffer
			stream = new DataStream(indexBufferContents.Count * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var indexInfo in indexBufferContents)
				stream.Write(indexInfo);

			stream.Position = 0;

			_indexBuffer = new Buffer(_device, stream,
				new BufferDescription {
					BindFlags = BindFlags.IndexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = indexBufferContents.Count * Marshal.SizeOf(typeof(uint)),
					Usage = ResourceUsage.Default
				});

			stream.Dispose();

			SetupAccBuffers();
		}

		private void SetupAccBuffers()
		{
			var allPoints = Model.Faces.SelectMany(f => f.Points).ToList();

			var accIndexBuffer =
				Model.AccPatches
					.SelectMany(p => p.Points.Pad(32))
					.Select(p => Convert.ToUInt32(allPoints.FindIndex(point => point == p)))
					.ToList();

			// Acc index buffer

			var stream = new DataStream(accIndexBuffer.Count * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var indexInfo in accIndexBuffer)
				stream.Write(indexInfo);

			stream.Position = 0;

			_accIndexBuffer = new Buffer(_device, stream,
				new BufferDescription {
					BindFlags = BindFlags.IndexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = accIndexBuffer.Count * Marshal.SizeOf(typeof(uint)),
					Usage = ResourceUsage.Default
				});

			stream.Dispose();

			// Valence[4] and Prefix[4] buffer

			stream = new DataStream(Model.AccPatches.Count * 8 * Marshal.SizeOf(typeof(uint)), true, true);

			foreach (var extraordinaryPatch in Model.AccPatches)
			{
				foreach (var valence in extraordinaryPatch.Valences)
					stream.Write(Convert.ToUInt32(valence));

				foreach (var prefix in extraordinaryPatch.Prefixes)
					stream.Write(Convert.ToUInt32(prefix));
			}

			stream.Position = 0;

			_valencePrefixBuffer = new Buffer(_device, stream,
				new BufferDescription {
					BindFlags = BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = Model.AccPatches.Count * 8 * Marshal.SizeOf(typeof(uint)),
					Usage = ResourceUsage.Default
				});

			stream.Dispose();

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

		public void Dispose()
		{
			if (_inputLayout != null)
				_inputLayout.Dispose();
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

	enum ShaderMode
	{
		Flat,
		Phong,
		PnQuads,
		Acc
	}

	struct RenderParameters
	{
		public bool WireFrame;
		public float TessellationFactor;
		public float TessellationStep;
		public long TicksLastFrame;
		public bool Textured;
		public bool FlatShading;
		public float FrameRate;
	}
}
