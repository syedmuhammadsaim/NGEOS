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
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
