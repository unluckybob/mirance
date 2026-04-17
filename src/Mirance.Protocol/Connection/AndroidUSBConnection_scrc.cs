#define TRACE
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirPlayLibrary;
using Core.MD.Render.API.Android;
using Core.MirroringConnection.Interface;
using Utilities.ThreadInvoke;

namespace Core.MirroringConnection.Connection;

public class AndroidUSBConnection_scrc : IConnection
{
	private static AndroidUSBConnection_scrc _instance;

	private static object _lockInit = new object();

	private bool isConnecting;

	private Semaphore semaphore = new Semaphore(1, 1);

	private Stopwatch stopwatch = new Stopwatch();

	private AndroidUSBSocketService androidUSBSocketService;

	public string DeviceNumber { private get; set; }

	public string DeviceModel { get; set; }

	public string AndroidVersion { get; set; }

	public static AndroidUSBConnection_scrc Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new AndroidUSBConnection_scrc();
					}
				}
			}
			return _instance;
		}
	}

	private AndroidUSBConnection_scrc()
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
		if (isConnecting)
		{
			return;
		}
		isConnecting = true;
		semaphore.Close();
		semaphore = new Semaphore(0, 1);
		KillProcess("adb");
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				VideoConfig videoConfig = MirrorConfig.Instance.GetVideoConfig(VideoPlatfrom.Android, isCheckPro: true);
				_ = videoConfig.Height;
				_ = videoConfig.FPS;
				string deviceNumber = DeviceNumber;
				if (!string.IsNullOrEmpty(deviceNumber))
				{
					Thread.Sleep(300);
					string text = ExecuteCmd($"adb -s {deviceNumber} shell getprop ro.build.version.release");
					string[] array = text.Split('\n');
					if (array[4].Contains("connection reset") || text.Contains("not found") || (array.Length > 6 && array[6].Contains("connection reset")))
					{
						Thread.Sleep(500);
						array = ExecuteCmd($"adb -s {deviceNumber} shell getprop ro.build.version.release").Split('\n');
					}
					if (text.Contains("not found"))
					{
						Trace.TraceInformation("connect android is not found");
						androidUSBSocketService?.CloseClient();
						stopwatch?.Stop();
						isConnecting = false;
					}
					else
					{
						int num = int.Parse(array[4].Substring(0, (array[4].IndexOf(".") > 0) ? array[4].IndexOf(".") : array[4].IndexOf("\r")));
						Trace.TraceInformation($"android version, {num}");
						try
						{
							AndroidVersion = $"Android{num.ToString()}";
							string[] array2 = ExecuteCmd($"adb.exe -s {deviceNumber} shell getprop ro.product.model").Split('\r');
							string[] array3 = ExecuteCmd($"adb.exe -s {deviceNumber} shell getprop ro.product.brand").Split('\r');
							DeviceModel = array3[4].Trim() + " " + array2[4].Trim();
						}
						catch (Exception ex)
						{
							Trace.TraceInformation("get device information exception: " + ex.Message);
						}
						Trace.TraceInformation("start android usb mirror service");
						LibScrcpyMSVC.Load();
						FunctionInvoke.InvokeByMainThread(delegate
						{
						});
					}
				}
			}
			catch (Exception ex2)
			{
				Trace.TraceInformation("connect android exception: " + ex2.Message);
				EventCenter.OnConnectFailed();
			}
			finally
			{
				androidUSBSocketService?.CloseClient();
				stopwatch?.Stop();
				isConnecting = false;
				semaphore.Release();
			}
		});
		thread.Name = "AndroidConnect-USB";
		thread.Start();
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
		KillProcess("adb");
		AndroidVersion = "";
		DeviceModel = "";
		androidUSBSocketService.CloseClient();
	}
}
