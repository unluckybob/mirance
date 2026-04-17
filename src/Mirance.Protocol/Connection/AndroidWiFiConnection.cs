using Core.MirroringConnection.Interface;

namespace Core.MirroringConnection.Connection;

public class AndroidWiFiConnection : IConnection
{
	private static AndroidWiFiConnection _instance;

	private static object _lockInit = new object();

	private AndroidWiFiSocketService androidWiFiSocketService;

	public static AndroidWiFiConnection Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_lockInit)
				{
					if (_instance == null)
					{
						_instance = new AndroidWiFiConnection();
					}
				}
			}
			return _instance;
		}
	}

	private AndroidWiFiConnection()
	{
		androidWiFiSocketService = new AndroidWiFiSocketService();
	}

	public void Init()
	{
		androidWiFiSocketService.Start();
	}

	public void Connect()
	{
	}

	public void StopConnect()
	{
		androidWiFiSocketService.CloseClient();
	}
}
