using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.Windows;

namespace SubdivisionRenderer
{
	static class Program
	{
		private static ModelRenderer _modelRenderer;
		private static D3DManager _dxManager;
		private static readonly Queue<Double> FrameCounter = new Queue<double>();
		private static readonly List<string> Models = new List<string>();
		private static int _currentModelIndex;

		private static RenderParameters _renderParameters = RenderParameters.Default();

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var renderForm = new RenderForm("Subdivision");
			_dxManager = new D3DManager(renderForm);

			renderForm.FormClosing += OnExit;
			renderForm.KeyDown += HandleKeyboardStart;
			renderForm.KeyUp += HandleKeyboardEnd;
			renderForm.UserResized += FormResized;
			renderForm.MouseDown += HandleMouseDown;
			renderForm.MouseUp += HandleMouseUp;
			renderForm.MouseMove += HandleMouseMove;
			renderForm.MouseWheel += HandleMouseWheel;

			FindModels();

			_modelRenderer = new ModelRenderer(_dxManager.Device, new Model(Models.First()));

			var stopwatch = Stopwatch.StartNew();
			var timeSinceTitleUpdate = 1f;
			MessagePump.Run(renderForm, () => {

				_dxManager.Device.ImmediateContext.ClearRenderTargetView(_dxManager.RenderTargetView, Color.Cornsilk);
				_dxManager.Device.ImmediateContext.ClearDepthStencilView(_dxManager.DepthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

				_modelRenderer.Render(_renderParameters);

				_dxManager.SwapChain.Present(0, PresentFlags.None);

				_renderParameters.TicksLastFrame = stopwatch.ElapsedTicks;
				
				if (FrameCounter.Count > 50)
					FrameCounter.Dequeue();
				FrameCounter.Enqueue(_renderParameters.TicksLastFrame);

				_renderParameters.FrameRate = 1f / (float)(FrameCounter.Average() / Stopwatch.Frequency);

				if (timeSinceTitleUpdate > 0.5f)
				{
					UpdateTitle(renderForm);
					timeSinceTitleUpdate = 0f;
				}
				timeSinceTitleUpdate += 1f / _renderParameters.FrameRate;

				stopwatch = Stopwatch.StartNew();
			});
		}

		private static void UpdateTitle(Control renderForm)
		{
			renderForm.Text =
				String.Format("{0} fps | Subdivion: {1} | TessellationFactor: {2} | Textures: {3} | Wireframe: {4} | Faces: {5}",
				_renderParameters.FrameRate.ToString("F0", CultureInfo.InvariantCulture),
				_modelRenderer.GetSubdivisionMode(),
				_renderParameters.TessellationFactor.ToString("0.0", CultureInfo.InvariantCulture),
				_renderParameters.Textured ? "ON" : "OFF",
				_renderParameters.WireFrame ? "ON" : "OFF",
				_modelRenderer.GetFaceCount() * Math.Floor(_renderParameters.TessellationFactor) * Math.Floor(_renderParameters.TessellationFactor));
		}

		private static void FindModels()
		{
			Models.Clear();
			foreach (var fileName in Directory.EnumerateFiles("Models").Where(f => f.EndsWith(".obj")))
				Models.Add(fileName);
		}

		public static float GetFrameRate()
		{
			return _renderParameters.FrameRate;
		}

		private static void FormResized(Object sender, EventArgs e)
		{
			var form = sender as RenderForm;
			_dxManager.ResizeRenderTargets(form.ClientSize.Width, form.ClientSize.Height);
		}

		private static void HandleKeyboardStart(Object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Controls.Shader1:
					_modelRenderer.ChangeShader(ShaderMode.Bilinear);
					break;
				case Controls.Shader2:
					_modelRenderer.ChangeShader(ShaderMode.Phong);
					break;
				case Controls.Shader3:
					_modelRenderer.ChangeShader(ShaderMode.PnQuads);
					break;
				case Controls.Shader4:
					_modelRenderer.ChangeShader(ShaderMode.Acc);
					break;
				case Controls.Wireframe:
					_renderParameters.WireFrame = !_renderParameters.WireFrame;
					_dxManager.ChangeWireframe(_renderParameters.WireFrame);
					break;
				case Controls.ShadingToggle:
					_renderParameters.FlatShading = !_renderParameters.FlatShading;
					break;
				case Controls.DisplayNormals:
					_renderParameters.DisplayNormals = !_renderParameters.DisplayNormals;
					break;
				case Controls.TessFactorUp:
					_renderParameters.TessellationFactor =
						_renderParameters.TessellationFactor + _renderParameters.TessellationStep > 64f
							? _renderParameters.TessellationFactor
							: _renderParameters.TessellationFactor + _renderParameters.TessellationStep;
					break;
				case Controls.TessFactorDown:
					_renderParameters.TessellationFactor =
						_renderParameters.TessellationFactor - _renderParameters.TessellationStep < 1f
							? _renderParameters.TessellationFactor
							: _renderParameters.TessellationFactor - _renderParameters.TessellationStep;
					break;
				case Controls.TextureToggle:
					_renderParameters.Textured = !_renderParameters.Textured;
					break;
				case Controls.ChangeModel:
					_currentModelIndex = (_currentModelIndex + 1) % Models.Count;
					_modelRenderer.Model = new Model(Models[_currentModelIndex]);
					break;
				case Controls.Reset:
					Camera.Reset();
					break;
				default:
					Camera.HandleKeyboardStart(e);
					break;
			}
		}

		private static void HandleKeyboardEnd(Object sender, KeyEventArgs e)
		{
			Camera.HandleKeyboardEnd(e);
		}

		private static void HandleMouseDown(object sender, MouseEventArgs e)
		{
			Camera.HandleMouseDown(e);
		}

		private static void HandleMouseUp(object sender, MouseEventArgs e)
		{
			Camera.HandleMouseUp(e);
		}

		private static void HandleMouseMove(object sender, MouseEventArgs e)
		{
			Camera.HandleMouseMove(e);
		}

		private static void HandleMouseWheel(object sender, MouseEventArgs e)
		{
			Camera.HandleMouseWheel(e);
		}

		private static void OnExit(Object sender, FormClosingEventArgs e)
		{
			if (_dxManager != null)
				_dxManager.Dispose();

			if (_modelRenderer != null)
				_modelRenderer.Dispose();
		}
	}

	struct RenderParameters
	{
		public bool WireFrame;
		public float TessellationFactor;
		public float TessellationStep;
		public bool Textured;
		public bool FlatShading;
		public bool DisplayNormals;
		public long TicksLastFrame;
		public float FrameRate;

		public static RenderParameters Default()
		{
			return new RenderParameters
			{
				WireFrame = false,
				TessellationFactor = 1f,
				TessellationStep = 0.25f,
				Textured = false,
				FlatShading = false,
				DisplayNormals = false,
				TicksLastFrame = 200,
				FrameRate = 100f
			};
		}
	}
}
