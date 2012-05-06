using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.Windows;

namespace SubdivisionRenderer
{
	static class Program
	{
		private static D3DManager _dxManager;
		private static readonly RenderForm RenderForm;
		private static ModelRenderer _modelRenderer;

		private static readonly Queue<long> FrameCounter = new Queue<long>();
		
		private static List<string> _models;
		private static List<string> Models {
			get { if (_models == null) FindModels(); return _models; }
		}
		
		private static int _currentModelIndex;

		public static float FrameRate;

		private const float TessellationStep = 1f;
		private const int SampleSize = 500;

		private static readonly Size WindowSize = new Size(1280, 720);

		static Program()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			RenderForm = new RenderForm("Subdivision") { ClientSize = WindowSize };

			RenderForm.FormClosing += OnExit;

			RenderForm.KeyDown += HandleKeyboardStart;
			RenderForm.KeyUp += HandleKeyboardEnd;

			RenderForm.UserResized += FormResized;

			RenderForm.MouseDown += HandleMouseDown;
			RenderForm.MouseUp += HandleMouseUp;
			RenderForm.MouseMove += HandleMouseMove;
			RenderForm.MouseWheel += HandleMouseWheel;

			SetupRendering();
		}

		[STAThread]
		public static void Main()
		{
			var stopwatch = Stopwatch.StartNew();
			var timeSinceTitleUpdate = 0f;
			MessagePump.Run(RenderForm, () => {

				_dxManager.Device.ImmediateContext.ClearRenderTargetView(_dxManager.RenderTargetView, _modelRenderer.RenderParameters.Lighting.BackgroundColor);
				_dxManager.Device.ImmediateContext.ClearDepthStencilView(_dxManager.DepthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

				_modelRenderer.Render();

				_dxManager.SwapChain.Present(0, PresentFlags.None);

				UpdateFrameCounter(stopwatch.ElapsedTicks);

				if (timeSinceTitleUpdate > 0.5f)
				{
					UpdateTitle(RenderForm);
					timeSinceTitleUpdate = 0f;
				}
				timeSinceTitleUpdate += 1f / FrameRate;

				stopwatch = Stopwatch.StartNew();
			});
		}

		private static void SetupRendering()
		{
			try
			{
				_dxManager = new D3DManager(RenderForm);
				_modelRenderer = new ModelRenderer(_dxManager.Device, new Model(Models.First()));
				_modelRenderer.RenderParameters.Camera.Aspect = (float)RenderForm.ClientSize.Width / RenderForm.ClientSize.Height;
			}
			catch (Direct3D11Exception direct3D11Exception)
			{
				MessageBox.Show(RenderForm, "Error setting up D3D. \nMessage: '" + direct3D11Exception.Message + "'.");
				Environment.Exit(1);
			}
			catch (InvalidOperationException invalidOperationException)
			{
				MessageBox.Show(RenderForm, "No models found in the 'Models' directory.");
				Environment.Exit(1);
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				MessageBox.Show(RenderForm, "Either no shader named 'Tessellation.hlsl' in the Shaders folder, or no texture named 'Texture.dds' in the Textures directory was found.");
				Environment.Exit(1);
			}
			catch (FileLoadException fileLoadException)
			{
				MessageBox.Show(RenderForm, "Error loading the model. \nMessage: '" + fileLoadException.Message + "'.");
				Environment.Exit(1);
			}
			catch (CompilationException compilationException)
			{
				MessageBox.Show(RenderForm, "Error compiling the shaders. \nMessage: '" + compilationException.Message + "'.");
				Environment.Exit(1);
			}
		}

		private static void UpdateFrameCounter(long newFrameTicks)
		{
			if (FrameCounter.Count >= SampleSize)
				FrameCounter.Dequeue();
			FrameCounter.Enqueue(newFrameTicks);

			FrameRate = 1f / (float)(FrameCounter.Average() / Stopwatch.Frequency);
		}

		private static void UpdateTitle(Control renderForm)
		{
			var avg = FrameCounter.Average();
			var sum = FrameCounter.Sum(val => Math.Pow(val - avg, 2));

			var dev = Math.Sqrt(sum / (FrameCounter.Count - 1));

			dev = dev / avg * FrameRate;

			renderForm.Text =
				String.Format("{0:n0} FPS - {1:n0} StdDev | Subdivsion: {2} | Tessellation Factor: {3:n1} | Model: '{4}' - {5:n0} Triangles | Textures: {6} | Wireframe: {7}",
				              FrameRate,
							  dev,
				              _modelRenderer.RenderParameters.ShaderMode,
				              _modelRenderer.RenderParameters.TessellationFactor,
							  Models[_currentModelIndex].Substring(9),
							  _modelRenderer.Model.Faces.Count * 2 * _modelRenderer.RenderParameters.TessellationFactor * _modelRenderer.RenderParameters.TessellationFactor,
							  _modelRenderer.RenderParameters.Textured ? "ON" : "OFF",
							  _modelRenderer.RenderParameters.WireFrame ? "ON" : "OFF");
		}

		private static void FindModels()
		{
			_models = new List<string>();
			foreach (var fileName in Directory.EnumerateFiles("Models").Where(f => f.EndsWith(".obj")))
				_models.Add(fileName);
		}

		private static void FormResized(Object sender, EventArgs e)
		{
			var form = sender as RenderForm;
			_dxManager.ResizeRenderTargets(form.ClientSize.Width, form.ClientSize.Height);
			_modelRenderer.RenderParameters.Camera.Aspect = (float) form.ClientSize.Width / form.ClientSize.Height;
		}

		private static void HandleKeyboardStart(Object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Controls.Shader1:
					_modelRenderer.RenderParameters.ShaderMode = ShaderMode.Bilinear;
					break;
				case Controls.Shader2:
					_modelRenderer.RenderParameters.ShaderMode = ShaderMode.Phong;
					break;
				case Controls.Shader3:
					_modelRenderer.RenderParameters.ShaderMode = ShaderMode.PnQuads;
					break;
				case Controls.Shader4:
					_modelRenderer.RenderParameters.ShaderMode = ShaderMode.Acc;
					break;
				case Controls.Wireframe:
					_modelRenderer.RenderParameters.WireFrame = !_modelRenderer.RenderParameters.WireFrame;
					break;
				case Controls.ShadingToggle:
					_modelRenderer.RenderParameters.FlatShading = !_modelRenderer.RenderParameters.FlatShading;
					break;
				case Controls.DisplayNormals:
					_modelRenderer.RenderParameters.DisplayNormals = !_modelRenderer.RenderParameters.DisplayNormals;
					break;
				case Controls.TessFactorUp:
					_modelRenderer.RenderParameters.TessellationFactor =
						_modelRenderer.RenderParameters.TessellationFactor + TessellationStep > 64f
							? _modelRenderer.RenderParameters.TessellationFactor
							: _modelRenderer.RenderParameters.TessellationFactor + TessellationStep;
					break;
				case Controls.TessFactorDown:
					_modelRenderer.RenderParameters.TessellationFactor =
						_modelRenderer.RenderParameters.TessellationFactor - TessellationStep < 1f
							? _modelRenderer.RenderParameters.TessellationFactor
							: _modelRenderer.RenderParameters.TessellationFactor - TessellationStep;
					break;
				case Controls.TextureToggle:
					_modelRenderer.RenderParameters.Textured = !_modelRenderer.RenderParameters.Textured;
					break;
				case Controls.ChangeModel:
					_currentModelIndex = (_currentModelIndex + 1) % Models.Count;
					try
					{
						_modelRenderer.Model = new Model(Models[_currentModelIndex]);
					}
					catch (FileLoadException fileLoadException)
					{
						MessageBox.Show(RenderForm, 
							String.Format("Error loading '{0}'. Only quadrilateral meshes are supported. \nMessage: '{1}'.", 
								Models[_currentModelIndex], 
								fileLoadException.Message));
					}
					break;
				case Controls.Reset:
					_modelRenderer.RenderParameters.Camera.Reset();
					break;
				default:
					_modelRenderer.RenderParameters.Camera.HandleKeyboardStart(e);
					break;
			}
		}

		private static void HandleKeyboardEnd(Object sender, KeyEventArgs e)
		{
			_modelRenderer.RenderParameters.Camera.HandleKeyboardEnd(e);
		}

		private static void HandleMouseDown(object sender, MouseEventArgs e)
		{
			_modelRenderer.RenderParameters.Camera.HandleMouseDown(e);
		}

		private static void HandleMouseUp(object sender, MouseEventArgs e)
		{
			_modelRenderer.RenderParameters.Camera.HandleMouseUp(e);
		}

		private static void HandleMouseMove(object sender, MouseEventArgs e)
		{
			_modelRenderer.RenderParameters.Camera.HandleMouseMove(e);
		}

		private static void HandleMouseWheel(object sender, MouseEventArgs e)
		{
			_modelRenderer.RenderParameters.Camera.HandleMouseWheel(e);
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
