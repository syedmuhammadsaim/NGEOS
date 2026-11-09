using System;
using System.IO;
using System.Linq;
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
        private MainViewModel? _mainVm;

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

            ShutdownMode = ShutdownMode.OnMainWindowClose;

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

                    // Admin ViewModels
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<CausesViewModel>();
                    services.AddTransient<DonationsViewModel>();
                    services.AddTransient<ReportsViewModel>();
                    services.AddTransient<BackupViewModel>();
                    services.AddTransient<AuditLogsViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // Website Portal ViewModels
                    services.AddTransient<HomePortalViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new HomePortalViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<PublicDonateViewModel>();

                    services.AddTransient<ProgrammesViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new ProgrammesViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync("Donate", param);
                            }
                        });
                    });

                    services.AddTransient<ZakatCalculatorViewModel>(sp =>
                    {
                        return new ZakatCalculatorViewModel(async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<AboutViewModel>();
                    services.AddTransient<ContactViewModel>();
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

            // Fetch default Admin user for session
            User? defaultAdmin = null;
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();
                defaultAdmin = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            }

            // Create MainViewModel
            var authService = _host.Services.GetRequiredService<IAuthService>();
            var syncService = _host.Services.GetRequiredService<ISyncService>();
            var dashboardVm = _host.Services.GetRequiredService<DashboardViewModel>();
            var causesVm = _host.Services.GetRequiredService<CausesViewModel>();
            var donationsVm = _host.Services.GetRequiredService<DonationsViewModel>();
            var reportsVm = _host.Services.GetRequiredService<ReportsViewModel>();
            var backupVm = _host.Services.GetRequiredService<BackupViewModel>();
            var auditVm = _host.Services.GetRequiredService<AuditLogsViewModel>();
            var settingsVm = _host.Services.GetRequiredService<SettingsViewModel>();

            var homeVm = _host.Services.GetRequiredService<HomePortalViewModel>();
            var publicDonateVm = _host.Services.GetRequiredService<PublicDonateViewModel>();
            var programmesVm = _host.Services.GetRequiredService<ProgrammesViewModel>();
            var zakatVm = _host.Services.GetRequiredService<ZakatCalculatorViewModel>();
            var aboutVm = _host.Services.GetRequiredService<AboutViewModel>();
            var contactVm = _host.Services.GetRequiredService<ContactViewModel>();

            _mainVm = new MainViewModel(
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
                homeVm,
                publicDonateVm,
                programmesVm,
                zakatVm,
                aboutVm,
                contactVm,
                () => { });

            if (defaultAdmin != null)
            {
                await _mainVm.InitializeUserAsync(defaultAdmin);
            }

            _mainWindow = new MainWindow
            {
                DataContext = _mainVm
            };

            MainWindow = _mainWindow;
            _mainWindow.Show();
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
