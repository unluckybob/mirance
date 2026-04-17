using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public byte[] Flags;       // Bytes 1-3: Flags
        public uint Sequence;       // Bytes 4-7: Sequence (little-endian)
        public ulong Timestamp;     // Bytes 8-15: Timestamp (little-endian)
        public uint Size;           // Bytes 16-19: Size (little-endian)
        
        // Data (41068 bytes)
        public byte[] Data;         // Bytes 20+
        
        public const int FrameSize = 41088;
        public const int HeaderSize = 20;
        public const int DataSize = 41068;
        
        /// <summary>
        /// Parse a video frame from raw bytes
        /// </summary>
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
        
        /// <summary>
        /// Serialize frame to bytes
        /// </summary>
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
            _listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            _listener.Start();
            _running = true;
            
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
                            // Partial frame or error
                            Console.WriteLine($"Received {bytesRead} bytes (expected {VideoFrame.FrameSize})");
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_running)
                        Console.WriteLine($"Video receiver error: {ex.Message}");
                }
            }
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}
