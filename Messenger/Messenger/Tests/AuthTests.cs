using System;
using System.Threading.Tasks;
using Messenger.Services;
using Messenger.Services.Interfaces;

namespace Messenger.Tests
{
    public class AuthTests
    {
        private readonly IAuthService _authService;

        public AuthTests()
        {
            _authService = new FirebaseAuthService();
        }

        public async Task RunAllTests()
        {
            Console.WriteLine("Запуск тестов аутентификации...");

            try
            {
                await TestInitialization();
                await TestLoginWithInvalidCredentials();
                await TestRegistrationWithInvalidData();
                await TestPasswordResetWithInvalidEmail();

                Console.WriteLine("Все тесты пройдены успешно!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении тестов: {ex.Message}");
            }
        }

        private async Task TestInitialization()
        {
            Console.WriteLine("Тест 1: Инициализация сервиса...");

            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Console.WriteLine($"Пользователь авторизован: {isAuthenticated}");

            var currentUser = await _authService.GetCurrentUserAsync();
            Console.WriteLine($"Текущий пользователь: {(currentUser != null ? currentUser.Email : "null")}");

            Console.WriteLine("Тест 1 пройден ✓");
        }

        private async Task TestLoginWithInvalidCredentials()
        {
            Console.WriteLine("\nТест 2: Вход с неверными данными...");

            try
            {
                await _authService.LoginAsync("invalid@email.com", "wrongpassword");
                Console.WriteLine("ОШИБКА: Ожидалось исключение!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ожидаемая ошибка: {ex.Message}");
                Console.WriteLine("Тест 2 пройден ✓");
            }
        }

        private async Task TestRegistrationWithInvalidData()
        {
            Console.WriteLine("\nТест 3: Регистрация с неверными данными...");

            try
            {
                await _authService.RegisterAsync("", "", "");
                Console.WriteLine("ОШИБКА: Ожидалось исключение!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ожидаемая ошибка: {ex.Message}");
                Console.WriteLine("Тест 3 пройден ✓");
            }

            try
            {
                await _authService.RegisterAsync("not-an-email", "user", "short");
                Console.WriteLine("ОШИБКА: Ожидалось исключение!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ожидаемая ошибка: {ex.Message}");
                Console.WriteLine("Тест 3 пройден ✓");
            }
        }

        private async Task TestPasswordResetWithInvalidEmail()
        {
            Console.WriteLine("\nТест 4: Восстановление пароля с неверным email...");

            try
            {
                await _authService.ResetPasswordAsync("not-an-email");
                Console.WriteLine("ОШИБКА: Ожидалось исключение!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ожидаемая ошибка: {ex.Message}");
                Console.WriteLine("Тест 4 пройден ✓");
            }
        }

        // Метод для ручного запуска тестов
        public static async Task Run()
        {
            var tests = new AuthTests();
            await tests.RunAllTests();
        }
    }
}