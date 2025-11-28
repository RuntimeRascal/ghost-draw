using System.Text.Json.Serialization;

namespace GhostDraw
{
    /// <summary>
    /// Application settings that persist across sessions
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Brush color in hex format (e.g., "#FF0000" for red)
        /// </summary>
        [JsonPropertyName("brushColor")]
        public string BrushColor { get; set; } = "#FF0000";

        /// <summary>
        /// Brush thickness in pixels
        /// </summary>
        [JsonPropertyName("brushThickness")]
        public double BrushThickness { get; set; } = 3.0;

        /// <summary>
        /// Minimum allowed brush thickness
        /// </summary>
        [JsonPropertyName("minBrushThickness")]
        public double MinBrushThickness { get; set; } = 1.0;

        /// <summary>
        /// Maximum allowed brush thickness
        /// </summary>
        [JsonPropertyName("maxBrushThickness")]
        public double MaxBrushThickness { get; set; } = 20.0;

        /// <summary>
        /// Primary hotkey modifier (e.g., "Control", "Alt", "Shift")
        /// </summary>
        [JsonPropertyName("hotkeyModifier1")]
        public string HotkeyModifier1 { get; set; } = "Control";

        /// <summary>
        /// Secondary hotkey modifier
        /// </summary>
        [JsonPropertyName("hotkeyModifier2")]
        public string HotkeyModifier2 { get; set; } = "Alt";

        /// <summary>
        /// Hotkey key (e.g., "D")
        /// </summary>
        [JsonPropertyName("hotkeyKey")]
        public string HotkeyKey { get; set; } = "D";

        /// <summary>
        /// If true, drawing mode locks on first hotkey press and unlocks on second press
        /// If false, drawing mode is active only while hotkey is held down
        /// </summary>
        [JsonPropertyName("lockDrawingMode")]
        public bool LockDrawingMode { get; set; } = false;

        /// <summary>
        /// Log level (Verbose, Debug, Information, Warning, Error, Fatal)
        /// </summary>
        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Available color palette for quick cycling
        /// </summary>
        [JsonPropertyName("colorPalette")]
        public List<string> ColorPalette { get; set; } = new()
        {
            "#FF0000", // Red
            "#00FF00", // Green
            "#0000FF", // Blue
            "#FFFF00", // Yellow
            "#FF00FF", // Magenta
            "#00FFFF", // Cyan
            "#FFFFFF", // White
            "#000000", // Black
            "#FFA500", // Orange
            "#800080"  // Purple
        };

        /// <summary>
        /// Creates a deep copy of the settings
        /// </summary>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                BrushColor = this.BrushColor,
                BrushThickness = this.BrushThickness,
                MinBrushThickness = this.MinBrushThickness,
                MaxBrushThickness = this.MaxBrushThickness,
                HotkeyModifier1 = this.HotkeyModifier1,
                HotkeyModifier2 = this.HotkeyModifier2,
                HotkeyKey = this.HotkeyKey,
                LockDrawingMode = this.LockDrawingMode,
                LogLevel = this.LogLevel,
                ColorPalette = new List<string>(this.ColorPalette)
            };
        }
    }
}
