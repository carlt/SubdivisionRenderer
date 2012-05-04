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
		private static readonly D3DManager DxManager;
		private static readonly RenderForm RenderForm;
		private static readonly ModelRenderer ModelRenderer;

		private static readonly Queue<long> FrameCounter = new Queue<long>();
		private static readonly List<string> Models = new List<string>();
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

			try
			{
				DxManager = new D3DManager(RenderForm);
			}
			catch (Exception e)
			{
				MessageBox.Show(RenderForm, "Error setting up D3D. \nMessage: '" + e.Message + "'.");
				Application.Exit();
			}
			
			FindModels();

			try
			{
				ModelRenderer = new ModelRenderer(DxManager.Device, new Model(Models.First()));
				ModelRenderer.RenderParameters.Camera.Aspect = (float) RenderForm.ClientSize.Width / RenderForm.ClientSize.Height;
			}
			catch (Exception e)
			{
				MessageBox.Show(RenderForm, "Error creating the ModelRenderer. Make sure only quadrilateral models are in the Models folder. \nMessage: '" + e.Message + "'.");
				Application.Exit();
			}

			RenderForm.FormClosing += OnExit;

			RenderForm.KeyDown += HandleKeyboardStart;
			RenderForm.KeyUp += HandleKeyboardEnd;

			RenderForm.UserResized += FormResized;

			RenderForm.MouseDown += HandleMouseDown;
			RenderForm.MouseUp += HandleMouseUp;
			RenderForm.MouseMove += HandleMouseMove;
			RenderForm.MouseWheel += HandleMouseWheel;
		}

		[STAThread]
		public static void Main()
		{
			var stopwatch = Stopwatch.StartNew();
			var timeSinceTitleUpdate = 0f;
			MessagePump.Run(RenderForm, () => {

				DxManager.Device.ImmediateContext.ClearRenderTargetView(DxManager.RenderTargetView, Color.White);
				DxManager.Device.ImmediateContext.ClearDepthStencilView(DxManager.DepthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

				ModelRenderer.Render();

				DxManager.SwapChain.Present(0, PresentFlags.None);

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
				String.Format("{0:n0} FPS - {1:n0} StdDev | Subdivion: {2} | TessellationFactor: {3:n1} | Model: '{4}' - {5:n0} Triangles | Textures: {6} | Wireframe: {7}",
				              FrameRate,
							  dev,
				              ModelRenderer.RenderParameters.ShaderMode,
				              ModelRenderer.RenderParameters.TessellationFactor,
							  Models[_currentModelIndex].Substring(9),
							  ModelRenderer.Model.Faces.Count * 2 * ModelRenderer.RenderParameters.TessellationFactor * ModelRenderer.RenderParameters.TessellationFactor,
							  ModelRenderer.RenderParameters.Textured ? "ON" : "OFF",
							  ModelRenderer.RenderParameters.WireFrame ? "ON" : "OFF");
		}

		private static void FindModels()
		{
			Models.Clear();
			foreach (var fileName in Directory.EnumerateFiles("Models").Where(f => f.EndsWith(".obj")))
				Models.Add(fileName);
		}

		private static void FormResized(Object sender, EventArgs e)
		{
			var form = sender as RenderForm;
			DxManager.ResizeRenderTargets(form.ClientSize.Width, form.ClientSize.Height);
			ModelRenderer.RenderParameters.Camera.Aspect = (float) form.ClientSize.Width / form.ClientSize.Height;
		}

		private static void HandleKeyboardStart(Object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Controls.Shader1:
					ModelRenderer.RenderParameters.ShaderMode = ShaderMode.Bilinear;
					break;
				case Controls.Shader2:
					ModelRenderer.RenderParameters.ShaderMode = ShaderMode.Phong;
					break;
				case Controls.Shader3:
					ModelRenderer.RenderParameters.ShaderMode = ShaderMode.PnQuads;
					break;
				case Controls.Shader4:
					ModelRenderer.RenderParameters.ShaderMode = ShaderMode.Acc;
					break;
				case Controls.Wireframe:
					ModelRenderer.RenderParameters.WireFrame = !ModelRenderer.RenderParameters.WireFrame;
					break;
				case Controls.ShadingToggle:
					ModelRenderer.RenderParameters.FlatShading = !ModelRenderer.RenderParameters.FlatShading;
					break;
				case Controls.DisplayNormals:
					ModelRenderer.RenderParameters.DisplayNormals = !ModelRenderer.RenderParameters.DisplayNormals;
					break;
				case Controls.TessFactorUp:
					ModelRenderer.RenderParameters.TessellationFactor =
						ModelRenderer.RenderParameters.TessellationFactor + TessellationStep > 64f
							? ModelRenderer.RenderParameters.TessellationFactor
							: ModelRenderer.RenderParameters.TessellationFactor + TessellationStep;
					break;
				case Controls.TessFactorDown:
					ModelRenderer.RenderParameters.TessellationFactor =
						ModelRenderer.RenderParameters.TessellationFactor - TessellationStep < 1f
							? ModelRenderer.RenderParameters.TessellationFactor
							: ModelRenderer.RenderParameters.TessellationFactor - TessellationStep;
					break;
				case Controls.TextureToggle:
					ModelRenderer.RenderParameters.Textured = !ModelRenderer.RenderParameters.Textured;
					break;
				case Controls.ChangeModel:
					_currentModelIndex = (_currentModelIndex + 1) % Models.Count;
					try
					{
						ModelRenderer.Model = new Model(Models[_currentModelIndex]);
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
					ModelRenderer.RenderParameters.Camera.Reset();
					break;
				default:
					ModelRenderer.RenderParameters.Camera.HandleKeyboardStart(e);
					break;
			}
		}

		private static void HandleKeyboardEnd(Object sender, KeyEventArgs e)
		{
			ModelRenderer.RenderParameters.Camera.HandleKeyboardEnd(e);
		}

		private static void HandleMouseDown(object sender, MouseEventArgs e)
		{
			ModelRenderer.RenderParameters.Camera.HandleMouseDown(e);
		}

		private static void HandleMouseUp(object sender, MouseEventArgs e)
		{
			ModelRenderer.RenderParameters.Camera.HandleMouseUp(e);
		}

		private static void HandleMouseMove(object sender, MouseEventArgs e)
		{
			ModelRenderer.RenderParameters.Camera.HandleMouseMove(e);
		}

		private static void HandleMouseWheel(object sender, MouseEventArgs e)
		{
			ModelRenderer.RenderParameters.Camera.HandleMouseWheel(e);
		}

		private static void OnExit(Object sender, FormClosingEventArgs e)
		{
			if (DxManager != null)
				DxManager.Dispose();

			if (ModelRenderer != null)
				ModelRenderer.Dispose();
		}
	}
}
