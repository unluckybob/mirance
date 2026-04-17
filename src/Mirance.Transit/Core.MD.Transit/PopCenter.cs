#define TRACE
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mirance.Model;

namespace Mirance;

public class PopCenter
{
	private static PopCenter _instance;

	private static object _lockInit = new object();

	public static PopCenter Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new PopCenter();
					}
				}
			}
			return _instance;
		}
	}

	public event Action<TransitDataBase> PopAudioEvent;

	public event Action<TransitDataBase> PopFrameEvent;

	private PopCenter()
	{
	}

	internal async Task PopAudioAsync(TransitDataBase data)
	{
		await Task.Run(delegate
		{
			PopAudio(data);
		});
	}

	internal void PopAudio(TransitDataBase data)
	{
		try
		{
			this.PopAudioEvent?.Invoke(data);
		}
		catch (Exception arg)
		{
			Trace.TraceWarning($"pop audio exception: {arg}");
		}
	}

	internal async Task PopFrameAsync(TransitDataBase data)
	{
		await Task.Run(delegate
		{
			PopFrame(data);
		});
	}

	internal void PopFrame(TransitDataBase data)
	{
		try
		{
			while (this.PopFrameEvent == null)
			{
				Thread.Sleep(10);
			}
			this.PopFrameEvent?.Invoke(data);
		}
		catch (Exception arg)
		{
			Trace.TraceWarning($"pop frame exception: {arg}");
		}
	}
}
