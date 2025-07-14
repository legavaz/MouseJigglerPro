using System.Text.Json.Serialization;

namespace MouseJigglerPro.MVVM.Model
{
    public class Settings
    {
        // Jiggle Engine Settings
        public int MinIntervalSeconds { get; set; } = 10;
        public int MaxIntervalSeconds { get; set; } = 45;
        public int JiggleDistance { get; set; } = 5;

        // Zen Mode Settings
        public bool IsZenModeEnabled { get; set; } = true;
        public int ZenModeIdleTimeSeconds { get; set; } = 60;

        // Additional Emulation
        public bool IsPhantomKeystrokeEnabled { get; set; } = false;

        // Application Settings
        public bool StartWithWindows { get; set; } = false; // Note: Implementation requires more than just a setting.
        public bool StartMinimized { get; set; } = false;

        [JsonIgnore] // This property should not be saved in settings file
        public bool IsDirty { get; set; } = false;
    }
}