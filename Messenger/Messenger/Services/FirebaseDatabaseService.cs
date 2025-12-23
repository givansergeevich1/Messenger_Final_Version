using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;
using Newtonsoft.Json;

namespace Messenger.Services
{
    public class FirebaseDatabaseService : IDatabaseService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly Dictionary<string, IDisposable> _messageSubscriptions = new Dictionary<string, IDisposable>();
        private readonly Dictionary<string, IDisposable> _chatSubscriptions = new Dictionary<string, IDisposable>();

        public FirebaseDatabaseService()
        {
            try
            {
                _firebaseClient = new FirebaseClient(
                    FirebaseConfig.DatabaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = async () =>
                        {
                            var token = await SecureStorage.GetAsync("auth_token");
                            return token ?? string.Empty;
                        },
                        AsAccessToken = true
                    }
                );

                Console.WriteLine("FirebaseDatabaseService initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing FirebaseDatabaseService: {ex.Message}");
                throw;
            }
        }

        private string GetFirebasePath(params string[] pathSegments)
        {
            return string.Join("/", pathSegments.Where(s => !string.IsNullOrEmpty(s)));
        }

        // Методы для работы с пользователями
        public async Task<User?> GetUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, userId);
                var userData = await _firebaseClient
                    .Child(userPath)
                    .OnceSingleAsync<Dictionary<string, object>>();

                if (userData == null)
                    return null;

                return User.FromDictionary(userData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                var usersPath = GetFirebasePath(FirebaseConfig.UsersPath);
                var usersData = await _firebaseClient
                    .Child(usersPath)
                    .OnceAsync<Dictionary<string, object>>();

                var users = new List<User>();

                foreach (var userSnapshot in usersData)
                {
                    try
                    {
                        var userData = userSnapshot.Object;
                        userData["id"] = userSnapshot.Key;
                        var user = User.FromDictionary(userData);
                        users.Add(user);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing user {userSnapshot.Key}: {ex.Message}");
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                if (string.IsNullOrEmpty(user.Id))
                    throw new ArgumentException("User Id cannot be null or empty");

                // Проверяем, существует ли уже пользователь с таким ID
                var existingUser = await GetUserAsync(user.Id);
                if (existingUser != null)
                {
                    throw new Exception($"User with id {user.Id} already exists");
                }

                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, user.Id);
                var userDict = user.ToDictionary();

                await _firebaseClient
                    .Child(userPath)
                    .PutAsync(userDict);

                Console.WriteLine($"User {user.Id} created successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user {user?.Id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                if (string.IsNullOrEmpty(user.Id))
                    throw new ArgumentException("User Id cannot be null or empty");

                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, user.Id);
                var userDict = user.ToDictionary();

                await _firebaseClient
                    .Child(userPath)
                    .PutAsync(userDict);

                Console.WriteLine($"User {user.Id} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user {user?.Id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, userId);

                await _firebaseClient
                    .Child(userPath)
                    .DeleteAsync();

                Console.WriteLine($"User {userId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserStatusAsync(string userId, bool isOnline, DateTime lastSeen)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, userId);

                var updates = new Dictionary<string, object>
                {
                    { "isOnline", isOnline },
                    { "lastSeen", lastSeen.ToString("o") }
                };

                await _firebaseClient
                    .Child(userPath)
                    .PatchAsync(updates);

                Console.WriteLine($"User {userId} status updated: Online={isOnline}, LastSeen={lastSeen}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user status {userId}: {ex.Message}");
                return false;
            }
        }

        // Остальные методы будут реализованы позже
        public Task<ChatRoom?> GetChatAsync(string chatId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ChatRoom>> GetUserChatsAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<ChatRoom> CreateChatAsync(string chatName, string createdBy, List<string> participantIds, bool isGroupChat = false)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateChatAsync(ChatRoom chat)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteChatAsync(string chatId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddUserToChatAsync(string chatId, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveUserFromChatAsync(string chatId, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<Message?> GetMessageAsync(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Message>> GetChatMessagesAsync(string chatId, int limit = 50)
        {
            throw new NotImplementedException();
        }

        public Task<Message> SendMessageAsync(string chatId, string senderId, string senderName, string content)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateMessageAsync(Message message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteMessageAsync(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> MarkMessageAsReadAsync(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToChatMessages(string chatId, Action<Message> onMessageAdded)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToUserChats(string userId, Action<ChatRoom> onChatUpdated)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeFromChatMessages(string chatId)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeFromUserChats(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> SearchUsersAsync(string searchQuery)
        {
            throw new NotImplementedException();
        }

        public Task<List<Message>> SearchMessagesAsync(string chatId, string searchQuery)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetUnreadMessagesCountAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}