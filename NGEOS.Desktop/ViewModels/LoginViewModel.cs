using System;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Data;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;
using NGEOS.Desktop.Services;

namespace NGEOS.Desktop.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private readonly Action<LocalUser> _onLoginSuccess;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isOnlineMode = true;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsOnlineMode
        {
            get => _isOnlineMode;
            set => SetProperty(ref _isOnlineMode, value);
        }

        public IAsyncRelayCommand LoginCommand { get; }
        public IRelayCommand AutoFillAdminCommand { get; }
        public IAsyncRelayCommand CheckConnectionCommand { get; }

        public LoginViewModel(IApiService apiService, ISecureStorageService secureStorage, Action<LocalUser> onLoginSuccess)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
            _onLoginSuccess = onLoginSuccess;

            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
            AutoFillAdminCommand = new RelayCommand(ExecuteAutoFillAdmin);
            CheckConnectionCommand = new AsyncRelayCommand(CheckConnectivityAsync);

            // Check initial online status
            Task.Run(async () => await CheckConnectivityAsync());
        }

        private async Task CheckConnectivityAsync()
        {
            IsOnlineMode = await _apiService.CheckConnectivityAsync();
        }

        private void ExecuteAutoFillAdmin()
        {
            Email = "admin@ngeos.org";
            Password = "Admin@123";
        }

        private LocalUser? TryOfflineLogin(string email, string password)
        {
            try
            {
                var creds = _secureStorage.GetOfflineCredentials(email);
                if (creds == null) return null;

                var enteredHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
                if (enteredHash != creds.PasswordHash) return null;

                using var db = new OfflineDbContext();
                db.Database.EnsureCreated();
                var localUser = db.Users.FirstOrDefault(u => u.Email.ToLower() == email.Trim().ToLower())
                    ?? new LocalUser
                    {
                        Id = creds.UserId,
                        Email = creds.Email,
                        FullName = creds.FullName,
                        Role = creds.Role
                    };

                return localUser;
            }
            catch
            {
                return null;
            }
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both Email and Password.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Authenticating...";

            try
            {
                var isOnline = await _apiService.CheckConnectivityAsync();
                IsOnlineMode = isOnline;

                if (isOnline)
                {
                    // 1. ONLINE AUTHENTICATION VIA REST API
                    var response = await _apiService.LoginAsync(Email, Password);
                    if (response.Success && response.Data != null)
                    {
                        var user = response.Data;

                        // Save user in local SQLite for offline access
                        using var db = new OfflineDbContext();
                        await db.Database.EnsureCreatedAsync();

                        var localUser = await db.Users.FindAsync(user.Id);
                        if (localUser == null)
                        {
                            db.Users.Add(user);
                        }
                        else
                        {
                            localUser.FullName = user.FullName;
                            localUser.Role = user.Role;
                            localUser.ProfileImageUrl = user.ProfileImageUrl;
                            localUser.LastLoginAt = DateTime.UtcNow;
                        }
                        await db.SaveChangesAsync();

                        // Cache credentials securely using Windows DPAPI
                        var passHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Password)));
                        _secureStorage.SaveOfflineCredentials(user.Email, passHash, user.Role, user.Id, user.FullName);

                        _onLoginSuccess(user);
                        return;
                    }

                    // Fallback to offline credentials if live API v1 is not deployed yet or returns 404
                    var offlineUser = TryOfflineLogin(Email, Password);
                    if (offlineUser != null)
                    {
                        _onLoginSuccess(offlineUser);
                        return;
                    }

                    ErrorMessage = response.Message ?? "Invalid email or password.";
                }
                else
                {
                    // 2. OFFLINE AUTHENTICATION VIA DPAPI CACHED CREDENTIALS
                    var offlineUser = TryOfflineLogin(Email, Password);
                    if (offlineUser != null)
                    {
                        _onLoginSuccess(offlineUser);
                        return;
                    }

                    ErrorMessage = "Device is offline. No matching offline credentials found on this machine.";
                }
            }
            catch (Exception ex)
            {
                // Fallback attempt on any network or parsing failure
                var offlineUser = TryOfflineLogin(Email, Password);
                if (offlineUser != null)
                {
                    _onLoginSuccess(offlineUser);
                    return;
                }

                ErrorMessage = $"Authentication error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }
    }
}
