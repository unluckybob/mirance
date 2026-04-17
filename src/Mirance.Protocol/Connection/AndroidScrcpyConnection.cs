#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AirPlayLibrary;
using Core.Connection.Android;
using Core.MD.Render;
using Core.MD.Render.API.Android;
using Mirance;
using Mirance.Model;
using Core.MirroringConnection.Interface;
using Utilities.AppTool;
using Utilities.FileOperation;
using Utilities.Win32.Native;

namespace Core.MirroringConnection.Connection;

public class AndroidScrcpyConnection : IConnection
{
	private static AndroidScrcpyConnection _instance;

	private static object _lockInit = new object();

	private string identifer = "";

	private bool isConnecting;

	private Semaphore semaphore = new Semaphore(1, 1);

	private Stopwatch stopwatch = new Stopwatch();

	private AndroidUSBSocketService androidUSBSocketService;

	private ScrcpyMSVC.PushVideoDelegate pushVideoDelegate;

	private ScrcpyMSVC.PushDelegate pushDelegate;

	private IntPtr _scrcpy;

	private object lock_Connect = new object();

	public string DeviceNumber { private get; set; }

	public string DeviceModel { get; set; }

	public string AndroidVersion { get; set; }

	public static AndroidScrcpyConnection Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new AndroidScrcpyConnection();
					}
				}
			}
			return _instance;
		}
	}

	public string IPport { get; set; }

	public event Action OnConnected;

	public event Action OnConnectFailed;

	private AndroidScrcpyConnection()
	{
	}

	public void Init()
	{
	}

	public async Task Reconnect(Action callback)
	{
		await Task.Run(delegate
		{
			Semaphore obj = semaphore;
			if (obj != null && obj.WaitOne(10000))
			{
				callback?.Invoke();
			}
		});
	}

	public void Connect()
	{
		Connect(ConnectFromType.Other);
	}

	public void Connect(ConnectFromType fromType)
	{
		if (isConnecting)
		{
			return;
		}
		isConnecting = true;
		semaphore.Close();
		semaphore = new Semaphore(0, 1);
		string number = DeviceNumber;
		string ednPoint = IPport;
		Thread thread = new Thread((ThreadStart)delegate
		{
			lock (lock_Connect)
			{
				if (fromType == ConnectFromType.USB)
				{
					DeviceConnection.Instance.AirPlayingSerialNumber = number;
				}
				DoConnect(fromType, number, ednPoint);
			}
		});
		thread.Name = "AndroidConnect-USB";
		thread.Start();
	}

	private void DoConnect(ConnectFromType fromType, string deviceNumber, string endPoint)
	{
		Trace.TraceInformation("begin android usb mirror");
		try
		{
			VideoConfig videoConfig = MirrorConfig.Instance.GetVideoConfig(VideoPlatfrom.Android, isCheckPro: true);
			int num = Math.Max(videoConfig.Height, videoConfig.Width);
			int fPS = videoConfig.FPS;
			_scrcpy = IntPtr.Zero;
			MirrorType mirrorType = MirrorType.AndroidUSB;
			string text = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "scrcpy-server", SearchOption.AllDirectories).FirstOrDefault();
			if (text == null)
			{
				Trace.TraceInformation("scrcpy-server path is not exist");
				EventCenter.OnConnectFailed();
			}
			else
			{
				string text2 = Path.Combine(ApplicationTool.GetAPPDATAPath(), "scrcpy-server");
				if (FileTool.CopyFile(text, text2) == 0)
				{
					text = text2;
				}
				bool isDecodeWidthYUV = RenderEntry.Instance.IsDecodeWidthYUV;
				string deviceModel = "Unknown";
				string text3 = "None";
				string androidBrand = "";
				if (string.IsNullOrEmpty(deviceNumber))
				{
					identifer = endPoint;
					_scrcpy = ScrcpyMSVC.init_device($"txt --tcpip={endPoint}  --audio-buffer=10 --max-size={num} --max-fps={fPS} --serv={text}");
					mirrorType = MirrorType.AndroidWiFi;
					Trace.TraceInformation("connect android port:" + endPoint);
				}
				else
				{
					ExecuteCmd("adb -s " + deviceNumber + " shell am stopservice scrcpy-server");
					deviceModel = GetDeviceProductModel(deviceNumber);
					text3 = GetDeviceVersion(deviceNumber);
					androidBrand = GetDeviceProductBrand(deviceNumber);
					identifer = deviceNumber;
					_scrcpy = ScrcpyMSVC.init_device($"txt -s {deviceNumber} --audio-buffer=10 --max-size={num} --max-fps={fPS} --serv={text}");
					Trace.TraceInformation("connect android number:" + deviceNumber);
				}
				if (_scrcpy.ToInt32() > 3)
				{
					Trace.TraceInformation($"new scrcpy handle: {_scrcpy}");
					try
					{
						MirroringMode mirroringMode = new MirroringMode
						{
							Width = 720,
							Height = 1280,
							Identity = identifer,
							MirrorType = mirrorType,
							DeviceModel = deviceModel,
							Version = "Android" + text3,
							AndroidBrand = androidBrand,
							RequestMirroringResult = OnRequestFailed
						};
						switch (fromType)
						{
						case ConnectFromType.Input:
							mirroringMode.MirrorType = MirrorType.AndroidWiFi;
							mirroringMode.AndroidConnectFrom = 1;
							break;
						case ConnectFromType.ScanQR:
							mirroringMode.MirrorType = MirrorType.AndroidWiFi;
							mirroringMode.AndroidConnectFrom = 2;
							break;
						case ConnectFromType.USB:
							mirroringMode.MirrorType = MirrorType.AndroidUSB;
							break;
						case ConnectFromType.Other:
							mirroringMode.MirrorType = MirrorType.AndroidWiFi;
							mirroringMode.AndroidConnectFrom = 3;
							break;
						}
						ConnectCenter.Instance.RequestConnection(mirroringMode);
					}
					catch (Exception)
					{
					}
					pushVideoDelegate = PushVideoCallback;
					ScrcpyMSVC.register_Video_Frame(_scrcpy, pushVideoDelegate, isDecodeWidthYUV);
					pushDelegate = PushAudioCallback;
					ScrcpyMSVC.register_Audio_Frame(_scrcpy, pushDelegate);
					stopwatch.Start();
					this.OnConnected?.Invoke();
					ScrcpyMSVC.Start_device(_scrcpy);
				}
				else
				{
					Trace.TraceInformation("connect android failed");
					EventCenter.OnConnectFailed();
				}
			}
		}
		catch (Exception ex2)
		{
			Trace.TraceInformation("connect android exception: " + ex2.Message);
			EventCenter.OnConnectFailed();
		}
		DeviceConnection.Instance.AirPlayingSerialNumber = null;
		EventCenter.OnConnectionClosed(identifer);
		stopwatch?.Stop();
		isConnecting = false;
		semaphore.Release();
		Trace.TraceInformation($"android mirror closed, time: {stopwatch.Elapsed}");
	}

	private void OnRequestFailed(bool successed)
	{
		if (!successed)
		{
			StopConnect();
		}
	}

	private bool PushVideoCallback(IntPtr data)
	{
		ScrcpyMSVC.VideoFreamArgs freamArgs = Marshal.PtrToStructure<ScrcpyMSVC.VideoFreamArgs>(data);
		RenderEntry.Instance.PushFrame(freamArgs);
		return true;
	}

	private bool PushAudioCallback(long pts, IntPtr data, int length, int flags)
	{
		TransitAudioData transitAudioData = new TransitAudioData
		{
			identity = identifer,
			flags = (byte)flags,
			pts = (ulong)pts
		};
		transitAudioData.data = new byte[length];
		Marshal.Copy(data, transitAudioData.data, 0, length);
		PushCenter.Instance.PushAudio(transitAudioData);
		return true;
	}

	private bool PushCallback(long pts, IntPtr data, int length, int flags)
	{
		TransitFrameData transitFrameData = new TransitFrameData
		{
			identity = identifer,
			flags = (byte)flags,
			pts = (ulong)pts,
			dataPtrlength = length
		};
		transitFrameData.dataPtr = Marshal.AllocHGlobal(length);
		NativeSharedMethods1.CopyMemory(transitFrameData.dataPtr, data, (uint)length);
		PushCenter.Instance.PushFrame(transitFrameData);
		return true;
	}

	internal string GetDeviceVersion(string number)
	{
		string result = "";
		string[] array = ExecuteCmd("adb -s " + number + " shell getprop ro.build.version.release").Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 4)
		{
			result = array[3];
		}
		return result;
	}

	internal string GetDeviceProductModel(string number)
	{
		string result = "";
		string[] array = ExecuteCmd("adb -s " + number + " shell getprop ro.product.model").Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 4)
		{
			result = array[3];
		}
		return result;
	}

	internal string GetDeviceProductBrand(string number)
	{
		string result = "";
		string[] array = ExecuteCmd("adb -s " + number + " shell getprop ro.product.brand").Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 4)
		{
			result = array[3];
		}
		return result;
	}

	public string ExecuteCmd(string cmd)
	{
		Process process = new Process();
		process.StartInfo.FileName = "cmd.exe";
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardInput = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
		process.Start();
		process.StandardInput.WriteLine(cmd + "&exit");
		process.StandardInput.AutoFlush = true;
		string text = process.StandardOutput.ReadToEnd();
		if (text.Length < 400)
		{
			Trace.TraceInformation(text);
		}
		string text2 = process.StandardError.ReadToEnd();
		if (!string.IsNullOrEmpty(text2))
		{
			Trace.TraceInformation(text2);
		}
		process.WaitForExit();
		process.Close();
		return text + text2;
	}

	public void KillProcess(string name)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName(name);
			if (processesByName != null && processesByName.Count() > 0)
			{
				Process[] array = processesByName;
				foreach (Process process in array)
				{
					Trace.TraceInformation("kill {0}", name);
					process.Kill();
				}
			}
		}
		catch (Exception ex)
		{
			Trace.TraceError(ex.Message);
		}
	}

	public void StopConnect()
	{
		isConnecting = false;
		AndroidVersion = "";
		DeviceModel = "";
		ScrcpyMSVC.Stop_device(_scrcpy);
		Trace.TraceInformation($"destory scrcpy handle: {_scrcpy}");
	}
}
