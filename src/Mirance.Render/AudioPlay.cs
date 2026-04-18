#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Mirance.Render.API.Android;
using Core.MD.Transit.Model;
using NAudio.Wave;

namespace Mirance.Render;

internal class AudioPlay
{
	private CancellationTokenSource pcmCancellation;

	private IntPtr handle_audioPlayer;

	private libdecode.audio_callabck callBack_Audio;

	private object lock_audio = new object();

	private BufferedProvider bufferedWaveProvider;

	private WaveOutEvent WaveOut;

	private bool stoped;

	private bool canPlay;

	private BlockingCollection<TransitAudioData> _audioPCMCollection = new BlockingCollection<TransitAudioData>();

	private bool firstGetAudio = true;

	public void Init(bool android, int sampleRate)
	{
		if (NAudio.Wave.WaveOut.DeviceCount <= 0)
		{
			return;
		}
		canPlay = true;
		pcmCancellation = new CancellationTokenSource();
		try
		{
			callBack_Audio = OnGetAudioCallBack;
			handle_audioPlayer = libdecode.create_audio_decoder();
			if (!libdecode.init_audio(handle_audioPlayer, android ? 960 : 480, sampleRate, 2, callBack_Audio))
			{
				Trace.TraceInformation("play audio width naudio");
				bufferedWaveProvider = new BufferedProvider(new WaveFormat(sampleRate, 16, 2));
				bufferedWaveProvider.DiscardOnBufferOverflow = true;
				if (android)
				{
					bufferedWaveProvider.ReadFully = false;
					bufferedWaveProvider.BufferLength = 38400;
				}
				PushPCM(android);
			}
		}
		catch (Exception ex)
		{
			Trace.TraceWarning("init audio play ex: " + ex.Message);
			canPlay = false;
		}
	}

	public void Add(TransitDataBase obj)
	{
		if (!canPlay)
		{
			return;
		}
		if (_audioPCMCollection.Count > 60)
		{
			TransitAudioData item;
			while (_audioPCMCollection.TryTake(out item, 1))
			{
			}
		}
		TransitAudioData item2 = (TransitAudioData)obj;
		_audioPCMCollection.TryAdd(item2);
	}

	private void PushPCM(bool android)
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			WaveOut?.Stop();
			WaveOut = new WaveOutEvent();
			WaveOut.DesiredLatency = (android ? 250 : 100);
			WaveOut.PlaybackStopped += WaveOut_PlaybackStopped;
			while (!stoped && !pcmCancellation.IsCancellationRequested)
			{
				try
				{
					TransitAudioData transitAudioData = _audioPCMCollection.Take(pcmCancellation.Token);
					if (transitAudioData != null && WaveOut != null)
					{
						if (WaveOut.PlaybackState != PlaybackState.Playing && NAudio.Wave.WaveOut.DeviceCount > 0)
						{
							bufferedWaveProvider.ClearBuffer();
							WaveOut.Init(bufferedWaveProvider);
							WaveOut.Play();
						}
						bufferedWaveProvider.AddSamples(transitAudioData.data, 0, transitAudioData.data.Length);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceInformation("audio play ex:" + ex.Message);
				}
			}
			canPlay = false;
		});
		thread.Priority = ThreadPriority.AboveNormal;
		thread.Name = "PCM Player";
		thread.Start();
	}

	private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
	{
		Trace.TraceInformation("audio play stoped");
		(sender as WaveOutEvent).Dispose();
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

	public void Stop()
	{
		if (!stoped)
		{
			canPlay = false;
			stoped = true;
			TransitAudioData item;
			while (_audioPCMCollection.TryTake(out item, 10))
			{
			}
			pcmCancellation.Cancel();
			if (handle_audioPlayer != IntPtr.Zero)
			{
				libdecode.destory_audio(handle_audioPlayer);
				handle_audioPlayer = IntPtr.Zero;
			}
		}
	}
}
