using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.IO;

namespace Mirance.App
{
    public partial class MainWindow : Window
    {
        private bool _isConnected;
        private int _frameCount;
        private DateTime _lastFpsUpdate;
        private WriteableBitmap _videoBitmap;
        
        // AnyMiro service references - loaded at runtime
        private object _mirroringService;
        
        public MainWindow()
        {
            InitializeComponent();
            _lastFpsUpdate = DateTime.Now;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize video display
            InitializeVideoDisplay();
            
            // Try to start usbmuxd service
            StartUsbmuxdService();
            
            // Start device detection
            RefreshDevices();
        }
        
        private void InitializeVideoDisplay()
        {
            // Create a simple black placeholder image
            _videoBitmap = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr32, null);
            VideoDisplay.Source = _videoBitmap;
        }
        
        private void StartUsbmuxdService()
        {
            try
            {
                // Check if usbmuxd is running, if not try to start it
                var usbmuxdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usbmuxd", "usbmuxd.exe");
                if (File.Exists(usbmuxdPath))
                {
                    StatusText.Text = "usbmuxd service found";
                }
                else
                {
                    StatusText.Text = "usbmuxd not found - will search for devices";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Note: {ex.Message}";
            }
        }
        
        private void RefreshDevices()
        {
            DeviceList.Items.Clear();
            
            // Scan for connected iOS devices via usbmuxd
            try
            {
                // Check for connected iOS devices
                // In a real implementation, this would communicate with usbmuxd
                
                var placeholder = new ListBoxItem 
                { 
                    Content = "Connect an iPhone via USB to start",
                    IsEnabled = false
                };
                DeviceList.Items.Add(placeholder);
                
                StatusText.Text = "Ready - connect your iPhone";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error scanning: {ex.Message}";
            }
        }
        
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }
        
        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Device selection changed
        }
        
        private void StartMirroring()
        {
            if (_isConnected) return;
            
            try
            {
                StatusText.Text = "Starting mirroring...";
                
                // Here we would use the AnyMiro Service.Mirroring.dll
                // to start the actual mirroring
                
                // For now, simulate connection
                _isConnected = true;
                Placeholder.Visibility = Visibility.Collapsed;
                StatusText.Text = "Mirroring active";
                
                // Start FPS counter simulation
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, args) =>
                {
                    FPSCounter.Text = $"FPS: {_frameCount}";
                    _frameCount = 0;
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }
        
        private void Disconnect()
        {
            _isConnected = false;
            Placeholder.Visibility = Visibility.Visible;
            StatusText.Text = "Disconnected";
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }
    }
}
