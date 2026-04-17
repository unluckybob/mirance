#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Mirance.Model;

namespace Mirance.Transit;

internal abstract class TransitBase
{
	private bool _isStop;

	private int fps_push;

	private int fps_decode;

	private CancellationTokenSource tokenSource;

	private AutoResetEvent resetEvent = new AutoResetEvent(initialState: false);

	private ConcurrentQueue<TransitDataBase> Data { get; set; }

	public string Identity { get; private set; }

	public TransitBase(string identity)
	{
		Identity = identity;
		Data = new ConcurrentQueue<TransitDataBase>();
		tokenSource = new CancellationTokenSource();
		LoopPop();
	}

	public void Push(TransitDataBase data)
	{
		fps_push++;
		Data.Enqueue(data);
		resetEvent.Set();
	}

	public void Stop()
	{
		_isStop = true;
		tokenSource.Cancel(throwOnFirstException: true);
		resetEvent.Set();
	}

	private void LoopPop()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				TransitDataBase result;
				while (!_isStop)
				{
					if (Data.TryDequeue(out result))
					{
						OnPop(result);
						fps_decode++;
					}
					else
					{
						resetEvent.WaitOne();
					}
				}
				while (Data.TryDequeue(out result))
				{
				}
			}
			catch (Exception ex)
			{
				Trace.TraceWarning(GetType().Name + "<" + Identity + "> pop data exception: " + ex.Message);
			}
		});
		thread.Priority = ThreadPriority.AboveNormal;
		thread.Name = "LoopPop<" + GetType().Name + ">";
		thread.IsBackground = true;
		thread.Start();
	}

	protected void TransitWatcher()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			while (!_isStop)
			{
				int num = fps_push;
				fps_push = 0;
				int num2 = fps_decode;
				fps_decode = 0;
				Trace.WriteLine($"{GetType().Name}<{Identity}> Push FPS:{num}, Decode FPS:{num2}, Left Count: {Data.Count}");
				Thread.Sleep(1000);
			}
		});
		thread.Name = "Watcher<" + GetType().Name + ">";
		thread.IsBackground = true;
		thread.Start();
	}

	protected abstract void OnPop(TransitDataBase data);
}
