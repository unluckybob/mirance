#define TRACE
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirPlayLibrary;
using Core.MirroringConnection.Interface;

namespace Core.MirroringConnection.Connection;

public class AndroidUSBConnection : IConnection
{
	private static AndroidUSBConnection _instance;

	private static object _lockInit = new object();

	private bool isConnecting;

	private Semaphore semaphore = new Semaphore(1, 1);

	private Stopwatch stopwatch = new Stopwatch();

	private AndroidUSBSocketService androidUSBSocketService;

	public string DeviceNumber { private get; set; }

	public string DeviceModel { get; set; }

	public string AndroidVersion { get; set; }

	public static AndroidUSBConnection Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new AndroidUSBConnection();
					}
				}
			}
			return _instance;
		}
	}

	private AndroidUSBConnection()
	{
	}

	public void Init()
	{
		KillProcess("adb");
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
		try
		{
			androidUSBSocketService = new AndroidUSBSocketService();
			androidUSBSocketService.Start();
		}
		catch (Exception ex)
		{
			Trace.TraceWarning("listen android port failed: " + ex.Message);
			isConnecting = false;
			return;
		}
		semaphore.Close();
		semaphore = new Semaphore(0, 1);
		KillProcess("adb");
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				string text = "4d596886";
				VideoConfig videoConfig = MirrorConfig.Instance.GetVideoConfig(VideoPlatfrom.Android, isCheckPro: true);
				int height = videoConfig.Height;
				int fPS = videoConfig.FPS;
				string deviceNumber = DeviceNumber;
				if (!string.IsNullOrEmpty(deviceNumber))
				{
					Thread.Sleep(300);
					string text2 = ExecuteCmd($"adb -s {deviceNumber} shell getprop ro.build.version.release");
					string[] array = text2.Split('\n');
					if (array[4].Contains("connection reset") || text2.Contains("not found") || (array.Length > 6 && array[6].Contains("connection reset")))
					{
						Thread.Sleep(500);
						array = ExecuteCmd($"adb -s {deviceNumber} shell getprop ro.build.version.release").Split('\n');
					}
					if (text2.Contains("not found"))
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
						catch (Exception ex2)
						{
							Trace.TraceInformation("get device information exception: " + ex2.Message);
						}
						Trace.TraceInformation("start android usb mirror service");
						string text3 = ExecuteCmd($"adb -s {deviceNumber} push scrcpy-server /data/local/tmp/scrcpy-server.jar");
						if (text3.Contains("1 file pushed"))
						{
							text3 = ExecuteCmd(string.Format("adb -s {0} reverse localabstract:scrcpy_{2} tcp:{1}", deviceNumber, PortConfig.AndroidUSB, text));
							stopwatch.Start();
							string text4 = "";
							text4 = ((videoConfig.VideoResolutionRatio != VideoResolutionRatio.Original) ? string.Format("adb -s {0} shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.imobie.scrcpy.Server 3.0.imobie scid={1} log_level=debug {2} max_size={3} max_fps={4}", deviceNumber, text, (num >= 11) ? "audio_codec=raw" : "", height, fPS) : string.Format("adb -s {0} shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.imobie.scrcpy.Server 3.0.imobie scid={2} log_level=debug {1}", deviceNumber, (num >= 11) ? "audio_codec=raw" : "", text));
							text3 = ExecuteCmd(text4);
							Trace.TraceInformation("start server cmd : " + text3);
							stopwatch.Stop();
							Trace.TraceInformation($"android usb mirror closed, time: {stopwatch.Elapsed.TotalSeconds}");
							text3 = ExecuteCmd($"adb -s {deviceNumber} reverse --remove localabstract:scrcpy_{text}");
							KillProcess("adb");
							if (stopwatch.Elapsed.TotalSeconds < 3.0)
							{
								EventCenter.OnConnectFailed();
							}
						}
						else
						{
							Trace.TraceInformation("connect android failed, cmd result: " + text3);
							EventCenter.OnConnectFailed();
						}
					}
				}
			}
			catch (Exception ex3)
			{
				Trace.TraceInformation("connect android exception: " + ex3.Message);
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
