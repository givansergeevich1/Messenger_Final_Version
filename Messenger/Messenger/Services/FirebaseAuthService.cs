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

        public async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Email и пароль не могут быть пустыми");
                }

                if (!email.IsValidEmail())
                {
                    throw new ArgumentException("Некорректный формат email");
                }

                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"accounts:signInWithPassword?key={FirebaseConfig.ApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);

                    if (result != null && !string.IsNullOrEmpty(result.LocalId))
                    {
                        _currentUser = new User(result.LocalId, email, email.Split('@')[0])
                        {
                            DisplayName = email.Split('@')[0]
                        };

                        // Сохраняем токен
                        await SecureStorage.SetAsync("auth_token", result.IdToken);
                        await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
                        await SecureStorage.SetAsync("user_id", result.LocalId);

                        OnAuthStateChanged(_currentUser);
                        return _currentUser;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<FirebaseErrorResponse>(errorContent);
                    throw new Exception(error?.Error?.Message ?? "Ошибка аутентификации");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка входа: {ex.Message}", ex);
            }
        }

        protected virtual void OnAuthStateChanged(User? user)
        {
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(user));
        }

        // Вспомогательные классы для десериализации
        private class FirebaseAuthResponse
        {
            [JsonProperty("kind")]
            public string Kind { get; set; } = string.Empty;

            [JsonProperty("localId")]
            public string LocalId { get; set; } = string.Empty;

            [JsonProperty("email")]
            public string Email { get; set; } = string.Empty;

            [JsonProperty("displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonProperty("idToken")]
            public string IdToken { get; set; } = string.Empty;

            [JsonProperty("registered")]
            public bool Registered { get; set; }

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonProperty("expiresIn")]
            public string ExpiresIn { get; set; } = string.Empty;
        }

        private class FirebaseErrorResponse
        {
            [JsonProperty("error")]
            public FirebaseError? Error { get; set; }
        }

        private class FirebaseError
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("errors")]
            public List<ErrorDetail> Errors { get; set; } = new List<ErrorDetail>();
        }

        private class ErrorDetail
        {
            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("domain")]
            public string Domain { get; set; } = string.Empty;

            [JsonProperty("reason")]
            public string Reason { get; set; } = string.Empty;
        }

        // Остальные методы будут реализованы позже
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