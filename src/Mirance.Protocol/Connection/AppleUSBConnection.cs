using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mirance;
using Mirance.Model;
using Core.MirroringConnection.Connection.EXE;

namespace Core.MirroringConnection.Connection;

public class AppleUSBConnection
{
	private static AppleUSBConnection _instance;

	private static object _lockInit = new object();

	private IOSUSBProtectEXE iOSUSBProtect;

	private IOSConnectionEXE iOSConnection;

	private bool isTrustOpened;

	private bool needTrust;

	private int errorCountDown;

	private int nolangidCountDown;

	private bool startStream;

	private Dictionary<string, object> installedDriver = new Dictionary<string, object>();

	public Action<AppleUSBMsgModel> EventUSBMessage;

	private string SerialNumber { get; set; }

	public static AppleUSBConnection Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new AppleUSBConnection();
					}
				}
			}
			return _instance;
		}
	}

	private AppleUSBConnection()
	{
		iOSUSBProtect = new IOSUSBProtectEXE();
		iOSUSBProtect.KillEXE();
		iOSUSBProtect.EventOutputMsg += IOSUSBProtect_EventOutputMsg;
		iOSUSBProtect.EventErrorMsg += IOSUSBProtect_EventErrorMsg;
		iOSUSBProtect.EventExited += IOSUSBProtect_EventExited;
		iOSConnection = new IOSConnectionEXE();
		iOSConnection.KillEXE();
		iOSConnection.EventErrorMsg += IOSConnection_EventErrorMsg;
		iOSConnection.EventOutputMsg += IOSConnection_EventOutputMsg;
		iOSConnection.EventExited += IOSConnection_EventExited;
		iOSConnection.EventFinished += IOSConnection_EventFinished;
	}

	private void IOSConnection_EventFinished()
	{
	}

	private void IOSConnection_EventErrorMsg(string obj)
	{
		if (obj.Contains("libusb0-dll:err"))
		{
			return;
		}
		if (obj.Contains("The device has no langid"))
		{
			nolangidCountDown++;
			if (nolangidCountDown >= 5)
			{
				needTrust = false;
				startStream = false;
				Thread.Sleep(200);
				if (!needTrust)
				{
					iOSConnection.Run();
				}
			}
		}
		else if (obj.Contains("AudioVideo-Stream has start success"))
		{
			errorCountDown = 0;
			nolangidCountDown = 0;
		}
		else if (obj.Contains("start receive video and audio data"))
		{
			startStream = true;
			EventUSBMessage?.Invoke(new AppleUSBMsgModel
			{
				MsgType = AppleUSBMessageType.StreamStarted,
				SerialNumber = SerialNumber
			});
		}
	}

	private void IOSConnection_EventOutputMsg(string obj)
	{
		ConnectCenter.Instance.RequestConnection(new MirroringMode
		{
			Width = 10,
			Height = 10,
			Identity = SerialNumber,
			MirrorType = MirrorType.IOSUSB,
			RequestMirroringResult = OnMirroringFailed
		});
	}

	private void OnMirroringFailed(bool successed)
	{
		if (!successed)
		{
			iOSConnection.Exit();
		}
	}

	private void IOSConnection_EventExited()
	{
		iOSUSBProtect.Run();
	}

	private void IOSUSBProtect_EventExited()
	{
		iOSUSBProtect.Run();
	}

	private void IOSUSBProtect_EventErrorMsg(string obj)
	{
		if (obj.Contains("Connect success!"))
		{
			if (ConnectCenter.Instance.RequestConnect(SerialNumber))
			{
				Task.Run(delegate
				{
					EventUSBMessage?.Invoke(new AppleUSBMsgModel
					{
						MsgType = AppleUSBMessageType.InstallDriverSuccess,
						SerialNumber = SerialNumber
					});
				});
				iOSConnection.Run();
			}
		}
		else if (obj.Contains("libusb0-dll:err"))
		{
			if (ConnectCenter.Instance.RequestConnect(SerialNumber))
			{
				iOSConnection.KillEXE();
				needTrust = false;
				startStream = false;
				if (!needTrust)
				{
					iOSConnection.Run();
				}
			}
		}
		else if (obj.Contains("LIBUSB_ERROR") || obj.Contains("LIBUSB_ERROR_ACCESS"))
		{
			Match match = Regex.Match(obj, "[0-9]{1,}-[0-9]{1,}");
			if (match.Success && !installedDriver.TryGetValue(match.Value, out var _))
			{
				installedDriver.Add(match.Value, null);
				EventUSBMessage?.Invoke(new AppleUSBMsgModel
				{
					MsgType = AppleUSBMessageType.NoDriver,
					SerialNumber = SerialNumber
				});
			}
		}
		else if (obj.Contains("Input/Output Error"))
		{
			EventUSBMessage?.Invoke(new AppleUSBMsgModel
			{
				MsgType = AppleUSBMessageType.ReconnectDevice,
				SerialNumber = SerialNumber
			});
		}
		else if (obj.Contains("Enabling hidden QT config"))
		{
			errorCountDown++;
			if (errorCountDown >= 5)
			{
				iOSConnection.KillEXE();
				needTrust = false;
				startStream = false;
				if (!needTrust)
				{
					iOSConnection.Run();
				}
			}
		}
		else if (obj.Contains("Error 2 when sending") || obj.Contains("Error 0 when sending") || obj.Contains("Connect failed, Error code=2"))
		{
			if (needTrust || startStream)
			{
				return;
			}
			needTrust = true;
			iOSConnection.KillEXE();
			if (isTrustOpened)
			{
				return;
			}
			isTrustOpened = true;
			Task.Run(delegate
			{
				EventUSBMessage?.Invoke(new AppleUSBMsgModel
				{
					MsgType = AppleUSBMessageType.TrustDevice,
					SerialNumber = SerialNumber
				});
				isTrustOpened = false;
				if (!startStream)
				{
					needTrust = false;
					iOSConnection.KillEXE();
					iOSConnection.Run();
				}
			});
		}
		else if (obj.Contains("'main' due to unhandled exception"))
		{
			iOSConnection.KillEXE();
			needTrust = false;
			startStream = false;
			if (!needTrust)
			{
				iOSConnection.Run();
			}
		}
		else if (obj.Contains("未找到 iOS 连接设备"))
		{
			needTrust = false;
			startStream = false;
			iOSConnection.KillEXE();
			iOSConnection.Run();
		}
		else if (obj.Contains("Removed device"))
		{
			errorCountDown = 0;
			nolangidCountDown = 0;
			EventUSBMessage?.Invoke(new AppleUSBMsgModel
			{
				MsgType = AppleUSBMessageType.Disconnect,
				SerialNumber = SerialNumber
			});
			ConnectCenter.Instance.CloseConnection(SerialNumber);
			_ = Regex.Match(obj, "[0-9]{1,}-[0-9]{1,}").Success;
		}
		else if (obj.Contains("with serial number"))
		{
			string[] source = obj.Split(new string[1] { " " }, StringSplitOptions.None);
			SerialNumber = source.LastOrDefault();
		}
	}

	private void IOSUSBProtect_EventOutputMsg(string obj)
	{
	}

	public void Init(Func<IntPtr> getWindowHandleCallBack)
	{
		iOSConnection.EventGetWindowHandlerMsg = getWindowHandleCallBack;
		iOSUSBProtect.KillEXE();
		iOSUSBProtect.Run();
	}

	public void Connect()
	{
		iOSConnection.KillEXE();
		iOSConnection.Run();
	}

	public void StopConnect()
	{
		iOSConnection.KillEXE();
	}
}
