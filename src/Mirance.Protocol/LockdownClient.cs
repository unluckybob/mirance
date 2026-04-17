using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Mirance.Protocol
{
    /// <summary>
    /// Lockdown service message types
    /// </summary>
    public enum LockdownMessageType
    {
        QueryType,
        StartSession,
        StopSession,
        GetValue,
        SetValue,
        RemoveValue,
        Lookup,
        ReceiveFile,
        SendFile
    }

    /// <summary>
    /// Lockdown error codes
    /// </summary>
    public enum LockdownError
    {
        Success = 0,
        InvalidArgument = -1,
        InvalidResponse = -2,
        MissingArgument = -3,
        NotFound = -4,
        LockdownReceiveError = -5,
        LockdownSendError = -6,
        ConnectionFailed = -7,
        SessionActive = -8,
        SessionInactive = -9,
        NoConflictingAssignment = -10,
        PairingError = -11,
        ServiceNotAvailable = -12
    }

    /// <summary>
    /// iOS Service information
    /// </summary>
    public class IOSService
    {
        public string Name { get; set; }
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
    }

    /// <summary>
    /// Lockdown client for iOS device communication
    /// Implements the Lockdown protocol over TLS
    /// </summary>
    public class LockdownClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private SslStream _sslStream;
        private bool _sslEnabled;
        private string _sessionID;
        private int _devicePort;
        private bool _connected;
        private string _hostID;
        
        public const int DefaultPort = 32498;
        
        public bool IsConnected => _connected;
        public bool IsSSLEnabled => _sslEnabled;
        public string SessionID => _sessionID;

        /// <summary>
        /// Connect to Lockdown service on the device
        /// </summary>
        public async Task<bool> ConnectAsync(int port = DefaultPort)
        {
            try
            {
                _devicePort = port;
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", port);
                _stream = _client.GetStream();
                _stream.ReadTimeout = 30000;
                _stream.WriteTimeout = 30000;
                _connected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lockdown] Failed to connect: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start a session with the device
        /// </summary>
        public async Task<LockdownError> StartSessionAsync(string hostID, string systemBUID)
        {
            _hostID = hostID;
            
            var request = new Dictionary<string, object>
            {
                { "Label", "MIRANCE" },
                { "Request", "StartSession" },
                { "HostID", hostID },
                { "SystemBUID", systemBUID }
            };
            
            var response = await SendPlistAsync(request);
            
            if (response != null && response.ContainsKey("SessionID"))
            {
                _sessionID = response["SessionID"].ToString();
                
                if (response.ContainsKey("EnableSessionSSL") && 
                    response["EnableSessionSSL"].ToString() == "true")
                {
                    await EnableSSLAsync();
                }
                
                return LockdownError.Success;
            }
            
            return LockdownError.InvalidResponse;
        }

        /// <summary>
        /// Enable SSL for the session
        /// </summary>
        public async Task<bool> EnableSSLAsync()
        {
            try
            {
                _sslStream = new SslStream(
                    _stream,
                    false,
                    (sender, certificate, chain, errors) => true
                );
                
                await _sslStream.AuthenticateAsClientAsync("localhost");
                _sslEnabled = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lockdown] SSL enable failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Query device for a service type
        /// </summary>
        public async Task<IOSService> QueryTypeAsync(string serviceType)
        {
            var request = new Dictionary<string, object>
            {
                { "Label", "MIRANCE" },
                { "Request", "QueryType" },
                { "Type", serviceType }
            };
            
            var response = await SendPlistAsync(request);
            
            if (response != null && response.ContainsKey("Port"))
            {
                return new IOSService
                {
                    Name = serviceType,
                    Port = int.Parse(response["Port"].ToString()),
                    EnableSSL = response.ContainsKey("EnableSSL") && 
                               response["EnableSSL"].ToString() == "true"
                };
            }
            
            return null;
        }

        /// <summary>
        /// Query all available services
        /// </summary>
        public async Task<Dictionary<string, IOSService>> QueryServicesAsync()
        {
            var services = new Dictionary<string, IOSService>();
            
            // Query for common services
            var serviceTypes = new[]
            {
                "com.apple.mobile.lockdown",
                "com.apple.screen_sharing",
                "com.apple.ScreenCaptureService",
                "com.apple.AirPlay"
            };
            
            foreach (var type in serviceTypes)
            {
                var service = await QueryTypeAsync(type);
                if (service != null)
                {
                    services[type] = service;
                }
            }
            
            return services;
        }

        /// <summary>
        /// Get a value from the device
        /// </summary>
        public async Task<string> GetValueAsync(string key)
        {
            var request = new Dictionary<string, object>
            {
                { "Label", "MIRANCE" },
                { "Request", "GetValue" },
                { "Key", key }
            };
            
            var response = await SendPlistAsync(request);
            
            if (response != null && response.ContainsKey("Value"))
            {
                return response["Value"].ToString();
            }
            
            return null;
        }

        /// <summary>
        /// Set a value on the device
        /// </summary>
        public async Task<bool> SetValueAsync(string key, string value)
        {
            var request = new Dictionary<string, object>
            {
                { "Label", "MIRANCE" },
                { "Request", "SetValue" },
                { "Key", key },
                { "Value", value }
            };
            
            var response = await SendPlistAsync(request);
            return response != null;
        }

        /// <summary>
        /// Send a plist request and receive response
        /// </summary>
        private async Task<Dictionary<string, string>> SendPlistAsync(Dictionary<string, object> data)
        {
            try
            {
                // Build plist XML
                var xml = BuildPlist(data);
                var bytes = Encoding.UTF8.GetBytes(xml);
                
                // Send length prefix (4 bytes, big-endian)
                var length = new byte[4];
                length[0] = (byte)(bytes.Length >> 24);
                length[1] = (byte)(bytes.Length >> 16);
                length[2] = (byte)(bytes.Length >> 8);
                length[3] = (byte)bytes.Length;
                
                var stream = _sslEnabled ? (Stream)_sslStream : _stream;
                
                await stream.WriteAsync(length, 0, 4);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                await stream.FlushAsync();
                
                // Receive response
                var responseLength = new byte[4];
                int read = await stream.ReadAsync(responseLength, 0, 4);
                if (read < 4) return null;
                
                int respLen = (responseLength[0] << 24) | (responseLength[1] << 16) | 
                              (responseLength[2] << 8) | responseLength[3];
                
                var responseBytes = new byte[respLen];
                int totalRead = 0;
                while (totalRead < respLen)
                {
                    read = await stream.ReadAsync(responseBytes, totalRead, respLen - totalRead);
                    if (read <= 0) break;
                    totalRead += read;
                }
                
                // Parse XML response
                var responseXml = Encoding.UTF8.GetString(responseBytes);
                return ParsePlist(responseXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lockdown] Request failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Build XML plist from dictionary
        /// </summary>
        private string BuildPlist(Dictionary<string, object> data)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("plist",
                    new XAttribute("version", "1.0"),
                    new XElement("dict",
                        data.Select(kvp => 
                            new XElement("key", kvp.Key))
                        .Concat(
                            data.Select(kvp => FormatPlistValue(kvp.Value))
                        )
                    )
                )
            );
            
            return doc.ToString();
        }

        /// <summary>
        /// Format a value for plist
        /// </summary>
        private XElement FormatPlistValue(object value)
        {
            if (value is string str)
            {
                return new XElement("string", str);
            }
            else if (value is int intVal)
            {
                return new XElement("integer", intVal.ToString());
            }
            else if (value is bool boolVal)
            {
                return new XElement(boolVal ? "true" : "false");
            }
            else
            {
                return new XElement("string", value.ToString());
            }
        }

        /// <summary>
        /// Parse XML plist to dictionary
        /// </summary>
        private Dictionary<string, string> ParsePlist(string xml)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                var doc = XDocument.Parse(xml);
                var dict = doc.Descendants("dict").FirstOrDefault();
                if (dict == null) return result;
                
                var keys = dict.Elements("key").ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i].Value;
                    var next = keys[i].NextNode as XElement;
                    if (next != null)
                    {
                        string value = "";
                        switch (next.Name.LocalName)
                        {
                            case "string":
                            case "integer":
                                value = next.Value;
                                break;
                            case "true":
                                value = "true";
                                break;
                            case "false":
                                value = "false";
                                break;
                        }
                        result[key] = value;
                    }
                }
            }
            catch { }
            
            return result;
        }

        /// <summary>
        /// Disconnect from Lockdown service
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _sslStream?.Close();
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            _connected = false;
            _sslEnabled = false;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
