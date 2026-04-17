#define TRACE
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Core.MirroringConnection.MSocket;

public class Client
{
	private const int endData = 16909060;

	public Action<Client> OnConnectionClosing;

	private string _remoteIP;

	public Socket Socket { get; private set; }

	public bool IsClosed { get; private set; }

	public string RemoteIP
	{
		get
		{
			if (string.IsNullOrEmpty(_remoteIP))
			{
				_remoteIP = ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString();
			}
			return _remoteIP;
		}
	}

	internal Client(Socket socket)
	{
		Socket = socket;
		ClientManager.AddClient(this);
	}

	public static Client TryConnect(string ip, int port)
	{
		if (!IPAddress.TryParse(ip, out var address))
		{
			Trace.TraceInformation($"try parse <{ip}:{port}> failed.");
			return null;
		}
		return TryConnect(new IPEndPoint(address, port));
	}

	public static Client TryConnect(IPEndPoint endPoint)
	{
		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			socket.Connect(endPoint);
		}
		catch (Exception ex)
		{
			Trace.TraceWarning($"connect <{endPoint}> failed: {ex.Message}");
			return null;
		}
		return ClientManager.NewClient(socket);
	}

	public void CheckHeader()
	{
		byte[] array = Receive(4);
		Array.Reverse(array);
		if (BitConverter.ToInt32(array, 0) == 16909060)
		{
			return;
		}
		while (!IsClosed)
		{
			while (!IsClosed && Receive(1)[0] != 1)
			{
			}
			byte[] array2 = Receive(3);
			if (array2[0] == 2 && array2[1] == 3 && array2[2] == 4)
			{
				break;
			}
		}
	}

	public byte[] Receive(int length)
	{
		return Receive(length, SocketFlags.None);
	}

	public byte[] Receive(int length, SocketFlags socketFlags)
	{
		byte[] array = new byte[length];
		int num = 0;
		while (true)
		{
			int num2 = Socket.Receive(array, num, length - num, socketFlags);
			num += num2;
			if (num >= length)
			{
				break;
			}
			if (num2 == 0)
			{
				IsClosed = true;
				break;
			}
		}
		return array;
	}

	public void Send(byte[] data)
	{
		if (data != null)
		{
			Socket.Send(data);
		}
	}

	public void Close()
	{
		IsClosed = true;
		try
		{
			OnConnectionClosing?.Invoke(this);
			Socket.Disconnect(reuseSocket: false);
		}
		catch (Exception)
		{
		}
		finally
		{
			OnConnectionClosing = null;
		}
	}

	protected void WriteLog(string msg)
	{
		Trace.TraceInformation(GetType().Name + ": " + msg);
	}
}
