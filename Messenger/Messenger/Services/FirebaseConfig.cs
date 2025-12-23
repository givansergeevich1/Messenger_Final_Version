namespace Messenger.Services
{
    public static class FirebaseConfig
    {
        // Firebase configuration - будет загружена из appsettings.json позже
        public const string ApiKey = "YOUR_FIREBASE_API_KEY";
        public const string AuthDomain = "YOUR_FIREBASE_AUTH_DOMAIN";
        public const string DatabaseUrl = "YOUR_FIREBASE_DATABASE_URL";
        public const string ProjectId = "YOUR_FIREBASE_PROJECT_ID";
        public const string StorageBucket = "YOUR_FIREBASE_STORAGE_BUCKET";
        public const string MessagingSenderId = "YOUR_FIREBASE_MESSAGING_SENDER_ID";
        public const string AppId = "YOUR_FIREBASE_APP_ID";

        // Database paths
        public const string UsersPath = "users";
        public const string ChatsPath = "chats";
        public const string MessagesPath = "messages";
        public const string UserChatsPath = "userChats";

        // Authentication endpoints
        public const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
        public const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";
        public const string ResetPasswordUrl = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode";
    }
}