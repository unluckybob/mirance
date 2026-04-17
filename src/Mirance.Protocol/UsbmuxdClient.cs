using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Mirance.Protocol
{
    /// <summary>
    /// iOS Device information
    /// </summary>
    public class IOSDevice
    {
        public int DeviceID { get; set; }
        public string SerialNumber { get; set; }
        public string ProductID { get; set; }
        public string LocationID { get; set; }
        public string ConnectionType { get; set; }
        public int ConnectionSpeed { get; set; }
        public string UDID { get; set; }
        
        public override string ToString()
        {
            return $"iPhone (UDID: {UDID?.Substring(0, Math.Min(8, UDID?.Length ?? 0))}...)";
        }
    }

    /// <summary>
    /// usbmuxd protocol message types
    /// </summary>
    public enum UsbmuxMessageType : uint
    {
        Result = 1,
        Connect = 2,
        Disconnect = 3,
        ListDevices = 4,
        ListDevicesLong = 5,
        ReadBUID = 6,
        ReadDeviceInfo = 7,
        DeletePairRecord = 8,
        StartSession = 9,
        StopSession = 10,
        Pair = 11,
        Unpair = 12
    }

    /// <summary>
    /// usbmuxd result codes
    /// </summary>
    public enum UsbmuxResultCode : int
    {
        OK = 0,
        BadCommand = -1,
        BadDevice = -2,
        ConnectionRefused = -3,
        MalformedRequest = -4,
        BadVersion = -5,
        StartSessionFailed = -6
    }

    /// <summary>
    /// usbmuxd protocol header
    /// </summary>
    public struct UsbmuxHeader
    {
        public uint Length;
        public UsbmuxMessageType MessageType;
        public uint Version;
        public uint RequestID;
        
        public const int Size = 16;
        
        public byte[] ToBytes()
        {
            var buffer = new byte[Size];
            BitConverter.GetBytes(Length).CopyTo(buffer, 0);
            BitConverter.GetBytes((uint)MessageType).CopyTo(buffer, 4);
            BitConverter.GetBytes(Version).CopyTo(buffer, 8);
            BitConverter.GetBytes(RequestID).CopyTo(buffer, 12);
            return buffer;
        }
        
        public static UsbmuxHeader Parse(byte[] data)
        {
            return new UsbmuxHeader
            {
                Length = BitConverter.ToUInt32(data, 0),
                MessageType = (UsbmuxMessageType)BitConverter.ToUInt32(data, 4),
                Version = BitConverter.ToUInt32(data, 8),
                RequestID = BitConverter.ToUInt32(data, 12)
            };
        }
    }

    /// <summary>
    /// usbmuxd Protocol - Communicates with iOS devices via USB
    /// Uses libusbmuxd protocol on TCP port 27019
    /// </summary>
    public class UsbmuxdClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _connected;
        private uint _requestID = 1;
        private readonly object _lock = new object();
        
        public const int DefaultPort = 27019;
        public const string Localhost = "127.0.0.1";
        
        public const string ClientVersion = "libusbmuxd 1.1.0";
        public const string ProgName = "MIRANCE";
        public const int LibUsbmuxVersion = 3;

        public bool IsConnected => _connected;

        /// <summary>
        /// Connect to usbmuxd daemon
        /// </summary>
        public async Task<bool> ConnectAsync(string host = Localhost, int port = DefaultPort)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _stream.ReadTimeout = 10000;
                _stream.WriteTimeout = 10000;
                _connected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Usbmuxd] Failed to connect: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from usbmuxd
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            _connected = false;
        }

        /// <summary>
        /// Get list of connected devices
        /// </summary>
        public async Task<List<IOSDevice>> GetDevicesAsync()
        {
            var devices = new List<IOSDevice>();
            
            if (!_connected) return devices;
            
            // Send ListDevices message
            var request = CreateRequest(UsbmuxMessageType.ListDevices, new byte[0]);
            await SendAsync(request);
            
            // Receive response
            var response = await ReceiveAsync();
            if (response != null && response.Length > 16)
            {
                // Parse device list from response
                // Binary format: number of devices (4 bytes) followed by device entries
                int deviceCount = BitConverter.ToInt32(response, 16);
                int offset = 20;
                
                for (int i = 0; i < deviceCount && offset + 48 <= response.Length; i++)
                {
                    var device = new IOSDevice
                    {
                        DeviceID = BitConverter.ToInt32(response, offset),
                        LocationID = BitConverter.ToUInt32(response, offset + 4).ToString("X8"),
                        SerialNumber = Encoding.ASCII.GetString(response, offset + 8, 32).Trim('\0'),
                        ProductID = BitConverter.ToUInt16(response, offset + 40).ToString("X4"),
                        ConnectionType = (response[offset + 42] == 1) ? "USB" : "Network",
                        ConnectionSpeed = BitConverter.ToInt32(response, offset + 44)
                    };
                    devices.Add(device);
                    offset += 48;
                }
            }
            
            return devices;
        }

        /// <summary>
        /// Read BUID (Board Unique ID) from device
        /// </summary>
        public async Task<string> ReadBUIDAsync()
        {
            if (!_connected) return null;
            
            var request = CreateRequest(UsbmuxMessageType.ReadBUID, new byte[0]);
            await SendAsync(request);
            
            var response = await ReceiveAsync();
            if (response != null && response.Length > 16)
            {
                // BUID is in the response data
                return Encoding.ASCII.GetString(response, 16, response.Length - 16).Trim('\0');
            }
            return null;
        }

        /// <summary>
        /// Connect to a device and request a port forwarding
        /// </summary>
        public async Task<bool> ConnectToDeviceAsync(int deviceID, int port)
        {
            if (!_connected) return false;
            
            // Create Connect plist-like message
            // Format: DeviceID (4 bytes) + Port (4 bytes) + reserved (8 bytes)
            var data = new byte[16];
            BitConverter.GetBytes((uint)deviceID).CopyTo(data, 0);
            BitConverter.GetBytes((uint)port).CopyTo(data, 4);
            
            var request = CreateRequest(UsbmuxMessageType.Connect, data);
            await SendAsync(request);
            
            var response = await ReceiveAsync();
            if (response != null && response.Length >= 20)
            {
                int result = BitConverter.ToInt32(response, 16);
                return result == 0;
            }
            return false;
        }

        /// <summary>
        /// Start a session with the device
        /// </summary>
        public async Task<string> StartSessionAsync(int deviceID)
        {
            if (!_connected) return null;
            
            // StartSession is done through Lockdown service after connecting
            // This returns a session that can be used for Lockdown
            return $"session_{deviceID}_{_requestID}";
        }

        private byte[] CreateRequest(UsbmuxMessageType type, byte[] data)
        {
            lock (_lock)
            {
                var header = new UsbmuxHeader
                {
                    Length = (uint)(16 + data.Length),
                    MessageType = type,
                    Version = 0,
                    RequestID = _requestID++
                };
                
                var buffer = new byte[16 + data.Length];
                header.ToBytes().CopyTo(buffer, 0);
                data.CopyTo(buffer, 16);
                return buffer;
            }
        }

        private async Task SendAsync(byte[] data)
        {
            if (_stream == null) return;
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        private async Task<byte[]> ReceiveAsync()
        {
            if (_stream == null) return null;
            
            // Read header first
            var headerBuffer = new byte[16];
            int bytesRead = await _stream.ReadAsync(headerBuffer, 0, 16);
            if (bytesRead < 16) return null;
            
            var header = UsbmuxHeader.Parse(headerBuffer);
            int totalLength = (int)header.Length;
            
            // Read rest of data
            var data = new byte[totalLength];
            headerBuffer.CopyTo(data, 0);
            
            int remaining = totalLength - 16;
            if (remaining > 0)
            {
                int offset = 16;
                while (offset < totalLength)
                {
                    bytesRead = await _stream.ReadAsync(data, offset, totalLength - offset);
                    if (bytesRead <= 0) break;
                    offset += bytesRead;
                }
            }
            
            return data;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
