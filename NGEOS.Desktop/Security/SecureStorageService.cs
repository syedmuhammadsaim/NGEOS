using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NGEOS.Desktop.Security
{
    public interface ISecureStorageService
    {
        void SaveToken(string token);
        string? GetToken();
        void ClearToken();
        void SaveOfflineCredentials(string email, string passwordHash, string role, int userId, string fullName);
        OfflineCredentials? GetOfflineCredentials(string email);
        void SaveSetting(string key, string value);
        string? GetSetting(string key);
    }

    public class OfflineCredentials
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class SecureStorageService : ISecureStorageService
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("NGEOS_WPF_DESKTOP_DPAPI_ENTROPY_2026");
        private readonly string _storageFolder;

        public SecureStorageService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storageFolder = Path.Combine(appData, "NGEOS", "Security");
            if (!Directory.Exists(_storageFolder))
            {
                Directory.CreateDirectory(_storageFolder);
            }
        }

        private string GetFilePath(string key) => Path.Combine(_storageFolder, $"{key}.sec");

        public void SaveToken(string token)
        {
            SaveEncryptedString("jwt_token", token);
        }

        public string? GetToken()
        {
            return ReadEncryptedString("jwt_token");
        }

        public void ClearToken()
        {
            var path = GetFilePath("jwt_token");
            if (File.Exists(path)) File.Delete(path);
        }

        public void SaveOfflineCredentials(string email, string passwordHash, string role, int userId, string fullName)
        {
            var creds = new OfflineCredentials
            {
                Email = email.Trim().ToLower(),
                PasswordHash = passwordHash,
                Role = role,
                UserId = userId,
                FullName = fullName,
                SavedAt = DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(creds);
            var safeKey = "cred_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(creds.Email)));
            SaveEncryptedString(safeKey, json);
        }

        public OfflineCredentials? GetOfflineCredentials(string email)
        {
            var safeKey = "cred_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLower())));
            var json = ReadEncryptedString(safeKey);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<OfflineCredentials>(json);
            }
            catch
            {
                return null;
            }
        }

        public void SaveSetting(string key, string value)
        {
            SaveEncryptedString("set_" + key, value);
        }

        public string? GetSetting(string key)
        {
            return ReadEncryptedString("set_" + key);
        }

        private void SaveEncryptedString(string key, string plainText)
        {
            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(GetFilePath(key), encryptedBytes);
            }
            catch
            {
                // Fallback or ignore
            }
        }

        private string? ReadEncryptedString(string key)
        {
            try
            {
                var path = GetFilePath(key);
                if (!File.Exists(path)) return null;

                var encryptedBytes = File.ReadAllBytes(path);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}
