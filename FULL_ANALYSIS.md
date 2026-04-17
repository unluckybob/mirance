# COMPLETE ANYMIRO ANALYSIS - ALL FILES AND COMPONENTS

**Date:** April 2026  
**Scope:** iPhone USB Screen Mirroring Only

---

# PART 1: COMPLETE FILE STRUCTURE

## Root Directory Files (298 total)

### Main Executables
| File | Size | Purpose |
|------|------|---------|
| AnyMiro.exe | 3.27 MB | Main WPF Application |
| AnyMiro.Update.exe | 3.78 MB | Auto-updater |
| iosusb.exe | 9.72 MB | iOS Protocol (Python-based) |
| driver.exe | 4.60 MB | USB Driver Manager |
| adb.exe | 5.8 MB | Android Debug Bridge |
| uninstall.exe | 1.8 MB | Uninstaller |

### Core .NET Libraries
| File | Size | Purpose |
|------|------|---------|
| Core.Connection.dll | 11.35 MB | Device Connection Management |
| Core.MD.Render.dll | 41.97 MB | DirectX Rendering |
| Service.Mirroring.dll | 33 KB | Mirroring Service |
| Service.VideoCapture.dll | 45 KB | Video Capture (Windows.Graphics.Capture API) |
| Service.AudioCapture.dll | 46 KB | Audio Capture |
| Core.MirroringConnection.dll | 60 KB | Mirroring Connection |
| AirPlayLibrary.dll | 77 KB | AirPlay Protocol (WiFi) |
| Renderer.Core.dll | 58 KB | Core Renderer |

### USB Components
| File | Size | Purpose |
|------|------|---------|
| usbmuxd/usbmuxd.exe | 2.07 MB | USB Multiplexing Daemon |
| usbmuxd/libusb-1.0.dll | 106 KB | libusb-1.0 Library |
| usbmuxd/libusb0.dll | 67 KB | libusb0 Driver |

### Video/Audio Libraries
| File | Size | Purpose |
|------|------|---------|
| avcodec-58.dll | 34 MB | FFmpeg Video Codec |
| avformat-58.dll | 11 MB | FFmpeg Container |
| avutil-56.dll | 795 KB | FFmpeg Utilities |
| swscale-5.dll | 516 KB | FFmpeg Scaling |
| swresample-3.dll | 317 KB | FFmpeg Audio |
| NAudio.dll | 502 KB | Windows Audio API |
| NAudio.Core.dll | 184 KB | NAudio Core |
| NAudio.Lame.dll | 90 KB | MP3 Encoding |
| ALACDecoder.dll | 23 KB | Apple Lossless |
| PCMLib.dll | 21 KB | PCM Library |
| libfdk.dll | 3.5 MB | Fraunhofer AAC |

### Rendering Libraries (SharpDX)
| File | Size | Purpose |
|------|------|---------|
| SharpDX.dll | 269 KB | SharpDX Core |
| SharpDX.Direct3D11.dll | 274 KB | DirectX 11 |
| SharpDX.Direct3D9.dll | 331 KB | DirectX 9 |
| SharpDX.Direct3D12.dll | 169 KB | DirectX 12 |
| SharpDX.Direct2D1.dll | 472 KB | Direct2D |
| SharpDX.DXGI.dll | 144 KB | DirectX Graphics |
| SharpDX.XAudio2.dll | 92 KB | Audio |
| SharpDX.D3DCompiler.dll | 58 KB | Shader Compiler |

### UI Framework
| File | Size | Purpose |
|------|------|---------|
| HandyControl.dll | 1.34 MB | UI Controls |
| Theme.Default.dll | 17 MB | Default Theme |
| Prism.Wpf.dll | 144 KB | MVVM Framework |
| Unity.Container.dll | 143 KB | DI Container |
| BouncyCastle.Crypto.dll | 2.2 MB | Cryptography |

### FFmpeg Folder (Built-in FFmpeg)
| File | Size |
|------|------|
| avcodec-60.dll | 17 MB |
| avformat-60.dll | 3.5 MB |
| avfilter-9.dll | 8.9 MB |
| avutil-58.dll | 997 KB |
| swresample-4.dll | 116 KB |
| swscale-7.dll | 484 KB |
| ffmpeg.exe | 368 KB |

### scrcpy Folder (Android Mirror)
| File | Size |
|------|------|
| scrcpy-server | 89 KB |
| adb.exe | 5.8 MB |
| avcodec-60.dll | 17 MB |
| SDL2.dll | 1.4 MB |

---

# PART 2: TECHNOLOGY STACK (EMPIRICALLY VERIFIED)

## .NET Framework
```
.NET Framework 4.6.1
Runtime: v4.0.30319
```

## Key Components

### Service.VideoCapture.dll
Uses Windows Graphics Capture API:
- GraphicsCaptureItem
- Direct3D11CaptureFrame
- D3DFrame
- StartCapture / StopCapture

### Service.Mirroring.dll
Key classes:
- AppleUSBMirror
- ConnectAsync
- DisconnectAsync
- EventAppleUSBMirrorSizeChanged
- Core.MD.Render.API.iOS
- iScreenW / iScreenH

### Core.Connection.dll
Key components:
- UsbWatcherService
- OnUSBInserted / OnUSBRemoveEvent
- VideoIPPort / AudioIPPort
- GetUsbDevices

### Core.MD.Render.dll
Key components:
- TransitFrameData
- PushVideoDelegate
- ScFrameSink
- register_frame_sink
- Width / Height / FrameSize

---

# PART 3: VIDEO STREAM (FROM PCAP)

## Frame Format (Port 49350)

```
Total: 41088 bytes

Header (20 bytes):
  Byte 0:     Type (0x01)
  Bytes 1-3:  Flags (0x00 0x00 0x00)
  Bytes 4-7:  Sequence (LE uint32)
  Bytes 8-15:  Timestamp (LE uint64)
  Bytes 16-19: Size (LE uint32)

Data: 41068 bytes (offset 20)
```

---

# PART 4: INTERNAL TCP SERVERS

| Port | Purpose |
|------|---------|
| 27019 | usbmuxd |
| 4720 | HTTP API |
| 4793 | Control |
| 49350 | Video |
| 49678 | Control |

---

# PART 5: USBMUXD PROTOCOL

Messages:
- Connect (ClientVersion, DeviceID, PortNumber)
- ReadBUID
- StartSession
- EnableSessionSSL
- ReadPairRecord

---

# PART 6: WHAT'S DIFFERENT - USB vs WIFI

## AirPlayLibrary.dll (WiFi - NOT USB)
Contains:
- RTSP protocol
- VideoDataPort / AudioDataPort / TimingPort
- For wireless AirPlay mirroring

## Service.VideoCapture.dll (USB)
Uses:
- Windows.Graphics.Capture API
- Direct3D11CaptureFrame
- For USB screen capture

---

# END