#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Mirance.Model;
using Mirance.Transit;

namespace Mirance;

public class PushCenter
{
	private static PushCenter _instance;

	private static object _lockInit = new object();

	private object _lockAudio = new object();

	private ConcurrentDictionary<string, TransitBase> AudioCollection;

	private object _lockFarme = new object();

	private ConcurrentDictionary<string, TransitBase> FrameCollection;

	private ulong _videoPts;

	public bool IsCanPushAudio { get; set; } = true;

	public static PushCenter Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new PushCenter();
					}
				}
			}
			return _instance;
		}
	}

	private PushCenter()
	{
		FrameCollection = new ConcurrentDictionary<string, TransitBase>();
		AudioCollection = new ConcurrentDictionary<string, TransitBase>();
	}

	public void PushFrame(TransitDataBase dataBase)
	{
		if (string.IsNullOrWhiteSpace(dataBase?.identity))
		{
			return;
		}
		_videoPts = dataBase.pts;
		if (!FrameCollection.TryGetValue(dataBase.identity, out var value))
		{
			lock (_lockFarme)
			{
				if (!FrameCollection.TryGetValue(dataBase.identity, out value))
				{
					value = new TransitFrame(dataBase.identity);
					FrameCollection[dataBase.identity] = value;
				}
			}
		}
		value.Push(dataBase);
	}

	public void PushAudio(TransitDataBase dataBase)
	{
		if (string.IsNullOrWhiteSpace(dataBase?.identity))
		{
			return;
		}
		if (dataBase.pts == 0L)
		{
			dataBase.pts = (uint)DateTime.Now.Ticks;
		}
		if (!AudioCollection.TryGetValue(dataBase.identity, out var value))
		{
			lock (_lockAudio)
			{
				if (!AudioCollection.TryGetValue(dataBase.identity, out value))
				{
					value = new TransitAudio(dataBase.identity);
					AudioCollection[dataBase.identity] = value;
				}
			}
		}
		value.Push(dataBase);
	}

	public void StopAudioPush(string identity)
	{
		if (!string.IsNullOrWhiteSpace(identity))
		{
			if (AudioCollection.TryRemove(identity, out var value))
			{
				value.Stop();
			}
			Trace.TraceInformation("<" + identity + "> stop audio push");
		}
	}

	public void StopFramePush(string identity)
	{
		StopAudioPush(identity);
		if (!string.IsNullOrWhiteSpace(identity))
		{
			if (FrameCollection.TryRemove(identity, out var value))
			{
				value.Stop();
			}
			Trace.TraceInformation("<" + identity + "> stop frame push");
		}
	}

	public void StopAll()
	{
		foreach (TransitBase item in AudioCollection.Values.ToList())
		{
			item.Stop();
		}
		foreach (TransitBase item2 in FrameCollection.Values.ToList())
		{
			item2.Stop();
		}
	}
}
