# MIRANCE - iPhone USB Screen Mirroring for Windows

<p align="center">
  <img src="assets/logo.png" alt="MIRANCE" width="200"/>
</p>

<p align="center">
  <strong>The highest quality, lowest latency iPhone USB Screen Mirroring for Windows</strong>
</p>

---

## ✨ Features

- **Ultra-low latency** - Direct USB connection delivers the fastest possible mirroring
- **High quality** - Full resolution screen capture from your iPhone
- **No limitations** - Unlimited session duration, no delays
- **Open source** - Completely free and transparent
- **Modern UI** - Clean, native Windows experience

## 📋 Requirements

- Windows 10/11 (64-bit)
- iPhone with Lightning cable
- USB 2.0 or USB 3.0 port

## 🚀 Getting Started

### Build from Source

```bash
# Clone the repository
git clone https://github.com/unluckybob/mirance.git
cd mirance

# Build
dotnet build src/Mirance.sln
```

### Run

```bash
dotnet run --project src/Mirance.App
```

## 📖 How It Works

MIRANCE connects to your iPhone via USB and captures the screen using iOS's built-in screen capture service. The video stream is transferred over USB through a local TCP proxy, ensuring:

1. **USB Connection** - Uses `libusbmuxd` for device communication
2. **Secure Session** - Establishes TLS-encrypted Lockdown session
3. **Screen Service** - Connects to iOS ScreenCaptureService
4. **Local Streaming** - Receives video frames via localhost TCP
5. **GPU Rendering** - Displays with DirectX acceleration

## 🏗️ Architecture

```
┌─────────────┐     USB      ┌──────────────┐     localhost TCP     ┌─────────────┐
│   iPhone    │◄──────────►│   MIRANCE    │◄─────────────────►│  Display   │
│  (UDID)    │   Cable    │  (USB)      │     (Frames)        │  (DirectX)  │
└─────────────┘            └──────────────┘                    └─────────────┘
```

## 📦 Components

| Component | Description |
|-----------|-------------|
| Mirance.App | Main WPF application |
| Mirance.Protocol | iOS protocol handling (usbmuxd + Lockdown) |
| Mirance.Video | Video stream processing |
| Mirance.Render | DirectX rendering |

## 🛠️ Tech Stack

- **.NET Framework 4.6.1** - Runtime (Windows Desktop)
- **WPF** - UI Framework
- **SharpDX** - GPU Rendering (DirectX 11)
- **libusbmuxd** - USB Device Communication
- **NAudio** - Audio (future)

## 📄 License

MIT License - See [LICENSE](LICENSE)

---

<p align="center">
  <strong>MIRANCE</strong> - Open Source iPhone USB Screen Mirroring
</p>