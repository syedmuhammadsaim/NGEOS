using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Helpers;
using NGEOS.Models;

namespace NGEOS.Services
{
    public class PasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PasswordResetService> _logger;

        public const int ExpirationMinutes = 10;
        public const int CooldownSeconds = 60;
        public const int MaxVerificationAttempts = 5;
        public const int MaxRequestsPerHour = 5;

        public PasswordResetService(
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<PasswordResetService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public string GenerateSecure6DigitCode()
        {
            // Cryptographically secure 6-digit number between 100000 and 999999
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        /// <summary>
        /// Initiates or resends the password reset verification code flow.
        /// Returns (Success, ErrorMessage, SecondsRemainingForCooldown, UserFound)
        /// Note: To prevent account enumeration, caller should show a generic message to user unless there is a cooldown error.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage, int SecondsRemaining, bool UserExists)> RequestResetCodeAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Please enter a valid email address.", 0, false);
            }

            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                // Do not reveal whether user exists to prevent email harvesting
                _logger.LogInformation("Password reset requested for non-existent email {Email}", normalizedEmail);
                return (true, null, 0, false);
            }

            if (!user.IsActive)
            {
                return (false, "This account is inactive. Please contact NGEOS support.", 0, true);
            }

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentTokensCount = await _context.PasswordResetTokens
                .CountAsync(t => t.UserId == user.Id && t.CreatedAt >= oneHourAgo);

            if (recentTokensCount >= MaxRequestsPerHour)
            {
                return (false, "Too many password reset requests. For your security, please wait 1 hour before trying again.", 0, true);
            }

            // Check for resend cooldown (e.g. 60 seconds)
            var latestToken = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestToken != null)
            {
                var elapsedSeconds = (DateTime.UtcNow - latestToken.CreatedAt).TotalSeconds;
                if (elapsedSeconds < CooldownSeconds)
                {
                    var remaining = CooldownSeconds - (int)elapsedSeconds;
                    return (false, $"Please wait {remaining} second(s) before requesting another code.", remaining, true);
                }
            }

            // Invalidate any previous unverified tokens for this user
            var existingPendingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();

            foreach (var tok in existingPendingTokens)
            {
                tok.IsUsed = true; // Invalidate old tokens so only the newest code is valid
            }

            var code = GenerateSecure6DigitCode();
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
                IsUsed = false,
                IsVerified = false,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            try
            {
                await _emailSender.SendPasswordResetCodeAsync(user.Email, user.FullName, code, ExpirationMinutes);
                _logger.LogInformation("Password reset code sent to {Email}", user.Email);
                return (true, null, 0, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver password reset email to {Email}", user.Email);
                return (false, "Unable to deliver verification email at this moment. Please check your email configuration or contact support.", 0, true);
            }
        }

        /// <summary>
        /// Verifies the 6-digit code for the specified email.
        /// If valid, generates a one-time ResetSessionToken and returns it.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage, string? ResetSessionToken)> VerifyCodeAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return (false, "Email and 6-digit verification code are required.", null);
            }

            var normalizedEmail = email.Trim().ToLower();
            var trimmedCode = code.Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return (false, "Invalid verification code or email.", null);
            }

            var token = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                return (false, "No active password reset request found. Please request a new code.", null);
            }

            if (DateTime.UtcNow > token.Expiration)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
                return (false, "Verification code has expired. Please request a new one.", null);
            }

            if (token.Attempts >= MaxVerificationAttempts)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
                return (false, "Too many incorrect attempts. This code is now invalid. Please request a new code.", null);
            }

            if (!string.Equals(token.Code, trimmedCode, StringComparison.Ordinal))
            {
                token.Attempts++;
                await _context.SaveChangesAsync();
                var remainingAttempts = MaxVerificationAttempts - token.Attempts;
                return (false, $"Invalid verification code. You have {remainingAttempts} attempt(s) remaining.", null);
            }

            // Code verified successfully!
            token.IsVerified = true;
            token.ResetSessionToken = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset code verified successfully for user {Email}", user.Email);
            return (true, null, token.ResetSessionToken);
        }

        /// <summary>
        /// Validates that the ResetSessionToken is authorized and valid for password reset.
        /// </summary>
        public async Task<(bool IsValid, string? ErrorMessage)> ValidateResetSessionTokenAsync(string email, string resetSessionToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(resetSessionToken))
            {
                return (false, "Unauthorized password reset request.");
            }

            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return (false, "Invalid user.");
            }

            var token = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id &&
                            t.ResetSessionToken == resetSessionToken &&
                            t.IsVerified &&
                            !t.IsUsed &&
                            t.Expiration > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                return (false, "Password reset session has expired or is invalid. Please restart the process.");
            }

            return (true, null);
        }

        /// <summary>
        /// Resets the user's password securely and invalidates all reset tokens.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(string email, string resetSessionToken, string newPassword)
        {
            var (isValid, errorMessage) = await ValidateResetSessionTokenAsync(email, resetSessionToken);
            if (!isValid)
            {
                return (false, errorMessage);
            }

            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return (false, "User account not found.");
            }

            // Secure hash using the existing NGEOS PasswordHelper
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);

            // Invalidate ALL tokens for this user
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.IsUsed = true;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Password successfully reset for user {Email}", user.Email);
            return (true, null);
        }
    }
}
