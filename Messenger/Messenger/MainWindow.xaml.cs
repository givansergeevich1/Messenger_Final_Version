using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Messenger.Services.Interfaces;
using Messenger.ViewModels;

namespace Messenger
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private bool _isInitialized = false;
        private bool _isDataLoaded = false;

        public MainWindow(MainViewModel mainViewModel, IAuthService authService, IDatabaseService databaseService)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            DataContext = _mainViewModel;

            // Настраиваем навигацию
            ConfigureNavigation();

            // Подписываемся на события
            SubscribeToEvents();

            // Загружаем начальное состояние
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            StateChanged += OnWindowStateChanged;

            // Инициализируем приложение
            InitializeApplicationAsync().ConfigureAwait(false);
        }

        private async Task InitializeApplicationAsync()
        {
            try
            {
                // Проверяем соединение с Firebase
                await CheckFirebaseConnectionAsync();

                // Загружаем начальные данные
                await LoadInitialDataAsync();

                _isDataLoaded = true;

                // Обновляем интерфейс
                Dispatcher.Invoke(() =>
                {
                    UpdateApplicationState();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowConnectionError(ex);
                });
            }
        }

        private async Task CheckFirebaseConnectionAsync()
        {
            try
            {
                // Простая проверка соединения - пытаемся получить текущего пользователя
                var currentUser = await _authService.GetCurrentUserAsync();
                // Если исключение не было выброшено, соединение работает
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "CheckFirebaseConnection");
                throw new Exception("Не удалось подключиться к серверу. Проверьте интернет-соединение.", ex);
            }
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                // Загружаем данные пользователя, если он авторизован
                if (await _authService.IsAuthenticatedAsync())
                {
                    // Обновляем статус пользователя
                    var currentUser = await _authService.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        await _databaseService.UpdateUserStatusAsync(currentUser.Id, true, DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "LoadInitialData");
                // Не прерываем запуск приложения из-за ошибки загрузки данных
            }
        }

        private void ConfigureNavigation()
        {
            // Устанавливаем обработчик навигации для Frame
            MainFrame.Navigated += OnFrameNavigated;
            MainFrame.NavigationFailed += OnFrameNavigationFailed;
            MainFrame.NavigationStopped += OnFrameNavigationStopped;

            // Отключаем стандартную навигацию
            MainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;

            // Настраиваем поведение Frame
            MainFrame.JournalOwnership = JournalOwnership.OwnsJournal;
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на изменения свойств MainViewModel
            _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
        }

        private void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(MainViewModel.CurrentViewModel):
                        UpdateCurrentPage();
                        break;

                    case nameof(MainViewModel.IsBusy):
                        UpdateBusyState();
                        break;

                    case nameof(MainViewModel.IsAuthenticated):
                        UpdateNavigationVisibility();
                        break;

                    case nameof(MainViewModel.UnreadMessagesCount):
                        UpdateUnreadCounter();
                        break;

                    case nameof(MainViewModel.StatusMessage):
                        UpdateStatusMessage();
                        break;
                }
            });
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Восстанавливаем состояние окна
            RestoreWindowState();

            // Показываем splash screen, если данные еще не загружены
            if (!_isDataLoaded)
            {
                ShowSplashScreen();
            }
            else
            {
                // Устанавливаем начальную страницу
                UpdateCurrentPage();
            }

            _isInitialized = true;
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Сохраняем состояние окна
            SaveWindowState();

            // Обновляем статус пользователя на "не в сети"
            UpdateUserStatusOnClose();

            // Отписываемся от событий
            UnsubscribeFromEvents();
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            // Обновляем видимость элементов в зависимости от состояния окна
            UpdateWindowStateDependentElements();
        }

        private void UnsubscribeFromEvents()
        {
            _mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;
            Loaded -= OnWindowLoaded;
            Closing -= OnWindowClosing;
            StateChanged -= OnWindowStateChanged;

            MainFrame.Navigated -= OnFrameNavigated;
            MainFrame.NavigationFailed -= OnFrameNavigationFailed;
            MainFrame.NavigationStopped -= OnFrameNavigationStopped;
        }

        private void ShowSplashScreen()
        {
            try
            {
                var splashPage = new Page();
                var grid = new Grid
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(255, 30, 30, 30))
                };

                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Анимация загрузки
                var progressRing = new ProgressBar
                {
                    Width = 100,
                    Height = 100,
                    IsIndeterminate = true,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                // Стилизуем ProgressBar
                progressRing.Style = (Style)FindResource(typeof(ProgressBar));

                stackPanel.Children.Add(progressRing);

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Messenger",
                    FontSize = 32,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Загрузка приложения...",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                grid.Children.Add(stackPanel);
                splashPage.Content = grid;

                MainFrame.Navigate(splashPage);
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "ShowSplashScreen");
            }
        }

        private void ShowConnectionError(Exception ex)
        {
            try
            {
                var errorPage = new Page();
                var grid = new Grid
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(255, 30, 30, 30))
                };

                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 400
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "📡",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Ошибка соединения",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Не удалось подключиться к серверу Messenger.",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Проверьте подключение к интернету и повторите попытку.",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                var detailsExpander = new Expander
                {
                    Header = "Подробности ошибки",
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    IsExpanded = false,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                detailsExpander.Content = new TextBlock
                {
                    Text = ex.Message,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.DarkGray,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanel.Children.Add(detailsExpander);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var retryButton = new Button
                {
                    Content = "Повторить",
                    Margin = new Thickness(5),
                    Padding = new Thickness(20, 10, 20, 10)
                };

                retryButton.Click += async (s, e) =>
                {
                    try
                    {
                        ShowSplashScreen();
                        await InitializeApplicationAsync();
                    }
                    catch (Exception retryEx)
                    {
                        ShowConnectionError(retryEx);
                    }
                };

                buttonPanel.Children.Add(retryButton);

                var exitButton = new Button
                {
                    Content = "Выйти",
                    Margin = new Thickness(5),
                    Padding = new Thickness(20, 10, 20, 10)
                };

                exitButton.Click += (s, e) => Application.Current.Shutdown();

                buttonPanel.Children.Add(exitButton);

                stackPanel.Children.Add(buttonPanel);

                grid.Children.Add(stackPanel);
                errorPage.Content = grid;

                MainFrame.Navigate(errorPage);
            }
            catch (Exception errorEx)
            {
                Utils.ErrorHandler.LogException(errorEx, "ShowConnectionError");
            }
        }

        private void UpdateApplicationState()
        {
            if (_isDataLoaded && _isInitialized)
            {
                // Скрываем splash screen и показываем основное содержимое
                UpdateCurrentPage();
            }
        }

        private void UpdateCurrentPage()
        {
            if (!_isInitialized || _mainViewModel.CurrentViewModel == null)
                return;

            var pageType = GetPageTypeForViewModel(_mainViewModel.CurrentViewModel);
            if (pageType != null)
            {
                try
                {
                    // Создаем страницу и устанавливаем DataContext
                    var page = (Page)Activator.CreateInstance(pageType);
                    page.DataContext = _mainViewModel.CurrentViewModel;

                    // Применяем анимацию перехода
                    ApplyPageTransition(page);
                }
                catch (Exception ex)
                {
                    Utils.ErrorHandler.HandleException(ex, "UpdateCurrentPage");
                    ShowErrorPage($"Не удалось загрузить страницу: {ex.Message}");
                }
            }
        }

        private void ApplyPageTransition(Page page)
        {
            try
            {
                // Если Frame пустой, просто устанавливаем страницу
                if (MainFrame.Content == null)
                {
                    MainFrame.Navigate(page);
                    return;
                }

                // Сохраняем текущий контент для анимации
                var oldContent = MainFrame.Content as UIElement;

                // Создаем анимацию перехода
                var transition = new DoubleAnimationUsingKeyFrames();
                transition.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0)));
                transition.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(0.2)));

                transition.Completed += (s, e) =>
                {
                    // Устанавливаем новую страницу
                    MainFrame.Navigate(page);

                    // Анимируем появление новой страницы
                    if (MainFrame.Content is UIElement newContent)
                    {
                        var fadeIn = new DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        newContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    }
                };

                // Запускаем анимацию исчезновения
                if (oldContent != null)
                {
                    oldContent.BeginAnimation(UIElement.OpacityProperty, transition);
                }
                else
                {
                    MainFrame.Navigate(page);
                }
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "ApplyPageTransition");
                MainFrame.Navigate(page);
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

            // Обновляем состояние навигационных кнопок
            UpdateNavigationButtonsState();
        }

        private void OnFrameNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Utils.ErrorHandler.HandleException(e.Exception, "Frame Navigation Failed");
            e.Handled = true;

            ShowErrorPage($"Ошибка навигации: {e.Exception.Message}");
        }

        private void OnFrameNavigationStopped(object sender, NavigationEventArgs e)
        {
            // Логируем остановку навигации
            Utils.ErrorHandler.LogException(new Exception("Navigation stopped by user or system"), "Frame Navigation");
        }

        private void ShowErrorPage(string errorMessage)
        {
            try
            {
                var errorPage = new Page();
                var stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "😞",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Произошла ошибка",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = errorMessage,
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400,
                    TextAlignment = TextAlignment.Center
                });

                var retryButton = new Button
                {
                    Content = "Повторить",
                    Margin = new Thickness(0, 20, 0, 0),
                    Padding = new Thickness(20, 10, 20, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                retryButton.Click += (s, e) => UpdateCurrentPage();

                stackPanel.Children.Add(retryButton);

                errorPage.Content = stackPanel;
                MainFrame.Navigate(errorPage);
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.LogException(ex, "ShowErrorPage");
            }
        }

        private void UpdateWindowTitle()
        {
            if (_mainViewModel.CurrentViewModel is BaseViewModel currentViewModel)
            {
                if (!string.IsNullOrEmpty(currentViewModel.Title))
                {
                    Title = $"{currentViewModel.Title} - Messenger";
                }
                else
                {
                    Title = "Messenger";
                }
            }
            else
            {
                Title = "Messenger";
            }
        }

        private void UpdateBusyState()
        {
            Dispatcher.Invoke(() =>
            {
                // Обновляем состояние курсора в зависимости от IsBusy
                Cursor = _mainViewModel.IsBusy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

                // Блокируем/разблокируем навигационные кнопки
                UpdateNavigationButtonsState();
            });
        }

        private void UpdateNavigationButtonsState()
        {
            // Этот метод будет реализован при добавлении именованных кнопок
            // Пока оставляем как заглушку
        }

        private void UpdateNavigationVisibility()
        {
            // Обновляем видимость элементов навигации в зависимости от состояния аутентификации
            // Реализация будет в XAML через привязки
        }

        private void UpdateUnreadCounter()
        {
            // Обновляем отображение счетчика непрочитанных сообщений
            // Реализация будет в XAML через привязки
        }

        private void UpdateStatusMessage()
        {
            // Обновляем отображение статусного сообщения
            // Реализация будет в XAML через привязки
        }

        private void UpdateWindowStateDependentElements()
        {
            // Обновляем элементы интерфейса в зависимости от состояния окна
            // Например, скрываем/показываем некоторые элементы при полноэкранном режиме
        }

        private void RestoreWindowState()
        {
            try
            {
                // Восстанавливаем размер и положение окна из настроек
                var windowSettings = Utils.WindowSettings.Load();

                if (windowSettings.Width > 0 && windowSettings.Height > 0)
                {
                    Width = windowSettings.Width;
                    Height = windowSettings.Height;
                }

                if (windowSettings.Left >= 0 && windowSettings.Top >= 0)
                {
                    Left = windowSettings.Left;
                    Top = windowSettings.Top;
                }

                if (windowSettings.WindowState != WindowState.Minimized)
                {
                    WindowState = windowSettings.WindowState;
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
                // Сохраняем состояние окна
                var windowSettings = new Utils.WindowSettings
                {
                    WindowState = WindowState,
                    Width = RestoreBounds.Width,
                    Height = RestoreBounds.Height,
                    Left = RestoreBounds.Left,
                    Top = RestoreBounds.Top
                };

                Utils.WindowSettings.Save(windowSettings);
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
                Task.Run(async () =>
                {
                    try
                    {
                        await _databaseService.UpdateUserStatusAsync(
                            _mainViewModel.CurrentUser.Id,
                            false,
                            DateTime.UtcNow
                        );
                    }
                    catch (Exception ex)
                    {
                        Utils.ErrorHandler.LogException(ex, "UpdateUserStatusOnClose");
                    }
                });
            }
        }
    }
}