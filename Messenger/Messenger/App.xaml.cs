using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Messenger.ViewModels;
using Messenger.Services;
using Messenger.Services.Interfaces;

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
            // Регистрация сервисов
            services.AddSingleton<IAuthService, FirebaseAuthService>();
            services.AddSingleton<IDatabaseService, FirebaseDatabaseService>();

            // Регистрация ViewModels с передачей ServiceProvider
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<UserProfileViewModel>();

            // Регистрация ServiceProvider для использования в ViewModels
            services.AddSingleton<IServiceProvider>(provider => provider);

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