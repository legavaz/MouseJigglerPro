using MouseJigglerPro.MVVM.Model;
using MouseJigglerPro.Services;
using System;
using System.Windows.Input;

namespace MouseJigglerPro.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private Settings _originalSettings;
        private Settings _editableSettings;

        public Settings EditableSettings
        {
            get => _editableSettings;
            set => SetProperty(ref _editableSettings, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetToDefaultCommand { get; }

        // This event will be used to close the window from the ViewModel
        public event Action? RequestClose;

        public SettingsViewModel(SettingsService settingsService, Settings currentSettings)
        {
            _settingsService = settingsService;
            _originalSettings = currentSettings;
            // Create a deep copy for editing to avoid changing the original settings until saved
            _editableSettings = new Settings
            {
                MinIntervalSeconds = _originalSettings.MinIntervalSeconds,
                MaxIntervalSeconds = _originalSettings.MaxIntervalSeconds,
                JiggleDistance = _originalSettings.JiggleDistance,
                IsZenModeEnabled = _originalSettings.IsZenModeEnabled,
                ZenModeIdleTimeSeconds = _originalSettings.ZenModeIdleTimeSeconds,
                IsPhantomKeystrokeEnabled = _originalSettings.IsPhantomKeystrokeEnabled,
                StartWithWindows = _originalSettings.StartWithWindows,
                StartMinimized = _originalSettings.StartMinimized
            };

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);
        }

        private void Save(object? parameter)
        {
            // Copy edited values back to the original settings object
            _originalSettings.MinIntervalSeconds = EditableSettings.MinIntervalSeconds;
            _originalSettings.MaxIntervalSeconds = EditableSettings.MaxIntervalSeconds;
            _originalSettings.JiggleDistance = EditableSettings.JiggleDistance;
            _originalSettings.IsZenModeEnabled = EditableSettings.IsZenModeEnabled;
            _originalSettings.ZenModeIdleTimeSeconds = EditableSettings.ZenModeIdleTimeSeconds;
            _originalSettings.IsPhantomKeystrokeEnabled = EditableSettings.IsPhantomKeystrokeEnabled;
            _originalSettings.StartWithWindows = EditableSettings.StartWithWindows;
            _originalSettings.StartMinimized = EditableSettings.StartMinimized;

            _settingsService.SaveSettings(_originalSettings);
            RequestClose?.Invoke();
        }

        private void Cancel(object? parameter)
        {
            RequestClose?.Invoke();
        }

        private void ResetToDefault(object? parameter)
        {
            // Create a new default settings object
            var defaultSettings = new Settings();
            // Update the UI
            EditableSettings = defaultSettings;
            // Apply and save these default settings immediately
            Save(parameter);
        }
    }
}