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

		private static RenderParameters _renderParameters = 
			new RenderParameters {
				WireFrame = false,
				TessellationFactor = 1f,
				TessellationStep = 0.25f,
				Textured = false,
				FlatShading = false,
				TicksLastFrame = 200,
				FrameRate = 100f
			};

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
				String.Format("{0} fps | Subdivion: {1} | TessellationFactor: {2} | Textures: {3} | Wireframe: {4}",
				_renderParameters.FrameRate.ToString("F0", CultureInfo.InvariantCulture),
				_modelRenderer.GetSubdivisionMode(),
				_renderParameters.TessellationFactor.ToString("F0", CultureInfo.InvariantCulture),
				_renderParameters.Textured ? "ON" : "OFF",
				_renderParameters.WireFrame ? "ON" : "OFF");
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
			if (e.KeyCode == Controls.Shader1)
			{
				_modelRenderer.ChangeShader(ShaderMode.Flat);
			}
			else if (e.KeyCode == Controls.Shader2)
			{
				_modelRenderer.ChangeShader(ShaderMode.Phong);
			}
			else if (e.KeyCode == Controls.Shader3)
			{
				_modelRenderer.ChangeShader(ShaderMode.PnQuads);
			}
			else if (e.KeyCode == Controls.Shader4)
			{
				_modelRenderer.ChangeShader(ShaderMode.Acc);
			}
			else if (e.KeyCode == Controls.Wireframe)
			{
				_renderParameters.WireFrame = !_renderParameters.WireFrame;
				_dxManager.ChangeWireframe(_renderParameters.WireFrame);
			}
			else if (e.KeyCode == Controls.ShadingToggle)
			{
				_renderParameters.FlatShading = !_renderParameters.FlatShading;
			}
			else if (e.KeyCode == Controls.TessFactorUp)
			{
				_renderParameters.TessellationFactor =
					_renderParameters.TessellationFactor + _renderParameters.TessellationStep > 64f
					? _renderParameters.TessellationFactor
					: _renderParameters.TessellationFactor + _renderParameters.TessellationStep;
			}
			else if (e.KeyCode == Controls.TessFactorDown)
			{
				_renderParameters.TessellationFactor =
					_renderParameters.TessellationFactor - _renderParameters.TessellationStep < 1f
					? _renderParameters.TessellationFactor
					: _renderParameters.TessellationFactor - _renderParameters.TessellationStep;
			}
			else if (e.KeyCode == Controls.TextureToggle)
			{
				_renderParameters.Textured = !_renderParameters.Textured;
			}
			else if (e.KeyCode == Controls.ChangeModel)
			{
				_currentModelIndex = (_currentModelIndex + 1) % Models.Count;
				_modelRenderer.Model = new Model(Models[_currentModelIndex]);
			}
			else
			{
				Camera.HandleKeyboardStart(e);
			}
		}

		private static void HandleKeyboardEnd(Object sender, KeyEventArgs e)
		{
			Camera.HandleKeyboardEnd(e);
		}

		private static void OnExit(Object sender, FormClosingEventArgs e)
		{
			if (_dxManager != null)
				_dxManager.Dispose();

			if (_modelRenderer != null)
				_modelRenderer.Dispose();
		}
	}
}
