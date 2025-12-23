using System;
using System.Collections.Generic;

namespace Messenger.Models
{
    public class ChatRoom
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> ParticipantIds { get; set; } = new List<string>();
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; } = DateTime.UtcNow;
        public bool IsGroupChat { get; set; } = false;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string LastMessageSender { get; set; } = string.Empty;

        public ChatRoom() { }

        public ChatRoom(string name, string createdBy, bool isGroupChat = false)
        {
            Name = name;
            CreatedBy = createdBy;
            IsGroupChat = isGroupChat;
            CreatedAt = DateTime.UtcNow;
            LastMessageTime = DateTime.UtcNow;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "name", Name },
                { "participantIds", ParticipantIds },
                { "lastMessage", LastMessage ?? "" },
                { "lastMessageTime", LastMessageTime.ToString("o") },
                { "lastMessageSender", LastMessageSender ?? "" },
                { "isGroupChat", IsGroupChat },
                { "createdBy", CreatedBy },
                { "createdAt", CreatedAt.ToString("o") }
            };
        }

        public static ChatRoom FromDictionary(Dictionary<string, object> dict)
        {
            var chat = new ChatRoom
            {
                Id = dict.GetValueOrDefault("id")?.ToString() ?? string.Empty,
                Name = dict.GetValueOrDefault("name")?.ToString() ?? string.Empty,
                LastMessage = dict.GetValueOrDefault("lastMessage")?.ToString() ?? string.Empty,
                LastMessageSender = dict.GetValueOrDefault("lastMessageSender")?.ToString() ?? string.Empty,
                IsGroupChat = bool.Parse(dict.GetValueOrDefault("isGroupChat")?.ToString() ?? "false"),
                CreatedBy = dict.GetValueOrDefault("createdBy")?.ToString() ?? string.Empty
            };

            if (DateTime.TryParse(dict.GetValueOrDefault("createdAt")?.ToString(), out var createdAt))
                chat.CreatedAt = createdAt;

            if (DateTime.TryParse(dict.GetValueOrDefault("lastMessageTime")?.ToString(), out var lastMessageTime))
                chat.LastMessageTime = lastMessageTime;

            if (dict.TryGetValue("participantIds", out var participantsObj) && participantsObj is List<object> participantsList)
            {
                chat.ParticipantIds = participantsList.ConvertAll(id => id.ToString() ?? string.Empty);
            }

            return chat;
        }

        public void AddParticipant(string userId)
        {
            if (!ParticipantIds.Contains(userId))
            {
                ParticipantIds.Add(userId);
            }
        }

        public void RemoveParticipant(string userId)
        {
            ParticipantIds.Remove(userId);
        }

        public bool HasParticipant(string userId)
        {
            return ParticipantIds.Contains(userId);
        }
    }
}