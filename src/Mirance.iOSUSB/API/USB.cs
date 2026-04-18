using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mirance.iOSUSB;

public class USB
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int initUsbScreen(CallBacks.CallBackGetHwnd a1, CallBacks.CallBackReportMsg a2, CallBacks.CallBackWndState a3, CallBacks.CallBackGetDecodeMode a4, CallBacks.CallBackGetRenderMode a5, CallBacks.CallBackGetDisplayMode a6, CallBacks.CallBackGetRotateAngle a7, CallBacks.CallBackGetRecordStatus a8);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int startUsbScreen();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int getDriverState();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int destroyUsbScreen();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int getDevTrustState();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public delegate int setLog(string path);

	static USB()
	{
	}

	public static int Init(CallBackMsg msg)
	{
		LibUSB.Load();
		return LibUSB.Invoke<initUsbScreen>("initUsbScreen")(msg.m_CallBackGetHwnd, msg.m_CallBackReportMsg, msg.m_CallBackWndState, msg.m_CallBackGetDecodeMode, msg.m_CallBackGetRenderMode, msg.m_CallBackGetDisplayMode, msg.m_CallBackGetRotateAngle, msg.m_CallBackGetRecordStatus);
	}

	public static int Connect()
	{
		return LibUSB.Invoke<startUsbScreen>("startUsbScreen")?.Invoke() ?? (-1);
	}

	public static int GetDriverState()
	{
		return LibUSB.Invoke<getDriverState>("getDriverState")();
	}

	public static int Disconnect()
	{
		return LibUSB.Invoke<destroyUsbScreen>("destroyUsbScreen")();
	}

	public static async Task FreeLibraryAsync()
	{
		await Task.Run(delegate
		{
			FreeLibrary();
		});
	}

	public static void FreeLibrary()
	{
		LibUSB.Unloaded();
	}

	public static int GetTrustState()
	{
		return LibUSB.Invoke<getDevTrustState>("getDevTrustState")();
	}

	public static int SetLogPath(string logFileName)
	{
		return LibUSB.Invoke<setLog>("setLog")(logFileName);
	}
}
