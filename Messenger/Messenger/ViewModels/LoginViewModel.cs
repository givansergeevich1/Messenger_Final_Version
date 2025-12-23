using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Services.Interfaces;
using Messenger.Utils;
using Messenger.ViewModels;

namespace Messenger.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private MainViewModel? _parentViewModel;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _email = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe = true;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            InitializeCommands();

            // Загружаем сохраненные данные, если есть
            LoadSavedCredentialsAsync().ConfigureAwait(false);
        }

        public MainViewModel? ParentViewModel
        {
            get => _parentViewModel;
            set => SetProperty(ref _parentViewModel, value);
        }

        private void InitializeCommands()
        {
            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            NavigateToRegisterCommand = new RelayCommand(NavigateToRegister);
            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync);
        }

        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                // Загружаем сохраненный email, если пользователь выбрал "Запомнить меня"
                var savedEmail = await SecureStorage.GetAsync("remembered_email");
                var savedRememberMe = await SecureStorage.GetAsync("remember_me");

                if (!string.IsNullOrEmpty(savedEmail) &&
                    !string.IsNullOrEmpty(savedRememberMe) &&
                    bool.TryParse(savedRememberMe, out var rememberMe) &&
                    rememberMe)
                {
                    Email = savedEmail;
                    RememberMe = true;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "LoadSavedCredentialsAsync");
            }
        }

        private async Task SaveCredentialsAsync()
        {
            try
            {
                if (RememberMe && !string.IsNullOrEmpty(Email))
                {
                    await SecureStorage.SetAsync("remembered_email", Email);
                    await SecureStorage.SetAsync("remember_me", RememberMe.ToString());
                }
                else
                {
                    SecureStorage.Remove("remembered_email");
                    SecureStorage.Remove("remember_me");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "SaveCredentialsAsync");
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Email.IsValidEmail() &&
                   Password.Length >= AppConstants.MinPasswordLength &&
                   !IsBusy;
        }

        // Команды аутентификации
        public ICommand LoginCommand { get; private set; } = null!;
        public ICommand NavigateToRegisterCommand { get; private set; } = null!;
        public ICommand ResetPasswordCommand { get; private set; } = null!;

        private async Task LoginAsync()
        {
            ClearErrors();

            if (!CanLogin())
            {
                SetError("Пожалуйста, заполните все поля корректно");
                return;
            }

            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    // Сохраняем учетные данные, если выбрано "Запомнить меня"
                    await SaveCredentialsAsync();

                    // Выполняем вход
                    var user = await _authService.LoginAsync(Email, Password);

                    if (user == null)
                    {
                        SetError("Неверный email или пароль");
                        return;
                    }

                    // Очищаем поля после успешного входа
                    Password = string.Empty;

                    // Навигация будет выполнена через MainViewModel
                    ErrorHandler.ShowInfoMessage($"Добро пожаловать, {user.DisplayName}!", "Успешный вход");
                });
            }
            catch (Exception ex)
            {
                SetError(ErrorHandler.GetUserFriendlyMessage(ex));
                ErrorHandler.LogException(ex, "LoginAsync");
            }
        }

        private void NavigateToRegister()
        {
            try
            {
                ParentViewModel?.NavigateToRegisterCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToRegister");
            }
        }

        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || !Email.IsValidEmail())
            {
                SetError("Введите корректный email для восстановления пароля");
                return;
            }

            try
            {
                var result = await ErrorHandler.ShowConfirmationMessage(
                    $"Отправить инструкции по восстановлению пароля на {Email}?",
                    "Восстановление пароля"
                );

                if (!result) return;

                await ExecuteWithBusyStateAsync(async () =>
                {
                    var success = await _authService.ResetPasswordAsync(Email);

                    if (success)
                    {
                        ErrorHandler.ShowInfoMessage(
                            "Инструкции по восстановлению пароля отправлены на ваш email",
                            "Письмо отправлено"
                        );
                    }
                    else
                    {
                        SetError("Не удалось отправить письмо для восстановления пароля");
                    }
                });
            }
            catch (Exception ex)
            {
                SetError(ErrorHandler.GetUserFriendlyMessage(ex));
                ErrorHandler.LogException(ex, "ResetPasswordAsync");
            }
        }

        private void ClearErrors()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        private void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}