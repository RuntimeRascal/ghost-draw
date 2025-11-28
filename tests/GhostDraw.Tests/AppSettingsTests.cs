using Xunit;

namespace GhostDraw.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void AppSettings_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var settings = new AppSettings();

            // Assert
            Assert.Equal("#FF0000", settings.BrushColor);
            Assert.Equal(3.0, settings.BrushThickness);
            Assert.Equal(1.0, settings.MinBrushThickness);
            Assert.Equal(20.0, settings.MaxBrushThickness);
            Assert.Equal(new List<int> { 0xA2, 0xA4, 0x44 }, settings.HotkeyVirtualKeys); // Ctrl+Alt+D
            Assert.Equal("Ctrl + Alt + D", settings.HotkeyDisplayName);
            Assert.False(settings.LockDrawingMode);
            Assert.Equal(10, settings.ColorPalette.Count);
        }

        [Fact]
        public void AppSettings_ColorPalette_ShouldContainExpectedColors()
        {
            // Arrange & Act
            var settings = new AppSettings();

            // Assert
            Assert.Contains("#FF0000", settings.ColorPalette); // Red
            Assert.Contains("#00FF00", settings.ColorPalette); // Green
            Assert.Contains("#0000FF", settings.ColorPalette); // Blue
            Assert.Contains("#FFFF00", settings.ColorPalette); // Yellow
            Assert.Contains("#FF00FF", settings.ColorPalette); // Magenta
            Assert.Contains("#00FFFF", settings.ColorPalette); // Cyan
            Assert.Contains("#FFFFFF", settings.ColorPalette); // White
            Assert.Contains("#000000", settings.ColorPalette); // Black
            Assert.Contains("#FFA500", settings.ColorPalette); // Orange
            Assert.Contains("#800080", settings.ColorPalette); // Purple
        }

        [Fact]
        public void AppSettings_Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new AppSettings
            {
                BrushColor = "#0000FF",
                BrushThickness = 5.0,
                LockDrawingMode = true
            };

            // Act
            var clone = original.Clone();
            clone.BrushColor = "#00FF00";
            clone.BrushThickness = 10.0;

            // Assert
            Assert.Equal("#0000FF", original.BrushColor);
            Assert.Equal(5.0, original.BrushThickness);
            Assert.Equal("#00FF00", clone.BrushColor);
            Assert.Equal(10.0, clone.BrushThickness);
        }

        [Fact]
        public void AppSettings_Clone_ShouldCopyColorPalette()
        {
            // Arrange
            var original = new AppSettings();
            original.ColorPalette.Add("#123456");

            // Act
            var clone = original.Clone();
            clone.ColorPalette.Add("#ABCDEF");

            // Assert
            Assert.Equal(11, original.ColorPalette.Count);
            Assert.Equal(12, clone.ColorPalette.Count);
            Assert.Contains("#123456", original.ColorPalette);
            Assert.DoesNotContain("#ABCDEF", original.ColorPalette);
        }

        [Theory]
        [InlineData(1.0, 20.0)]
        [InlineData(0.5, 50.0)]
        [InlineData(5.0, 15.0)]
        public void AppSettings_BrushThicknessRange_ShouldAcceptValidValues(double min, double max)
        {
            // Arrange & Act
            var settings = new AppSettings
            {
                MinBrushThickness = min,
                MaxBrushThickness = max
            };

            // Assert
            Assert.Equal(min, settings.MinBrushThickness);
            Assert.Equal(max, settings.MaxBrushThickness);
        }

        [Theory]
        [InlineData("#FF0000")]
        [InlineData("#00FF00")]
        [InlineData("#0000FF")]
        [InlineData("#FFFFFF")]
        [InlineData("#000000")]
        public void AppSettings_BrushColor_ShouldAcceptValidHexColors(string color)
        {
            // Arrange & Act
            var settings = new AppSettings
            {
                BrushColor = color
            };

            // Assert
            Assert.Equal(color, settings.BrushColor);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppSettings_LockDrawingMode_ShouldAcceptBooleanValues(bool lockMode)
        {
            // Arrange & Act
            var settings = new AppSettings
            {
                LockDrawingMode = lockMode
            };

            // Assert
            Assert.Equal(lockMode, settings.LockDrawingMode);
        }
    }
}
