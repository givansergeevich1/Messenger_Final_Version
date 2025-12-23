using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Messenger.ViewModels;
using Messenger.Services;

namespace Messenger
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Регистрация сервисов будет добавлена позже
            services.AddSingleton<MainViewModel>();

            // Регистрация главного окна
            services.AddSingleton<MainWindow>();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow?.Show();
        }
    }
}