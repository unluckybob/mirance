using System;
using Mirance.Render.API.Android;

namespace Mirance.Render;

internal class RenderModel : IDisposable
{
	public int Width { get; set; }

	public int Height { get; set; }

	public int YPitch { get; set; }

	public int UPitch { get; set; }

	public int VPitch { get; set; }

	public IntPtr YPlan { get; set; }

	public IntPtr UPlan { get; set; }

	public IntPtr VPlan { get; set; }

	public IntPtr Frame { get; set; }

	public void Dispose()
	{
		if (Frame != IntPtr.Zero)
		{
			libdecode.destroy_frame(Frame);
		}
	}
}
