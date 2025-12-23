using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    public partial class ChatViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private MainViewModel? _parentViewModel;
        private ChatRoom? _selectedChat;
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChatRoom> _chats = new ObservableCollection<ChatRoom>();

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();

        [ObservableProperty]
        private ObservableCollection<User> _availableUsers = new ObservableCollection<User>();

        [ObservableProperty]
        private string _newMessageText = string.Empty;

        [ObservableProperty]
        private bool _isSearchVisible = false;

        [ObservableProperty]
        private string _newChatName = string.Empty;

        [ObservableProperty]
        private bool _isCreatingNewChat = false;

        [ObservableProperty]
        private bool _isGroupChat = false;

        public ChatViewModel(IDatabaseService databaseService, IAuthService authService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            InitializeCommands();

            // Загружаем данные при создании
            LoadDataAsync().ConfigureAwait(false);
        }

        public MainViewModel? ParentViewModel
        {
            get => _parentViewModel;
            set => SetProperty(ref _parentViewModel, value);
        }

        public ChatRoom? SelectedChat
        {
            get => _selectedChat;
            set
            {
                if (SetProperty(ref _selectedChat, value) && value != null)
                {
                    LoadChatMessagesAsync(value.Id).ConfigureAwait(false);
                    SubscribeToChatMessages(value.Id);
                }
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterChats();
                }
            }
        }

        private void InitializeCommands()
        {
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
            StartNewChatCommand = new RelayCommand(StartNewChat);
            CreateChatCommand = new AsyncRelayCommand(CreateChatAsync, CanCreateChat);
            CancelNewChatCommand = new RelayCommand(CancelNewChat);
            SearchCommand = new RelayCommand(ToggleSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            RefreshChatsCommand = new AsyncRelayCommand(RefreshChatsAsync);
            LoadMoreMessagesCommand = new AsyncRelayCommand(LoadMoreMessagesAsync);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    await RefreshChatsAsync();
                    await LoadAvailableUsersAsync();

                    if (ParentViewModel?.CurrentUser != null)
                    {
                        SubscribeToUserChats(ParentViewModel.CurrentUser.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "ChatViewModel initialization");
            }
        }

        private async Task RefreshChatsAsync()
        {
            if (ParentViewModel?.CurrentUser == null) return;

            try
            {
                var userChats = await _databaseService.GetUserChatsAsync(ParentViewModel.CurrentUser.Id);

                Chats.Clear();
                foreach (var chat in userChats)
                {
                    Chats.Add(chat);
                }

                // Если есть выбранный чат, обновляем его данные
                if (SelectedChat != null)
                {
                    SelectedChat = userChats.FirstOrDefault(c => c.Id == SelectedChat.Id);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "RefreshChatsAsync");
            }
        }

        private async Task LoadAvailableUsersAsync()
        {
            try
            {
                var allUsers = await _databaseService.GetUsersAsync();
                var currentUserId = ParentViewModel?.CurrentUser?.Id;

                AvailableUsers.Clear();
                foreach (var user in allUsers)
                {
                    // Не показываем текущего пользователя в списке доступных
                    if (user.Id != currentUserId)
                    {
                        AvailableUsers.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "LoadAvailableUsersAsync");
            }
        }

        private async Task LoadChatMessagesAsync(string chatId)
        {
            try
            {
                Messages.Clear();

                var chatMessages = await _databaseService.GetChatMessagesAsync(chatId);

                foreach (var message in chatMessages)
                {
                    Messages.Add(message);
                }

                // Прокручиваем к последнему сообщению
                ScrollToLastMessage();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, $"LoadChatMessagesAsync for chat {chatId}");
            }
        }

        private void SubscribeToUserChats(string userId)
        {
            try
            {
                _databaseService.SubscribeToUserChats(userId, OnChatUpdated);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "SubscribeToUserChats");
            }
        }

        private void SubscribeToChatMessages(string chatId)
        {
            try
            {
                _databaseService.SubscribeToChatMessages(chatId, OnNewMessage);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "SubscribeToChatMessages");
            }
        }

        private void OnChatUpdated(ChatRoom chat)
        {
            // Будет реализовано в следующем коммите
        }

        private void OnNewMessage(Message message)
        {
            // Будет реализовано в следующем коммите
        }

        private void FilterChats()
        {
            // Будет реализовано в следующем коммите
        }

        private void ScrollToLastMessage()
        {
            // Будет реализовано в UI слое
        }

        // Команды будут реализованы в следующем коммите
        public ICommand SendMessageCommand { get; private set; } = null!;
        public ICommand StartNewChatCommand { get; private set; } = null!;
        public ICommand CreateChatCommand { get; private set; } = null!;
        public ICommand CancelNewChatCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand RefreshChatsCommand { get; private set; } = null!;
        public ICommand LoadMoreMessagesCommand { get; private set; } = null!;

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(NewMessageText) &&
                   SelectedChat != null &&
                   !IsBusy;
        }

        private bool CanCreateChat()
        {
            return !string.IsNullOrWhiteSpace(NewChatName) &&
                   NewChatName.Length <= AppConstants.MaxChatNameLength &&
                   !IsBusy;
        }

        private async Task SendMessageAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }

        private void StartNewChat()
        {
            // Будет реализовано в следующем коммите
        }

        private async Task CreateChatAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }

        private void CancelNewChat()
        {
            // Будет реализовано в следующем коммите
        }

        private void ToggleSearch()
        {
            // Будет реализовано в следующем коммите
        }

        private void ClearSearch()
        {
            // Будет реализовано в следующем коммите
        }

        private async Task LoadMoreMessagesAsync()
        {
            // Будет реализовано в следующем коммите
            await Task.CompletedTask;
        }
    }
}