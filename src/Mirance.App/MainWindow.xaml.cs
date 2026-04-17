using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Threading.Tasks;
using Mirance.Protocol;
using Mirance.Video;
using Mirance.Render;

namespace Mirance.App
{
    public partial class MainWindow : Window
    {
        private UsbmuxdClient _usbmuxd;
        private LockdownClient _lockdown;
        private VideoReceiver _videoReceiver;
        private VideoRenderer _renderer;
        
        private bool _isConnected;
        private int _frameCount;
        private DateTime _lastFpsUpdate;
        
        public MainWindow()
        {
            InitializeComponent();
            
            _usbmuxd = new UsbmuxdClient();
            _videoReceiver = new VideoReceiver();
            _renderer = new VideoRenderer();
            
            _videoReceiver.FrameReceived += OnFrameReceived;
            _lastFpsUpdate = DateTime.Now;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize renderer
            var hwnd = new WindowInteropHelper(this).Handle;
            _renderer.Initialize(hwnd, 1920, 1080);
            
            // Start device detection
            await RefreshDevicesAsync();
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
            _renderer?.Dispose();
        }
        
        private async Task RefreshDevicesAsync()
        {
            try
            {
                StatusText.Text = "Searching for devices...";
                
                // Connect to usbmuxd
                if (await _usbmuxd.ConnectAsync())
                {
                    var devices = await _usbmuxd.GetDevicesAsync();
                    
                    DeviceList.Items.Clear();
                    
                    if (devices.Count == 0)
                    {
                        DeviceList.Items.Add(new ListBoxItem { Content = "No iOS devices found" });
                    }
                    else
                    {
                        foreach (var device in devices)
                        {
                            DeviceList.Items.Add(new ListBoxItem 
                            { 
                                Content = $"iPhone {device.SerialNumber}",
                                Tag = device 
                            });
                        }
                    }
                    
                    StatusText.Text = $"Found {devices.Count} device(s)";
                }
                else
                {
                    StatusText.Text = "Failed to connect to usbmuxd. Is the service running?";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }
        
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesAsync();
        }
        
        private async void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceList.SelectedItem is ListBoxItem item && item.Tag is IOSDevice device)
            {
                await ConnectToDeviceAsync(device);
            }
        }
        
        private async Task ConnectToDeviceAsync(IOSDevice device)
        {
            try
            {
                StatusText.Text = $"Connecting to device {device.DeviceID}...";
                
                // Connect to device via usbmuxd, requesting screen service port
                if (await _usbmuxd.ConnectToDeviceAsync(device.DeviceID, 32498))
                {
                    // Start Lockdown session
                    _lockdown = new LockdownClient();
                    await _lockdown.ConnectAsync(32498);
                    await _lockdown.StartSessionAsync(
                        Guid.NewGuid().ToString(),
                        "MIRANCE"
                    );
                    
                    // Start video receiver
                    await _videoReceiver.StartAsync();
                    
                    _isConnected = true;
                    Placeholder.Visibility = Visibility.Collapsed;
                    StatusText.Text = $"Connected - Mirroring";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Connection failed: {ex.Message}";
            }
        }
        
        private void OnFrameReceived(object sender, FrameReceivedEventArgs e)
        {
            // Update frame on UI thread
            Dispatcher.Invoke(() =>
            {
                _frameCount++;
                
                // Update FPS every second
                var now = DateTime.Now;
                if ((now - _lastFpsUpdate).TotalSeconds >= 1)
                {
                    FPSCounter.Text = $"FPS: {_frameCount}";
                    _frameCount = 0;
                    _lastFpsUpdate = now;
                }
                
                // Update video display
                _renderer.UpdateFrame(
                    e.Frame.Data,
                    (int)e.Frame.Size,
                    1
                );
            });
        }
        
        private void Disconnect()
        {
            _isConnected = false;
            _videoReceiver?.Stop();
            _lockdown?.Disconnect();
            _usbmuxd?.Disconnect();
        }
    }
}
