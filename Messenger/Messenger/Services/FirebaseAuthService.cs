using System;
using System.Collections.Generic;
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
        private bool _initialized = false;
        private readonly object _lock = new object();

        public User? CurrentUser
        {
            get
            {
                if (!_initialized)
                {
                    Task.Run(async () => await InitializeAsync()).Wait();
                }
                return _currentUser;
            }
            private set
            {
                lock (_lock)
                {
                    _currentUser = value;
                }
            }
        }

        public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        public FirebaseAuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Инициализируем асинхронно
            Task.Run(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                // Проверяем, есть ли сохраненный токен
                var authToken = await SecureStorage.GetAsync("auth_token");
                var userId = await SecureStorage.GetAsync("user_id");

                if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(userId))
                {
                    // Валидируем токен
                    var isValid = await ValidateTokenAsync(authToken);

                    if (isValid)
                    {
                        // Получаем информацию о пользователе
                        var userInfo = await GetUserInfoAsync(authToken);

                        if (userInfo != null && userInfo.Users.Count > 0)
                        {
                            var userData = userInfo.Users[0];
                            CurrentUser = new User(
                                userData.LocalId,
                                userData.Email,
                                userData.DisplayName ?? userData.Email.Split('@')[0]
                            )
                            {
                                DisplayName = userData.DisplayName ?? userData.Email.Split('@')[0],
                                PhotoUrl = userData.PhotoUrl ?? string.Empty,
                                IsOnline = true,
                                LastSeen = DateTime.UtcNow
                            };

                            OnAuthStateChanged(CurrentUser);
                        }
                    }
                    else
                    {
                        // Токен невалиден, очищаем данные
                        await ClearLocalAuthData();
                        CurrentUser = null;
                        OnAuthStateChanged(null);
                    }
                }
                else
                {
                    // Нет сохраненных данных
                    CurrentUser = null;
                    OnAuthStateChanged(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации AuthService: {ex.Message}");
                CurrentUser = null;
                OnAuthStateChanged(null);
            }
            finally
            {
                _initialized = true;
            }
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
                        CurrentUser = new User(result.LocalId, email, email.Split('@')[0])
                        {
                            DisplayName = result.DisplayName ?? email.Split('@')[0],
                            IsOnline = true,
                            LastSeen = DateTime.UtcNow
                        };

                        // Сохраняем токен
                        await SecureStorage.SetAsync("auth_token", result.IdToken);
                        await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
                        await SecureStorage.SetAsync("user_id", result.LocalId);

                        OnAuthStateChanged(CurrentUser);
                        return CurrentUser;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<FirebaseErrorResponse>(errorContent);

                    string errorMessage = error?.Error?.Message ?? "Ошибка аутентификации";

                    // Перевод стандартных ошибок Firebase
                    if (errorMessage.Contains("INVALID_LOGIN_CREDENTIALS") ||
                        errorMessage.Contains("EMAIL_NOT_FOUND") ||
                        errorMessage.Contains("INVALID_PASSWORD"))
                    {
                        errorMessage = "Неверный email или пароль";
                    }
                    else if (errorMessage.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                    {
                        errorMessage = "Слишком много попыток. Попробуйте позже";
                    }
                    else if (errorMessage.Contains("USER_DISABLED"))
                    {
                        errorMessage = "Аккаунт отключен администратором";
                    }

                    throw new Exception(errorMessage);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка входа: {ex.Message}", ex);
            }
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            try
            {
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
                        CurrentUser = new User(result.LocalId, email, username)
                        {
                            DisplayName = username,
                            CreatedAt = DateTime.UtcNow,
                            LastSeen = DateTime.UtcNow,
                            IsOnline = true
                        };

                        await SecureStorage.SetAsync("auth_token", result.IdToken);
                        await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
                        await SecureStorage.SetAsync("user_id", result.LocalId);

                        OnAuthStateChanged(CurrentUser);
                        return CurrentUser;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<FirebaseErrorResponse>(errorContent);

                    string errorMessage = error?.Error?.Message ?? "Ошибка регистрации";

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

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var refreshToken = await SecureStorage.GetAsync("refresh_token");

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        var requestData = new
                        {
                            token = refreshToken
                        };

                        var content = new StringContent(
                            JsonConvert.SerializeObject(requestData),
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );

                        await _httpClient.PostAsync(
                            $"https://securetoken.googleapis.com/v1/token?key={FirebaseConfig.ApiKey}",
                            content
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error revoking token: {ex.Message}");
                    }
                }

                await ClearLocalAuthData();
                CurrentUser = null;
                OnAuthStateChanged(null);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выхода: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new ArgumentException("Email не может быть пустым");
                }

                if (!email.IsValidEmail())
                {
                    throw new ArgumentException("Некорректный формат email");
                }

                var requestData = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"accounts:sendOobCode?key={FirebaseConfig.ApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PasswordResetResponse>(responseContent);

                    return result != null && result.Email == email;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<FirebaseErrorResponse>(errorContent);

                    string errorMessage = error?.Error?.Message ?? "Ошибка восстановления пароля";

                    if (errorMessage.Contains("EMAIL_NOT_FOUND"))
                    {
                        errorMessage = "Пользователь с таким email не найден";
                    }
                    else if (errorMessage.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                    {
                        errorMessage = "Слишком много попыток. Попробуйте позже";
                    }

                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка восстановления пароля: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            await InitializeAsync();
            return CurrentUser != null;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            await InitializeAsync();
            return CurrentUser;
        }

        public async Task<bool> UpdateUserProfileAsync(User user)
        {
            try
            {
                var authToken = await SecureStorage.GetAsync("auth_token");

                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("Пользователь не авторизован");

                var requestData = new
                {
                    idToken = authToken,
                    displayName = user.DisplayName,
                    photoUrl = user.PhotoUrl,
                    returnSecureToken = true
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"accounts:update?key={FirebaseConfig.ApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);

                    if (result != null && CurrentUser != null)
                    {
                        CurrentUser.DisplayName = user.DisplayName;
                        CurrentUser.PhotoUrl = user.PhotoUrl;

                        await SecureStorage.SetAsync("auth_token", result.IdToken);

                        OnAuthStateChanged(CurrentUser);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления профиля: {ex.Message}", ex);
            }
        }

        protected virtual void OnAuthStateChanged(User? user)
        {
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(user));
        }

        private async Task ClearLocalAuthData()
        {
            try
            {
                SecureStorage.Remove("auth_token");
                SecureStorage.Remove("refresh_token");
                SecureStorage.Remove("user_id");
                SecureStorage.Remove("remember_me");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing auth data: {ex.Message}");
            }
        }

        private async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var requestData = new
                {
                    idToken = token
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={FirebaseConfig.ApiKey}",
                    content
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<UserInfoResponse?> GetUserInfoAsync(string token)
        {
            try
            {
                var requestData = new
                {
                    idToken = token
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={FirebaseConfig.ApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<UserInfoResponse>(responseContent);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Вспомогательные классы для десериализации
        private class FirebaseAuthResponse
        {
            [JsonProperty("kind")] public string Kind { get; set; } = string.Empty;
            [JsonProperty("localId")] public string LocalId { get; set; } = string.Empty;
            [JsonProperty("email")] public string Email { get; set; } = string.Empty;
            [JsonProperty("displayName")] public string? DisplayName { get; set; }
            [JsonProperty("idToken")] public string IdToken { get; set; } = string.Empty;
            [JsonProperty("registered")] public bool Registered { get; set; }
            [JsonProperty("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
            [JsonProperty("expiresIn")] public string ExpiresIn { get; set; } = string.Empty;
        }

        private class FirebaseErrorResponse
        {
            [JsonProperty("error")] public FirebaseError? Error { get; set; }
        }

        private class FirebaseError
        {
            [JsonProperty("code")] public int Code { get; set; }
            [JsonProperty("message")] public string Message { get; set; } = string.Empty;
            [JsonProperty("errors")] public List<ErrorDetail> Errors { get; set; } = new List<ErrorDetail>();
        }

        private class ErrorDetail
        {
            [JsonProperty("message")] public string Message { get; set; } = string.Empty;
            [JsonProperty("domain")] public string Domain { get; set; } = string.Empty;
            [JsonProperty("reason")] public string Reason { get; set; } = string.Empty;
        }

        private class PasswordResetResponse
        {
            [JsonProperty("kind")] public string Kind { get; set; } = string.Empty;
            [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        }

        private class UserInfoResponse
        {
            [JsonProperty("users")] public List<UserInfo> Users { get; set; } = new List<UserInfo>();
        }

        private class UserInfo
        {
            [JsonProperty("localId")] public string LocalId { get; set; } = string.Empty;
            [JsonProperty("email")] public string Email { get; set; } = string.Empty;
            [JsonProperty("displayName")] public string? DisplayName { get; set; }
            [JsonProperty("photoUrl")] public string? PhotoUrl { get; set; }
        }
    }
}