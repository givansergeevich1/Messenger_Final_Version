using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;

namespace Messenger.Services
{
    public class FirebaseAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        public FirebaseAuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/");
        }

        protected virtual void OnAuthStateChanged(User? user)
        {
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(user));
        }

        // Методы будут реализованы в следующих коммитах
        public Task<User?> LoginAsync(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<User?> RegisterAsync(string email, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogoutAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetPasswordAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetCurrentUserAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUserProfileAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}