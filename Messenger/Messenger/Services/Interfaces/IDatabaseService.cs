using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Messenger.Models;

namespace Messenger.Services.Interfaces
{
    public interface IDatabaseService
    {
        // Методы для работы с пользователями
        Task<User?> GetUserAsync(string userId);
        Task<List<User>> GetUsersAsync();
        Task<bool> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateUserStatusAsync(string userId, bool isOnline, DateTime lastSeen);

        // Методы для работы с чатами
        Task<ChatRoom?> GetChatAsync(string chatId);
        Task<List<ChatRoom>> GetUserChatsAsync(string userId);
        Task<ChatRoom> CreateChatAsync(string chatName, string createdBy, List<string> participantIds, bool isGroupChat = false);
        Task<bool> UpdateChatAsync(ChatRoom chat);
        Task<bool> DeleteChatAsync(string chatId);
        Task<bool> AddUserToChatAsync(string chatId, string userId);
        Task<bool> RemoveUserFromChatAsync(string chatId, string userId);

        // Методы для работы с сообщениями
        Task<Message?> GetMessageAsync(string messageId);
        Task<List<Message>> GetChatMessagesAsync(string chatId, int limit = 50);
        Task<Message> SendMessageAsync(string chatId, string senderId, string senderName, string content);
        Task<bool> UpdateMessageAsync(Message message);
        Task<bool> DeleteMessageAsync(string messageId);
        Task<bool> MarkMessageAsReadAsync(string messageId);

        // Realtime listeners
        Task SubscribeToChatMessages(string chatId, Action<Message> onMessageAdded);
        Task SubscribeToUserChats(string userId, Action<ChatRoom> onChatUpdated);
        Task UnsubscribeFromChatMessages(string chatId);
        Task UnsubscribeFromUserChats(string userId);

        // Вспомогательные методы
        Task<List<User>> SearchUsersAsync(string searchQuery);
        Task<List<Message>> SearchMessagesAsync(string chatId, string searchQuery);
        Task<int> GetUnreadMessagesCountAsync(string userId);
    }
}