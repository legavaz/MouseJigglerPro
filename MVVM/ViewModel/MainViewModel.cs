using MouseJigglerPro.Core;
using MouseJigglerPro.MVVM.Model;
using MouseJigglerPro.Services;
using MouseJigglerPro.MVVM.View;
using System;
using System.Windows;
using System.Windows.Input;

namespace MouseJigglerPro.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel для главного окна. Управляет состоянием и логикой основного интерфейса.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Сервисы и движки, необходимые для работы ViewModel.
        private readonly SettingsService _settingsService;
        private readonly JiggleEngine _jiggleEngine;
        private readonly ZenModeService _zenModeService;
        private Settings _settings; // Текущие настройки приложения.

        // Свойство для текста статуса, отображаемого в окне.
        private string _statusText = "Неактивен";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // Свойство для текста на кнопке Старт/Стоп.
        private string _toggleButtonText = "Старт";
        public string ToggleButtonText
        {
            get => _toggleButtonText;
            set => SetProperty(ref _toggleButtonText, value);
        }

        // Команды, к которым привязывается View (кнопки, меню и т.д.).
        public ICommand ToggleJiggleCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ShowWindowCommand { get; }
        public ICommand ExitApplicationCommand { get; }

        public MainViewModel()
        {
            // Инициализация сервисов и загрузка настроек.
            _settingsService = new SettingsService();
            _settings = _settingsService.LoadSettings();
            _jiggleEngine = new JiggleEngine(_settings);
            _zenModeService = new ZenModeService(_settings, _jiggleEngine);
            // Подписка на событие изменения состояния Zen Mode.
            _zenModeService.StateChanged += OnZenModeStateChanged;

            // Инициализация команд.
            ToggleJiggleCommand = new RelayCommand(ToggleJiggle);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ShowWindowCommand = new RelayCommand(ShowWindow);
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }

        /// <summary>
        /// Метод, который запускает или останавливает движение мыши.
        /// </summary>
        private void ToggleJiggle(object? parameter)
        {
            // Если движок уже запущен, останавливаем его.
            if (_jiggleEngine.IsRunning || _zenModeService.IsActive)
            {
                if (_settings.IsZenModeEnabled)
                {
                    _zenModeService.Stop();
                    StatusText = "Zen Mode выключен";
                }
                else
                {
                    _jiggleEngine.Stop();
                }
                StatusText = "Неактивен";
                ToggleButtonText = "Старт";
                (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("default.ico");
            }
            // Если движок остановлен, запускаем его.
            else
            {
                if (_settings.IsZenModeEnabled)
                {
                    _zenModeService.Start();
                    StatusText = "Zen Mode (ожидание)";
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("zen.ico");
                }
                else
                {
                    _jiggleEngine.Start();
                    StatusText = "Активен";
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("active.ico");
                }
                ToggleButtonText = "Стоп";
            }
        }

        /// <summary>
        /// Открывает окно настроек.
        /// </summary>
        private void OpenSettings(object? parameter)
        {
            var settingsViewModel = new SettingsViewModel(_settingsService, _settings);
            var settingsWindow = new SettingsWindow
            {
                DataContext = settingsViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Показывает главное окно приложения.
        /// </summary>
        private void ShowWindow(object? parameter)
        {
            System.Windows.Application.Current.MainWindow?.Show();
            System.Windows.Application.Current.MainWindow?.Activate();
        }

        /// <summary>
        /// Корректно завершает работу приложения.
        /// </summary>
        private void ExitApplication(object? parameter)
        {
            // Освобождаем ресурсы перед выходом.
            _zenModeService.Dispose();
            _jiggleEngine.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Обработчик события изменения состояния Zen Mode. Обновляет UI.
        /// </summary>
        private void OnZenModeStateChanged(ZenModeService.ZenState state)
        {
            switch (state)
            {
                case ZenModeService.ZenState.Waiting:
                    StatusText = "Zen Mode (ожидание)";
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("zen.ico");
                    break;
                case ZenModeService.ZenState.Jiggling:
                    StatusText = "Zen Mode (активен)";
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("active.ico");
                    break;
                case ZenModeService.ZenState.Stopped:
                    StatusText = "Неактивен";
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("default.ico");
                    break;
            }
        }
    }

    // Simple ICommand implementation
    /// <summary>
    /// Простая реализация интерфейса ICommand для использования в паттерне MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}