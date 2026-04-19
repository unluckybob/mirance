using System;
using System.Windows;

namespace Mirance.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up global exception handler
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Error: {args.Exception.Message}", "MIRANCE Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
