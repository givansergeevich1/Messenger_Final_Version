using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Messenger.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IServiceProvider _serviceProvider;
        private ObservableObject? _currentViewModel;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated;

        [ObservableProperty]
        private string _appTitle = "Messenger";

        [ObservableProperty]
        private int _unreadMessagesCount;

        public MainViewModel(
            IAuthService authService,
            IDatabaseService databaseService,
            IServiceProvider serviceProvider)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeCommands();
            SubscribeToAuthEvents();

            // Инициализация
            InitializeAsync().ConfigureAwait(false);
        }

        public ObservableObject? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        private void InitializeCommands()
        {
            NavigateToLoginCommand = new RelayCommand(NavigateToLogin);
            NavigateToRegisterCommand = new RelayCommand(NavigateToRegister);
            NavigateToChatCommand = new RelayCommand(NavigateToChat);
            NavigateToProfileCommand = new RelayCommand(NavigateToProfile);
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        }

        private void SubscribeToAuthEvents()
        {
            _authService.AuthStateChanged += OnAuthStateChanged;
        }

        private async void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
        {
            IsAuthenticated = e.IsAuthenticated;
            CurrentUser = e.User;

            if (IsAuthenticated && CurrentUser != null)
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    await UpdateUserStatusAsync(true);
                    await LoadUnreadMessagesCountAsync();

                    // Автоматически переходим на страницу чатов после входа
                    NavigateToChat();
                });
            }
            else
            {
                // Переходим на страницу входа при выходе
                NavigateToLogin();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                // Проверяем текущее состояние аутентификации
                IsAuthenticated = await _authService.IsAuthenticatedAsync();
                CurrentUser = await _authService.GetCurrentUserAsync();

                if (IsAuthenticated && CurrentUser != null)
                {
                    await UpdateUserStatusAsync(true);
                    await LoadUnreadMessagesCountAsync();
                    NavigateToChat();
                }
                else
                {
                    NavigateToLogin();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "MainViewModel initialization");
                NavigateToLogin();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateUserStatusAsync(bool isOnline)
        {
            if (CurrentUser == null) return;

            try
            {
                await _databaseService.UpdateUserStatusAsync(
                    CurrentUser.Id,
                    isOnline,
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "UpdateUserStatusAsync");
            }
        }

        private async Task LoadUnreadMessagesCountAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                UnreadMessagesCount = await _databaseService.GetUnreadMessagesCountAsync(CurrentUser.Id);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "LoadUnreadMessagesCountAsync");
                UnreadMessagesCount = 0;
            }
        }

        // Команды навигации
        public ICommand NavigateToLoginCommand { get; private set; } = null!;
        public ICommand NavigateToRegisterCommand { get; private set; } = null!;
        public ICommand NavigateToChatCommand { get; private set; } = null!;
        public ICommand NavigateToProfileCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void NavigateToLogin()
        {
            try
            {
                var loginViewModel = _serviceProvider.GetService<LoginViewModel>();
                if (loginViewModel != null)
                {
                    loginViewModel.ParentViewModel = this;
                    CurrentViewModel = loginViewModel;
                    AppTitle = "Вход - Messenger";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToLogin");
            }
        }

        private void NavigateToRegister()
        {
            try
            {
                var registerViewModel = _serviceProvider.GetService<RegisterViewModel>();
                if (registerViewModel != null)
                {
                    registerViewModel.ParentViewModel = this;
                    CurrentViewModel = registerViewModel;
                    AppTitle = "Регистрация - Messenger";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToRegister");
            }
        }

        private void NavigateToChat()
        {
            try
            {
                var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
                if (chatViewModel != null)
                {
                    chatViewModel.ParentViewModel = this;
                    CurrentViewModel = chatViewModel;
                    AppTitle = $"Чат - Messenger";

                    // Обновляем счетчик непрочитанных сообщений
                    LoadUnreadMessagesCountAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToChat");
            }
        }

        private void NavigateToProfile()
        {
            try
            {
                var profileViewModel = _serviceProvider.GetService<UserProfileViewModel>();
                if (profileViewModel != null)
                {
                    profileViewModel.ParentViewModel = this;
                    CurrentViewModel = profileViewModel;
                    AppTitle = $"Профиль - Messenger";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "NavigateToProfile");
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                if (CurrentUser != null)
                {
                    await UpdateUserStatusAsync(false);
                }

                await _authService.LogoutAsync();

                // Navigation will be handled by AuthStateChanged event
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Logout");
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                await LoadUnreadMessagesCountAsync();

                if (CurrentViewModel is ChatViewModel chatViewModel)
                {
                    await chatViewModel.RefreshChatsAsync();
                }
                else if (CurrentViewModel is UserProfileViewModel profileViewModel)
                {
                    await profileViewModel.LoadUserDataAsync();
                }

                ErrorHandler.ShowInfoMessage("Данные обновлены", "Обновление");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Refresh");
            }
        }
    }
}