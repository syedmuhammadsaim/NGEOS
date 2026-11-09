using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Backup;
using NGEOS.Desktop.Infrastructure.Database;
using NGEOS.Desktop.Infrastructure.Export;
using NGEOS.Desktop.Infrastructure.Security;
using NGEOS.Desktop.Infrastructure.Services;
using NGEOS.Desktop.UI.ViewModels;
using NGEOS.Desktop.UI.Views;

namespace NGEOS.Desktop.UI
{
    public partial class App : Application
    {
        private IHost? _host;
        private MainWindow? _mainWindow;
        private LoginWindow? _loginWindow;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Application Error: {args.Exception.Message}\n\n{args.Exception}", "NGEOS Desktop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                MessageBox.Show($"Fatal Error: {args.ExceptionObject}", "NGEOS Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // EF Core SQLite DbContext
                    services.AddDbContext<NgeosDbContext>();

                    // Infrastructure & Core Services
                    services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IDatabaseBackupService, SqliteBackupService>();
                    services.AddScoped<IAuditService, AuditService>();
                    services.AddSingleton<IExcelExportService, ClosedXmlExportService>();
                    services.AddSingleton<IReportService, QuestPdfReportService>();

                    // Hybrid Connectivity & Two-Way Sync Engine
                    services.AddSingleton<IConnectivityService, ConnectivityService>();
                    services.AddSingleton<ISyncService, SyncService>();

                    // ViewModels
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<CausesViewModel>();
                    services.AddTransient<DonationsViewModel>();
                    services.AddTransient<ReportsViewModel>();
                    services.AddTransient<BackupViewModel>();
                    services.AddTransient<AuditLogsViewModel>();
                    services.AddTransient<SettingsViewModel>();
                })
                .Build();

            await _host.StartAsync();

            // Initialize SQLite Database and ensure seeds
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await db.InitializeAndSeedAsync(hasher);
            }

            // Start live network monitoring
            var connectivity = _host.Services.GetRequiredService<IConnectivityService>();
            connectivity.StartMonitoring(15);

            ShowLogin();
        }

        private void ShowLogin()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Close();
                _mainWindow = null;
            }

            var authService = _host!.Services.GetRequiredService<IAuthService>();
            var loginVm = new LoginViewModel(authService, OnLoginSuccess);

            _loginWindow = new LoginWindow
            {
                DataContext = loginVm
            };
            _loginWindow.Show();
        }

        private async void OnLoginSuccess(User user)
        {
            var authService = _host!.Services.GetRequiredService<IAuthService>();
            var connectivity = _host.Services.GetRequiredService<IConnectivityService>();
            var syncService = _host.Services.GetRequiredService<ISyncService>();
            var dashboardVm = _host.Services.GetRequiredService<DashboardViewModel>();
            var causesVm = _host.Services.GetRequiredService<CausesViewModel>();
            var donationsVm = _host.Services.GetRequiredService<DonationsViewModel>();
            var reportsVm = _host.Services.GetRequiredService<ReportsViewModel>();
            var backupVm = _host.Services.GetRequiredService<BackupViewModel>();
            var auditVm = _host.Services.GetRequiredService<AuditLogsViewModel>();
            var settingsVm = _host.Services.GetRequiredService<SettingsViewModel>();

            var mainVm = new MainViewModel(
                authService,
                _host.Services,
                connectivity,
                syncService,
                dashboardVm,
                causesVm,
                donationsVm,
                reportsVm,
                backupVm,
                auditVm,
                settingsVm,
                OnLogoutRequested);

            await mainVm.InitializeUserAsync(user);

            _mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            _mainWindow.Closed += (s, args) =>
            {
                if (_loginWindow == null || !_loginWindow.IsVisible)
                {
                    Shutdown();
                }
            };

            _mainWindow.Show();

            if (_loginWindow != null)
            {
                _loginWindow.Close();
                _loginWindow = null;
            }
        }

        private void OnLogoutRequested()
        {
            ShowLogin();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                var connectivity = _host.Services.GetService<IConnectivityService>();
                connectivity?.StopMonitoring();

                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
