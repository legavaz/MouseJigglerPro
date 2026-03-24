using System;
using System.Threading;
using System.Threading.Tasks;

namespace MouseJigglerPro.Core
{
    /// <summary>
    /// Сервис для отслеживания времени бездействия пользователя.
    /// Каждую секунду обновляет значение IdleTimeSeconds, которое сбрасывается в 0
    /// при любой активности пользователя (движение мыши, нажатие клавиш).
    /// Игнорирует программный ввод от JiggleEngine.
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
        
        /// <summary>
        /// Время (в мс) когда начался последний период программного ввода.
        /// Используется для корректировки времени бездействия.
        /// </summary>
        private uint _programmaticInputStartTime;
        
        /// <summary>
        /// Накопленное время программного ввода (в мс).
        /// </summary>
        private uint _accumulatedProgrammaticTime;

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
            _programmaticInputStartTime = 0;
            _accumulatedProgrammaticTime = 0;
            IdleTimeChanged?.Invoke(0);
        }

        /// <summary>
        /// Асинхронный цикл мониторинга бездействия.
        /// Каждую секунду проверяет время с последнего ввода пользователя.
        /// Игнорирует программный ввод от JiggleEngine.
        /// </summary>
        private async Task MonitorLoop(CancellationToken token)
        {
            int lastIdleTime = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);

                    // Проверяем, начался ли программный ввод
                    if (JiggleEngine.IsProgrammaticInputActive && _programmaticInputStartTime == 0)
                    {
                        _programmaticInputStartTime = (uint)Environment.TickCount;
                    }
                    // Проверяем, завершился ли программный ввод
                    else if (!JiggleEngine.IsProgrammaticInputActive && _programmaticInputStartTime != 0)
                    {
                        // Накапливаем время программного ввода
                        uint programmaticDuration = (uint)Environment.TickCount - _programmaticInputStartTime;
                        _accumulatedProgrammaticTime += programmaticDuration;
                        _programmaticInputStartTime = 0;
                    }

                    // Получаем время с момента последнего РЕАЛЬНОГО ввода в миллисекундах
                    uint idleTimeMs = (uint)Environment.TickCount - PInvokeHelper.GetLastRealInputTime();
                    
                    // Вычитаем накопленное время программного ввода
                    if (idleTimeMs > _accumulatedProgrammaticTime)
                    {
                        idleTimeMs -= _accumulatedProgrammaticTime;
                    }
                    else
                    {
                        idleTimeMs = 0;
                    }
                    
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
            _programmaticInputStartTime = 0;
            _accumulatedProgrammaticTime = 0;
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
