using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;
using Messenger.ViewModels;

namespace Messenger.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private MainViewModel? _parentViewModel;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _email = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _username = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private bool _passwordsMatch = true;

        public RegisterViewModel(IAuthService authService, IDatabaseService databaseService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            InitializeCommands();
        }

        public MainViewModel? ParentViewModel
        {
            get => _parentViewModel;
            set => SetProperty(ref _parentViewModel, value);
        }

        private void InitializeCommands()
        {
            RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
            NavigateToLoginCommand = new RelayCommand(NavigateToLogin);
        }

        partial void OnPasswordChanged(string value)
        {
            UpdatePasswordMatch();
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            UpdatePasswordMatch();
        }

        private void UpdatePasswordMatch()
        {
            PasswordsMatch = Password == ConfirmPassword;
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   Email.IsValidEmail() &&
                   Username.IsValidUsername() &&
                   Password.Length >= AppConstants.MinPasswordLength &&
                   ConfirmPassword.Length >= AppConstants.MinPasswordLength &&
                   PasswordsMatch &&
                   !IsBusy;
        }

        // Команды регистрации
        public ICommand RegisterCommand { get; private set; } = null!;
        public ICommand NavigateToLoginCommand { get; private set; } = null!;

        private async Task RegisterAsync()
        {
            ClearErrors();

            if (!CanRegister())
            {
                if (!PasswordsMatch)
                {
                    SetError("Пароли не совпадают");
                }
                else
                {
                    SetError("Пожалуйста, заполните все поля корректно");
                }
                return;
            }

            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    // Регистрируем пользователя
                    var user = await _authService.RegisterAsync(Email, Username, Password);

                    if (user == null)
                    {
                        SetError("Не удалось зарегистрировать пользователя");
                        return;
                    }

                    // Устанавливаем отображаемое имя
                    user.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;

                    // Создаем запись пользователя в базе данных
                    var success = await _databaseService.CreateUserAsync(user);

                    if (!success)
                    {
                        ErrorHandler.ShowWarningMessage(
                            "Пользователь создан, но произошла ошибка при сохранении дополнительных данных",
                            "Частичный успех"
                        );
                    }

                    // Очищаем поля после успешной регистрации
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;

                    // Показываем сообщение об успехе
                    ErrorHandler.ShowInfoMessage(
                        $"Регистрация успешно завершена! Добро пожаловать, {user.DisplayName}!",
                        "Успешная регистрация"
                    );

                    // Автоматический вход и навигация будут выполнены через MainViewModel
                });
            }
            catch (Exception ex)
            {
                SetError(ErrorHandler.GetUserFriendlyMessage(ex));
                ErrorHandler.LogException(ex, "RegisterAsync");
            }
        }

        private void NavigateToLogin()
        {
            try
            {
                ParentViewModel?.NavigateToLoginCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToLogin");
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