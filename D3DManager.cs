using System;
using System.Windows.Forms;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;

namespace SubdivisionRenderer
{
	class D3DManager : IDisposable
	{
		public readonly Device Device;
		public readonly SwapChain SwapChain;

		public RenderTargetView RenderTargetView { get; private set; }
		public DepthStencilView DepthStencilView { get; private set; }

		public D3DManager(Form renderForm)
		{
			if (renderForm == null) throw new ArgumentNullException("renderForm");

			var swapChainDescription = new SwapChainDescription {
				BufferCount = 1,
				IsWindowed = true,
				ModeDescription = new ModeDescription(renderForm.ClientSize.Width, renderForm.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
				OutputHandle = renderForm.Handle,
				SampleDescription = new SampleDescription(4, 4),
				SwapEffect = SwapEffect.Discard,
				Usage = Usage.RenderTargetOutput
			};

			Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out Device, out SwapChain);

			SwapChain.GetParent<Factory>().SetWindowAssociation(renderForm.Handle, WindowAssociationFlags.IgnoreAll);

			CreateRenderTargets(renderForm.ClientSize.Width, renderForm.ClientSize.Height);

			Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);

			Device.ImmediateContext.Rasterizer.State = RasterizerState.FromDescription(Device,
				new RasterizerStateDescription {
					CullMode = CullMode.Back,
					FillMode = FillMode.Solid,
					IsMultisampleEnabled = true
				});
		}

		private void CreateRenderTargets(int width, int height)
		{
			var backBuffer = Resource.FromSwapChain<Texture2D>(SwapChain, 0);
			RenderTargetView = new RenderTargetView(Device, backBuffer);

			var depthDescription = new Texture2DDescription {
				ArraySize = 1,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				Format = Format.D32_Float,
				Height = backBuffer.Description.Height,
				Width = backBuffer.Description.Width,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(4, 4),
				Usage = ResourceUsage.Default
			};

			var depthBuffer = new Texture2D(Device, depthDescription);

			var depthStencilViewDescription = new DepthStencilViewDescription {
				Format = depthDescription.Format,
				Flags = DepthStencilViewFlags.None,
				Dimension = depthDescription.SampleDescription.Count > 1 ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D,
				MipSlice = 0
			};

			DepthStencilView = new DepthStencilView(Device, depthBuffer, depthStencilViewDescription);

			Device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, width, height, 0f, 1f));

			backBuffer.Dispose();
			depthBuffer.Dispose();
		}

		public void ResizeRenderTargets(int width, int height)
		{
			RenderTargetView.Dispose();
			DepthStencilView.Dispose();

			SwapChain.ResizeBuffers(3, 0, 0, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
			CreateRenderTargets(width, height);
			Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
		}

		public void Dispose()
		{
			if (Device != null)
				Device.Dispose();

			if (SwapChain != null)
				SwapChain.Dispose();

			if (RenderTargetView != null)
				RenderTargetView.Dispose();

			if (DepthStencilView != null)
				DepthStencilView.Dispose();
		}
	}
}
