using System.Runtime.InteropServices;

namespace Mirance.iOSUSB;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct WindowStatus
{
	public int eWindowStatus;

	public int iScreenW;

	public int iScreenH;

	public int iIPad;
}
