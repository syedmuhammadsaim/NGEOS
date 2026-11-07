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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Ensure SQLite database is created locally
            try
            {
                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Offline database initialization notice: {ex.Message}", "NGEOS Desktop", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ShowLoginWindow();
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
            var apiService = ServiceProvider.GetRequiredService<IApiService>();
            var secureStorage = ServiceProvider.GetRequiredService<ISecureStorageService>();

            var loginVm = new LoginViewModel(apiService, secureStorage, user =>
            {
                ShowMainWindow(user);
            });

            var loginWindow = new LoginWindow
            {
                DataContext = loginVm
            };

            var currentMain = MainWindow;
            MainWindow = loginWindow;
            loginWindow.Show();
            currentMain?.Close();
        }

        public void ShowMainWindow(LocalUser user)
        {
            var apiService = ServiceProvider.GetRequiredService<IApiService>();
            var syncService = ServiceProvider.GetRequiredService<ISyncService>();
            var realtimeService = ServiceProvider.GetRequiredService<IRealtimeService>();
            var secureStorage = ServiceProvider.GetRequiredService<ISecureStorageService>();
            var reportService = ServiceProvider.GetRequiredService<IReportService>();

            var mainVm = new MainViewModel(user, apiService, syncService, realtimeService, secureStorage, reportService, () =>
            {
                ShowLoginWindow();
            });

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            var currentWindow = MainWindow;
            MainWindow = mainWindow;
            mainWindow.Show();
            currentWindow?.Close();
        }
    }
}
