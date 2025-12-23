using System;

namespace Messenger.Models
{
    public class Message
    {
        public string Id { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public string ChatId { get; set; } = string.Empty;

        public Message() { }

        public Message(string senderId, string senderName, string content, string chatId)
        {
            SenderId = senderId;
            SenderName = senderName;
            Content = content;
            ChatId = chatId;
            Timestamp = DateTime.UtcNow;
        }

        public string FormattedTime => Timestamp.ToLocalTime().ToString("HH:mm");
        public string FormattedDate => Timestamp.ToLocalTime().ToString("dd.MM.yyyy");
    }
}