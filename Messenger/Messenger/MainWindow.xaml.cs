using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Messenger.ViewModels;

namespace Messenger
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel ?? throw new System.ArgumentNullException(nameof(mainViewModel));
            DataContext = _mainViewModel;

            // Настраиваем навигацию
            ConfigureNavigation();

            // Подписываемся на события
            SubscribeToEvents();

            // Загружаем начальное состояние
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void ConfigureNavigation()
        {
            // Устанавливаем обработчик навигации для Frame
            MainFrame.Navigated += OnFrameNavigated;

            // Отключаем стандартную навигацию
            MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на изменения CurrentViewModel
            _mainViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
                {
                    UpdateCurrentPage();
                }
            };
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальную страницу
            UpdateCurrentPage();

            // Восстанавливаем состояние окна
            RestoreWindowState();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Сохраняем состояние окна
            SaveWindowState();

            // Обновляем статус пользователя на "не в сети"
            UpdateUserStatusOnClose();
        }

        private void UpdateCurrentPage()
        {
            if (_mainViewModel.CurrentViewModel == null)
                return;

            var pageType = GetPageTypeForViewModel(_mainViewModel.CurrentViewModel);
            if (pageType != null)
            {
                // Создаем страницу и устанавливаем DataContext
                var page = (Page)System.Activator.CreateInstance(pageType);
                page.DataContext = _mainViewModel.CurrentViewModel;

                // Переходим на страницу
                MainFrame.Navigate(page);
            }
        }

        private System.Type? GetPageTypeForViewModel(object viewModel)
        {
            return viewModel switch
            {
                LoginViewModel => typeof(Views.LoginPage),
                RegisterViewModel => typeof(Views.RegisterPage),
                ChatViewModel => typeof(Views.ChatPage),
                UserProfileViewModel => typeof(Views.UserProfilePage),
                _ => null
            };
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            // Убираем запись в журнале навигации для предотвращения возврата
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }

        private void RestoreWindowState()
        {
            // Здесь будет восстановление состояния окна (размер, положение)
            // Пока оставляем пустым
        }

        private void SaveWindowState()
        {
            // Здесь будет сохранение состояния окна
            // Пока оставляем пустым
        }

        private void UpdateUserStatusOnClose()
        {
            // Обновляем статус пользователя на "не в сети"
            if (_mainViewModel.CurrentUser != null)
            {
                // Асинхронный вызов будет выполнен в фоне
                Task.Run(async () =>
                {
                    try
                    {
                        var databaseService = ((App)Application.Current)._serviceProvider.GetService<Services.Interfaces.IDatabaseService>();
                        if (databaseService != null)
                        {
                            await databaseService.UpdateUserStatusAsync(
                                _mainViewModel.CurrentUser.Id,
                                false,
                                System.DateTime.UtcNow
                            );
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Utils.ErrorHandler.LogException(ex, "UpdateUserStatusOnClose");
                    }
                });
            }
        }

        // Методы для обработки нажатий кнопок навигации
        private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.NavigateToChatCommand?.Execute(null);
        }

        private void NavigateToProfile_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.NavigateToProfileCommand?.Execute(null);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.LogoutCommand?.Execute(null);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.RefreshCommand?.Execute(null);
        }
    }
}