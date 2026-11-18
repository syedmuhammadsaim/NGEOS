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

        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGEOS_Desktop", "app_startup.log");
        private static readonly object LogLock = new();

        public static void Log(string message)
        {
            try
            {
                lock (LogLock)
                {
                    var dir = Path.GetDirectoryName(LogFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                    using var stream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using var writer = new StreamWriter(stream);
                    writer.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}");
                }
            }
            catch { }
        }

        private static void WriteLastError(string error)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGEOS_Desktop");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "last_error.txt"), error);
            }
            catch { }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                var msg = $"DispatcherUnhandledException: {args.Exception}";
                Log(msg);
                WriteLastError(msg);
                MessageBox.Show($"Application Error: {args.Exception.Message}\n\n{args.Exception}", "NGEOS Desktop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var msg = $"AppDomain.UnhandledException: {args.ExceptionObject}";
                Log(msg);
                WriteLastError(msg);
                MessageBox.Show($"Fatal Error: {args.ExceptionObject}", "NGEOS Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                var msg = $"TaskScheduler.UnobservedTaskException: {args.Exception}";
                Log(msg);
                WriteLastError(msg);
            };

            // Prevent premature WPF shutdown while awaiting async initialization before the MainWindow is shown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                Log("Starting OnStartup host configuration...");
                _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // EF Core SQLite DbContext - Transient to ensure zero concurrency conflicts across ViewModels
                    services.AddDbContext<NgeosDbContext>(ServiceLifetime.Transient, ServiceLifetime.Transient);

                    // Infrastructure & Core Services
                    services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
                    services.AddTransient<IAuthService, AuthService>();
                    services.AddTransient<IDatabaseBackupService, SqliteBackupService>();
                    services.AddTransient<IAuditService, AuditService>();
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

                    services.AddTransient<CausesCatalogViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new CausesCatalogViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<CauseDetailsViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new CauseDetailsViewModel(db, async (target, param) =>
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
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<ProgrammeDetailsViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        var audit = sp.GetRequiredService<IAuditService>();
                        return new ProgrammeDetailsViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        }, audit);
                    });

                    services.AddTransient<EventsViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new EventsViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<GalleryViewModel>();
                    
                    services.AddTransient<NgosDirectoryViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new NgosDirectoryViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<HelpCentreFaqViewModel>(sp =>
                    {
                        var db = sp.GetRequiredService<NgeosDbContext>();
                        return new HelpCentreFaqViewModel(db, async (target, param) =>
                        {
                            if (_mainVm != null)
                            {
                                await _mainVm.HandlePortalNavigationAsync(target, param);
                            }
                        });
                    });

                    services.AddTransient<UserPortalViewModel>();

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

            Log("Host built successfully. Starting host...");
            await _host.StartAsync();
            Log("Host started. Initializing database and seeds...");

            // Initialize SQLite Database and ensure seeds
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await db.InitializeAndSeedAsync(hasher);
            }
            Log("Database initialized and seeded.");

            // Start live network monitoring
            var connectivity = _host.Services.GetRequiredService<IConnectivityService>();
            connectivity.StartMonitoring(15);
            Log("Connectivity monitoring started.");

            // Fetch default Admin user for session
            User? defaultAdmin = null;
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();
                defaultAdmin = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            }
            Log($"Default admin user found: {defaultAdmin?.FullName ?? "None"}");

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
            var causesCatalogVm = _host.Services.GetRequiredService<CausesCatalogViewModel>();
            var causeDetailsVm = _host.Services.GetRequiredService<CauseDetailsViewModel>();
            var publicDonateVm = _host.Services.GetRequiredService<PublicDonateViewModel>();
            var programmesVm = _host.Services.GetRequiredService<ProgrammesViewModel>();
            var programmeDetailsVm = _host.Services.GetRequiredService<ProgrammeDetailsViewModel>();
            var eventsVm = _host.Services.GetRequiredService<EventsViewModel>();
            var galleryVm = _host.Services.GetRequiredService<GalleryViewModel>();
            var ngosDirectoryVm = _host.Services.GetRequiredService<NgosDirectoryViewModel>();
            var helpCentreFaqVm = _host.Services.GetRequiredService<HelpCentreFaqViewModel>();
            var userPortalVm = _host.Services.GetRequiredService<UserPortalViewModel>();
            var zakatVm = _host.Services.GetRequiredService<ZakatCalculatorViewModel>();
            var aboutVm = _host.Services.GetRequiredService<AboutViewModel>();
            var contactVm = _host.Services.GetRequiredService<ContactViewModel>();
            Log("All ViewModels instantiated.");

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
                causesCatalogVm,
                causeDetailsVm,
                publicDonateVm,
                programmesVm,
                programmeDetailsVm,
                eventsVm,
                galleryVm,
                ngosDirectoryVm,
                helpCentreFaqVm,
                userPortalVm,
                zakatVm,
                aboutVm,
                contactVm,
                () => { });

            if (defaultAdmin != null)
            {
                await _mainVm.InitializeUserAsync(defaultAdmin);
                Log("MainViewModel initialized with admin user.");
            }

            Log("Diagnosing all Views instantiation...");
            try { var _ = new HomePortalView(); Log("HomePortalView OK"); } catch (Exception ex) { Log($"HomePortalView FAILED: {ex}"); }
            try { var _ = new CausesCatalogView(); Log("CausesCatalogView OK"); } catch (Exception ex) { Log($"CausesCatalogView FAILED: {ex}"); }
            try { var _ = new CauseDetailsView(); Log("CauseDetailsView OK"); } catch (Exception ex) { Log($"CauseDetailsView FAILED: {ex}"); }
            try { var _ = new PublicDonateView(); Log("PublicDonateView OK"); } catch (Exception ex) { Log($"PublicDonateView FAILED: {ex}"); }
            try { var _ = new ProgrammesView(); Log("ProgrammesView OK"); } catch (Exception ex) { Log($"ProgrammesView FAILED: {ex}"); }
            try { var _ = new ProgrammeDetailsView(); Log("ProgrammeDetailsView OK"); } catch (Exception ex) { Log($"ProgrammeDetailsView FAILED: {ex}"); }
            try { var _ = new EventsView(); Log("EventsView OK"); } catch (Exception ex) { Log($"EventsView FAILED: {ex}"); }
            try { var _ = new GalleryView(); Log("GalleryView OK"); } catch (Exception ex) { Log($"GalleryView FAILED: {ex}"); }
            try { var _ = new NgosDirectoryView(); Log("NgosDirectoryView OK"); } catch (Exception ex) { Log($"NgosDirectoryView FAILED: {ex}"); }
            try { var _ = new HelpCentreFaqView(); Log("HelpCentreFaqView OK"); } catch (Exception ex) { Log($"HelpCentreFaqView FAILED: {ex}"); }
            try { var _ = new UserPortalView(); Log("UserPortalView OK"); } catch (Exception ex) { Log($"UserPortalView FAILED: {ex}"); }
            try { var _ = new ZakatCalculatorView(); Log("ZakatCalculatorView OK"); } catch (Exception ex) { Log($"ZakatCalculatorView FAILED: {ex}"); }
            try { var _ = new AboutView(); Log("AboutView OK"); } catch (Exception ex) { Log($"AboutView FAILED: {ex}"); }
            try { var _ = new ContactView(); Log("ContactView OK"); } catch (Exception ex) { Log($"ContactView FAILED: {ex}"); }
            try { var _ = new DashboardView(); Log("DashboardView OK"); } catch (Exception ex) { Log($"DashboardView FAILED: {ex}"); }
            try { var _ = new CausesView(); Log("CausesView OK"); } catch (Exception ex) { Log($"CausesView FAILED: {ex}"); }
            try { var _ = new DonationsView(); Log("DonationsView OK"); } catch (Exception ex) { Log($"DonationsView FAILED: {ex}"); }
            try { var _ = new ReportsView(); Log("ReportsView OK"); } catch (Exception ex) { Log($"ReportsView FAILED: {ex}"); }
            try { var _ = new BackupView(); Log("BackupView OK"); } catch (Exception ex) { Log($"BackupView FAILED: {ex}"); }
            try { var _ = new AuditLogsView(); Log("AuditLogsView OK"); } catch (Exception ex) { Log($"AuditLogsView FAILED: {ex}"); }
            try { var _ = new SettingsView(); Log("SettingsView OK"); } catch (Exception ex) { Log($"SettingsView FAILED: {ex}"); }

            Log("Constructing MainWindow...");
            _mainWindow = new MainWindow
            {
                DataContext = _mainVm
            };
            Log("MainWindow constructed. Setting Application.MainWindow and calling Show()...");

            try
            {
                MainWindow = _mainWindow;
                _mainWindow.Closing += (s, e) =>
                {
                    Log("MainWindow.Closing triggered!");
                };
                _mainWindow.Closed += (s, e) =>
                {
                    Log("MainWindow.Closed triggered! Calling Application.Shutdown()...");
                    Shutdown();
                };

                _mainWindow.Show();
                Log("MainWindow shown successfully. Application running under OnExplicitShutdown.");
            }
            catch (Exception ex)
            {
                var msg = $"CRITICAL EXCEPTION during _mainWindow.Show(): {ex}";
                Log(msg);
                WriteLastError(msg);
                MessageBox.Show($"Window Display Error: {ex.Message}\n\n{ex}", "NGEOS Window Display Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            var msg = $"CRITICAL EXCEPTION in OnStartup: {ex}";
            Log(msg);
            WriteLastError(msg);
            MessageBox.Show($"Startup Error: {ex.Message}\n\n{ex}", "NGEOS Desktop Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log($"Application.OnExit called with ExitCode: {e.ApplicationExitCode}");
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
