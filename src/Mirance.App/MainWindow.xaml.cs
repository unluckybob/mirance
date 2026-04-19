using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mirance.App
{
    public partial class MainWindow : Window
    {
        private bool _isConnected;
        private int _frameCount;
        private DateTime _lastFpsUpdate;
        private WriteableBitmap _videoBitmap;
        
        public MainWindow()
        {
            InitializeComponent();
            _lastFpsUpdate = DateTime.Now;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeVideoDisplay();
            RefreshDevices();
        }
        
        private void InitializeVideoDisplay()
        {
            _videoBitmap = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr32, null);
            VideoDisplay.Source = _videoBitmap;
        }
        
        private void RefreshDevices()
        {
            DeviceList.Items.Clear();
            
            var placeholder = new ListBoxItem 
            { 
                Content = "Connect an iPhone via USB to start",
                IsEnabled = false
            };
            DeviceList.Items.Add(placeholder);
            
            StatusText.Text = "Ready - connect your iPhone";
        }
        
        private void StartMirroringButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }
        
        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Device selection will trigger connection
        }
        
        private void StartMirroring()
        {
            if (_isConnected) return;
            
            try
            {
                StatusText.Text = "Starting mirroring...";
                
                _isConnected = true;
                Placeholder.Visibility = Visibility.Collapsed;
                StatusText.Text = "Mirroring active";
                ConnectionStatus.Text = "● Connected";
                ConnectionStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(68, 200, 68));
                
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
            ConnectionStatus.Text = "● Disconnected";
            ConnectionStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 68, 68));
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }
    }
}
