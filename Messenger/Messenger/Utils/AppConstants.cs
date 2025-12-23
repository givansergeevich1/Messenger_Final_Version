namespace Messenger.Utils
{
    public static class AppConstants
    {
        // Application settings
        public const string AppName = "Messenger";
        public const string AppVersion = "1.0.0";

        // Validation constants
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 20;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 100;

        // Chat constants
        public const int MaxMessageLength = 1000;
        public const int MaxChatNameLength = 50;

        // UI constants
        public const double DefaultWindowWidth = 1200;
        public const double DefaultWindowHeight = 700;

        // Storage keys
        public const string AuthTokenKey = "auth_token";
        public const string UserIdKey = "user_id";
        public const string RememberMeKey = "remember_me";
    }
}