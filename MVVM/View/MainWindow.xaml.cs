using MouseJigglerPro.MVVM.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;

namespace MouseJigglerPro.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml.
    /// Этот класс управляет поведением главного окна и иконкой в системном трее.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Поле для хранения иконки в системном трее.
        private readonly NotifyIcon _notifyIcon;
        private bool _isWindowLoaded = false;

        public MainWindow()
        {
            // Инициализирует компоненты, определенные в XAML.
             InitializeComponent();

            // Создаем и настраиваем иконку для системного трея.
            _notifyIcon = new NotifyIcon
            {
                Visible = true, // Иконка будет видна сразу после создания.
                Text = "Mouse Jiggler Pro" // Текст, который появляется при наведении на иконку.
            };
            
            // Устанавливаем иконку по умолчанию при запуске.
            SetTrayIcon("default.ico");

            // Добавляем обработчик двойного клика по иконке в трее.
            _notifyIcon.DoubleClick += (s, e) =>
            {
                // При двойном клике окно показывается и переводится в нормальное состояние.
                Show();
                WindowState = WindowState.Normal;
            };

            // Настраиваем контекстное меню для иконки в трее.
            SetupContextMenu();

            // Устанавливаем флаг после полной загрузки окна.
            this.Loaded += (s, e) => { _isWindowLoaded = true; };
        }

        /// <summary>
        /// Создает и настраивает контекстное меню, которое появляется при клике правой кнопкой мыши по иконке в трее.
        /// </summary>
        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            // Получаем доступ к ViewModel, чтобы привязать команды к пунктам меню.
            var mainViewModel = DataContext as MainViewModel;

            if (mainViewModel != null)
            {
                var startStopItem = new ToolStripMenuItem("Старт");
                startStopItem.Click += (s, e) => mainViewModel.IsJigglingActive = !mainViewModel.IsJigglingActive;

                // Обновляем текст пункта меню при изменении состояния в ViewModel
                mainViewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.IsJigglingActive))
                    {
                        startStopItem.Text = mainViewModel.IsJigglingActive ? "Стоп" : "Старт";
                    }
                };

                var showItem = new ToolStripMenuItem("Показать окно");
                showItem.Click += (s, e) => mainViewModel.ShowWindowCommand.Execute(null);

                var exitItem = new ToolStripMenuItem("Выход");
                exitItem.Click += (s, e) => mainViewModel.ExitApplicationCommand.Execute(null);

                contextMenu.Items.Add(startStopItem);
                contextMenu.Items.Add(new ToolStripSeparator()); // Разделитель
                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(exitItem);
            }

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Вспомогательный метод для получения URI ресурса иконки.
        /// </summary>
        private Uri GetIconUri(string iconName)
        {
            // Иконки должны быть добавлены в проект как ресурсы в папку Assets/Icons.
            return new Uri($"pack://application:,,,/Assets/Icons/{iconName}");
        }

        /// <summary>
        /// Публичный метод, который позволяет ViewModel изменять иконку в трее.
        /// </summary>
        public void SetTrayIcon(string iconName)
        {
            try
            {
                var iconUri = GetIconUri(iconName);
                // Пытаемся загрузить иконку из ресурсов приложения.
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                }
                else
                {
                    // Если по какой-то причине streamInfo равен null, используем системную иконку.
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                // Если происходит исключение (например, ресурс не найден),
                // перехватываем его и устанавливаем стандартную системную иконку.
                // Это предотвращает сбой приложения.
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }

        /// <summary>
        /// Переопределенный метод, который вызывается при изменении состояния окна (свернуто, развернуто и т.д.).
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            // Если окно сворачивается, оно скрывается с панели задач.
            // Доступ к нему остается через иконку в трее.
            if (_isWindowLoaded && WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        /// <summary>
        /// Переопределенный метод, который вызывается перед закрытием окна.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            // Освобождаем ресурсы, занимаемые иконкой в трее, чтобы она не "зависла" после закрытия приложения.
            _notifyIcon.Dispose();
            base.OnClosing(e);
        }
    }
}