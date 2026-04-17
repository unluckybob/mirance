#define TRACE
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Core.MirroringConnection.MSocket;

public abstract class ServiceBase
{
	private Socket _socket;

	private bool isClosed;

	protected int BindPort { get; private set; }

	public event Action<string> EventClientClosed;

	public ServiceBase()
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	public bool Listen(string ip, int port)
	{
		if (!IPAddress.TryParse(ip, out var address))
		{
			WriteLog("parse ip<" + ip + "> failed");
			return false;
		}
		return Listen(address, port);
	}

	public bool Listen(IPAddress address, int port, bool isRetry = false)
	{
		BindPort = port;
		try
		{
			IPEndPoint localEP = new IPEndPoint(address, port);
			_socket.Bind(localEP);
			_socket.Listen(20);
			CreateListenThread();
		}
		catch (Exception ex)
		{
			KillProcess("adb");
			KillProcess("usbmuxd");
			if (isRetry)
			{
				WriteLog($"listen port<{port}> failed: {ex.Message}");
				throw ex;
			}
			Thread.Sleep(2000);
			return Listen(address, port, isRetry: true);
		}
		return true;
	}

	public void KillProcess(string name)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName(name);
			if (processesByName != null && processesByName.Count() > 0)
			{
				Process[] array = processesByName;
				foreach (Process process in array)
				{
					Trace.TraceInformation("kill {0}", name);
					process.Kill();
				}
			}
		}
		catch (Exception ex)
		{
			Trace.TraceError(ex.Message);
		}
	}

	private void CreateListenThread()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			while (!isClosed)
			{
				try
				{
					Socket socket = _socket.Accept();
					OnConnected(socket);
				}
				catch (Exception ex)
				{
					if (_socket.Connected)
					{
						WriteLog($"accept<{_socket.LocalEndPoint}> connection exception: {ex.Message}");
					}
				}
			}
		});
		thread.Name = $"Listener({BindPort})";
		thread.Start();
	}

	private void OnConnected(Socket socket)
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			Client client = ClientManager.NewClient(socket);
			try
			{
				if (!EventCenter.OnRequestConnect(client.RemoteIP))
				{
					WriteLog("<" + client.RemoteIP + "> connect request refused");
					ClientManager.CloseClient(client);
				}
				else
				{
					ClientConnected(client);
				}
			}
			catch (Exception arg)
			{
				WriteLog($"client<{socket.RemoteEndPoint}> connection exception: {arg}");
			}
		});
		thread.Name = $"Client({socket.RemoteEndPoint})";
		thread.Start();
	}

	public void StopListen()
	{
		WriteLog($"stop listen of port<{BindPort}>");
		try
		{
			isClosed = true;
			_socket?.Close();
		}
		catch (Exception)
		{
		}
	}

	protected void WriteLog(string msg)
	{
		Trace.TraceInformation(GetType().Name + ": " + msg);
	}

	protected abstract void ClientConnected(Client client);
}
