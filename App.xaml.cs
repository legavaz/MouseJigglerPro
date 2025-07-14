using MouseJigglerPro.Services;
using System.Windows;

namespace MouseJigglerPro
{
    /// <summary>
    /// Основной класс приложения, который управляет его жизненным циклом.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// Этот метод вызывается при запуске приложения.
        /// Он заменяет стандартный запуск через StartupUri в App.xaml,
        /// чтобы получить больше контроля над процессом инициализации.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Создаем экземпляр сервиса для работы с настройками.
                var settingsService = new SettingsService();
                // Загружаем настройки из файла (или используем значения по умолчанию).
                var settings = settingsService.LoadSettings();

                // Создаем главное окно приложения.
                var mainWindow = new MouseJigglerPro.MVVM.View.MainWindow();
                // Явно назначаем его главным окном. Это важно для правильной работы приложения.
                this.MainWindow = mainWindow;

                // Проверяем настройку "Запускать свернутым".
                // Окно будет показано только если эта опция отключена.
                if (!settings.StartMinimized)
                {
                    // Принудительно показываем окно, устанавливаем его состояние в "Normal"
                    // и активируем его, чтобы оно появилось на переднем плане.
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                // Если на любом этапе запуска произойдет ошибка, мы покажем ее в диалоговом окне.
                // Это поможет диагностировать проблему "тихого" падения.
                System.Windows.MessageBox.Show($"Произошла критическая ошибка при запуске:\n\n{ex.Message}\n\n{ex.StackTrace}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                // Завершаем работу приложения.
                this.Shutdown();
            }
        }
    }
}