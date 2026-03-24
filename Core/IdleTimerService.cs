using System;
using System.Threading;
using System.Threading.Tasks;

namespace MouseJigglerPro.Core
{
    /// <summary>
    /// Сервис для отслеживания времени бездействия пользователя.
    /// Каждую секунду обновляет значение IdleTimeSeconds, которое сбрасывается в 0
    /// при любой активности пользователя (движение мыши, нажатие клавиш).
    /// </summary>
    public class IdleTimerService : IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;
        
        /// <summary>
        /// Событие, возникающее при изменении времени бездействия.
        /// </summary>
        public event Action<int>? IdleTimeChanged;
        
        /// <summary>
        /// Текущее время бездействия в секундах.
        /// </summary>
        public int IdleTimeSeconds { get; private set; }
        
        /// <summary>
        /// Указывает, активен ли сервис мониторинга.
        /// </summary>
        public bool IsRunning { get; private set; }

        public IdleTimerService()
        {
        }

        /// <summary>
        /// Запускает сервис мониторинга бездействия.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            IsRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Останавливает сервис мониторинга.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;

            _cancellationTokenSource?.Cancel();
            IsRunning = false;
            IdleTimeSeconds = 0;
            IdleTimeChanged?.Invoke(0);
        }

        /// <summary>
        /// Асинхронный цикл мониторинга бездействия.
        /// Каждую секунду проверяет время с последнего ввода пользователя.
        /// </summary>
        private async Task MonitorLoop(CancellationToken token)
        {
            int lastIdleTime = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);

                    // Получаем время с момента последнего ввода в миллисекундах
                    uint idleTimeMs = (uint)Environment.TickCount - PInvokeHelper.GetLastInputTime();
                    
                    // Конвертируем в секунды
                    int currentIdleSeconds = (int)(idleTimeMs / 1000);
                    
                    // Если время бездействия меньше 1 секунды, сбрасываем счетчик
                    if (currentIdleSeconds < 1)
                    {
                        currentIdleSeconds = 0;
                    }
                    
                    // Обновляем значение, только если оно изменилось
                    if (currentIdleSeconds != lastIdleTime)
                    {
                        lastIdleTime = currentIdleSeconds;
                        IdleTimeSeconds = currentIdleSeconds;
                        IdleTimeChanged?.Invoke(IdleTimeSeconds);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Принудительно сбрасывает счетчик бездействия.
        /// </summary>
        public void Reset()
        {
            IdleTimeSeconds = 0;
            IdleTimeChanged?.Invoke(0);
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
