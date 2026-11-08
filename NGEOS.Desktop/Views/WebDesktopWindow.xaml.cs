using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace NGEOS.Desktop.Views
{
    public partial class WebDesktopWindow : Window
    {
        private const int DedicatedPort = 5555;
        private static readonly string BaseUrl = $"http://127.0.0.1:{DedicatedPort}";
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        private Process? _serverProcess;
        private bool _weStartedServer;
        private bool _isFullscreen;
        private WindowStyle _previousStyle = WindowStyle.SingleBorderWindow;
        private WindowState _previousState = WindowState.Maximized;

        public WebDesktopWindow()
        {
            InitializeComponent();

            Loaded += WebDesktopWindow_Loaded;
            Closing += WebDesktopWindow_Closing;
            KeyDown += WebDesktopWindow_KeyDown;
        }

        private async void WebDesktopWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeDesktopPlatformAsync();
        }

        private async Task InitializeDesktopPlatformAsync()
        {
            try
            {
                UpdateStatus("Checking local server state...");
                bool isRunning = await CheckServerHealthyAsync();

                if (!isRunning)
                {
                    UpdateStatus("Starting NGEOS full-stack offline server (port 5555)...");
                    StartLocalServer();

                    // Poll until server is responding
                    int maxAttempts = 30;
                    int attempts = 0;
                    while (attempts < maxAttempts)
                    {
                        await Task.Delay(600);
                        attempts++;
                        UpdateStatus($"Waiting for local server response... ({attempts}/{maxAttempts})");

                        if (await CheckServerHealthyAsync())
                        {
                            isRunning = true;
                            break;
                        }
                    }

                    if (!isRunning)
                    {
                        ShowErrorState("Server failed to respond within 20 seconds. Please ensure port 5555 is free.");
                        return;
                    }
                }

                UpdateStatus("Initializing native Edge WebView2 runtime...");

                // Configure isolated desktop user profile for WebView2
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NGEOS_Desktop_WebView2"
                );

                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebViewControl.EnsureCoreWebView2Async(env);

                // Configure WebView settings for modern desktop app feel
                WebViewControl.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                WebViewControl.CoreWebView2.Settings.AreDevToolsEnabled = true;
                WebViewControl.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebViewControl.CoreWebView2.Settings.IsZoomControlEnabled = true;

                WebViewControl.NavigationCompleted += (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        ServerStatusLed.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        ServerStatusLabel.Text = $"Local Server: Active ({DedicatedPort})";
                    }
                    else
                    {
                        UpdateStatus($"Navigation failed: {args.WebErrorStatus}. Retrying...");
                    }
                };

                UpdateStatus("Loading NGEOS Platform...");
                WebViewControl.CoreWebView2.Navigate(BaseUrl);
            }
            catch (Exception ex)
            {
                ShowErrorState($"Initialization error: {ex.Message}");
            }
        }

        private async Task<bool> CheckServerHealthyAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync(BaseUrl);
                return (int)response.StatusCode >= 200 && (int)response.StatusCode < 400;
            }
            catch
            {
                return false;
            }
        }

        private void StartLocalServer()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string? exePath = null;
                string? workingDir = null;

                // Search potential publish directories
                var searchDirs = new[]
                {
                    Path.Combine(baseDir, "publish"),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "publish")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "publish")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "publish")),
                    @"C:\Users\Saim\Desktop\NGEOS\publish"
                };

                foreach (var dir in searchDirs)
                {
                    var p = Path.Combine(dir, "NGEOS.exe");
                    if (File.Exists(p))
                    {
                        exePath = p;
                        workingDir = dir;
                        break;
                    }
                }

                // Search potential source project directories
                string? projectDir = null;
                string? projectFile = null;
                var projectDirs = new[]
                {
                    Path.Combine(baseDir, "NGEOS"),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "NGEOS")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "NGEOS")),
                    @"C:\Users\Saim\Desktop\NGEOS\NGEOS"
                };

                foreach (var dir in projectDirs)
                {
                    var p = Path.Combine(dir, "NGEOS.csproj");
                    if (File.Exists(p))
                    {
                        projectDir = dir;
                        projectFile = p;
                        break;
                    }
                }

                ProcessStartInfo psi;

                if (exePath != null && workingDir != null)
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = $"--urls http://127.0.0.1:{DedicatedPort}",
                        WorkingDirectory = workingDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                }
                else if (File.Exists(projectFile))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --no-build --urls http://127.0.0.1:{DedicatedPort}",
                        WorkingDirectory = projectDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                }
                else
                {
                    throw new FileNotFoundException("Could not find NGEOS.exe binary or NGEOS.csproj project.");
                }

                _serverProcess = Process.Start(psi);
                _weStartedServer = true;
            }
            catch (Exception ex)
            {
                ShowErrorState($"Failed to spawn server process: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = message;
            });
        }

        private void ShowErrorState(string error)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = error;
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                BtnManualRetry.Visibility = Visibility.Visible;
                ServerStatusLed.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                ServerStatusLabel.Text = "Server Offline";
            });
        }

        private async void BtnManualRetry_Click(object sender, RoutedEventArgs e)
        {
            BtnManualRetry.Visibility = Visibility.Collapsed;
            LoadingProgressBar.Visibility = Visibility.Visible;
            TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            await InitializeDesktopPlatformAsync();
        }

        #region Navigation Controls

        private void NavigateTo(string relativePath)
        {
            if (WebViewControl?.CoreWebView2 != null)
            {
                var target = relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? relativePath
                    : $"{BaseUrl}{relativePath}";
                WebViewControl.CoreWebView2.Navigate(target);
            }
        }

        private void NavHome_Click(object sender, RoutedEventArgs e) => NavigateTo("/");
        private void NavCauses_Click(object sender, RoutedEventArgs e) => NavigateTo("/Causes");
        private void NavDonate_Click(object sender, RoutedEventArgs e) => NavigateTo("/Donate");
        private void NavAdmin_Click(object sender, RoutedEventArgs e) => NavigateTo("/Admin");
        private void NavUser_Click(object sender, RoutedEventArgs e) => NavigateTo("/User/Dashboard");

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (WebViewControl?.CanGoBack == true) WebViewControl.GoBack();
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            if (WebViewControl?.CanGoForward == true) WebViewControl.GoForward();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            WebViewControl?.Reload();
        }

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void BtnHideBar_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopBar();
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _previousStyle = WindowStyle;
                _previousState = WindowState;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
            else
            {
                WindowStyle = _previousStyle;
                WindowState = _previousState;
                _isFullscreen = false;
            }
        }

        private void ToggleTopBar()
        {
            TopBarBorder.Visibility = TopBarBorder.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void WebDesktopWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                WebViewControl?.Reload();
                e.Handled = true;
            }
            else if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ToggleTopBar();
                e.Handled = true;
            }
        }

        #endregion

        private void WebDesktopWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_weStartedServer && _serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(true);
                    _serverProcess.Dispose();
                }
            }
            catch { }
        }
    }
}
