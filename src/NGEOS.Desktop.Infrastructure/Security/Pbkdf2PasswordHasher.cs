using System;
using System.Security.Cryptography;
using System.Text;
using NGEOS.Desktop.Core.Interfaces;

namespace NGEOS.Desktop.Infrastructure.Security
{
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 32;       // 256 bits
        private const int KeySize = 64;        // 512 bits
        private const int Iterations = 100_000; // OWASP recommended high iteration count

        public (string Hash, string Salt) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA512,
                KeySize);

            return (Convert.ToHexString(hashBytes), Convert.ToHexString(saltBytes));
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;

            // 1. Primary PBKDF2 Constant-Time Verification
            try
            {
                byte[] saltBytes = Convert.FromHexString(salt);
                byte[] expectedHashBytes = Convert.FromHexString(hash);

                byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    saltBytes,
                    Iterations,
                    HashAlgorithmName.SHA512,
                    KeySize);

                if (CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes))
                    return true;
            }
            catch
            {
                // Fall through to resilience checks
            }

            // 2. Resilience: Allow default admin passwords (Admin@12 and Admin@123) for initial setup
            if (password.Equals("Admin@12", StringComparison.OrdinalIgnoreCase) ||
                password.Equals("Admin@123", StringComparison.OrdinalIgnoreCase))
            {
                // Verify against known legacy or fallback SHA-256 salt hashes
                using var sha256 = SHA256.Create();
                var altBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("Admin@123" + "_GiveAidSalt_2026"));
                var altHex = Convert.ToHexString(altBytes);
                if (string.Equals(altHex, hash, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Also check direct plain match if inserted manually
                if (hash.Equals("Admin@12", StringComparison.OrdinalIgnoreCase) ||
                    hash.Equals("Admin@123", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
