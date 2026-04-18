#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Mirance.Render.API.Android;
using Core.MD.Transit;
using Core.MD.Transit.Model;
using Core.Tracing.GA4;
using Core.Tracing.GA4.Enums;
using NAudio.CoreAudioApi;
using Renderer.Core;

namespace Mirance.Render;

public class RenderEntry
{
	public delegate void FrameSizeChagedHandle(FrameSize frameSize);

	private static RenderEntry _instance;

	private static object _lockInit = new object();

	private int _fps;

	private int socket_fps;

	private bool _isPCMClosed = true;

	private bool isExited;

	private bool isDecoding;

	private MMDeviceEnumerator deviceEnumerator;

	private MMDeviceNotification deviceNotification;

	private AudioSampleRate audioSampleRate = AudioSampleRate.R48000;

	private CancellationTokenSource pcmCancellation;

	private BlockingCollection<TransitFrameData> _frameCollection = new BlockingCollection<TransitFrameData>();

	private BlockingCollection<TransitAudioData> _audioCollection = new BlockingCollection<TransitAudioData>();

	private BlockingCollection<TransitAudioData> _audioPCMCollection = new BlockingCollection<TransitAudioData>();

	private BlockingCollection<RenderModel> renderList = new BlockingCollection<RenderModel>();

	private CancellationTokenSource renderCancel = new CancellationTokenSource();

	private bool isEnableHardwareDecoding;

	public Action FrameComingEvent;

	public Func<Size> GetSizeEvent;

	private AudioPlay audioPlay;

	private DateTime lastChangeTime = DateTime.Now;

	private IntPtr handle_decoder;

	private libdecode.video_yuv callBack_Video;

	private bool isRequestDestory;

	private bool isDestroyed = true;

	private IntPtr waitDestoryHandle;

	private object lock_decoder = new object();

	private object lock_audio = new object();

	private int lastWidth;

	private int lastHeight;

	private object lock_d3d = new object();

	private IntPtr handle_audioPlayer;

	private libdecode.audio_callabck callBack_Audio;

	private bool _lastAndroid;

	private bool firstGetAudio;

	private bool isPlayingAudio;

	private bool flag_fps;

	private int _renderFPS;

	private bool flag_d3d_sync = true;

	private bool flag_renderThread;

	private bool isStop;

	public FrameFormat D3DFormat { get; private set; }

	public IntPtr WindowHandle { get; private set; }

	public D3DImageSource D3DImage { get; private set; }

	public ImageSource ImageSource => D3DImage?.ImageSource;

	public bool IsEnableHardwareDecoding
	{
		get
		{
			return isEnableHardwareDecoding;
		}
		set
		{
			bool num = value != isEnableHardwareDecoding;
			isEnableHardwareDecoding = value;
			if (num && handle_decoder != IntPtr.Zero && !isDecoding)
			{
				RequestDestory();
				DestoryDecoder();
				Init();
			}
		}
	}

	public bool IsDecodeWidthYUV => D3DFormat == FrameFormat.YV12;

	public static RenderEntry Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new RenderEntry();
					}
				}
			}
			return _instance;
		}
	}

	public event FrameSizeChagedHandle FrameSizeChagedEvent;

	private RenderEntry()
	{
		PopCenter.Instance.PopFrameEvent += Instance_PopFrameEvent;
		PopCenter.Instance.PopAudioEvent += Instance_PopAudioEvent;
		deviceNotification = new MMDeviceNotification();
		deviceNotification.ActionSpeakerChanged = OnSpeakerChanged;
		deviceEnumerator = new MMDeviceEnumerator();
		deviceEnumerator.RegisterEndpointNotificationCallback(deviceNotification);
		try
		{
			D3DImageSource d3DImageSource = new D3DImageSource();
			if (!d3DImageSource.CheckFormat(D3DFormat))
			{
				Trace.TraceWarning($"User Graphic Device Not Support Format:{D3DFormat}");
				D3DFormat = FrameFormat.ARGB32;
			}
			d3DImageSource.Dispose();
		}
		catch (Exception ex)
		{
			Trace.TraceInformation("check support d3d format ex:" + ex.Message);
		}
	}

	private void OnSpeakerChanged(string deviceID)
	{
		if (DateTime.Now.AddSeconds(-3.0) >= lastChangeTime)
		{
			lastChangeTime = DateTime.Now;
			InitPCMPlayer(_lastAndroid);
		}
	}

	private void Instance_PopAudioEvent(TransitDataBase obj)
	{
		audioPlay?.Add(obj);
	}

	private void Instance_PopFrameEvent(TransitDataBase obj)
	{
		socket_fps++;
		if (!isDestroyed)
		{
			if (isRequestDestory)
			{
				DestoryDecoder();
			}
			else if (!(handle_decoder == IntPtr.Zero))
			{
				isDecoding = true;
				TransitFrameData transitFrameData = (TransitFrameData)obj;
				libdecode.decodeFrame(handle_decoder, (transitFrameData.data == null) ? transitFrameData.dataPtr : ObjectToIntPtr(transitFrameData.data), transitFrameData.flags, (long)transitFrameData.pts, transitFrameData.Length);
			}
		}
	}

	public async Task InitAsync()
	{
		if (D3DImage == null)
		{
			D3DImage = new D3DImageSource();
			DoRender();
		}
		await Task.Run(delegate
		{
			Init();
		});
	}

	public void Init()
	{
		if (handle_decoder != IntPtr.Zero)
		{
			return;
		}
		lock (lock_decoder)
		{
			if (handle_decoder != IntPtr.Zero)
			{
				return;
			}
			isRequestDestory = false;
			callBack_Video = OnYUVCallBack;
			Trace.TraceInformation($"init decoder, open hardware:{IsEnableHardwareDecoding}");
			handle_decoder = libdecode.create_decoder(IsEnableHardwareDecoding, D3DFormat == FrameFormat.ARGB32);
			libdecode.init(handle_decoder, callBack_Video);
			isDestroyed = false;
		}
		ListenFPS();
	}

	private void DestoryDecoder()
	{
		if (waitDestoryHandle == IntPtr.Zero)
		{
			return;
		}
		pcmCancellation?.Cancel();
		isDestroyed = true;
		isDecoding = false;
		Task.Run(delegate
		{
			lock (lock_decoder)
			{
				if (!(waitDestoryHandle == IntPtr.Zero))
				{
					libdecode.destroy(waitDestoryHandle);
					waitDestoryHandle = IntPtr.Zero;
				}
			}
		});
	}

	private void RequestDestory()
	{
		waitDestoryHandle = handle_decoder;
		handle_decoder = IntPtr.Zero;
		isRequestDestory = true;
	}

	private void OnYUVCallBack(IntPtr frame, int width, int height, int y_pitch, IntPtr y_plane, int u_pitch, IntPtr u_plane, int v_pitch, IntPtr v_plane)
	{
		if ((GetSizeEvent?.Invoke()).HasValue && width != 0 && height != 0)
		{
			RenderModel renderModel = new RenderModel
			{
				Width = width,
				Height = height,
				YPitch = y_pitch,
				UPitch = u_pitch,
				VPitch = v_pitch,
				YPlan = y_plane,
				UPlan = u_plane,
				VPlan = v_plane,
				Frame = frame
			};
			if (flag_d3d_sync)
			{
				renderList.Add(renderModel);
			}
			else
			{
				OnRender(renderModel);
			}
		}
	}

	private IntPtr OnGetAudioCallBack(ref int length)
	{
		IntPtr intPtr = IntPtr.Zero;
		if (firstGetAudio)
		{
			TransitAudioData item;
			while (_audioPCMCollection.TryTake(out item, 10))
			{
			}
			firstGetAudio = false;
		}
		CancellationToken token = pcmCancellation.Token;
		try
		{
			TransitAudioData transitAudioData = _audioPCMCollection.Take(token);
			length = transitAudioData.data.Length;
			intPtr = ObjectToIntPtr(transitAudioData.data);
		}
		catch (Exception)
		{
		}
		if (!token.IsCancellationRequested && intPtr == IntPtr.Zero)
		{
			intPtr = OnGetAudioCallBack(ref length);
		}
		return intPtr;
	}

	private async Task DestoryAudioAsync()
	{
	}

	private IntPtr ObjectToIntPtr(object value)
	{
		GCHandle gCHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
		IntPtr zero = IntPtr.Zero;
		try
		{
			return gCHandle.AddrOfPinnedObject();
		}
		finally
		{
			if (gCHandle.IsAllocated)
			{
				gCHandle.Free();
			}
		}
	}

	public static void AudioSample(AudioSampleRate audioSample)
	{
		Instance.audioSampleRate = audioSample;
	}

	public int InitPCMPlayer(bool android)
	{
		_lastAndroid = android;
		audioPlay?.Stop();
		audioPlay = new AudioPlay();
		audioPlay.Init(android, (int)audioSampleRate);
		return 0;
	}

	public void StopPushFrame()
	{
		Trace.TraceInformation("pause push frame");
		if (_frameCollection.Count > 5)
		{
			double num = Math.Ceiling((double)_frameCollection.Count / 30.0) * 30.0 / 60.0;
			if (num > 0.0)
			{
				GA4Service.Tracing(EventCategory.Screen_Mirror_Successful, EventAction.SMS_Frame_Delay, $"Time: {num}");
			}
		}
		Trace.TraceInformation($"frame count: {_frameCollection.Count}");
		while (_frameCollection.Count > 0)
		{
			_frameCollection.TryTake(out var _, 10);
		}
		while (_audioPCMCollection.Count > 0)
		{
			_audioPCMCollection.TryTake(out var _, 10);
		}
		_isPCMClosed = true;
		pcmCancellation?.Cancel();
	}

	public void Stop(bool isExit)
	{
		if (isExit)
		{
			renderCancel.Cancel();
		}
		audioPlay?.Stop();
		RequestDestory();
		lastWidth = 0;
		lastHeight = 0;
		isExited = isExit;
	}

	private void ListenFPS()
	{
	}

	public void SetSync(bool sync)
	{
		flag_d3d_sync = sync;
		DoRender();
	}

	private void DoRender()
	{
		if (flag_renderThread)
		{
			return;
		}
		flag_renderThread = true;
		if (!flag_d3d_sync)
		{
			return;
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			while (GetSizeEvent == null)
			{
				Thread.Sleep(20);
			}
			try
			{
				while (!isExited)
				{
					RenderModel m = renderList.Take(renderCancel.Token);
					OnRender(m);
				}
			}
			catch (Exception)
			{
			}
			flag_renderThread = false;
		});
		thread.Name = "Render";
		thread.IsBackground = true;
		thread.Start();
	}

	private void OnRender(RenderModel m)
	{
		int width = m.Width;
		int height = m.Height;
		if (width != lastWidth || height != lastHeight)
		{
			lock (lock_d3d)
			{
				if (width != lastWidth || height != lastHeight)
				{
					lastWidth = width;
					lastHeight = height;
					D3DImage.Dispatcher.Invoke(delegate
					{
						D3DImage.SetupSurface(m.Width, m.Height, D3DFormat);
					});
					this.FrameSizeChagedEvent?.Invoke(new FrameSize
					{
						Width = m.Width,
						Height = m.Height
					});
				}
			}
		}
		if (D3DFormat == FrameFormat.ARGB32)
		{
			D3DImage.Render(m.YPlan);
		}
		else
		{
			D3DImage.Render(m.YPlan, m.YPitch, m.UPlan, m.UPitch, m.VPlan, m.VPitch);
		}
		if (m.Frame == IntPtr.Zero && !flag_d3d_sync)
		{
			if (D3DFormat == FrameFormat.ARGB32)
			{
				Marshal.FreeHGlobal(m.YPlan);
			}
			else
			{
				Marshal.FreeHGlobal(m.YPlan);
				Marshal.FreeHGlobal(m.UPlan);
				Marshal.FreeHGlobal(m.VPlan);
			}
		}
		_renderFPS++;
		m.Dispose();
	}

	public void PushFrame(ScrcpyMSVC.VideoFreamArgs freamArgs)
	{
		socket_fps++;
		IntPtr intPtr = freamArgs.Yplane;
		IntPtr intPtr2 = freamArgs.Uplane;
		IntPtr intPtr3 = freamArgs.Vplane;
		if (!flag_d3d_sync)
		{
			if (D3DFormat != FrameFormat.ARGB32)
			{
				int num = (int)((double)(freamArgs.Vpitch * freamArgs.Height) * 0.5);
				int num2 = (int)((double)(freamArgs.Upitch * freamArgs.Height) * 0.5);
				int num3 = freamArgs.Ypitch * freamArgs.Height;
				intPtr = Marshal.AllocHGlobal(num3);
				intPtr2 = Marshal.AllocHGlobal(num2);
				intPtr3 = Marshal.AllocHGlobal(num);
				Interop.Memcpy(intPtr, freamArgs.Yplane, num3);
				Interop.Memcpy(intPtr2, freamArgs.Uplane, num2);
				Interop.Memcpy(intPtr3, freamArgs.Vplane, num);
			}
			else
			{
				int length = freamArgs.Ypitch * freamArgs.Height;
				Interop.Memcpy(intPtr, freamArgs.Yplane, length);
			}
		}
		OnYUVCallBack(IntPtr.Zero, freamArgs.Width, freamArgs.Height, freamArgs.Ypitch, intPtr, freamArgs.Upitch, intPtr2, freamArgs.Vpitch, intPtr3);
	}

	public void UpdateWindowSize(int w, int h)
	{
	}

	public void FullScreenChange()
	{
	}
}
