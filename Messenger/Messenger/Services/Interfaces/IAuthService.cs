using System;
using System.Threading.Tasks;
using Messenger.Models;

namespace Messenger.Services.Interfaces
{
    public interface IAuthService
    {
        // Текущий авторизованный пользователь
        User? CurrentUser { get; }

        // Событие изменения состояния аутентификации
        event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        // Основные методы аутентификации
        Task<User?> LoginAsync(string email, string password);
        Task<User?> RegisterAsync(string email, string username, string password);
        Task<bool> LogoutAsync();
        Task<bool> ResetPasswordAsync(string email);

        // Вспомогательные методы
        Task<bool> IsAuthenticatedAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> UpdateUserProfileAsync(User user);
    }

    public class AuthStateChangedEventArgs : EventArgs
    {
        public User? User { get; }
        public bool IsAuthenticated { get; }

        public AuthStateChangedEventArgs(User? user)
        {
            User = user;
            IsAuthenticated = user != null;
        }
    }
}