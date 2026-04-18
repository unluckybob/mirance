namespace Mirance.iOSUSB;

public struct CallBackMsg
{
	public CallBacks.CallBackGetHwnd m_CallBackGetHwnd;

	public CallBacks.CallBackReportMsg m_CallBackReportMsg;

	public CallBacks.CallBackWndState m_CallBackWndState;

	public CallBacks.CallBackGetDecodeMode m_CallBackGetDecodeMode;

	public CallBacks.CallBackGetRenderMode m_CallBackGetRenderMode;

	public CallBacks.CallBackGetDisplayMode m_CallBackGetDisplayMode;

	public CallBacks.CallBackGetRotateAngle m_CallBackGetRotateAngle;

	public CallBacks.CallBackGetRecordStatus m_CallBackGetRecordStatus;
}
