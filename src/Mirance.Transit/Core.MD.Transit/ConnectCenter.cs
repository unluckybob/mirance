using System;
using System.Threading.Tasks;
using Mirance.Model;

namespace Mirance;

public class ConnectCenter
{
	private static ConnectCenter _instance;

	private static object _lockInit = new object();

	public static ConnectCenter Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new ConnectCenter();
					}
				}
			}
			return _instance;
		}
	}

	public event Action<string> EventCloseConnection;

	public event Action<MirroringMode> EventRequestConnection;

	public event Func<string, bool> EventRequestConnect;

	private ConnectCenter()
	{
	}

	public void CloseConnection(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			this.EventCloseConnection?.Invoke(id);
		}
	}

	public bool RequestConnect(string ip)
	{
		if (this.EventRequestConnect == null || string.IsNullOrEmpty(ip))
		{
			return false;
		}
		return this.EventRequestConnect(ip);
	}

	public void RequestConnection(MirroringMode mirroring)
	{
		if (mirroring != null)
		{
			Task.Run(delegate
			{
				this.EventRequestConnection?.Invoke(mirroring);
			});
		}
	}
}
