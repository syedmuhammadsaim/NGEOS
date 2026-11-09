using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly NgeosDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;

        public User? CurrentUser { get; private set; }
        public event Action<User?>? AuthStateChanged;

        public AuthService(NgeosDbContext context, IPasswordHasher passwordHasher, IAuditService auditService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Please enter both Email and Password.", null);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
            if (user == null)
            {
                await _auditService.LogAsync("LoginFailed", "User", null, $"Unknown user attempt: {email.Trim()}");
                return (false, "Invalid email address or password.", null);
            }

            if (user.IsLockedOut)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEndUtc!.Value - DateTime.UtcNow).TotalMinutes);
                await _auditService.LogAsync("LoginBlocked", "User", user.Id.ToString(), $"Account is locked out. Remaining: {remainingMinutes} mins.");
                return (false, $"Account is temporarily locked due to multiple failed attempts. Please try again in {remainingMinutes} minutes.", null);
            }

            if (!user.IsActive)
            {
                await _auditService.LogAsync("LoginRejected", "User", user.Id.ToString(), "Account is deactivated.");
                return (false, "Your account has been deactivated. Please contact an administrator.", null);
            }

            bool isValid = _passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
            if (!isValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync("AccountLocked", "User", user.Id.ToString(), "Account locked out after 5 consecutive failed attempts.");
                    return (false, "Account locked out for 15 minutes due to 5 consecutive failed attempts.", null);
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("LoginFailed", "User", user.Id.ToString(), $"Incorrect password attempt ({user.FailedLoginAttempts}/5).");
                return (false, $"Invalid email or password. Attempt {user.FailedLoginAttempts} of 5 before temporary lockout.", null);
            }

            // Successful Login
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            CurrentUser = user;
            AuthStateChanged?.Invoke(CurrentUser);

            await _auditService.LogAsync("LoginSuccess", "User", user.Id.ToString(), $"User logged in successfully with role: {user.Role}.");
            return (true, "Login successful.", user);
        }

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(string fullName, string email, string password, string profession, string organization, string? phone)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Full Name, Email, and Password are required.", null);

            var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower());
            if (existing)
                return (false, "An account with this email address already exists.", null);

            var (hash, salt) = _passwordHasher.HashPassword(password);
            var user = new User
            {
                FullName = fullName.Trim(),
                Email = email.Trim().ToLower(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Profession = profession.Trim(),
                Organization = organization.Trim(),
                Phone = phone?.Trim(),
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UserRegistered", "User", user.Id.ToString(), $"New user self-registered: {user.Email}.");
            return (true, "Registration successful. You can now log in.", user);
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                return (false, "Current password is incorrect.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "New password must be at least 6 characters long.");

            var (newHash, newSalt) = _passwordHasher.HashPassword(newPassword);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("PasswordChanged", "User", user.Id.ToString(), "User changed account password.");
            return (true, "Password changed successfully.");
        }

        public async Task<(bool Success, string Message)> ResetAdminPasswordAsync(string newPassword)
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (admin == null) return (false, "Administrator account not found.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Password must be at least 6 characters long.");

            var (newHash, newSalt) = _passwordHasher.HashPassword(newPassword);
            admin.PasswordHash = newHash;
            admin.PasswordSalt = newSalt;
            admin.FailedLoginAttempts = 0;
            admin.LockoutEndUtc = null;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("AdminPasswordReset", "User", admin.Id.ToString(), "Master administrator password reset via local secure recovery.");
            return (true, "Administrator password has been reset successfully.");
        }

        public void Logout()
        {
            if (CurrentUser != null)
            {
                _auditService.LogAsync("Logout", "User", CurrentUser.Id.ToString(), "User logged out.");
            }

            CurrentUser = null;
            AuthStateChanged?.Invoke(null);
        }
    }
}
