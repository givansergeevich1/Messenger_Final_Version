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

        // Команды будут реализованы в следующем коммите
        public ICommand LoginCommand { get; private set; } = null!;
        public ICommand NavigateToRegisterCommand { get; private set; } = null!;
        public ICommand ResetPasswordCommand { get; private set; } = null!;

        private async Task LoginAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }

        private void NavigateToRegister()
        {
            // Будет реализовано в следующем коммите
        }

        private async Task ResetPasswordAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
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