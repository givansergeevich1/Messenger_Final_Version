using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Messenger.Utils
{
    public static class ErrorHandler
    {
        public static void HandleException(Exception ex, string context = null)
        {
            try
            {
                // Логирование ошибки
                LogException(ex, context);

                // Определяем тип ошибки для пользовательского сообщения
                string userMessage = GetUserFriendlyMessage(ex);

                // Показываем сообщение об ошибке (в UI потоке)
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    MessageBox.Show(
                        $"{userMessage}\n\nДетали: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                });
            }
            catch (Exception handlerEx)
            {
                // Если обработчик ошибок сам сломался
                Debug.WriteLine($"Error in error handler: {handlerEx.Message}");
                Debug.WriteLine($"Original error: {ex.Message}");
            }
        }

        public static void LogException(Exception ex, string context = null)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";

                if (!string.IsNullOrEmpty(context))
                {
                    logMessage += $" [{context}]";
                }

                logMessage += $" {ex.GetType().Name}: {ex.Message}\n";
                logMessage += $"Stack Trace:\n{ex.StackTrace}\n";

                if (ex.InnerException != null)
                {
                    logMessage += $"Inner Exception: {ex.InnerException.Message}\n";
                    logMessage += $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
                }

                logMessage += new string('-', 80) + "\n";

                // Записываем в отладочный вывод
                Debug.WriteLine(logMessage);

                // Также записываем в файл (если нужно)
                WriteToLogFile(logMessage);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"Failed to log exception: {logEx.Message}");
            }
        }

        public static string GetUserFriendlyMessage(Exception ex)
        {
            if (ex == null)
                return "Произошла неизвестная ошибка";

            string message = ex.Message.ToLower();

            // Обработка сетевых ошибок
            if (message.Contains("network") || message.Contains("connection") ||
                message.Contains("timeout") || message.Contains("unreachable"))
            {
                return "Ошибка сети. Проверьте подключение к интернету.";
            }

            // Обработка ошибок аутентификации
            if (message.Contains("auth") || message.Contains("login") ||
                message.Contains("password") || message.Contains("invalid"))
            {
                if (message.Contains("invalid credentials") || message.Contains("неверный"))
                {
                    return "Неверный email или пароль.";
                }
                if (message.Contains("user not found") || message.Contains("пользователь не найден"))
                {
                    return "Пользователь не найден.";
                }
                if (message.Contains("email exists") || message.Contains("email уже существует"))
                {
                    return "Пользователь с таким email уже существует.";
                }
                return "Ошибка аутентификации. Проверьте введенные данные.";
            }

            // Обработка ошибок базы данных
            if (message.Contains("database") || message.Contains("firebase") ||
                message.Contains("permission") || message.Contains("доступ"))
            {
                if (message.Contains("permission denied") || message.Contains("доступ запрещен"))
                {
                    return "Доступ запрещен. Убедитесь, что вы авторизованы.";
                }
                return "Ошибка базы данных. Попробуйте позже.";
            }

            // Общие ошибки
            if (message.Contains("not implemented") || message.Contains("не реализовано"))
            {
                return "Функция пока не реализована.";
            }

            if (message.Contains("null") || message.Contains("пусто"))
            {
                return "Обязательные поля не заполнены.";
            }

            if (message.Contains("argument") || message.Contains("аргумент"))
            {
                return "Некорректные входные данные.";
            }

            // Дефолтное сообщение
            return "Произошла ошибка. Пожалуйста, попробуйте еще раз.";
        }

        public static void ShowInfoMessage(string message, string title = "Информация")
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public static void ShowWarningMessage(string message, string title = "Предупреждение")
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public static bool ShowConfirmationMessage(string message, string title = "Подтверждение")
        {
            bool result = false;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            });

            return result;
        }

        private static void WriteToLogFile(string logMessage)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "Messenger", "Logs");

                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                var logFile = Path.Combine(appFolder, $"error_{DateTime.Now:yyyy-MM-dd}.log");

                File.AppendAllText(logFile, logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}