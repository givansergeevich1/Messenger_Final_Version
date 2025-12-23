using System;
using System.Collections.Generic;

namespace Messenger.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; } = false;
        public List<string> ChatIds { get; set; } = new List<string>();

        public User() { }

        public User(string id, string email, string username)
        {
            Id = id;
            Email = email;
            Username = username;
            DisplayName = username;
            CreatedAt = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "email", Email },
                { "username", Username },
                { "displayName", DisplayName },
                { "photoUrl", PhotoUrl ?? "" },
                { "createdAt", CreatedAt.ToString("o") },
                { "lastSeen", LastSeen.ToString("o") },
                { "isOnline", IsOnline },
                { "chatIds", ChatIds ?? new List<string>() }
            };
        }

        public static User FromDictionary(Dictionary<string, object> dict)
        {
            var user = new User
            {
                Id = dict.GetValueOrDefault("id")?.ToString() ?? string.Empty,
                Email = dict.GetValueOrDefault("email")?.ToString() ?? string.Empty,
                Username = dict.GetValueOrDefault("username")?.ToString() ?? string.Empty,
                DisplayName = dict.GetValueOrDefault("displayName")?.ToString() ?? string.Empty,
                PhotoUrl = dict.GetValueOrDefault("photoUrl")?.ToString() ?? string.Empty,
                IsOnline = bool.Parse(dict.GetValueOrDefault("isOnline")?.ToString() ?? "false")
            };

            if (DateTime.TryParse(dict.GetValueOrDefault("createdAt")?.ToString(), out var createdAt))
                user.CreatedAt = createdAt;

            if (DateTime.TryParse(dict.GetValueOrDefault("lastSeen")?.ToString(), out var lastSeen))
                user.LastSeen = lastSeen;

            if (dict.TryGetValue("chatIds", out var chatIdsObj) && chatIdsObj is List<object> chatIdsList)
            {
                user.ChatIds = chatIdsList.ConvertAll(id => id.ToString() ?? string.Empty);
            }

            return user;
        }

        public override bool Equals(object obj)
        {
            return obj is User user && Id == user.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}