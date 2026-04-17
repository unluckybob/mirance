using System.Net;
using Core.MirroringConnection.Interface;
using Core.MirroringConnection.MSocket.Service;

namespace Core.MirroringConnection.Connection;

internal class AndroidWiFiSocketService : ISocketService
{
	private AndroidQRCodeService androidQRCodeService;

	public AndroidWiFiSocketService()
	{
		androidQRCodeService = new AndroidQRCodeService();
		androidQRCodeService.EventClientClosed += AndroidQRCodeService_EventClientClosed;
	}

	private void AndroidQRCodeService_EventClientClosed(string obj)
	{
	}

	public void Start()
	{
		androidQRCodeService.Listen(IPAddress.Any, 38438);
	}

	public void CloseClient()
	{
		androidQRCodeService.StopListen();
	}
}
