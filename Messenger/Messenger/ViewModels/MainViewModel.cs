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
            set => SetProperty(ref _currentViewModel, value, onChanged: OnCurrentViewModelChanged);
        }

        private void OnCurrentViewModelChanged()
        {
            // Обновляем заголовок при изменении текущей ViewModel
            UpdateAppTitle();
        }

        private void UpdateAppTitle()
        {
            var suffix = CurrentViewModel switch
            {
                LoginViewModel => " - Вход",
                RegisterViewModel => " - Регистрация",
                ChatViewModel => " - Чат",
                UserProfileViewModel => " - Профиль",
                _ => ""
            };

            AppTitle = $"Messenger{suffix}";
        }

        private void InitializeCommands()
        {
            NavigateToLoginCommand = CreateCommand(NavigateToLogin, () => !IsBusy);
            NavigateToRegisterCommand = CreateCommand(NavigateToRegister, () => !IsBusy);
            NavigateToChatCommand = CreateCommand(NavigateToChat, () => IsAuthenticated && !IsBusy);
            NavigateToProfileCommand = CreateCommand(NavigateToProfile, () => IsAuthenticated && !IsBusy);
            LogoutCommand = CreateAsyncCommand(LogoutAsync, () => IsAuthenticated && !IsBusy);
            RefreshCommand = CreateAsyncCommand(RefreshAsync, () => !IsBusy);
        }

        private void SubscribeToAuthEvents()
        {
            _authService.AuthStateChanged += OnAuthStateChanged;
        }

        private async void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
        {
            await ExecuteWithBusyStateAsync(async () =>
            {
                IsAuthenticated = e.IsAuthenticated;
                CurrentUser = e.User;

                if (IsAuthenticated && CurrentUser != null)
                {
                    await UpdateUserStatusAsync(true);
                    await LoadUnreadMessagesCountAsync();

                    // Автоматически переходим на страницу чатов после входа
                    NavigateToChat();
                }
                else
                {
                    // Переходим на страницу входа при выходе
                    NavigateToLogin();
                }
            }, "Обновление состояния аутентификации...");
        }

        private async Task InitializeAsync()
        {
            await ExecuteWithBusyStateAsync(async () =>
            {
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
            }, "Инициализация приложения...");
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
                SetStatus($"Ошибка обновления статуса: {ex.Message}", true);
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
                SetStatus($"Ошибка загрузки счетчика сообщений: {ex.Message}", true);
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
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка перехода к входу: {ex.Message}", true);
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
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка перехода к регистрации: {ex.Message}", true);
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

                    // Обновляем счетчик непрочитанных сообщений
                    LoadUnreadMessagesCountAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка перехода к чату: {ex.Message}", true);
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
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка перехода к профилю: {ex.Message}", true);
            }
        }

        private async Task LogoutAsync()
        {
            await ExecuteWithBusyStateAsync(async () =>
            {
                try
                {
                    if (CurrentUser != null)
                    {
                        await UpdateUserStatusAsync(false);
                    }

                    await _authService.LogoutAsync();
                    SetStatus("Выход выполнен успешно");
                }
                catch (Exception ex)
                {
                    SetStatus($"Ошибка выхода: {ex.Message}", true);
                }
            }, "Выход из системы...");
        }

        private async Task RefreshAsync()
        {
            await ExecuteWithBusyStateAsync(async () =>
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

                    SetStatus("Данные обновлены");
                }
                catch (Exception ex)
                {
                    SetStatus($"Ошибка обновления: {ex.Message}", true);
                }
            }, "Обновление данных...");
        }
    }
}