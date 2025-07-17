using MouseJigglerPro.MVVM.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MouseJigglerPro.Core
{
    /// <summary>
    /// Основной движок, отвечающий за имитацию движения мыши.
    /// </summary>
    public class JiggleEngine : IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource; // Источник токенов для отмены асинхронных операций.
        private readonly Settings _settings; // Настройки, влияющие на поведение движка.
        private readonly Random _random = new(); // Генератор случайных чисел.

        /// <summary>
        /// Указывает, запущен ли в данный момент движок.
        /// </summary>
        public bool IsRunning { get; private set; }

        public JiggleEngine(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Запускает асинхронный цикл движения мыши.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return; // Не запускать, если уже запущен.

            IsRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            // Запускаем цикл в фоновом потоке, чтобы не блокировать UI.
            Task.Run(() => JiggleLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Останавливает цикл движения мыши.
        /// </summary>
        public event Action? StoppedByUser;
        public void Stop()
        {
            if (!IsRunning) return; // Не останавливать, если уже остановлен.

            _cancellationTokenSource?.Cancel(); // Отправляем сигнал отмены.
            IsRunning = false;
            StoppedByUser?.Invoke();
        }

        /// <summary>
        /// Асинхронный цикл, который периодически вызывает движение мыши.
        /// </summary>
        private async Task JiggleLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Генерируем случайную задержку в заданном диапазоне.
                    int delay = _random.Next(_settings.MinIntervalSeconds * 1000, _settings.MaxIntervalSeconds * 1000);
                    // Ожидаем в течение сгенерированной задержки.
                    await Task.Delay(delay, token);

                    // Проверяем, не был ли запрошен выход из цикла.
                    if (token.IsCancellationRequested) break;

                    // Выполняем движение мыши.
                    JiggleMouse();
                }
                catch (TaskCanceledException)
                {
                    // Выходим из цикла, если задача была отменена.
                    break;
                }
            }
        }


        /// <summary>
        /// Имитирует однократное движение мыши "туда-обратно" по кривой Безье.
        /// </summary>
        private async void JiggleMouse()
        {
            // Генерируем случайные точки для построения кривой Безье.
            // Это делает движение более плавным и "человечным".
            var startPoint = new System.Drawing.Point(0, 0); // Движение относительное.
            var endPoint = new System.Drawing.Point(
                _random.Next(-_settings.JiggleDistance, _settings.JiggleDistance),
                _random.Next(-_settings.JiggleDistance, _settings.JiggleDistance)
            );
            var controlPoint1 = new System.Drawing.Point(
                _random.Next(-_settings.JiggleDistance * 2, _settings.JiggleDistance * 2),
                _random.Next(-_settings.JiggleDistance * 2, _settings.JiggleDistance * 2)
            );
            var controlPoint2 = new System.Drawing.Point(
                _random.Next(-_settings.JiggleDistance * 2, _settings.JiggleDistance * 2),
                _random.Next(-_settings.JiggleDistance * 2, _settings.JiggleDistance * 2)
            );

            // Анимируем движение по кривой к конечной точке.
            await AnimateMouseMovement(startPoint, controlPoint1, controlPoint2, endPoint, 100); // Длительность 100 мс.

            // Небольшая пауза.
            await Task.Delay(50);
            // Анимируем движение обратно в начальную точку.
            await AnimateMouseMovement(endPoint, controlPoint2, controlPoint1, startPoint, 100);

            // Если включена опция, имитируем нажатие "фантомной" клавиши (F15).
            if (_settings.IsPhantomKeystrokeEnabled)
            {
                PInvokeHelper.SendPhantomKeystroke();
            }
        }

        /// <summary>
        /// Анимирует движение мыши по заданной кривой Безье.
        /// </summary>
        private async Task AnimateMouseMovement(System.Drawing.Point p0, System.Drawing.Point p1, System.Drawing.Point p2, System.Drawing.Point p3, int durationMs)
        {
            int steps = 20; // Количество шагов анимации.
            var lastPoint = p0;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                var pointOnCurve = GetPointOnBezierCurve(p0, p1, p2, p3, t);
                
                // Вычисляем относительное смещение.
                int dx = pointOnCurve.X - lastPoint.X;
                int dy = pointOnCurve.Y - lastPoint.Y;

                // Отправляем команду на смещение курсора.
                PInvokeHelper.SendMouseInput(dx, dy, PInvokeHelper.MOUSEEVENTF_MOVE);
                lastPoint = pointOnCurve;
                // Небольшая задержка между шагами для создания эффекта анимации.
                await Task.Delay(durationMs / steps);
            }
        }

        /// <summary>
        /// Вычисляет точку на кубической кривой Безье.
        /// </summary>
        private System.Drawing.Point GetPointOnBezierCurve(System.Drawing.Point p0, System.Drawing.Point p1, System.Drawing.Point p2, System.Drawing.Point p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            float x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            float y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

            return new System.Drawing.Point((int)x, (int)y);
        }

        /// <summary>
        /// Освобождает ресурсы, используемые движком.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}