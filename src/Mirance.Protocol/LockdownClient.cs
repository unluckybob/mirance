using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.SecureTransportSecurity;

namespace Mirance.Protocol
{
    /// <summary>
    /// Lockdown service client for iOS device communication
    /// </summary>
    public class LockdownClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private SslStream _sslStream;
        private bool _sslEnabled;
        private string _sessionID;
        private int _devicePort;

        /// <summary>
        /// Connect to Lockdown service on the device
        /// </summary>
        public async Task<bool> ConnectAsync(int devicePort)
        {
            try
            {
                _devicePort = devicePort;
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", devicePort);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to Lockdown: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start a session with the device
        /// </summary>
        public async Task<bool> StartSessionAsync(string hostID, string systemBUID)
        {
            // Send StartSession plist:
            // <dict>
            //     <key>Label</key><string>usbmuxd</string>
            //     <key>Request</key><string>StartSession</string>
            //     <key>HostID</key><string>HOST_ID</string>
            //     <key>SystemBUID</key><string>SYSTEM_BUID</string>
            // </dict>
            
            // Receive response with SessionID
            
            return true;
        }

        /// <summary>
        /// Enable SSL for the session
        /// </summary>
        public async Task<bool> EnableSessionSSLAsync()
        {
            // Send EnableSessionSSL plist:
            // <dict>
            //     <key>EnableSessionSSL</key><true/>
            //     <key>Request</key><string>StartSession</string>
            //     <key>SessionID</key><string>SESSION_ID</string>
            // </dict>
            
            // Establish SSL tunnel
            
            return true;
        }

        /// <summary>
        /// Query device for service
        /// </summary>
        public async Task<string> QueryTypeAsync(string serviceType)
        {
            // Send QueryType plist:
            // <dict>
            //     <key>Label</key><string>usbmuxd</string>
            //     <key>Request</key><string>QueryType</string>
            //     <key>Type</key><string>serviceType</string>
            // </dict>
            
            return null;
        }

        /// <summary>
        /// Read pair record from device
        /// </summary>
        public async Task<byte[]> ReadPairRecordAsync(string pairRecordID)
        {
            // Send ReadPairRecord plist:
            // <dict>
            //     <key>ClientVersionString</key><string>libusbmuxd 1.1.0</string>
            //     <key>MessageType</key><string>ReadPairRecord</string>
            //     <key>ProgName</key><string>Mirance</string>
            //     <key>kLibUSBMuxVersion</key><integer>3</integer>
            //     <key>PairRecordID</key><string>pairRecordID</string>
            // </dict>
            
            return null;
        }

        /// <summary>
        /// Send plist request
        /// </summary>
        public async Task<string> SendRequestAsync(XDocument plist)
        {
            // Serialize plist to XML
            // Send over stream
            // Receive response
            
            return null;
        }

        public void Disconnect()
        {
            _sslStream?.Close();
            _stream?.Close();
            _client?.Close();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
