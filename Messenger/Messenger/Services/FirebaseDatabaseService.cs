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

        // Методы для работы с сообщениями
        public async Task<Message?> GetMessageAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentException("MessageId cannot be null or empty");

                // Поиск сообщения по всем чатам
                var chatsPath = GetFirebasePath(FirebaseConfig.ChatsPath);
                var chatsData = await _firebaseClient
                    .Child(chatsPath)
                    .OnceAsync<Dictionary<string, object>>();

                foreach (var chatSnapshot in chatsData)
                {
                    var chatId = chatSnapshot.Key;
                    var messagesPath = GetFirebasePath(FirebaseConfig.MessagesPath, chatId, messageId);

                    try
                    {
                        var messageData = await _firebaseClient
                            .Child(messagesPath)
                            .OnceSingleAsync<Dictionary<string, object>>();

                        if (messageData != null)
                        {
                            messageData["id"] = messageId;
                            messageData["chatId"] = chatId;
                            return Message.FromDictionary(messageData);
                        }
                    }
                    catch
                    {
                        // Продолжаем поиск в следующем чате
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting message {messageId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Message>> GetChatMessagesAsync(string chatId, int limit = 50)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                var messagesPath = GetFirebasePath(FirebaseConfig.MessagesPath, chatId);
                var messagesData = await _firebaseClient
                    .Child(messagesPath)
                    .OrderByKey()
                    .LimitToLast(limit)
                    .OnceAsync<Dictionary<string, object>>();

                var messages = new List<Message>();

                foreach (var messageSnapshot in messagesData)
                {
                    try
                    {
                        var messageData = messageSnapshot.Object;
                        messageData["id"] = messageSnapshot.Key;
                        messageData["chatId"] = chatId;

                        var message = Message.FromDictionary(messageData);
                        messages.Add(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing message {messageSnapshot.Key}: {ex.Message}");
                    }
                }

                // Сортируем по времени (сначала старые)
                return messages
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting messages for chat {chatId}: {ex.Message}");
                return new List<Message>();
            }
        }

        // Улучшенный метод SendMessageAsync с дополнительной проверкой
        public async Task<Message> SendMessageAsync(string chatId, string senderId, string senderName, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                if (string.IsNullOrEmpty(senderId))
                    throw new ArgumentException("SenderId cannot be null or empty");

                if (string.IsNullOrEmpty(senderName))
                    throw new ArgumentException("SenderName cannot be null or empty");

                if (string.IsNullOrEmpty(content))
                    throw new ArgumentException("Content cannot be null or empty");

                // Проверяем существование чата
                var chat = await GetChatAsync(chatId);
                if (chat == null)
                {
                    throw new Exception($"Chat {chatId} not found");
                }

                // Проверяем, является ли отправитель участником чата
                if (!chat.ParticipantIds.Contains(senderId))
                {
                    throw new Exception($"User {senderId} is not a participant of chat {chatId}");
                }

                // Ограничиваем длину сообщения
                if (content.Length > AppConstants.MaxMessageLength)
                {
                    content = content.Truncate(AppConstants.MaxMessageLength);
                }

                // Генерируем уникальный ID для сообщения
                var messageId = GenerateMessageId();
                var timestamp = DateTime.UtcNow;

                // Создаем новое сообщение
                var newMessage = new Message(senderId, senderName, content, chatId)
                {
                    Id = messageId,
                    Timestamp = timestamp,
                    IsRead = false,
                    MessageType = "text"
                };

                // Сохраняем сообщение в базе данных
                var messagePath = GetFirebasePath(FirebaseConfig.MessagesPath, chatId, messageId);
                var messageDict = newMessage.ToDictionary();

                await _firebaseClient
                    .Child(messagePath)
                    .PutAsync(messageDict);

                // Обновляем информацию о последнем сообщении в чате
                await UpdateChatLastMessageAsync(chatId, content, senderName, timestamp);

                // Отмечаем сообщение как отправленное
                Console.WriteLine($"Message {messageId} sent to chat {chatId} by {senderName}");

                return newMessage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to chat {chatId}: {ex.Message}");
                throw new Exception($"Failed to send message: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateMessageAsync(Message message)
        {
            try
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                if (string.IsNullOrEmpty(message.Id))
                    throw new ArgumentException("Message Id cannot be null or empty");

                if (string.IsNullOrEmpty(message.ChatId))
                    throw new ArgumentException("Message ChatId cannot be null or empty");

                var messagePath = GetFirebasePath(FirebaseConfig.MessagesPath, message.ChatId, message.Id);
                var messageDict = message.ToDictionary();

                await _firebaseClient
                    .Child(messagePath)
                    .PutAsync(messageDict);

                Console.WriteLine($"Message {message.Id} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating message {message?.Id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentException("MessageId cannot be null or empty");

                var message = await GetMessageAsync(messageId);
                if (message == null)
                {
                    Console.WriteLine($"Message {messageId} not found");
                    return false;
                }

                var messagePath = GetFirebasePath(FirebaseConfig.MessagesPath, message.ChatId, messageId);

                await _firebaseClient
                    .Child(messagePath)
                    .DeleteAsync();

                Console.WriteLine($"Message {messageId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message {messageId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentException("MessageId cannot be null or empty");

                var message = await GetMessageAsync(messageId);
                if (message == null)
                {
                    Console.WriteLine($"Message {messageId} not found");
                    return false;
                }

                var messagePath = GetFirebasePath(FirebaseConfig.MessagesPath, message.ChatId, messageId);

                var updates = new Dictionary<string, object>
                {
                    { "isRead", true }
                };

                await _firebaseClient
                    .Child(messagePath)
                    .PatchAsync(updates);

                Console.WriteLine($"Message {messageId} marked as read");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking message {messageId} as read: {ex.Message}");
                return false;
            }
        }

        // Realtime listeners
        public async Task SubscribeToChatMessages(string chatId, Action<Message> onMessageAdded)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                if (onMessageAdded == null)
                    throw new ArgumentNullException(nameof(onMessageAdded));

                // Отписываемся от существующей подписки, если есть
                await UnsubscribeFromChatMessages(chatId);

                var messagesPath = GetFirebasePath(FirebaseConfig.MessagesPath, chatId);

                var subscription = _firebaseClient
                    .Child(messagesPath)
                    .AsObservable<Dictionary<string, object>>(elementRoot: null)
                    .Subscribe(
                        onNext: snapshot =>
                        {
                            try
                            {
                                if (snapshot.Object != null && snapshot.Key != null)
                                {
                                    var messageData = snapshot.Object;
                                    messageData["id"] = snapshot.Key;
                                    messageData["chatId"] = chatId;

                                    var message = Message.FromDictionary(messageData);
                                    onMessageAdded?.Invoke(message);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing message update: {ex.Message}");
                            }
                        },
                        onError: ex =>
                        {
                            Console.WriteLine($"Error in chat messages subscription for {chatId}: {ex.Message}");
                            _messageSubscriptions.Remove(chatId);
                        }
                    );

                _messageSubscriptions[chatId] = subscription;
                Console.WriteLine($"Subscribed to chat messages for {chatId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing to chat messages for {chatId}: {ex.Message}");
                throw;
            }
        }

        public async Task SubscribeToUserChats(string userId, Action<ChatRoom> onChatUpdated)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                if (onChatUpdated == null)
                    throw new ArgumentNullException(nameof(onChatUpdated));

                // Отписываемся от существующей подписки, если есть
                await UnsubscribeFromUserChats(userId);

                var chatsPath = GetFirebasePath(FirebaseConfig.ChatsPath);

                var subscription = _firebaseClient
                    .Child(chatsPath)
                    .AsObservable<Dictionary<string, object>>(elementRoot: null)
                    .Subscribe(
                        onNext: snapshot =>
                        {
                            try
                            {
                                if (snapshot.Object != null && snapshot.Key != null)
                                {
                                    var chatData = snapshot.Object;

                                    // Проверяем, является ли пользователь участником чата
                                    if (chatData.TryGetValue("participantIds", out var participantsObj))
                                    {
                                        if (participantsObj is List<object> participantsList)
                                        {
                                            var participantIds = participantsList.ConvertAll(p => p.ToString() ?? string.Empty);

                                            if (participantIds.Contains(userId))
                                            {
                                                chatData["id"] = snapshot.Key;
                                                var chat = ChatRoom.FromDictionary(chatData);
                                                onChatUpdated?.Invoke(chat);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing chat update: {ex.Message}");
                            }
                        },
                        onError: ex =>
                        {
                            Console.WriteLine($"Error in user chats subscription for {userId}: {ex.Message}");
                            _chatSubscriptions.Remove(userId);
                        }
                    );

                _chatSubscriptions[userId] = subscription;
                Console.WriteLine($"Subscribed to user chats for {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing to user chats for {userId}: {ex.Message}");
                throw;
            }
        }

        public Task UnsubscribeFromChatMessages(string chatId)
        {
            try
            {
                if (_messageSubscriptions.TryGetValue(chatId, out var subscription))
                {
                    subscription?.Dispose();
                    _messageSubscriptions.Remove(chatId);
                    Console.WriteLine($"Unsubscribed from chat messages for {chatId}");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unsubscribing from chat messages for {chatId}: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        public Task UnsubscribeFromUserChats(string userId)
        {
            try
            {
                if (_chatSubscriptions.TryGetValue(userId, out var subscription))
                {
                    subscription?.Dispose();
                    _chatSubscriptions.Remove(userId);
                    Console.WriteLine($"Unsubscribed from user chats for {userId}");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unsubscribing from user chats for {userId}: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        private string GenerateMessageId()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        // Вспомогательные методы поиска
        public async Task<List<User>> SearchUsersAsync(string searchQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                    return new List<User>();

                var allUsers = await GetUsersAsync();
                var query = searchQuery.ToLowerInvariant().Trim();

                return allUsers
                    .Where(user =>
                        (!string.IsNullOrEmpty(user.Username) && user.Username.ToLowerInvariant().Contains(query)) ||
                        (!string.IsNullOrEmpty(user.DisplayName) && user.DisplayName.ToLowerInvariant().Contains(query)) ||
                        (!string.IsNullOrEmpty(user.Email) && user.Email.ToLowerInvariant().Contains(query)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<List<Message>> SearchMessagesAsync(string chatId, string searchQuery)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    throw new ArgumentException("ChatId cannot be null or empty");

                if (string.IsNullOrWhiteSpace(searchQuery))
                    return new List<Message>();

                var allMessages = await GetChatMessagesAsync(chatId, 1000); // Получаем больше сообщений для поиска
                var query = searchQuery.ToLowerInvariant().Trim();

                return allMessages
                    .Where(message =>
                        (!string.IsNullOrEmpty(message.Content) && message.Content.ToLowerInvariant().Contains(query)) ||
                        (!string.IsNullOrEmpty(message.SenderName) && message.SenderName.ToLowerInvariant().Contains(query)))
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching messages in chat {chatId}: {ex.Message}");
                return new List<Message>();
            }
        }

        public async Task<int> GetUnreadMessagesCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("UserId cannot be null or empty");

                var userChats = await GetUserChatsAsync(userId);
                int totalUnread = 0;

                foreach (var chat in userChats)
                {
                    // В реальном приложении здесь была бы более сложная логика
                    // подсчета непрочитанных сообщений для конкретного пользователя
                    // Для простоты считаем, что все сообщения, кроме отправленных пользователем, непрочитаны
                    var messages = await GetChatMessagesAsync(chat.Id, 100);
                    var unreadInChat = messages.Count(m => m.SenderId != userId && !m.IsRead);
                    totalUnread += unreadInChat;
                }

                return totalUnread;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread messages count for user {userId}: {ex.Message}");
                return 0;
            }
        }

        private async Task UpdateChatLastMessageAsync(string chatId, string lastMessage, string lastMessageSender, DateTime lastMessageTime)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId))
                    return;

                var chatPath = GetFirebasePath(FirebaseConfig.ChatsPath, chatId);

                var updates = new Dictionary<string, object>
                {
                    { "lastMessage", lastMessage },
                    { "lastMessageSender", lastMessageSender },
                    { "lastMessageTime", lastMessageTime.ToString("o") }
                };

                await _firebaseClient
                    .Child(chatPath)
                    .PatchAsync(updates);

                Console.WriteLine($"Updated last message for chat {chatId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating last message for chat {chatId}: {ex.Message}");
            }
        }
    }
}