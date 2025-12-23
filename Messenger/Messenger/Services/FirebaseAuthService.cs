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
            // Реализация из предыдущего коммита...
            throw new NotImplementedException();
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            try
            {
                // Валидация входных данных
                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Все поля обязательны для заполнения");
                }

                if (!email.IsValidEmail())
                {
                    throw new ArgumentException("Некорректный формат email");
                }

                if (!username.IsValidUsername())
                {
                    throw new ArgumentException($"Имя пользователя должно содержать от {AppConstants.MinUsernameLength} до {AppConstants.MaxUsernameLength} символов (латинские буквы, цифры и подчеркивание)");
                }

                if (password.Length < AppConstants.MinPasswordLength)
                {
                    throw new ArgumentException($"Пароль должен содержать не менее {AppConstants.MinPasswordLength} символов");
                }

                // Подготовка данных для регистрации
                var requestData = new
                {
                    email = email,
                    password = password,
                    displayName = username,
                    returnSecureToken = true
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // Отправка запроса на регистрацию
                var response = await _httpClient.PostAsync(
                    $"accounts:signUp?key={FirebaseConfig.ApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);

                    if (result != null && !string.IsNullOrEmpty(result.LocalId))
                    {
                        // Создаем объект пользователя
                        _currentUser = new User(result.LocalId, email, username)
                        {
                            DisplayName = username,
                            CreatedAt = DateTime.UtcNow,
                            LastSeen = DateTime.UtcNow,
                            IsOnline = true
                        };

                        // Сохраняем токены
                        await SecureStorage.SetAsync("auth_token", result.IdToken);
                        await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
                        await SecureStorage.SetAsync("user_id", result.LocalId);

                        // Уведомляем об изменении состояния аутентификации
                        OnAuthStateChanged(_currentUser);

                        // Здесь будет вызов для создания записи пользователя в базе данных
                        // (будет добавлен позже при реализации DatabaseService)

                        return _currentUser;
                    }
                }
                else
                {
                    // Обработка ошибок Firebase
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<FirebaseErrorResponse>(errorContent);

                    string errorMessage = error?.Error?.Message ?? "Ошибка регистрации";

                    // Перевод стандартных ошибок Firebase
                    if (errorMessage.Contains("EMAIL_EXISTS"))
                    {
                        errorMessage = "Пользователь с таким email уже существует";
                    }
                    else if (errorMessage.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                    {
                        errorMessage = "Слишком много попыток. Попробуйте позже";
                    }
                    else if (errorMessage.Contains("WEAK_PASSWORD"))
                    {
                        errorMessage = "Пароль слишком слабый";
                    }

                    throw new Exception(errorMessage);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации: {ex.Message}", ex);
            }
        }

        protected virtual void OnAuthStateChanged(User? user)
        {
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(user));
        }

        // Вспомогательные классы остаются теми же...

        // Остальные методы будут реализованы позже
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