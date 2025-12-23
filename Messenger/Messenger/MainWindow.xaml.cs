using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
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

            // Настраиваем поведение Frame
            MainFrame.JournalOwnership = JournalOwnership.OwnsJournal;
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
                else if (e.PropertyName == nameof(MainViewModel.IsBusy))
                {
                    UpdateBusyState();
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
                var page = (Page)Activator.CreateInstance(pageType);
                page.DataContext = _mainViewModel.CurrentViewModel;

                // Применяем анимацию перехода
                ApplyPageTransition(page);
            }
        }

        private void ApplyPageTransition(Page page)
        {
            // Сохраняем текущий контент для анимации
            var oldContent = MainFrame.Content as UIElement;

            // Устанавливаем новую страницу
            MainFrame.Navigate(page);

            // Если была предыдущая страница, анимируем переход
            if (oldContent != null)
            {
                // Создаем анимацию исчезновения старой страницы
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.2)
                };

                fadeOutAnimation.Completed += (s, e) =>
                {
                    // После исчезновения старой страницы анимируем появление новой
                    if (MainFrame.Content is UIElement newContent)
                    {
                        var fadeInAnimation = new DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };

                        newContent.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
                    }
                };

                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            }
            else
            {
                // Если это первая страница, просто делаем ее видимой
                if (MainFrame.Content is UIElement newContent)
                {
                    newContent.Opacity = 1.0;
                }
            }
        }

        private System.Type GetPageTypeForViewModel(object viewModel)
        {
            var viewModelType = viewModel.GetType();

            // Маппинг ViewModel типов на соответствующие Page типы
            if (viewModelType == typeof(LoginViewModel))
                return typeof(Views.LoginPage);
            else if (viewModelType == typeof(RegisterViewModel))
                return typeof(Views.RegisterPage);
            else if (viewModelType == typeof(ChatViewModel))
                return typeof(Views.ChatPage);
            else if (viewModelType == typeof(UserProfileViewModel))
                return typeof(Views.UserProfilePage);

            return null;
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            // Убираем запись в журнале навигации для предотвращения возврата
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }

            // Обновляем заголовок окна
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            if (_mainViewModel.CurrentViewModel is BaseViewModel currentViewModel)
            {
                if (!string.IsNullOrEmpty(currentViewModel.Title))
                {
                    Title = $"{currentViewModel.Title} - Messenger";
                }
            }
        }

        private void UpdateBusyState()
        {
            // Обновляем состояние курсора в зависимости от IsBusy
            Cursor = _mainViewModel.IsBusy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

            // Блокируем/разблокируем навигационные кнопки
            UpdateNavigationButtonsState();
        }

        private void UpdateNavigationButtonsState()
        {
            // Здесь будет обновление состояния навигационных кнопок
            // Пока оставляем как заглушку
        }

        private void RestoreWindowState()
        {
            try
            {
                // Восстанавливаем размер и положение окна из настроек
                var settings = Properties.Settings.Default;

                if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
                {
                    Width = settings.WindowWidth;
                    Height = settings.WindowHeight;
                }

                if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
                {
                    Left = settings.WindowLeft;
                    Top = settings.WindowTop;
                }

                if (settings.WindowState != WindowState.Minimized)
                {
                    WindowState = settings.WindowState;
                }
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "RestoreWindowState");
            }
        }

        private void SaveWindowState()
        {
            try
            {
                // Сохраняем состояние окна в настройки
                var settings = Properties.Settings.Default;

                if (WindowState == WindowState.Normal)
                {
                    settings.WindowWidth = Width;
                    settings.WindowHeight = Height;
                    settings.WindowLeft = Left;
                    settings.WindowTop = Top;
                }
                else
                {
                    // Сохраняем восстановленные размеры
                    settings.WindowWidth = RestoreBounds.Width;
                    settings.WindowHeight = RestoreBounds.Height;
                    settings.WindowLeft = RestoreBounds.Left;
                    settings.WindowTop = RestoreBounds.Top;
                }

                settings.WindowState = WindowState;
                settings.Save();
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "SaveWindowState");
            }
        }

        private void UpdateUserStatusOnClose()
        {
            // Обновляем статус пользователя на "не в сети"
            if (_mainViewModel.CurrentUser != null)
            {
                // Асинхронный вызов будет выполнен в фоне
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var databaseService = ((App)Application.Current)._serviceProvider.GetService<Services.Interfaces.IDatabaseService>();
                        if (databaseService != null)
                        {
                            await databaseService.UpdateUserStatusAsync(
                                _mainViewModel.CurrentUser.Id,
                                false,
                                DateTime.UtcNow
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.ErrorHandler.LogException(ex, "UpdateUserStatusOnClose");
                    }
                });
            }
        }

        // Обработчики для кнопок навигации (для прямого доступа из XAML)
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