using System;
using System.Windows;

namespace NGEOS.Desktop.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                App.Log("Entering MainWindow.ctor(), calling InitializeComponent()...");
                InitializeComponent();
                StateChanged += MainWindow_StateChanged;
                App.Log("InitializeComponent() completed successfully!");
            }
            catch (Exception ex)
            {
                App.Log($"FATAL EXCEPTION in MainWindow.ctor(): {ex}");
                throw;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (MaximizeBtn != null)
            {
                MaximizeBtn.Content = WindowState == WindowState.Maximized ? "\u29C9" : "\u25A1";
                MaximizeBtn.ToolTip = WindowState == WindowState.Maximized ? "Restore Down" : "Maximize";
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
