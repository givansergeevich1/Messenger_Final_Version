using System;
using System.Collections.Generic;

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
        public string MessageType { get; set; } = "text"; // text, image, file

        public Message() { }

        public Message(string senderId, string senderName, string content, string chatId)
        {
            SenderId = senderId;
            SenderName = senderName;
            Content = content;
            ChatId = chatId;
            Timestamp = DateTime.UtcNow;
            MessageType = "text";
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "senderId", SenderId },
                { "senderName", SenderName },
                { "content", Content },
                { "timestamp", Timestamp.ToString("o") },
                { "isRead", IsRead },
                { "chatId", ChatId },
                { "messageType", MessageType }
            };
        }

        public static Message FromDictionary(Dictionary<string, object> dict)
        {
            var message = new Message
            {
                Id = dict.GetValueOrDefault("id")?.ToString() ?? string.Empty,
                SenderId = dict.GetValueOrDefault("senderId")?.ToString() ?? string.Empty,
                SenderName = dict.GetValueOrDefault("senderName")?.ToString() ?? string.Empty,
                Content = dict.GetValueOrDefault("content")?.ToString() ?? string.Empty,
                IsRead = bool.Parse(dict.GetValueOrDefault("isRead")?.ToString() ?? "false"),
                ChatId = dict.GetValueOrDefault("chatId")?.ToString() ?? string.Empty,
                MessageType = dict.GetValueOrDefault("messageType")?.ToString() ?? "text"
            };

            if (DateTime.TryParse(dict.GetValueOrDefault("timestamp")?.ToString(), out var timestamp))
                message.Timestamp = timestamp;

            return message;
        }

        public string FormattedTime => Timestamp.ToLocalTime().ToString("HH:mm");
        public string FormattedDate => Timestamp.ToLocalTime().ToString("dd.MM.yyyy");
    }
}