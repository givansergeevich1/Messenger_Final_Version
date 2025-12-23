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

        // Методы для работы с чатами
        public async Task<ChatRoom?> GetChatAsync(string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chatId);
                var chatData = await _firebaseClient
                    .Child(chatPath)
                    .OnceSingleAsync<Dictionary<string, object>>();

                if (chatData == null)
                    return null;

                chatData["id"] = chatId;
                return ChatRoom.FromDictionary(chatData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting chat {chatId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ChatRoom>> GetUserChatsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                // Сначала получаем все чаты
                var chatsPath = GetFirebasePath(FirebaseConfig.ChatsPath);
                var allChatsData = await _firebaseClient
                    .Child(chatsPath)
                    .OnceAsync<Dictionary<string, object>>();

                var userChats = new List<ChatRoom>();

                foreach (var chatSnapshot in allChatsData)
                {
                    try
                    {
                        var chatData = chatSnapshot.Object;

                        // Проверяем, является ли пользователь участником чата
                        if (chatData.TryGetValue("participantIds", out var participantsObj))
                        {
                            if (participantsObj is List<object> participantsList)
                            {
                                var participantIds = participantsList.ConvertAll(p => p.ToString() ?? string.Empty);

                                if (participantIds.Contains(userId))
                                {
                                    chatData["id"] = chatSnapshot.Key;
                                    var chat = ChatRoom.FromDictionary(chatData);
                                    userChats.Add(chat);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing chat {chatSnapshot.Key}: {ex.Message}");
                    }
                }

                // Сортируем по времени последнего сообщения (сначала новые)
                return userChats
                    .OrderByDescending(c => c.LastMessageTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user chats for {userId}: {ex.Message}");
                return new List<ChatRoom>();
            }
        }

        public async Task<ChatRoom> CreateChatAsync(string chatName, string createdBy, List<string> participantIds, bool isGroupChat = false)
        {
            try
            {
                if (string.IsNullOrEmpty(chatName))
                    throw new ArgumentException("Chat name cannot be null or empty");

                if (string.IsNullOrEmpty(createdBy))
                    throw new ArgumentException("CreatedBy cannot be null or empty");

                if (participantIds == null || participantIds.Count == 0)
                    throw new ArgumentException("ParticipantIds cannot be null or empty");

                // Проверяем, что создатель есть в списке участников
                if (!participantIds.Contains(createdBy))
                {
                    participantIds.Add(createdBy);
                }

                // Создаем новый чат
                var newChat = new ChatRoom(chatName, createdBy, isGroupChat)
                {
                    Id = Guid.NewGuid().ToString(),
                    ParticipantIds = participantIds.Distinct().ToList(),
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTime = DateTime.UtcNow
                };

                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, newChat.Id);
                var chatDict = newChat.ToDictionary();

                await _firebaseClient
                    .Child(chatPath)
                    .PutAsync(chatDict);

                // Обновляем список чатов для каждого участника
                foreach (var participantId in participantIds)
                {
                    await UpdateUserChatListAsync(participantId, newChat.Id, true);
                }

                Console.WriteLine($"Chat {newChat.Id} created successfully with {participantIds.Count} participants");
                return newChat;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating chat: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateChatAsync(ChatRoom chat)
        {
            try
            {
                if (chat == null)
                    throw new ArgumentNullException(nameof(chat));

                if (string.IsNullOrEmpty(chat.Id))
                    throw new ArgumentException("Chat Id cannot be null or empty");

                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chat.Id);
                var chatDict = chat.ToDictionary();

                await _firebaseClient
                    .Child(chatPath)
                    .PutAsync(chatDict);

                Console.WriteLine($"Chat {chat.Id} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating chat {chat?.Id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteChatAsync(string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                // Сначала получаем информацию о чате
                var chat = await GetChatAsync(chatId);
                if (chat == null)
                {
                    Console.WriteLine($"Chat {chatId} not found");
                    return false;
                }

                // Удаляем чат
                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chatId);
                await _firebaseClient
                    .Child(chatPath)
                    .DeleteAsync();

                // Удаляем сообщения чата
                var messagesPath = GetFirebasePath(FirebaseConfig.MessagesPath, chatId);
                await _firebaseClient
                    .Child(messagesPath)
                    .DeleteAsync();

                // Обновляем список чатов для каждого участника
                foreach (var participantId in chat.ParticipantIds)
                {
                    await UpdateUserChatListAsync(participantId, chatId, false);
                }

                Console.WriteLine($"Chat {chatId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting chat {chatId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddUserToChatAsync(string chatId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                // Получаем текущий чат
                var chat = await GetChatAsync(chatId);
                if (chat == null)
                {
                    Console.WriteLine($"Chat {chatId} not found");
                    return false;
                }

                // Проверяем, не добавлен ли уже пользователь
                if (chat.ParticipantIds.Contains(userId))
                {
                    Console.WriteLine($"User {userId} is already in chat {chatId}");
                    return true;
                }

                // Добавляем пользователя
                chat.ParticipantIds.Add(userId);

                // Обновляем чат в базе данных
                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chatId);
                var updates = new Dictionary<string, object>
                {
                    { "participantIds", chat.ParticipantIds }
                };

                await _firebaseClient
                    .Child(chatPath)
                    .PatchAsync(updates);

                // Обновляем список чатов пользователя
                await UpdateUserChatListAsync(userId, chatId, true);

                Console.WriteLine($"User {userId} added to chat {chatId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user {userId} to chat {chatId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveUserFromChatAsync(string chatId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                // Получаем текущий чат
                var chat = await GetChatAsync(chatId);
                if (chat == null)
                {
                    Console.WriteLine($"Chat {chatId} not found");
                    return false;
                }

                // Проверяем, есть ли пользователь в чате
                if (!chat.ParticipantIds.Contains(userId))
                {
                    Console.WriteLine($"User {userId} is not in chat {chatId}");
                    return true;
                }

                // Удаляем пользователя
                chat.ParticipantIds.Remove(userId);

                // Если в чате не осталось участников, удаляем его
                if (chat.ParticipantIds.Count == 0)
                {
                    return await DeleteChatAsync(chatId);
                }

                // Обновляем чат в базе данных
                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chatId);
                var updates = new Dictionary<string, object>
                {
                    { "participantIds", chat.ParticipantIds }
                };

                await _firebaseClient
                    .Child(chatPath)
                    .PatchAsync(updates);

                // Обновляем список чатов пользователя
                await UpdateUserChatListAsync(userId, chatId, false);

                Console.WriteLine($"User {userId} removed from chat {chatId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing user {userId} from chat {chatId}: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateUserChatListAsync(string userId, string chatId, bool addChat)
        {
            try
            {
                var userPath = GetFirebasePath(FirebaseConfig.UsersPath, userId);

                // Получаем текущего пользователя
                var userData = await _firebaseClient
                    .Child(userPath)
                    .OnceSingleAsync<Dictionary<string, object>>();

                if (userData == null)
                    return;

                // Получаем текущий список чатов пользователя
                List<string> chatIds = new List<string>();
                if (userData.TryGetValue("chatIds", out var chatIdsObj))
                {
                    if (chatIdsObj is List<object> chatIdsList)
                    {
                        chatIds = chatIdsList.ConvertAll(id => id.ToString() ?? string.Empty);
                    }
                }

                // Обновляем список
                if (addChat)
                {
                    if (!chatIds.Contains(chatId))
                    {
                        chatIds.Add(chatId);
                    }
                }
                else
                {
                    chatIds.Remove(chatId);
                }

                // Обновляем пользователя в базе данных
                var updates = new Dictionary<string, object>
                {
                    { "chatIds", chatIds }
                };

                await _firebaseClient
                    .Child(userPath)
                    .PatchAsync(updates);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating chat list for user {userId}: {ex.Message}");
            }
        }

        // Методы для работы с сообщениями (будут реализованы в следующих коммитах)
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

        // Realtime listeners (будут реализованы в следующих коммитах)
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

        // Вспомогательные методы (будут реализованы в следующих коммитах)
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