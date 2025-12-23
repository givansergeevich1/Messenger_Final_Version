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

        public ChatRoom() { }

        public ChatRoom(string name, string createdBy, bool isGroupChat = false)
        {
            Name = name;
            CreatedBy = createdBy;
            IsGroupChat = isGroupChat;
            CreatedAt = DateTime.UtcNow;
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