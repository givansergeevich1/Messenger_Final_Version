using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Messenger.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private string _title = string.Empty;
        private string _statusMessage = string.Empty;
        private Dictionary<string, object> _propertyBackingStore = new Dictionary<string, object>();

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Расширенный метод SetProperty с поддержкой валидации
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action? onChanged = null,
            Func<T, T, bool>? validateValue = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            if (validateValue != null && !validateValue(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        // Метод для безопасного получения значения из словаря свойств
        protected T GetProperty<T>([CallerMemberName] string propertyName = "", T defaultValue = default!)
        {
            if (_propertyBackingStore.TryGetValue(propertyName, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        // Метод для безопасной установки значения в словарь свойств
        protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = "",
            Action? onChanged = null, Func<T, T, bool>? validateValue = null)
        {
            var oldValue = GetProperty<T>(propertyName);

            if (EqualityComparer<T>.Default.Equals(oldValue, value))
                return false;

            if (validateValue != null && !validateValue(oldValue, value))
                return false;

            _propertyBackingStore[propertyName] = value!;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void ExecuteWithBusyState(Action action, string? busyMessage = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = busyMessage ?? "Выполнение...";
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.HandleException(ex, "ExecuteWithBusyState");
                throw;
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        protected async Task ExecuteWithBusyStateAsync(Func<Task> action, string? busyMessage = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = busyMessage ?? "Выполнение...";
                await action?.Invoke();
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.HandleException(ex, "ExecuteWithBusyStateAsync");
                throw;
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        protected async Task<T> ExecuteWithBusyStateAsync<T>(Func<Task<T>> action, string? busyMessage = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = busyMessage ?? "Выполнение...";
                return await action();
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.HandleException(ex, "ExecuteWithBusyStateAsync<T>");
                throw;
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        protected RelayCommand CreateCommand(Action execute, Func<bool>? canExecute = null)
        {
            return new RelayCommand(execute, canExecute);
        }

        protected RelayCommand<T> CreateCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            return new RelayCommand<T>(execute, canExecute);
        }

        protected AsyncRelayCommand CreateAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            return new AsyncRelayCommand(execute, canExecute);
        }

        protected AsyncRelayCommand<T> CreateAsyncCommand<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null)
        {
            return new AsyncRelayCommand<T>(execute, canExecute);
        }

        protected void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(string.Empty);
        }

        protected void SetStatus(string message, bool isError = false)
        {
            StatusMessage = message;
            if (isError)
            {
                Utils.ErrorHandler.LogException(new Exception(message), "ViewModel Status");
            }
        }

        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        // Метод для валидации строковых свойств
        protected bool ValidateStringProperty(string value, int minLength, int maxLength,
            bool allowEmpty = false, string? regexPattern = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return allowEmpty;

            if (value.Length < minLength || value.Length > maxLength)
                return false;

            if (!string.IsNullOrEmpty(regexPattern))
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(regexPattern);
                    return regex.IsMatch(value);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        // Метод для валидации email
        protected bool ValidateEmail(string email)
        {
            return Utils.StringExtensions.IsValidEmail(email);
        }

        // Метод для валидации имени пользователя
        protected bool ValidateUsername(string username)
        {
            return Utils.StringExtensions.IsValidUsername(username);
        }
    }
}