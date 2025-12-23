using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services.Interfaces;
using Messenger.Utils;
using Messenger.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace Messenger.ViewModels
{
    public partial class UserProfileViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private MainViewModel? _parentViewModel;
        private User? _originalUser;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _photoUrl = string.Empty;

        [ObservableProperty]
        private DateTime _createdAt = DateTime.UtcNow;

        [ObservableProperty]
        private DateTime _lastSeen = DateTime.UtcNow;

        [ObservableProperty]
        private bool _isOnline = false;

        [ObservableProperty]
        private bool _isEditing = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public UserProfileViewModel(IAuthService authService, IDatabaseService databaseService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            InitializeCommands();

            // Загружаем данные пользователя при создании
            LoadUserDataAsync().ConfigureAwait(false);
        }

        public MainViewModel? ParentViewModel
        {
            get => _parentViewModel;
            set => SetProperty(ref _parentViewModel, value);
        }

        private void InitializeCommands()
        {
            StartEditCommand = new RelayCommand(StartEdit);
            SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync, CanSaveProfile);
            CancelEditCommand = new RelayCommand(CancelEdit);
            ChangePhotoCommand = new AsyncRelayCommand(ChangePhotoAsync);
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        }

        public async Task LoadUserDataAsync()
        {
            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    if (ParentViewModel?.CurrentUser == null)
                    {
                        var currentUser = await _authService.GetCurrentUserAsync();
                        if (currentUser == null)
                        {
                            SetError("Пользователь не найден");
                            return;
                        }

                        UpdateUserData(currentUser);
                    }
                    else
                    {
                        UpdateUserData(ParentViewModel.CurrentUser);
                    }

                    // Загружаем полные данные пользователя из базы данных
                    if (!string.IsNullOrEmpty(Email))
                    {
                        var userId = ParentViewModel?.CurrentUser?.Id;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var fullUserData = await _databaseService.GetUserAsync(userId);
                            if (fullUserData != null)
                            {
                                UpdateUserData(fullUserData);
                            }
                        }
                    }

                    ClearErrors();
                    StatusMessage = "Данные загружены";
                });
            }
            catch (Exception ex)
            {
                SetError(ErrorHandler.GetUserFriendlyMessage(ex));
                ErrorHandler.LogException(ex, "LoadUserDataAsync");
            }
        }

        private void UpdateUserData(User user)
        {
            DisplayName = user.DisplayName ?? string.Empty;
            Username = user.Username ?? string.Empty;
            Email = user.Email ?? string.Empty;
            PhotoUrl = user.PhotoUrl ?? string.Empty;
            CreatedAt = user.CreatedAt;
            LastSeen = user.LastSeen;
            IsOnline = user.IsOnline;

            // Сохраняем оригинальные данные для отмены редактирования
            _originalUser = new User(user.Id, user.Email, user.Username)
            {
                DisplayName = user.DisplayName,
                PhotoUrl = user.PhotoUrl,
                CreatedAt = user.CreatedAt,
                LastSeen = user.LastSeen,
                IsOnline = user.IsOnline
            };
        }

        private bool CanSaveProfile()
        {
            return IsEditing &&
                   !string.IsNullOrWhiteSpace(DisplayName) &&
                   DisplayName.Length <= AppConstants.MaxUsernameLength &&
                   !IsBusy;
        }

        // Команды профиля пользователя
        public ICommand StartEditCommand { get; private set; } = null!;
        public ICommand SaveProfileCommand { get; private set; } = null!;
        public ICommand CancelEditCommand { get; private set; } = null!;
        public ICommand ChangePhotoCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void StartEdit()
        {
            IsEditing = true;
            StatusMessage = "Режим редактирования";
        }

        private async Task SaveProfileAsync()
        {
            if (ParentViewModel?.CurrentUser == null) return;

            try
            {
                await ExecuteWithBusyStateAsync(async () =>
                {
                    // Создаем обновленный объект пользователя
                    var updatedUser = new User(ParentViewModel.CurrentUser.Id, Email, Username)
                    {
                        DisplayName = DisplayName.Trim(),
                        PhotoUrl = PhotoUrl,
                        CreatedAt = CreatedAt,
                        LastSeen = LastSeen,
                        IsOnline = IsOnline
                    };

                    // Обновляем профиль в сервисе аутентификации
                    var authSuccess = await _authService.UpdateUserProfileAsync(updatedUser);

                    // Обновляем профиль в базе данных
                    var dbSuccess = await _databaseService.UpdateUserAsync(updatedUser);

                    if (authSuccess || dbSuccess)
                    {
                        IsEditing = false;
                        _originalUser = updatedUser;

                        // Обновляем CurrentUser в ParentViewModel
                        if (ParentViewModel != null)
                        {
                            ParentViewModel.CurrentUser = updatedUser;
                        }

                        ClearErrors();
                        StatusMessage = "Профиль успешно обновлен";
                        ErrorHandler.ShowInfoMessage("Профиль успешно обновлен", "Успех");
                    }
                    else
                    {
                        SetError("Не удалось обновить профиль");
                    }
                });
            }
            catch (Exception ex)
            {
                SetError(ErrorHandler.GetUserFriendlyMessage(ex));
                ErrorHandler.LogException(ex, "SaveProfileAsync");
            }
        }

        private void CancelEdit()
        {
            if (_originalUser != null)
            {
                UpdateUserData(_originalUser);
            }

            IsEditing = false;
            ClearErrors();
            StatusMessage = "Редактирование отменено";
        }

        private async Task ChangePhotoAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|Все файлы (*.*)|*.*",
                    Title = "Выберите изображение профиля",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;

                    // Здесь будет реализация загрузки изображения на сервер
                    // Пока просто сохраняем локальный путь
                    PhotoUrl = filePath;

                    StatusMessage = "Изображение выбрано";

                    // Если мы в режиме редактирования, автоматически сохраняем
                    if (IsEditing)
                    {
                        await SaveProfileAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "ChangePhotoAsync");
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                var confirm = ErrorHandler.ShowConfirmationMessage(
                    "Вы действительно хотите выйти?",
                    "Подтверждение выхода"
                );

                if (confirm)
                {
                    await ExecuteWithBusyStateAsync(async () =>
                    {
                        await ParentViewModel?.LogoutCommand?.ExecuteAsync(null);
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "LogoutAsync");
            }
        }

        private async Task RefreshAsync()
        {
            await LoadUserDataAsync();
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