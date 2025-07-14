using MouseJigglerPro.MVVM.ViewModel;
using System.Windows;

namespace MouseJigglerPro.MVVM.View
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.RequestClose += Close;
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.RequestClose -= Close;
            }
        }
    }
}