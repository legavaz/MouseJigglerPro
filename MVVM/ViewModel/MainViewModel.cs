using MouseJigglerPro.Core;
using MouseJigglerPro.MVVM.Model;
using MouseJigglerPro.Services;
using MouseJigglerPro.MVVM.View;
using System;
using System.Threading;
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
        private readonly IdleTimerService _idleTimerService;
        private Settings _settings; // Текущие настройки приложения.

        // Свойство для времени бездействия пользователя в секундах.
        private int _idleTimeSeconds = 0;
        public int IdleTimeSeconds
        {
            get => _idleTimeSeconds;
            set => SetProperty(ref _idleTimeSeconds, value);
        }

        // Свойство для текста статуса, отображаемого в окне.
        private string _statusText = "Неактивен";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // Свойство для текущего языка ввода
        private string _inputLanguage = "EN";
        public string InputLanguage
        {
            get => _inputLanguage;
            set => SetProperty(ref _inputLanguage, value);
        }

        private bool _isJigglingActive;
        public bool IsJigglingActive
        {
            get => _isJigglingActive;
            set
            {
                if (SetProperty(ref _isJigglingActive, value))
                {
                    ToggleJiggle();
                }
            }
        }

        // Команды, к которым привязывается View (кнопки, меню и т.д.).
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
            _idleTimerService = new IdleTimerService();
            
            // Подписка на событие изменения состояния Zen Mode.
            _zenModeService.StateChanged += OnZenModeStateChanged;
            
            // Подписка на событие изменения времени бездействия.
            _idleTimerService.IdleTimeChanged += OnIdleTimeChanged;
            
            // Подписка на событие изменения языка ввода
            Core.PInvokeHelper.InputLanguageChanged += OnInputLanguageChanged;
            
            // Запускаем мониторинг бездействия.
            _idleTimerService.Start();

            // Инициализация команд.
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ShowWindowCommand = new RelayCommand(ShowWindow);
            ExitApplicationCommand = new RelayCommand(ExitApplication);

            // Инициализация языка ввода
            UpdateInputLanguage();
        }

        /// <summary>
        /// Обновляет текущий язык ввода на основе активного окна системы.
        /// </summary>
        public void UpdateInputLanguage()
        {
            InputLanguage = Core.PInvokeHelper.GetCurrentInputLanguage();
        }

        /// <summary>
        /// Обработчик события изменения языка ввода.
        /// </summary>
        private void OnInputLanguageChanged(string language)
        {
            InputLanguage = language;
        }

        /// <summary>
        /// Метод, который запускает или останавливает движение мыши.
        /// При включении всегда ожидает таймаут бездействия перед началом эмуляции.
        /// </summary>
        private void ToggleJiggle()
        {
            if (IsJigglingActive)
            {
                // Всегда показываем ожидание при включении
                // Движение начнётся автоматически когда idle time достигнет таймаута
                StatusText = "Ожидание бездействия...";
                (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("zen.ico");
                
                // Запускаем мониторинг бездействия для автоматического старта
                StartIdleMonitoring();
            }
            else
            {
                // Останавливаем все сервисы при выключении
                StopAllServices();
                StatusText = "Неактивен";
                (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("default.ico");
            }
        }
        
        /// <summary>
        /// Запускает мониторинг бездействия для автоматического начала эмуляции.
        /// </summary>
        private void StartIdleMonitoring()
        {
            // Если уже запущен мониторинг - не запускать повторно
            if (_idleMonitoringActive) return;
            
            _idleMonitoringActive = true;
            _idleMonitoringCancel = new CancellationTokenSource();
            
            Task.Run(() => IdleMonitoringLoop(_idleMonitoringCancel.Token));
        }
        
        private bool _idleMonitoringActive;
        private CancellationTokenSource? _idleMonitoringCancel;
        
        /// <summary>
        /// Цикл мониторинга бездействия. Запускает движение мыши когда idle time достигает таймаута,
        /// и останавливает когда пользователь активен.
        /// </summary>
        private async Task IdleMonitoringLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsJigglingActive)
            {
                try
                {
                    await Task.Delay(1000, token);
                    
                    // Проверяем текущий idle time (реальный ввод)
                    uint idleTimeMs = (uint)Environment.TickCount - Core.PInvokeHelper.GetLastRealInputTime();
                    int currentIdleSeconds = (int)(idleTimeMs / 1000);
                    
                    // Проверяем условие: idle time >= таймаут из настроек
                    // Учитываем, что программное движение мыши (от JiggleEngine) не должно
                    // сбрасывать счетчик бездействия
                    bool shouldJiggle = currentIdleSeconds >= _settings.ZenModeIdleTimeSeconds || _jiggleEngine.IsJiggling;
                    
                    if (shouldJiggle)
                    {
                        // Запускаем движок если ещё не запущен
                        if (!_jiggleEngine.IsRunning)
                        {
                            _jiggleEngine.Start();
                            
                            // Обновляем UI в главном потоке
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "Активен";
                                (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("active.ico");
                            });
                        }
                    }
                    else
                    {
                        // Пользователь активен - останавливаем движок если запущен
                        if (_jiggleEngine.IsRunning)
                        {
                            _jiggleEngine.Stop();
                            
                            // Обновляем UI в главном потоке
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "Ожидание бездействия...";
                                (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("zen.ico");
                            });
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            
            _idleMonitoringActive = false;
        }
        
        /// <summary>
        /// Останавливает все активные сервисы.
        /// </summary>
        private void StopAllServices()
        {
            // Останавливаем мониторинг бездействия
            if (_idleMonitoringCancel != null)
            {
                _idleMonitoringCancel.Cancel();
                _idleMonitoringCancel.Dispose();
                _idleMonitoringCancel = null;
            }
            _idleMonitoringActive = false;
            
            // Останавливаем ZenMode если активен
            if (_zenModeService.IsActive)
            {
                _zenModeService.Stop();
            }
            
            // Останавливаем JiggleEngine если запущен
            if (_jiggleEngine.IsRunning)
            {
                _jiggleEngine.Stop();
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
            StopAllServices(); // Останавливаем мониторинг бездействия и все сервисы
            _idleTimerService.Dispose();
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
                    IsJigglingActive = false; // Ensure toggle is off
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SetTrayIcon("default.ico");
                    break;
            }
        }

        /// <summary>
        /// Обработчик события изменения времени бездействия.
        /// </summary>
        private void OnIdleTimeChanged(int idleTimeSeconds)
        {
            IdleTimeSeconds = idleTimeSeconds;
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