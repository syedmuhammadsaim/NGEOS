using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.UI.ViewModels
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _successMessage = string.Empty;

        protected void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }

    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly Action<User> _onLoginSuccess;

        [ObservableProperty]
        private string _email = "admin@ngeos.org";

        [ObservableProperty]
        private string _password = "Admin@12";

        [ObservableProperty]
        private bool _isRegisterMode = false;

        [ObservableProperty]
        private string _regFullName = string.Empty;

        [ObservableProperty]
        private string _regProfession = "Philanthropist";

        [ObservableProperty]
        private string _regOrganization = "Humanitarian Community";

        [ObservableProperty]
        private string _regPhone = "03001234567";

        public LoginViewModel(IAuthService authService, Action<User> onLoginSuccess)
        {
            _authService = authService;
            _onLoginSuccess = onLoginSuccess;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ClearMessages();
            IsBusy = true;
            StatusMessage = "Authenticating with secure PBKDF2 parameters...";

            try
            {
                var result = await _authService.LoginAsync(Email, Password);
                if (result.Success && result.User != null)
                {
                    _onLoginSuccess(result.User);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Authentication error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ClearMessages();
            IsBusy = true;
            StatusMessage = "Creating secure local user record...";

            try
            {
                var result = await _authService.RegisterAsync(RegFullName, Email, Password, RegProfession, RegOrganization, RegPhone);
                if (result.Success)
                {
                    SuccessMessage = "Account registered successfully! You can now sign in.";
                    IsRegisterMode = false;
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AutoFillAdmin()
        {
            Email = "admin@ngeos.org";
            Password = "Admin@12";
            ClearMessages();
        }

        [RelayCommand]
        private void ToggleMode()
        {
            IsRegisterMode = !IsRegisterMode;
            ClearMessages();
        }
    }

    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;

        [ObservableProperty]
        private decimal _totalRaised;

        [ObservableProperty]
        private int _totalDonors;

        [ObservableProperty]
        private int _activeCauses;

        [ObservableProperty]
        private int _totalBeneficiaries;

        public ObservableCollection<Donation> RecentDonations { get; } = new();
        public ObservableCollection<Cause> TopCauses { get; } = new();

        public DashboardViewModel(NgeosDbContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var completedDonations = await _context.Donations
                    .Where(d => d.PaymentStatus == "Completed")
                    .ToListAsync();

                TotalRaised = completedDonations.Sum(d => d.Amount);
                TotalDonors = completedDonations.Select(d => d.DonorEmail.ToLower()).Distinct().Count();

                var causes = await _context.Causes.ToListAsync();
                ActiveCauses = causes.Count(c => c.IsActive);
                TotalBeneficiaries = causes.Sum(c => c.BeneficiariesCount);

                RecentDonations.Clear();
                var recent = await _context.Donations
                    .OrderByDescending(d => d.DonationDate)
                    .Take(7)
                    .ToListAsync();
                foreach (var d in recent) RecentDonations.Add(d);

                TopCauses.Clear();
                foreach (var c in causes.Take(4)) TopCauses.Add(c);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Dashboard loading error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class CausesViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly IExcelExportService _excelService;
        private readonly IAuditService _auditService;

        public ObservableCollection<Cause> Causes { get; } = new();
        public ObservableCollection<string> Categories { get; } = new() { "All Categories", "Education", "Water Relief", "Healthcare", "Livelihood", "Emergency" };

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private string _searchText = string.Empty;

        // Modal fields
        [ObservableProperty]
        private bool _isAddModalOpen;

        [ObservableProperty]
        private string _newTitle = string.Empty;

        [ObservableProperty]
        private string _newDescription = string.Empty;

        [ObservableProperty]
        private string _newCategory = "Education";

        [ObservableProperty]
        private decimal _newTarget = 500000;

        [ObservableProperty]
        private int _newBeneficiaries = 250;

        public CausesViewModel(NgeosDbContext context, IExcelExportService excelService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var query = _context.Causes.AsQueryable();

                if (SelectedCategory != "All Categories")
                    query = query.Where(c => c.Category == SelectedCategory);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var st = SearchText.Trim().ToLower();
                    query = query.Where(c => c.Title.ToLower().Contains(st) || c.Description.ToLower().Contains(st));
                }

                var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
                Causes.Clear();
                foreach (var c in list) Causes.Add(c);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenAddModal()
        {
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            NewTarget = 500000;
            NewBeneficiaries = 200;
            IsAddModalOpen = true;
        }

        [RelayCommand]
        private void CloseAddModal() => IsAddModalOpen = false;

        [RelayCommand]
        private async Task SaveCauseAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTitle))
            {
                ErrorMessage = "Cause Title is required.";
                return;
            }

            var cause = new Cause
            {
                Title = NewTitle.Trim(),
                Description = NewDescription.Trim(),
                Category = NewCategory,
                TargetAmount = NewTarget,
                RaisedAmount = 0,
                BeneficiariesCount = NewBeneficiaries,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Causes.AddAsync(cause);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("CreateCause", "Cause", cause.Id.ToString(), $"Created new welfare cause: {cause.Title}.");

            IsAddModalOpen = false;
            SuccessMessage = $"Cause '{cause.Title}' created successfully!";
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ToggleStatusAsync(Cause? cause)
        {
            if (cause == null) return;
            cause.IsActive = !cause.IsActive;
            cause.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("ToggleCauseStatus", "Cause", cause.Id.ToString(), $"Cause status toggled to Active: {cause.IsActive}.");
            await LoadAsync();
        }

        [RelayCommand]
        private async Task DeleteCauseAsync(Cause? cause)
        {
            if (cause == null) return;
            var res = MessageBox.Show($"Are you sure you want to delete cause '{cause.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                _context.Causes.Remove(cause);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("DeleteCause", "Cause", cause.Id.ToString(), $"Deleted cause: {cause.Title}.");
                await LoadAsync();
            }
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"NGEOS_Causes_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    var bytes = _excelService.ExportCausesToExcel(Causes.ToList());
                    await File.WriteAllBytesAsync(sfd.FileName, bytes);
                    SuccessMessage = $"Causes exported to: {Path.GetFileName(sfd.FileName)}";
                    await _auditService.LogAsync("ExportCauses", "Excel", null, $"Exported {Causes.Count} causes.");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    public partial class DonationsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly IExcelExportService _excelService;
        private readonly IReportService _reportService;
        private readonly IAuditService _auditService;

        public ObservableCollection<Donation> Donations { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusFilter = "All";

        [ObservableProperty]
        private bool _isAddModalOpen;

        [ObservableProperty]
        private string _newDonorName = string.Empty;

        [ObservableProperty]
        private string _newDonorEmail = string.Empty;

        [ObservableProperty]
        private decimal _newAmount = 25000;

        [ObservableProperty]
        private string _newCauseTitle = "Universal Education";

        [ObservableProperty]
        private string _newPaymentMethod = "Bank Transfer / Cash";

        public DonationsViewModel(NgeosDbContext context, IExcelExportService excelService, IReportService reportService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _reportService = reportService;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var q = _context.Donations.AsQueryable();
                if (StatusFilter != "All") q = q.Where(d => d.PaymentStatus == StatusFilter);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var st = SearchText.Trim().ToLower();
                    q = q.Where(d => d.DonorName.ToLower().Contains(st) || d.TransactionId.ToLower().Contains(st) || d.CauseTitle.ToLower().Contains(st));
                }

                var list = await q.OrderByDescending(d => d.DonationDate).ToListAsync();
                Donations.Clear();
                foreach (var d in list) Donations.Add(d);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenAddModal()
        {
            NewDonorName = string.Empty;
            NewDonorEmail = string.Empty;
            NewAmount = 25000;
            IsAddModalOpen = true;
        }

        [RelayCommand]
        private void CloseAddModal() => IsAddModalOpen = false;

        [RelayCommand]
        private async Task SaveDonationAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDonorName) || NewAmount <= 0)
            {
                ErrorMessage = "Valid donor name and amount are required.";
                return;
            }

            var txnId = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var rcpId = $"RCP-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

            var d = new Donation
            {
                TransactionId = txnId,
                ReceiptNumber = rcpId,
                DonorName = NewDonorName.Trim(),
                DonorEmail = string.IsNullOrWhiteSpace(NewDonorEmail) ? "donor@example.com" : NewDonorEmail.Trim(),
                Amount = NewAmount,
                CauseTitle = NewCauseTitle,
                PaymentMethod = NewPaymentMethod,
                PaymentStatus = "Completed",
                DonationDate = DateTime.UtcNow
            };

            await _context.Donations.AddAsync(d);

            // Update matching cause raised amount
            var cause = await _context.Causes.FirstOrDefaultAsync(c => c.Title.Contains(NewCauseTitle) || NewCauseTitle.Contains(c.Title));
            if (cause != null)
            {
                cause.RaisedAmount += NewAmount;
                d.CauseId = cause.Id;
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("CreateDonation", "Donation", d.Id.ToString(), $"Recorded donation of Rs. {d.Amount:N0} from {d.DonorName}.");

            IsAddModalOpen = false;
            SuccessMessage = $"Donation {txnId} recorded successfully!";
            await LoadAsync();
        }

        [RelayCommand]
        private async Task PrintReceiptAsync(Donation? donation)
        {
            if (donation == null) return;
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    FileName = $"NGEOS_Receipt_{donation.ReceiptNumber}.pdf"
                };

                if (sfd.ShowDialog() == true)
                {
                    var pdfBytes = _reportService.GenerateDonationReceiptPdf(donation);
                    await File.WriteAllBytesAsync(sfd.FileName, pdfBytes);
                    SuccessMessage = $"Receipt saved: {Path.GetFileName(sfd.FileName)}";
                    await _auditService.LogAsync("GenerateReceiptPdf", "PDF", donation.Id.ToString(), $"Generated PDF receipt {donation.ReceiptNumber}.");

                    // Offer to open PDF
                    var res = MessageBox.Show("Receipt generated successfully! Would you like to view it now?", "Receipt Ready", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Receipt error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"NGEOS_Donations_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    var bytes = _excelService.ExportDonationsToExcel(Donations.ToList());
                    await File.WriteAllBytesAsync(sfd.FileName, bytes);
                    SuccessMessage = $"Donations ledger saved: {Path.GetFileName(sfd.FileName)}";
                    await _auditService.LogAsync("ExportDonations", "Excel", null, $"Exported {Donations.Count} records.");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    public partial class BackupViewModel : ViewModelBase
    {
        private readonly IDatabaseBackupService _backupService;
        private readonly IAuditService _auditService;

        public ObservableCollection<BackupRecord> Backups { get; } = new();

        [ObservableProperty]
        private string _backupFolderPath = string.Empty;

        public BackupViewModel(IDatabaseBackupService backupService, IAuditService auditService)
        {
            _backupService = backupService;
            _auditService = auditService;
            BackupFolderPath = _backupService.BackupDirectory;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _backupService.GetBackupHistoryAsync();
                Backups.Clear();
                foreach (var b in list) Backups.Add(b);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CreateBackupAsync()
        {
            IsBusy = true;
            ClearMessages();
            StatusMessage = "Creating transactionally consistent SQLite snapshot...";

            try
            {
                var record = await _backupService.CreateBackupAsync(isAutomatic: false);
                SuccessMessage = $"Database backup created successfully: {Path.GetFileName(record.BackupPath)} ({record.FormattedSize})";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Backup creation failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync(BackupRecord? record)
        {
            if (record == null) return;

            var confirm = MessageBox.Show(
                $"WARNING: Restoring will overwrite the current active database with the state from {record.CreatedAt:dd MMM yyyy, HH:mm}.\n\nA safety backup of your current database will be saved before restore proceeds.\n\nAre you sure you want to continue?",
                "Confirm Database Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            ClearMessages();
            StatusMessage = "Verifying database integrity and restoring...";

            try
            {
                var res = await _backupService.RestoreBackupAsync(record.BackupPath);
                if (res.Success)
                {
                    SuccessMessage = res.Message;
                    MessageBox.Show(res.Message, "Restore Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ErrorMessage = res.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Restore error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenBackupFolder()
        {
            if (Directory.Exists(BackupFolderPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = BackupFolderPath,
                    UseShellExecute = true
                });
            }
        }
    }

    public partial class ReportsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly IReportService _reportService;
        private readonly IExcelExportService _excelService;
        private readonly IAuditService _auditService;

        public ReportsViewModel(NgeosDbContext context, IReportService reportService, IExcelExportService excelService, IAuditService auditService)
        {
            _context = context;
            _reportService = reportService;
            _excelService = excelService;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task GenerateExecutiveAuditPdfAsync()
        {
            IsBusy = true;
            ClearMessages();
            StatusMessage = "Synthesizing executive financial audit via QuestPDF engine...";

            try
            {
                var donations = await _context.Donations.OrderByDescending(d => d.DonationDate).ToListAsync();
                var causes = await _context.Causes.ToListAsync();
                var totalRaised = donations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
                var totalDonors = donations.Select(d => d.DonorEmail.ToLower()).Distinct().Count();

                var sfd = new SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    FileName = $"NGEOS_Audit_Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
                };

                if (sfd.ShowDialog() == true)
                {
                    var pdfBytes = _reportService.GenerateAuditReportPdf(donations, causes, totalRaised, totalDonors);
                    await File.WriteAllBytesAsync(sfd.FileName, pdfBytes);
                    SuccessMessage = $"Audit PDF report saved: {Path.GetFileName(sfd.FileName)}";
                    await _auditService.LogAsync("GenerateAuditReport", "PDF", null, "Generated Executive Audit Report.");

                    var res = MessageBox.Show("Report generated! Would you like to view it now?", "Report Ready", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"PDF generation error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class AuditLogsViewModel : ViewModelBase
    {
        private readonly IAuditService _auditService;
        public ObservableCollection<AuditLog> Logs { get; } = new();

        public AuditLogsViewModel(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _auditService.GetRecentLogsAsync(100);
                Logs.Clear();
                foreach (var log in list) Logs.Add(log);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;

        [ObservableProperty]
        private string _databaseLocation = string.Empty;

        [ObservableProperty]
        private string _sqliteMode = "WAL (Write-Ahead Logging)";

        [ObservableProperty]
        private string _appVersion = "v2.0.0 (Pure WPF Desktop Architecture)";

        public SettingsViewModel(NgeosDbContext context)
        {
            _context = context;
            DatabaseLocation = NgeosDbContext.GetDatabasePath();
        }
    }
}
