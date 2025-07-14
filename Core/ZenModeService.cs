using MouseJigglerPro.MVVM.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MouseJigglerPro.Core
{
    /// <summary>
    /// Сервис, реализующий "умный" режим Zen Mode.
    /// В этом режиме движение мыши активируется только тогда, когда пользователь неактивен
    /// и на переднем плане нет полноэкранного приложения.
    /// </summary>
    public class ZenModeService : IDisposable
    {
        private readonly Settings _settings;
        private readonly JiggleEngine _jiggleEngine;
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Событие, которое возникает при изменении состояния Zen Mode.
        /// </summary>
        public event Action<ZenState>? StateChanged;
        
        /// <summary>
        /// Возможные состояния Zen Mode.
        /// </summary>
        public enum ZenState { Waiting, Jiggling, Stopped }

        /// <summary>
        /// Указывает, активен ли в данный момент сервис Zen Mode.
        /// </summary>
        public bool IsActive { get; private set; }

        public ZenModeService(Settings settings, JiggleEngine jiggleEngine)
        {
            _settings = settings;
            _jiggleEngine = jiggleEngine;
        }

        /// <summary>
        /// Запускает сервис Zen Mode.
        /// </summary>
        public void Start()
        {
            // Не запускать, если уже активен или выключен в настройках.
            if (IsActive || !_settings.IsZenModeEnabled) return;

            IsActive = true;
            _cancellationTokenSource = new CancellationTokenSource();
            // Запускаем цикл мониторинга в фоновом потоке.
            Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Останавливает сервис Zen Mode.
        /// </summary>
        public void Stop()
        {
            if (!IsActive) return;

            _cancellationTokenSource?.Cancel();
            IsActive = false;

            // Убеждаемся, что основной движок также остановлен, если он был запущен этим сервисом.
            if (_jiggleEngine.IsRunning)
            {
                _jiggleEngine.Stop();
            }
        }

        /// <summary>
        /// Асинхронный цикл, который отслеживает активность пользователя.
        /// </summary>
        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Проверяем состояние каждую секунду.
                    await Task.Delay(1000, token);

                    // Получаем время с момента последнего ввода пользователя.
                    uint idleTime = (uint)Environment.TickCount - PInvokeHelper.GetLastInputTime();
                    // Проверяем, превышает ли время простоя заданное в настройках.
                    bool isUserIdle = idleTime > _settings.ZenModeIdleTimeSeconds * 1000;
                    // Проверяем, является ли активное окно полноэкранным.
                    bool isFullScreen = PInvokeHelper.IsForegroundWindowFullScreen();

                    // Условие для активации движения: пользователь неактивен И окно не полноэкранное.
                    if (isUserIdle && !isFullScreen)
                    {
                        // Запускаем движок, только если он еще не запущен.
                        if (!_jiggleEngine.IsRunning)
                        {
                            _jiggleEngine.Start();
                            // Уведомляем подписчиков об изменении состояния.
                            StateChanged?.Invoke(ZenState.Jiggling);
                        }
                    }
                    else
                    {
                        // Если условие не выполняется, останавливаем движок, если он был запущен.
                        if (_jiggleEngine.IsRunning)
                        {
                            _jiggleEngine.Stop();
                            // Уведомляем подписчиков об изменении состояния.
                            StateChanged?.Invoke(ZenState.Waiting);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Освобождает ресурсы, используемые сервисом.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}