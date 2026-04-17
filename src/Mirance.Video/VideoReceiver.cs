using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace Mirance.Video
{
    /// <summary>
    /// Video frame structure (41088 bytes total)
    /// Header: 20 bytes
    /// Data: 41068 bytes
    /// </summary>
    public struct VideoFrame
    {
        // Header (20 bytes)
        public byte Type;           // Byte 0: Frame type (0x01)
        public byte[] Flags;        // Bytes 1-3: Flags
        public uint Sequence;       // Bytes 4-7: Sequence (little-endian)
        public ulong Timestamp;     // Bytes 8-15: Timestamp (little-endian)
        public uint Size;          // Bytes 16-19: Size (little-endian)
        
        // Data (41068 bytes)
        public byte[] Data;        // Bytes 20+
        
        public const int FrameSize = 41088;
        public const int HeaderSize = 20;
        public const int DataSize = 41068;
        
        public static VideoFrame Parse(byte[] raw)
        {
            if (raw.Length < FrameSize)
                throw new ArgumentException($"Frame must be {FrameSize} bytes");
            
            return new VideoFrame
            {
                Type = raw[0],
                Flags = new byte[] { raw[1], raw[2], raw[3] },
                Sequence = BitConverter.ToUInt32(raw, 4),
                Timestamp = BitConverter.ToUInt64(raw, 8),
                Size = BitConverter.ToUInt32(raw, 16),
                Data = new ArraySegment<byte>(raw, HeaderSize, DataSize).Array
            };
        }
        
        public byte[] ToBytes()
        {
            var buffer = new byte[FrameSize];
            
            buffer[0] = Type;
            if (Flags != null && Flags.Length >= 3)
            {
                buffer[1] = Flags[0];
                buffer[2] = Flags[1];
                buffer[3] = Flags[2];
            }
            
            BitConverter.GetBytes(Sequence).CopyTo(buffer, 4);
            BitConverter.GetBytes(Timestamp).CopyTo(buffer, 8);
            BitConverter.GetBytes(Size).CopyTo(buffer, 16);
            
            if (Data != null && Data.Length >= DataSize)
            {
                Array.Copy(Data, 0, buffer, HeaderSize, DataSize);
            }
            
            return buffer;
        }
    }

    /// <summary>
    /// Video frame received event args
    /// </summary>
    public class FrameReceivedEventArgs : EventArgs
    {
        public VideoFrame Frame { get; }
        public DateTime Timestamp { get; }
        
        public FrameReceivedEventArgs(VideoFrame frame)
        {
            Frame = frame;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Internal TCP server for video/control
    /// Mirrors uses localhost ports 4720, 4793, 49350, 49678
    /// </summary>
    public class InternalTcpServer : IDisposable
    {
        // Server ports (from PCAP analysis)
        public const int PortHttpApi = 4720;
        public const int PortControl = 4793;
        public const int PortVideo = 49350;
        public const int PortAuxControl = 49678;
        
        private TcpListener _httpListener;
        private TcpListener _controlListener;
        private TcpListener _videoListener;
        private TcpListener _auxListener;
        
        private bool _running;
        private Task _httpTask;
        private Task _controlTask;
        
        public event EventHandler<FrameReceivedEventArgs> FrameReceived;
        public event EventHandler<string> ControlReceived;
        
        public bool IsRunning => _running;
        
        /// <summary>
        /// Start all internal TCP servers
        /// </summary>
        public async Task StartAsync()
        {
            _running = true;
            
            // Start HTTP API server (port 4720)
            _httpListener = new TcpListener(IPAddress.Loopback, PortHttpApi);
            _httpListener.Start();
            _httpTask = Task.Run(async () => await HttpServerLoopAsync());
            
            // Start Control server (port 4793)
            _controlListener = new TcpListener(IPAddress.Loopback, PortControl);
            _controlListener.Start();
            _controlTask = Task.Run(async () => await ControlServerLoopAsync());
            
            Console.WriteLine("[Internal] Servers started on ports 4720, 4793");
        }
        
        /// <summary>
        /// Stop all servers
        /// </summary>
        public void Stop()
        {
            _running = false;
            _httpListener?.Stop();
            _controlListener?.Stop();
        }
        
        private async Task HttpServerLoopAsync()
        {
            while (_running)
            {
                try
                {
                    var client = await _httpListener.AcceptTcpClientAsync();
                    _ = Task.Run(async () => await HandleHttpClientAsync(client));
                }
                catch { }
            }
        }
        
        private async Task HandleHttpClientAsync(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                
                if (read > 0)
                {
                    var request = Encoding.ASCII.GetString(buffer, 0, read);
                    
                    // Handle /ping
                    if (request.Contains("GET /ping"))
                    {
                        var response = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: application/json\r\n" +
                                     "Content-Length: 29\r\n\r\n" +
                                     "{\"path\": \"/ping\", \"data\": true}";
                        var responseBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                
                client.Close();
            }
            catch { }
        }
        
        private async Task ControlServerLoopAsync()
        {
            while (_running)
            {
                try
                {
                    var client = await _controlListener.AcceptTcpClientAsync();
                    _ = Task.Run(async () => await HandleControlClientAsync(client));
                }
                catch { }
            }
        }
        
        private async Task HandleControlClientAsync(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                
                if (read > 0)
                {
                    var data = Encoding.ASCII.GetString(buffer, 0, read);
                    ControlReceived?.Invoke(this, data);
                }
                
                client.Close();
            }
            catch { }
        }
        
        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// Video stream receiver - listens on port 49350
    /// </summary>
    public class VideoReceiver : IDisposable
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _running;
        private Task _receiveTask;
        
        public const int VideoPort = 49350;
        
        public event EventHandler<FrameReceivedEventArgs> FrameReceived;
        
        /// <summary>
        /// Start listening for video frames
        /// </summary>
        public async Task StartAsync(int port = VideoPort)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _running = true;
            
            Console.WriteLine($"[Video] Listening on port {port}");
            
            _receiveTask = Task.Run(async () => await ReceiveLoopAsync());
        }
        
        /// <summary>
        /// Stop receiving
        /// </summary>
        public void Stop()
        {
            _running = false;
            _client?.Close();
            _listener?.Stop();
        }
        
        private async Task ReceiveLoopAsync()
        {
            while (_running)
            {
                try
                {
                    _client = await _listener.AcceptTcpClientAsync();
                    _stream = _client.GetStream();
                    
                    var buffer = new byte[VideoFrame.FrameSize];
                    
                    while (_running && _client.Connected)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                        
                        if (bytesRead == VideoFrame.FrameSize)
                        {
                            var frame = VideoFrame.Parse(buffer);
                            FrameReceived?.Invoke(this, new FrameReceivedEventArgs(frame));
                        }
                        else if (bytesRead > 0)
                        {
                            Console.WriteLine($"[Video] Received {bytesRead} bytes (expected {VideoFrame.FrameSize})");
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    _client.Close();
                }
                catch (Exception ex)
                {
                    if (_running)
                        Console.WriteLine($"[Video] Error: {ex.Message}");
                }
            }
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}
