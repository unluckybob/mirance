using System;
using System.Windows;
using System.Windows.Controls;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace Mirance.Render
{
    /// <summary>
    /// Video renderer using SharpDX DirectX 11
    /// </summary>
    public class VideoRenderer : IDisposable
    {
        private Device _device;
        private DeviceContext _context;
        private SwapChain _swapChain;
        private RenderTargetView _renderTarget;
        private Texture2D _videoTexture;
        private bool _initialized;
        
        private int _width;
        private int _height;
        
        /// <summary>
        /// Initialize renderer with a window handle
        /// </summary>
        public bool Initialize(IntPtr windowHandle, int width, int height)
        {
            try
            {
                _width = width;
                _height = height;
                
                // Create device and context
                _device = new Device(SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
                _context = _device.ImmediateContext;
                
                // Create swap chain description
                var desc = new SwapChainDescription()
                {
                    BufferCount = 2,
                    Flags = SwapChainFlags.AllowModeSwitch,
                    IsWindowed = true,
                    ModeDescription = new ModeDescription(
                        width, height,
                        new Rational(60, 1),
                        Format.B8G8R8A8_UNorm),
                    OutputHandle = windowHandle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = RenderTargetUsage.RenderTargetOutput
                };
                
                // Create swap chain
                _swapChain = new SwapChain(new Factory1(), _device, desc);
                
                // Create render target
                var backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderTarget = new RenderTargetView(_device, backBuffer);
                
                _initialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Renderer initialization failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Update video frame
        /// </summary>
        public void UpdateFrame(byte[] frameData, int width, int height)
        {
            if (!_initialized) return;
            
            try
            {
                // Create or update video texture
                // This is a simplified implementation
                // Real implementation would handle YUV to RGB conversion
                
                // For now, just display the frame
                _context.ClearRenderTargetView(_renderTarget, new Color4(0, 0, 0, 1));
                _swapChain.Present(1, PresentFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Frame update failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Render the current frame
        /// </summary>
        public void Render()
        {
            if (!_initialized) return;
            
            try
            {
                _context.OutputMerger.SetRenderTargets(_renderTarget);
                _swapChain.Present(1, PresentFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Render failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resize the renderer
        /// </summary>
        public void Resize(int width, int height)
        {
            if (!_initialized) return;
            
            _width = width;
            _height = height;
            
            // Dispose old resources
            _renderTarget?.Dispose();
            
            // Resize swap chain
            _swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowModeSwitch);
            
            // Create new render target
            var backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderTarget = new RenderTargetView(_device, backBuffer);
        }
        
        public void Dispose()
        {
            _renderTarget?.Dispose();
            _swapChain?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
        }
    }
    
    /// <summary>
    /// WPF Image control for rendering video
    /// </summary>
    public class VideoDisplay : Image
    {
        private VideoRenderer _renderer;
        
        public VideoDisplay()
        {
            _renderer = new VideoRenderer();
        }
        
        public bool Initialize(IntPtr windowHandle, int width, int height)
        {
            return _renderer.Initialize(windowHandle, width, height);
        }
        
        public void UpdateFrame(byte[] frameData, int width, int height)
        {
            _renderer.UpdateFrame(frameData, width, height);
        }
        
        public new void Dispose()
        {
            _renderer?.Dispose();
        }
    }
}
