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
        private ObservableCollection<string> _selectedUsersForNewChat = new ObservableCollection<string>();

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

                    // Помечаем сообщения как прочитанные
                    MarkMessagesAsReadAsync(value.Id).ConfigureAwait(false);
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

        public ObservableCollection<string> SelectedUsersForNewChat
        {
            get => _selectedUsersForNewChat;
            set => SetProperty(ref _selectedUsersForNewChat, value);
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
            SelectUserForChatCommand = new RelayCommand<string>(SelectUserForChat);
            RemoveUserFromSelectionCommand = new RelayCommand<string>(RemoveUserFromSelection);
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

        private async Task MarkMessagesAsReadAsync(string chatId)
        {
            try
            {
                // Помечаем все непрочитанные сообщения в чате как прочитанные
                foreach (var message in Messages.Where(m => !m.IsRead && m.SenderId != ParentViewModel?.CurrentUser?.Id))
                {
                    await _databaseService.MarkMessageAsReadAsync(message.Id);
                }

                // Обновляем счетчик непрочитанных сообщений
                if (ParentViewModel != null)
                {
                    await ParentViewModel.LoadUnreadMessagesCountAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "MarkMessagesAsReadAsync");
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
            try
            {
                // Обновляем или добавляем чат в список
                var existingChat = Chats.FirstOrDefault(c => c.Id == chat.Id);

                if (existingChat != null)
                {
                    // Обновляем существующий чат
                    var index = Chats.IndexOf(existingChat);
                    Chats[index] = chat;

                    // Если это выбранный чат, обновляем его
                    if (SelectedChat?.Id == chat.Id)
                    {
                        SelectedChat = chat;
                    }
                }
                else
                {
                    // Добавляем новый чат
                    Chats.Add(chat);
                }

                // Сортируем чаты по времени последнего сообщения
                var sortedChats = Chats.OrderByDescending(c => c.LastMessageTime).ToList();
                Chats.Clear();
                foreach (var sortedChat in sortedChats)
                {
                    Chats.Add(sortedChat);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "OnChatUpdated");
            }
        }

        private void OnNewMessage(Message message)
        {
            try
            {
                // Добавляем сообщение только если оно из выбранного чата
                if (SelectedChat?.Id == message.ChatId)
                {
                    Messages.Add(message);
                    ScrollToLastMessage();

                    // Помечаем как прочитанное, если оно не от текущего пользователя
                    if (message.SenderId != ParentViewModel?.CurrentUser?.Id)
                    {
                        _databaseService.MarkMessageAsReadAsync(message.Id).ConfigureAwait(false);
                    }
                }

                // Обновляем счетчик непрочитанных сообщений
                if (ParentViewModel != null && message.SenderId != ParentViewModel.CurrentUser?.Id)
                {
                    ParentViewModel.LoadUnreadMessagesCountAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "OnNewMessage");
            }
        }

        private void FilterChats()
        {
            // Реализация будет в UI
        }

        private void ScrollToLastMessage()
        {
            // Реализация будет в UI
        }

        // Команды отправки сообщений и управления чатами
        public ICommand SendMessageCommand { get; private set; } = null!;
        public ICommand StartNewChatCommand { get; private set; } = null!;
        public ICommand CreateChatCommand { get; private set; } = null!;
        public ICommand CancelNewChatCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand RefreshChatsCommand { get; private set; } = null!;
        public ICommand LoadMoreMessagesCommand { get; private set; } = null!;
        public ICommand SelectUserForChatCommand { get; private set; } = null!;
        public ICommand RemoveUserFromSelectionCommand { get; private set; } = null!;

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
                   (IsGroupChat || SelectedUsersForNewChat.Count > 0) &&
                   !IsBusy;
        }

        private async Task SendMessageAsync()
        {
            if (SelectedChat == null || ParentViewModel?.CurrentUser == null) return;

            var messageText = NewMessageText.Trim();
            if (string.IsNullOrWhiteSpace(messageText)) return;

            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    // Отправляем сообщение
                    var message = await _databaseService.SendMessageAsync(
                        SelectedChat.Id,
                        ParentViewModel.CurrentUser.Id,
                        ParentViewModel.CurrentUser.DisplayName,
                        messageText
                    );

                    // Очищаем поле ввода
                    NewMessageText = string.Empty;

                    // Добавляем сообщение в список
                    Messages.Add(message);
                    ScrollToLastMessage();
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "SendMessageAsync");
            }
        }

        private void StartNewChat()
        {
            IsCreatingNewChat = true;
            NewChatName = string.Empty;
            SelectedUsersForNewChat.Clear();
            IsGroupChat = false;
        }

        private async Task CreateChatAsync()
        {
            if (ParentViewModel?.CurrentUser == null) return;

            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    // Собираем список участников
                    var participantIds = SelectedUsersForNewChat.ToList();

                    // Создаем чат
                    var newChat = await _databaseService.CreateChatAsync(
                        NewChatName,
                        ParentViewModel.CurrentUser.Id,
                        participantIds,
                        IsGroupChat
                    );

                    // Закрываем диалог создания чата
                    CancelNewChat();

                    // Выбираем новый чат
                    SelectedChat = newChat;

                    // Обновляем список чатов
                    await RefreshChatsAsync();

                    ErrorHandler.ShowInfoMessage($"Чат '{NewChatName}' создан", "Успех");
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "CreateChatAsync");
            }
        }

        private void CancelNewChat()
        {
            IsCreatingNewChat = false;
            NewChatName = string.Empty;
            SelectedUsersForNewChat.Clear();
            IsGroupChat = false;
        }

        private void ToggleSearch()
        {
            IsSearchVisible = !IsSearchVisible;
            if (!IsSearchVisible)
            {
                SearchQuery = string.Empty;
            }
        }

        private void ClearSearch()
        {
            SearchQuery = string.Empty;
        }

        private async Task LoadMoreMessagesAsync()
        {
            if (SelectedChat == null) return;

            try
            {
                // Загружаем дополнительные сообщения
                var currentCount = Messages.Count;
                var moreMessages = await _databaseService.GetChatMessagesAsync(SelectedChat.Id, currentCount + 50);

                if (moreMessages.Count > currentCount)
                {
                    Messages.Clear();
                    foreach (var message in moreMessages)
                    {
                        Messages.Add(message);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "LoadMoreMessagesAsync");
            }
        }

        private void SelectUserForChat(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            if (!SelectedUsersForNewChat.Contains(userId))
            {
                SelectedUsersForNewChat.Add(userId);
            }
        }

        private void RemoveUserFromSelection(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            SelectedUsersForNewChat.Remove(userId);
        }
    }
}