using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Messenger.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private string _title = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        protected void ExecuteWithBusyState(Action action)
        {
            try
            {
                IsBusy = true;
                action?.Invoke();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task ExecuteWithBusyStateAsync(Func<Task> action)
        {
            try
            {
                IsBusy = true;
                await action?.Invoke();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected RelayCommand CreateCommand(Action execute, Func<bool>? canExecute = null)
        {
            return new RelayCommand(execute, canExecute);
        }

        protected RelayCommand<T> CreateCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            return new RelayCommand<T>(execute, canExecute);
        }

        protected AsyncRelayCommand CreateAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            return new AsyncRelayCommand(execute, canExecute);
        }

        protected AsyncRelayCommand<T> CreateAsyncCommand<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null)
        {
            return new AsyncRelayCommand<T>(execute, canExecute);
        }
    }
}