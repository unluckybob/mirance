# COMPLETE ANYMIRO IPHONE USB SCREEN MIRRORING - FULL TECHNICAL ANALYSIS
# For Replicating as MIRANCE on GitHub

**VERSION:** 2.0 - Complete Analysis
**DATE:** April 2026
**SCOPE:** iPhone USB Screen Mirroring ONLY (NOT AirPlay, NOT Android)

---

# PART 1: ANYMIRO BINARY ANALYSIS

## 1.1 File Structure (from AnyMiro.zip)

### Main Executables
| File | Size | Purpose |
|------|------|---------|
| AnyMiro.exe | 3.27 MB | Main WPF application |
| AnyMiro.Update.exe | 3.78 MB | Auto-updater |
| iosusb.exe | 9.72 MB | iOS protocol handler (embedded Python) |
| driver.exe | 4.60 MB | USB driver manager |
| uninstall.exe | 1.83 MB | Uninstaller |
| adb.exe | 6.02 MB | Android Debug Bridge |

### Core DLLs
| File | Size | Purpose |
|------|------|---------|
| Core.Connection.dll | 11.35 MB | Device connection management |
| Core.MD.Render.dll | 41.97 MB | DirectX video rendering |
| Service.Mirroring.dll | 33 KB | Mirroring service |
| AirPlayLibrary.dll | 78 KB | Streaming protocol |
| Service.VideoCapture.dll | 45 KB | Video capture |
| Service.AudioCapture.dll | 46 KB | Audio capture |

### USB Components (usbmuxd folder)
| File | Size | Purpose |
|------|------|---------|
| usbmuxd.exe | 2.07 MB | USB multiplexing daemon |
| libusb-1.0.dll | 108 KB | libusb Windows |
| libusb0.dll | 68 KB | libusb0 driver |

### Video/Audio Libraries
| File | Size | Purpose |
|------|------|---------|
| avcodec-58.dll | 34.54 MB | FFmpeg video codec |
| avformat-58.dll | 10.75 MB | FFmpeg container |
| avutil-56.dll | 814 KB | FFmpeg utilities |
| swscale-5.dll | 528 KB | FFmpeg scaling |
| swresample-3.dll | 324 KB | FFmpeg audio |
| NAudio.dll | 513 KB | Windows audio API |
| ALACDecoder.dll | 23 KB | Apple Lossless decoder |

### UI Framework
| File | Size | Purpose |
|------|------|---------|
| HandyControl.dll | 1.34 MB | UI controls |
| Prism.Wpf.dll | 147 KB | MVVM framework |
| Unity.Container.dll | 146 KB | Dependency injection |

---

## 1.2 Key Binaries Analysis

### iosusb.exe (Python-based)
- Contains embedded Python 3.8 runtime
- Implements iOS Lockdown protocol
- Uses plist-cil.dll for plist parsing
- Size: 9.72 MB due to embedded Python

### usbmuxd.exe
- Standard libusbmuxd implementation
- TCP port 27019 for device multiplexing
- Handles USB device enumeration

### Core.Connection.dll (11.35 MB)
Largest DLL - contains all connection logic:
- UsbWatcherService - USB device detection
- Android USB driver management  
- iOS device detection and connection
- Device pairing management
- String references: "libusbmuxd", "lockdown", "iOSConnectError"

---

## 1.3 Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 4.6.1, WPF |
| UI | HandyControl |
| DI | Unity.Container, Autofac |
| MVVM | Prism.Wpf |
| Video | SharpDX, DirectX 11 |
| Audio | NAudio |
| iOS USB | libusbmuxd 1.1.0 |
| Video Codec | FFmpeg (avcodec-58) |

---

# PART 2: PCAP CAPTURE ANALYSIS

## 2.1 Captured Files

| File | Size | Description |
|------|------|-------------|
| anym_capture.pcapng | 99 MB | Network capture |
| usb_iface10.pcapng | 91 MB | USB interface 1,0 |
| usb_iface13.pcapng | 197 MB | USB interface 1,3 |

---

## 2.2 Complete Port List

### TCP Ports Used
```
443, 4720, 4793, 5354, 27019,
49213, 49253, 49254, 49350, 49355-49363,
49546-49551, 49678, 49721, 49919-49922,
50684, 50855, 51297, 51311, 51346,
51416, 51495, 51563, 51564, 51874,
51917, 51918, 51957, 52067, 52106,
52122, 52150, 52182, 52287-52289,
52862, 52863, 52939, 53016-53018,
53104, 53259-53264, 53358, 53494-53495,
53621-53624, 53650-53652, 53838-53841,
54361-54368, 54420-54428, 55045, 55479-55486,
55702, 56309, 56421-56426, 56610-56623,
56759, 56797-56801, 57004, 57318, 57560,
57565, 57652, 57861-57862, 57970,
58489-58515, 58742-58743, 58903-58908,
58946, 58948, 58951-58952, 59033,
59466-59469, 59605-59609, 59796-59797,
60050-60056, 60164, 60504-60505, 60549,
60654-60656, 60674, 60689, 60785, 60801,
60873, 60926, 61332, 61519-61547, 61601-61603,
61648, 61908, 62151, 62439, 62569-62572,
62691, 62987, 63853, 63856-63858, 63867,
64253, 64255, 64651-64656, 64798-64799,
65228-65230, 65347
```

### UDP Ports Used
```
53, 5353, 8850, 53967, 53968, 55854
```

---

## 2.3 Internal TCP Architecture

### Server Ports (Critical for Implementation)
| Port | Purpose | Clients | Packets |
|------|---------|---------|---------|
| 27019 | usbmuxd | 44 | 506 |
| 4793 | Control | 95 | 467 |
| 49350 | Video | 31 | 155 |
| 49678 | Control | 23 | 62 |

### Port 4720 - HTTP API
```
Request:  get /ping
Response: {"path": "/ping", "data": true}
```

---

## 2.4 Video Stream Protocol (Port 49350)

### Frame Structure (41088 bytes total)
```
┌─────────────────────────────────────────────────────────────┐
│                         VIDEO FRAME                          │
├──────────┬──────────┬──────────┬──────────┬───────────────┤
│ Byte 0   │ Bytes 1-3│ Bytes 4-7│ Bytes 8-15│ Bytes 16-19 │
├──────────┼──────────┼──────────┼──────────┼───────────────┤
│ Type     │ Flags    │ Sequence │ Timestamp │ Size         │
│ 0x01     │ 0x000000 │ LE uint32│ LE uint64 │ LE uint32    │
└──────────┴──────────┴──────────┴──────────┴───────────────┘
Offset 20+: Frame Data (41068 bytes)
```

### Frame Statistics
- Total frames captured: 31
- Frame size: 41088 bytes (fixed)
- Header: 20 bytes
- Data: 41068 bytes
- Sequence: Always 0 in capture
- Timestamp: 4611686018427387906 (unknown format)

---

## 2.5 usbmuxd Protocol (Port 27019)

### All Plist Keys Found
```
BUID, ClientVersionString, ConnectionSpeed, ConnectionType,
DeviceID, DeviceList, EnableSessionSSL, HostID, Key,
Label, LocationID, MessageType, Number, PairRecordData,
PairRecordID, PortNumber, ProductID, ProgName, Properties,
Request, SerialNumber, SessionID, SystemBUID, Type,
Value, kLibUSBMuxVersion
```

### Complete Message Flow

#### Step 1: Connect
```xml
<dict>
    <key>ClientVersionString</key>
    <string>libusbmuxd 1.1.0</string>
    <key>MessageType</key>
    <string>Connect</string>
    <key>ProgName</key>
    <string>AnyMiro.exe</string>
    <key>kLibUSBMuxVersion</key>
    <integer>3</integer>
    <key>DeviceID</key>
    <integer>1</integer>
    <key>PortNumber</key>
    <integer>32498</integer>
</dict>
```

#### Step 2: Result
```xml
<dict>
    <key>MessageType</key>
    <string>Result</string>
    <key>Number</key>
    <integer>0</integer>
</dict>
```

#### Step 3: ReadBUID
```xml
<dict>
    <key>ClientVersionString</key>
    <string>libusbmuxd 1.1.0</string>
    <key>MessageType</key>
    <string>ReadBUID</string>
    <key>ProgName</key>
    <string>AnyMiro.exe</string>
    <key>kLibUSBMuxVersion</key>
    <integer>3</integer>
</dict>
```

#### Step 4: StartSession
```xml
<dict>
    <key>Label</key>
    <string>usbmuxd</string>
    <key>Request</key>
    <string>StartSession</string>
    <key>HostID</key>
    <string>EDA44D48-0212-401E-8424-B34550B3F256</string>
    <key>SystemBUID</key>
    <string>30987161388752136308115836</string>
</dict>
```

#### Step 5: EnableSessionSSL
```xml
<dict>
    <key>EnableSessionSSL</key>
    <true/>
    <key>Request</key>
    <string>StartSession</string>
    <key>SessionID</key>
    <string>D81E2253-6599-40E6-AD06-838CF966A295</string>
</dict>
```

#### Step 6: ReadPairRecord
```xml
<dict>
    <key>ClientVersionString</key>
    <string>libusbmuxd 1.1.0</string>
    <key>MessageType</key>
    <string>ReadPairRecord</string>
    <key>ProgName</key>
    <string>AnyMiro.exe</string>
    <key>kLibUSBMuxVersion</key>
    <integer>3</integer>
    <key>PairRecordID</key>
    <string>00008120000E0C9E2247C01E</string>
</dict>
```

---

## 2.6 External Connections (During Mirroring)

| Remote IP | Port | Purpose |
|-----------|------|---------|
| 77.111.245.11 | 443 | HTTPS (analytics/telemetry) |

Note: Main mirroring traffic is all localhost!

---

# PART 3: COMPLETE CONNECTION SEQUENCE

## Step-by-Step Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    IPHONE USB SCREEN MIRRORING                          │
│                      CONNECTION SEQUENCE                                │
└─────────────────────────────────────────────────────────────────────────┘

1. USB DEVICE CONNECTION
   ├── iPhone Lightning cable connected to PC
   ├── Windows detects Apple device
   ├── usbmuxd daemon discovers device
   └── Device assigned DeviceID (e.g., 1)

2. USBMUXD HANDSHAKE (Port 27019)
   ├── Connect to 127.0.0.1:27019
   ├── Send Connect plist
   │   ├── ClientVersionString: "libusbmuxd 1.1.0"
   │   ├── ProgName: "AnyMiro.exe"
   │   ├── DeviceID: 1
   │   ├── PortNumber: 32498 (requested Lockdown port)
   │   └── kLibUSBMuxVersion: 3
   ├── Receive Result: Number=0 (success)
   └── ReadBUID for device identification

3. LOCKDOWN SESSION ESTABLISHMENT
   ├── StartSession request
   │   ├── HostID: UUID
   │   └── SystemBUID: from device
   ├── Receive SessionID (UUID)
   └── EnableSessionSSL: true (establish TLS)

4. DEVICE AUTHENTICATION
   ├── ReadPairRecord (PairRecordID)
   ├── Receive PairRecordData (13KB certificate)
   └── Validate device certificate

5. SERVICE DISCOVERY
   ├── QueryType: "com.apple.mobile.lockdown"
   └── Identify required services

6. SCREEN CAPTURE SERVICE CONNECTION
   ├── Request port from device (e.g., 32498)
   └── Connect to ScreenCaptureService

7. INTERNAL SERVER STARTUP
   ├── Port 4720: HTTP health API
   ├── Port 4793: Control server
   ├── Port 49350: Video stream server
   └── Port 49678: Control channel

8. VIDEO STREAMING
   ├── Receive frames on port 49350
   ├── Each frame: 41088 bytes
   ├── Custom header: 20 bytes
   └── Video data: 41068 bytes

9. RENDERING
   ├── Decode video data
   ├── Render with DirectX
   └── Display in WPF window
```

---

# PART 4: IMPLEMENTATION FOR MIRANCE

## 4.1 Required Components

### Core Components
1. **libusbmuxd** - USB device communication
   - Use libusbmuxd library or implement protocol
   - Port 27019 for device multiplexing
   
2. **Lockdown Protocol** - iOS service communication
   - Plist message exchange
   - SSL/TLS session handling
   - Device pairing

3. **Video Decoder** - Custom frame parsing
   - Parse 41088-byte frames
   - Extract 20-byte header
   - Decode video data

4. **Rendering** - DirectX display
   - SharpDX integration
   - WPF window

## 4.2 Internal TCP Servers

```csharp
// Pseudocode for internal servers
TcpServer apiServer = new TcpServer(4720);    // HTTP API
TcpServer controlServer = new TcpServer(4793); // Control
TcpServer videoServer = new TcpServer(49350);  // Video stream
TcpServer auxServer = new TcpServer(49678);    // Aux control
```

## 4.3 Video Frame Parser

```csharp
public class VideoFrame
{
    public byte Type { get; }          // Byte 0
    public byte[] Flags { get; }       // Bytes 1-3
    public uint Sequence { get; }      // Bytes 4-7 (LE)
    public ulong Timestamp { get; }    // Bytes 8-15 (LE)
    public uint Size { get; }          // Bytes 16-19 (LE)
    public byte[] Data { get; }        // Bytes 20+
    
    public static VideoFrame Parse(byte[] raw)
    {
        return new VideoFrame
        {
            Type = raw[0],
            Flags = raw[1..4],
            Sequence = BitConverter.ToUInt32(raw, 4),
            Timestamp = BitConverter.ToUInt64(raw, 8),
            Size = BitConverter.ToUInt32(raw, 16),
            Data = raw[20..]
        };
    }
}
```

## 4.4 Dependencies for MIRANCE

| Component | NuGet Package | Purpose |
|-----------|---------------|---------|
| libusbmuxd | libimobiledevice | USB iOS |
| SharpDX | SharpDX | DirectX rendering |
| NAudio | NAudio | Audio playback |
| Newtonsoft.Json | Newtonsoft.Json | JSON parsing |
| BouncyCastle | BouncyCastle.Crypto | SSL/TLS |
| Unity | Unity | Dependency injection |

---

# PART 5: KEY TECHNICAL DETAILS

## 5.1 What IS Used (Empirically Verified)
- ✅ libusbmuxd 1.1.0
- ✅ TCP port 27019 for usbmuxd
- ✅ Lockdown protocol (plist/XML)
- ✅ Custom video protocol (port 49350)
- ✅ Localhost TCP for internal communication

## 5.2 What is NOT Used
- ❌ AirPlay protocol (no RTSP port 7010)
- ❌ RTP video (no port 7000)
- ❌ RAOP audio (no ports 5000-5001)
- ❌ Standard H.264 NAL units
- ❌ mDNS/Bonjour discovery

## 5.3 Video Frame Observations
- Frames are exactly 41088 bytes
- Header is 20 bytes (fixed)
- Data portion has very low entropy (0.07-0.32)
- Data starts with zeros - possibly:
  - Compressed format
  - Screen was black during capture
  - Proprietary encoding

---

# APPENDIX A: ALL ANYMIRO FILES

## Main Application Files (~200 files, 145MB)

### Root Directory
- AnyMiro.exe, AnyMiro.Update.exe, driver.exe, iosusb.exe, uninstall.exe

### .NET Libraries (~80 DLLs)
- Core.Connection.dll, Core.MD.Render.dll, Service.Mirroring.dll, etc.

### Native Libraries
- usbmuxd/, scrcpy/, FFmpeg/, x86/, x64/

---

# APPENDIX B: PCAP STATISTICS

| Metric | Value |
|--------|-------|
| Total IP packets | 4,856 |
| TCP packets | 4,573 |
| UDP packets | 35 |
| usbmuxd packets | 960 |
| Video packets | 279 |
| External connections | Minimal |

---

# APPENDIX C: BUILD INFORMATION

From binary strings:
- Build path: `I:\Gitee\AnyMiro\Service\Service.Mirroring\obj\Release\`
- .NET Target: 4.6.1
- Signed with iMobie certificate (GlobalSign)
- PDB files present for debugging

---

# END OF ANALYSIS