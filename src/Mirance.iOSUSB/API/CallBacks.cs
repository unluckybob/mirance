using System;
using System.Runtime.InteropServices;

namespace Mirance.iOSUSB;

public class CallBacks
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackGetDecodeMode();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackGetDisplayMode();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate IntPtr CallBackGetHwnd(ulong id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackGetRecordStatus();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackGetRenderMode();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackGetRotateAngle();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackReportMsg(int code);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int CallBackWndState(ref WindowStatus window);
}
