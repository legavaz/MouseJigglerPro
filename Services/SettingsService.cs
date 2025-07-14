using MouseJigglerPro.MVVM.Model;
using System;
using System.IO;
using System.Text.Json;

namespace MouseJigglerPro.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public SettingsService(string settingsFileName = "settings.json")
        {
            // Store settings in the same directory as the executable
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, settingsFileName);
        }

        public Settings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Return default settings if file doesn't exist
                return new Settings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            catch (Exception)
            {
                // In case of corruption or other errors, return default settings
                return new Settings();
            }
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _options);
                File.WriteAllText(_settingsFilePath, json);
                settings.IsDirty = false;
            }
            catch (Exception)
            {
                // Handle potential exceptions during file write (e.g., permissions)
                // For now, we just ignore it, but logging could be added here.
            }
        }
    }
}