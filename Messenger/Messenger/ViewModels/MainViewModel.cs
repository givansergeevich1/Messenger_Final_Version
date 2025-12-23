using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;

namespace Messenger.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private ObservableObject? _currentViewModel;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated;

        [ObservableProperty]
        private string _appTitle = "Messenger";

        [ObservableProperty]
        private int _unreadMessagesCount;

        public MainViewModel(IAuthService authService, IDatabaseService databaseService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

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

        // Команды будут определены в следующем коммите
        public ICommand NavigateToLoginCommand { get; private set; } = null!;
        public ICommand NavigateToRegisterCommand { get; private set; } = null!;
        public ICommand NavigateToChatCommand { get; private set; } = null!;
        public ICommand NavigateToProfileCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void NavigateToLogin()
        {
            // Будет реализовано в следующем коммите
        }

        private void NavigateToRegister()
        {
            // Будет реализовано в следующем коммите
        }

        private void NavigateToChat()
        {
            // Будет реализовано в следующем коммите
        }

        private void NavigateToProfile()
        {
            // Будет реализовано в следующем коммите
        }

        private async Task LogoutAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }
    }
}