using System.Text.RegularExpressions;

namespace Messenger.Utils
{
    public static class StringExtensions
    {
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidUsername(this string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (username.Length < AppConstants.MinUsernameLength ||
                username.Length > AppConstants.MaxUsernameLength)
                return false;

            var regex = new Regex("^[a-zA-Z0-9_]+$");
            return regex.IsMatch(username);
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(value) || startIndex >= value.Length)
                return string.Empty;

            if (startIndex + length > value.Length)
                length = value.Length - startIndex;

            return value.Substring(startIndex, length);
        }
    }
}