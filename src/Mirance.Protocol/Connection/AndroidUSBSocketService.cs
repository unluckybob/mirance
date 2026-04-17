using System.Net;
using Core.MirroringConnection.Interface;
using Core.MirroringConnection.MSocket.Service;

namespace Core.MirroringConnection.Connection;

internal class AndroidUSBSocketService : ISocketService
{
	private AndroidUSBService androidUSBService;

	public AndroidUSBSocketService()
	{
		androidUSBService = new AndroidUSBService();
		androidUSBService.EventClientClosed += AndroidUSBService_EventClientClosed;
	}

	private void AndroidUSBService_EventClientClosed(string obj)
	{
	}

	public void Start()
	{
		androidUSBService.Listen(IPAddress.Any, PortConfig.AndroidUSB);
	}

	public void CloseClient()
	{
		androidUSBService.StopListen();
	}
}
