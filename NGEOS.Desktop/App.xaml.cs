using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NGEOS.Desktop.Data;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;
using NGEOS.Desktop.Services;
using NGEOS.Desktop.ViewModels;
using NGEOS.Desktop.Views;

namespace NGEOS.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGEOS", "desktop_trace.log");

        public static void Trace(string msg)
        {
            Console.WriteLine($"[TRACE] {msg}");
            try
            {
                var dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\n");
            }
            catch { }
        }

        public App()
        {
            Trace("App: constructor entered");
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            DispatcherUnhandledException += (s, e) =>
            {
                Trace($"FATAL DispatcherUnhandledException: {e.Exception}");
                MessageBox.Show($"Application Error:\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}", "NGEOS Desktop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Trace($"FATAL AppDomain.UnhandledException: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Trace($"UnobservedTaskException: {e.Exception}");
                e.SetObserved();
            };
            Trace("App: constructor completed");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Trace("OnStartup: entered");
            base.OnStartup(e);

            try
            {
                Trace("OnStartup: Configuring Services...");
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                Trace("OnStartup: Services built");

                // Ensure SQLite database is created locally
                try
                {
                    Trace("OnStartup: Creating SQLite DB if needed...");
                    using var db = new OfflineDbContext();
                    db.Database.EnsureCreated();
                    Trace("OnStartup: SQLite DB ready");
                }
                catch (Exception ex)
                {
                    Trace($"OnStartup: SQLite notice: {ex.Message}");
                }

                Trace("OnStartup: calling ShowLoginWindow...");
                ShowLoginWindow();
                Trace("OnStartup: ShowLoginWindow called successfully");
            }
            catch (Exception ex)
            {
                Trace($"OnStartup: CRASH: {ex}");
                MessageBox.Show($"Startup failed:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", "NGEOS Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Security & Storage
            services.AddSingleton<ISecureStorageService, SecureStorageService>();

            // API & Core Services
            services.AddSingleton<IApiService, ApiService>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<IRealtimeService, RealtimeService>();
            services.AddSingleton<IReportService, ReportService>();
        }

        public void ShowLoginWindow()
        {
            Trace("ShowLoginWindow: entered");
            var apiService = ServiceProvider.GetRequiredService<IApiService>();
            var secureStorage = ServiceProvider.GetRequiredService<ISecureStorageService>();

            Trace("ShowLoginWindow: creating LoginViewModel...");
            var loginVm = new LoginViewModel(apiService, secureStorage, user =>
            {
                ShowMainWindow(user);
            });
            Trace("ShowLoginWindow: LoginViewModel created");

            Trace("ShowLoginWindow: creating LoginWindow...");
            var loginWindow = new LoginWindow
            {
                DataContext = loginVm
            };
            Trace("ShowLoginWindow: LoginWindow created");

            loginWindow.Closed += (s, e) =>
            {
                Trace("loginWindow.Closed fired");
                if (MainWindow == loginWindow)
                {
                    Trace("loginWindow.Closed: calling Shutdown");
                    Shutdown();
                }
            };

            var previousWindow = MainWindow;
            MainWindow = loginWindow;
            Trace("ShowLoginWindow: calling loginWindow.Show()...");
            loginWindow.Show();
            Trace("ShowLoginWindow: loginWindow.Show() returned");

            if (previousWindow != null && previousWindow != loginWindow)
            {
                Trace("ShowLoginWindow: closing previousWindow...");
                previousWindow.Close();
            }
        }

        public void ShowMainWindow(LocalUser user)
        {
            Trace($"ShowMainWindow: entered for user: {user.FullName} ({user.Role})");
            var apiService = ServiceProvider.GetRequiredService<IApiService>();
            var syncService = ServiceProvider.GetRequiredService<ISyncService>();
            var realtimeService = ServiceProvider.GetRequiredService<IRealtimeService>();
            var secureStorage = ServiceProvider.GetRequiredService<ISecureStorageService>();
            var reportService = ServiceProvider.GetRequiredService<IReportService>();

            Trace("ShowMainWindow: creating MainViewModel...");
            var mainVm = new MainViewModel(user, apiService, syncService, realtimeService, secureStorage, reportService, () =>
            {
                ShowLoginWindow();
            });
            Trace("ShowMainWindow: MainViewModel created");

            Trace("ShowMainWindow: creating MainWindow...");
            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };
            Trace("ShowMainWindow: MainWindow created");

            mainWindow.Closed += (s, e) =>
            {
                Trace("mainWindow.Closed fired");
                if (MainWindow == mainWindow)
                {
                    Trace("mainWindow.Closed: calling Shutdown");
                    Shutdown();
                }
            };

            var previousWindow = MainWindow;
            MainWindow = mainWindow;
            Trace("ShowMainWindow: calling mainWindow.Show()...");
            mainWindow.Show();
            Trace("ShowMainWindow: mainWindow.Show() returned");

            if (previousWindow != null && previousWindow != mainWindow)
            {
                Trace("ShowMainWindow: closing previousWindow...");
                previousWindow.Close();
            }
        }
    }
}
