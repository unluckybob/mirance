# COMPLETE IPHONE USB SCREEN MIRRORING - TECHNICAL ANALYSIS
# For MIRANCE Implementation

**VERSION:** 2.1
**DATE:** April 2026
**SCOPE:** iPhone USB Screen Mirroring ONLY

---

# PART 1: TECHNOLOGY STACK (EMPIRICALLY VERIFIED)

## 1.1 .NET Framework

From binary analysis (verified via PE headers and strings):
```
.NET Framework 4.6.1
v4.0.30319
```

All AnyMiro binaries target .NET Framework 4.6.1:
- AnyMiro.exe
- Core.Connection.dll (11.35 MB)
- Core.MD.Render.dll (41.97 MB)
- Service.Mirroring.dll
- All other .NET DLLs

## 1.2 Complete Technology Stack

| Component | Technology | Evidence |
|-----------|------------|----------|
| Runtime | .NET Framework 4.6.1 | PE header: v4.0.30319 |
| UI Framework | WPF | PresentationFramework.dll reference |
| Rendering | SharpDX (DirectX 11) | SharpDX.Direct3D11.dll present |
| Audio | NAudio | NAudio.dll present |
| iOS USB | libusbmuxd 1.1.0 | usbmuxd.exe + strings |
| Video Codec | FFmpeg | avcodec-58.dll (34.54 MB) |

## 1.3 File Structure

### Main Executables
| File | Size | Purpose |
|------|------|---------|
| AnyMiro.exe | 3.27 MB | Main WPF application |
| AnyMiro.Update.exe | 3.78 MB | Auto-updater |
| iosusb.exe | 9.72 MB | iOS protocol (embedded Python) |
| driver.exe | 4.60 MB | USB driver manager |
| usbmuxd.exe | 2.07 MB | USB multiplexing |

### Core DLLs
| File | Size | Purpose |
|------|------|---------|
| Core.Connection.dll | 11.35 MB | Device connection |
| Core.MD.Render.dll | 41.97 MB | DirectX rendering |
| Service.Mirroring.dll | 33 KB | Mirroring service |
| SharpDX.Direct3D11.dll | 280 KB | DirectX 11 |
| NAudio.dll | 513 KB | Audio API |

---

# PART 2: USB DEVICE COMMUNICATION

## 2.1 usbmuxd Protocol

Uses **libusbmuxd 1.1.0** (open source library):
- TCP Port: **27019**
- Protocol: Plist over TCP
- Encryption: SSL/TLS

## 2.2 All usbmuxd Messages

```
BUID, ClientVersionString, ConnectionSpeed, ConnectionType,
DeviceID, DeviceList, EnableSessionSSL, HostID, Key,
Label, LocationID, MessageType, Number, PairRecordData,
PairRecordID, PortNumber, ProductID, ProgName, Properties,
Request, SerialNumber, SessionID, SystemBUID, Type,
Value, kLibUSBMuxVersion
```

## 2.3 Message Sequence

### Connect
```xml
<dict>
    <key>ClientVersionString</key><string>libusbmuxd 1.1.0</string>
    <key>MessageType</key><string>Connect</string>
    <key>ProgName</key><string>AnyMiro.exe</string>
    <key>kLibUSBMuxVersion</key><integer>3</integer>
    <key>DeviceID</key><integer>1</integer>
    <key>PortNumber</key><integer>32498</integer>
</dict>
```

### StartSession
```xml
<dict>
    <key>Label</key><string>usbmuxd</string>
    <key>Request</key><string>StartSession</string>
    <key>HostID</key><string>EDA44D48-0212-401E-8424-B34550B3F256</string>
    <key>SystemBUID</key><string>30987161388752136308115836</string>
</dict>
```

### EnableSessionSSL
```xml
<dict>
    <key>EnableSessionSSL</key><true/>
    <key>Request</key><string>StartSession</string>
    <key>SessionID</key><string>D81E2253-6599-40E6-AD06-838CF966A295</string>
</dict>
```

---

# PART 3: INTERNAL TCP ARCHITECTURE

## 3.1 Server Ports (Verified from PCAP)

| Port | Purpose | Clients | Packets |
|------|---------|---------|---------|
| 27019 | usbmuxd | 44 | 506 |
| 4793 | Control | 95 | 467 |
| 49350 | Video | 31 | 155 |
| 49678 | Control | 23 | 62 |
| 4720 | HTTP API | - | 100 |

## 3.2 Port 4720 - HTTP API
```
Request:  get /ping
Response: {"path": "/ping", "data": true}
```

---

# PART 4: VIDEO STREAM PROTOCOL

## 4.1 Frame Structure (Port 49350)

```
Total: 41088 bytes (fixed)

Header (20 bytes):
  Byte 0:     Type (0x01)
  Bytes 1-3:  Flags (0x00 0x00 0x00)
  Bytes 4-7:  Sequence (LE uint32)
  Bytes 8-15: Timestamp (LE uint64)
  Bytes 16-19: Size (LE uint32)

Data: 41068 bytes (starts at offset 20)
```

## 4.2 Statistics
- Frame size: 41088 bytes (constant)
- Header: 20 bytes
- Data: 41068 bytes
- Frames captured: 31

---

# PART 5: COMPLETE CONNECTION SEQUENCE

```
1. USB Device Connection
   ├── iPhone connected via Lightning
   └── usbmuxd discovers device

2. usbmuxd Handshake (port 27019)
   ├── Connect plist → DeviceID, PortNumber
   ├── Receive Result (success)
   └── ReadBUID

3. Lockdown Session
   ├── StartSession
   ├── Receive SessionID
   └── EnableSessionSSL

4. Device Authentication
   ├── ReadPairRecord
   └── Validate certificate

5. Service Discovery
   └── QueryType: com.apple.mobile.lockdown

6. Internal Servers
   ├── Port 4720: HTTP API
   ├── Port 4793: Control
   ├── Port 49350: Video
   └── Port 49678: Control

7. Video Streaming
   └── Receive 41088-byte frames
```

---

# PART 6: IMPLEMENTATION FOR MIRANCE

## 6.1 Required Components

1. **libusbmuxd** - USB device communication
2. **Lockdown Protocol** - Plist + SSL/TLS
3. **TCP Servers** - Ports 27019, 4720, 4793, 49350, 49678
4. **Video Parser** - Parse 41088-byte frames
5. **Rendering** - SharpDX/DirectX 11
6. **UI** - WPF

## 6.2 Dependencies

| Package | Purpose |
|---------|---------|
| SharpDX | DirectX rendering |
| NAudio | Audio playback |
| BouncyCastle | SSL/TLS |
| Unity | Dependency injection |

## 6.3 Video Frame Parser

```csharp
public struct VideoFrame
{
    public byte Type;           // Byte 0
    public byte[] Flags;        // Bytes 1-3
    public uint Sequence;        // Bytes 4-7 (LE)
    public ulong Timestamp;      // Bytes 8-15 (LE)
    public uint Size;           // Bytes 16-19 (LE)
    public byte[] Data;         // Bytes 20+
}
```

---

# APPENDIX: BUILD INFO

- **Target:** .NET Framework 4.6.1
- **Runtime:** v4.0.30319
- **UI:** WPF (Windows Presentation Foundation)
- **Build path:** I:\Gitee\AnyMiro\Service\Service.Mirroring\obj\Release\

---

# END