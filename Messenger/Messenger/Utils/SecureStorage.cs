using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Utils
{
    public static class SecureStorage
    {
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("MessengerSecureStorageSalt");

        public static async Task SetAsync(string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Key cannot be null or empty");

                var encryptedData = Protect(Encoding.UTF8.GetBytes(value));
                var filePath = GetFilePath(key);

                await File.WriteAllBytesAsync(filePath, encryptedData);
            }
            catch (Exception ex)
            {
                // В случае ошибки просто не сохраняем данные
                Console.WriteLine($"SecureStorage.SetAsync error: {ex.Message}");
            }
        }

        public static async Task<string?> GetAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return null;

                var filePath = GetFilePath(key);

                if (!File.Exists(filePath))
                    return null;

                var encryptedData = await File.ReadAllBytesAsync(filePath);
                var decryptedData = Unprotect(encryptedData);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecureStorage.GetAsync error: {ex.Message}");
                return null;
            }
        }

        public static bool Remove(string key)
        {
            try
            {
                var filePath = GetFilePath(key);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecureStorage.Remove error: {ex.Message}");
                return false;
            }
        }

        private static byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
            }
            catch
            {
                // Fallback: просто возвращаем данные без шифрования
                return data;
            }
        }

        private static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, _entropy, DataProtectionScope.CurrentUser);
            }
            catch
            {
                // Fallback: предполагаем, что данные не были зашифрованы
                return data;
            }
        }

        private static string GetFilePath(string key)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "Messenger");

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            // Используем хэш ключа в качестве имени файла
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            var fileName = Convert.ToBase64String(hashBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");

            return Path.Combine(appFolder, $"{fileName}.dat");
        }
    }
}