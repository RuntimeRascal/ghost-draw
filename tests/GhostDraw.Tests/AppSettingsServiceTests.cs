using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;
using System;
using System.Text.Json;
using GhostDraw.Services;
using System.Collections.Generic;

namespace GhostDraw.Tests
{
    public class AppSettingsServiceTests : IDisposable
    {
        private readonly string _testSettingsPath;
        private readonly string _testDirectory;
        private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
        private readonly string _actualSettingsPath;

        public AppSettingsServiceTests()
        {
            _mockLogger = new Mock<ILogger<AppSettingsService>>();
            
            // Create a temporary directory for test settings
            _testDirectory = Path.Combine(Path.GetTempPath(), $"GhostDrawTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testSettingsPath = Path.Combine(_testDirectory, "settings.json");

            // Determine the actual settings path used by AppSettingsService
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsDirectory = Path.Combine(appData, "GhostDraw");
            _actualSettingsPath = Path.Combine(settingsDirectory, "settings.json");

            // Clean up any existing settings file before tests to ensure clean state
            CleanupSettingsFile();
        }

        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            // Also cleanup the actual settings file to prevent pollution between test runs
            CleanupSettingsFile();
        }

        /// <summary>
        /// Deletes the actual settings file to ensure tests start with a clean slate
        /// </summary>
        private void CleanupSettingsFile()
        {
            try
            {
                if (File.Exists(_actualSettingsPath))
                {
                    File.Delete(_actualSettingsPath);
                }
            }
            catch
            {
                // Ignore if file is locked or doesn't exist
            }
        }

        /// <summary>
        /// Helper to create a testable service by using the actual LocalApplicationData,
        /// but we'll verify behavior through public API
        /// </summary>
        private AppSettingsService CreateService()
        {
            return new AppSettingsService(_mockLogger.Object);
        }

        [Fact]
        public void AppSettingsService_ShouldCreateWithDefaultSettings()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            var settings = service.CurrentSettings;
            Assert.NotNull(settings);
            Assert.Equal("#FF0000", settings.BrushColor);
            Assert.Equal(3.0, settings.BrushThickness);
            Assert.False(settings.LockDrawingMode);
        }

        [Fact]
        public void CurrentSettings_ShouldReturnClonedCopy()
        {
            // Arrange
            var service = CreateService();
            
            // Act
            var settings1 = service.CurrentSettings;
            var settings2 = service.CurrentSettings;
            settings1.BrushColor = "#FFFFFF";

            // Assert - Verify they are different instances
            Assert.NotSame(settings1, settings2);
            Assert.Equal("#FF0000", settings2.BrushColor); // settings2 should still have default
        }

        [Theory]
        [InlineData("#FF0000")]
        [InlineData("#00FF00")]
        [InlineData("#0000FF")]
        public void SetBrushColor_ShouldUpdateColor(string color)
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetBrushColor(color);

            // Assert
            Assert.Equal(color, service.CurrentSettings.BrushColor);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(5.0)]
        [InlineData(10.0)]
        [InlineData(20.0)]
        public void SetBrushThickness_ShouldUpdateThickness(double thickness)
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetBrushThickness(thickness);

            // Assert
            Assert.Equal(thickness, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThickness_ShouldClampBelowMinimum()
        {
            // Arrange
            var service = CreateService();
            var minThickness = service.CurrentSettings.MinBrushThickness;

            // Act
            service.SetBrushThickness(minThickness - 5.0);

            // Assert
            Assert.Equal(minThickness, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThickness_ShouldClampAboveMaximum()
        {
            // Arrange
            var service = CreateService();
            var maxThickness = service.CurrentSettings.MaxBrushThickness;

            // Act
            service.SetBrushThickness(maxThickness + 10.0);

            // Assert
            Assert.Equal(maxThickness, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void GetNextColor_ShouldCycleThroughPalette()
        {
            // Arrange
            var service = CreateService();
            var firstColor = service.CurrentSettings.BrushColor;
            var palette = service.CurrentSettings.ColorPalette;

            // Act
            var nextColor = service.GetNextColor();

            // Assert
            var expectedIndex = (palette.IndexOf(firstColor) + 1) % palette.Count;
            Assert.Equal(palette[expectedIndex], nextColor);
            Assert.Equal(nextColor, service.CurrentSettings.BrushColor);
        }

        [Fact]
        public void GetNextColor_ShouldWrapAroundAtEnd()
        {
            // Arrange
            var service = CreateService();
            var palette = service.CurrentSettings.ColorPalette;
            
            // Set to last color in palette
            service.SetBrushColor(palette[palette.Count - 1]);

            // Act
            var nextColor = service.GetNextColor();

            // Assert - Should wrap to first color
            Assert.Equal(palette[0], nextColor);
        }

        [Fact]
        public void ResetToDefaults_ShouldRestoreDefaultSettings()
        {
            // Arrange
            var service = CreateService();
            service.SetBrushColor("#123456");
            service.SetBrushThickness(15.0);
            service.SetLockDrawingMode(true);

            // Act
            service.ResetToDefaults();

            // Assert
            var settings = service.CurrentSettings;
            Assert.Equal("#FF0000", settings.BrushColor);
            Assert.Equal(3.0, settings.BrushThickness);
            Assert.False(settings.LockDrawingMode);
        }

        [Theory]
        [InlineData(new int[] { 0xA2, 0xA4, 0x44 })]  // Ctrl + Alt + D
        [InlineData(new int[] { 0xA2, 0xA0, 0x46 })]  // Ctrl + Shift + F
        [InlineData(new int[] { 0xA4, 0xA0, 0x58 })]  // Alt + Shift + X
        public void SetHotkey_ShouldUpdateHotkeyConfiguration(int[] vkArray)
        {
            // Arrange
            var service = CreateService();
            var vks = new List<int>(vkArray);

            // Act
            service.SetHotkey(vks);

            // Assert
            var settings = service.CurrentSettings;
            Assert.Equal(vks, settings.HotkeyVirtualKeys);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetLockDrawingMode_ShouldUpdateLockMode(bool lockMode)
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetLockDrawingMode(lockMode);

            // Assert
            Assert.Equal(lockMode, service.CurrentSettings.LockDrawingMode);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldUpdateMinAndMax()
        {
            // Arrange
            var service = CreateService();
            double newMin = 2.0;
            double newMax = 25.0;

            // Act
            service.SetBrushThicknessRange(newMin, newMax);

            // Assert
            var settings = service.CurrentSettings;
            Assert.Equal(newMin, settings.MinBrushThickness);
            Assert.Equal(newMax, settings.MaxBrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldAdjustCurrentThicknessIfBelowMin()
        {
            // Arrange
            var service = CreateService();
            service.SetBrushThickness(3.0);
            
            // Act - Set new range where min is above current thickness
            service.SetBrushThicknessRange(5.0, 20.0);

            // Assert
            Assert.Equal(5.0, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldAdjustCurrentThicknessIfAboveMax()
        {
            // Arrange
            var service = CreateService();
            service.SetBrushThickness(15.0);
            
            // Act - Set new range where max is below current thickness
            service.SetBrushThicknessRange(1.0, 10.0);

            // Assert
            Assert.Equal(10.0, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldPersistMinAndMax()
        {
            // Arrange
            var service = new AppSettingsService(_mockLogger.Object);
            
            // Act
            service.SetBrushThicknessRange(5, 30);
            
            // Assert - values should be set in memory
            Assert.Equal(5, service.CurrentSettings.MinBrushThickness);
            Assert.Equal(30, service.CurrentSettings.MaxBrushThickness);
            
            // Create a new service instance to verify persistence
            var newService = new AppSettingsService(_mockLogger.Object);
            Assert.Equal(5, newService.CurrentSettings.MinBrushThickness);
            Assert.Equal(30, newService.CurrentSettings.MaxBrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldAdjustCurrentThicknessIfOutOfRange()
        {
            // Arrange
            var service = new AppSettingsService(_mockLogger.Object);
            service.SetBrushThickness(15);
            
            // Act - set range that excludes current value
            service.SetBrushThicknessRange(1, 10);
            
            // Assert - current thickness should be adjusted to max
            Assert.Equal(10, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldNotAdjustCurrentThicknessIfInRange()
        {
            // Arrange
            var service = new AppSettingsService(_mockLogger.Object);
            service.SetBrushThickness(8);
            
            // Act - set range that includes current value
            service.SetBrushThicknessRange(5, 30);
            
            // Assert - current thickness should remain unchanged
            Assert.Equal(8, service.CurrentSettings.BrushThickness);
        }

        [Fact]
        public void SetBrushThicknessRange_ShouldRaiseEvent()
        {
            // Arrange
            var service = new AppSettingsService(_mockLogger.Object);
            (double min, double max) eventData = (0, 0);
            service.BrushThicknessRangeChanged += (sender, data) => eventData = data;
            
            // Act
            service.SetBrushThicknessRange(2, 50);
            
            // Assert
            Assert.Equal(2, eventData.min);
            Assert.Equal(50, eventData.max);
        }

        [Fact]
        public void AppSettings_MinMaxThicknessShouldSerializeToJson()
        {
            // Arrange
            var settings = new AppSettings
            {
                MinBrushThickness = 3,
                MaxBrushThickness = 40
            };
            
            // Act
            var json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);
            
            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.MinBrushThickness);
            Assert.Equal(40, deserialized.MaxBrushThickness);
        }

        [Fact]
        public void SetBrushColor_ShouldTriggerLogging()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetBrushColor("#ABCDEF");

            // Assert - Verify logger was called (using Moq verification)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("#ABCDEF")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void GetNextColor_WithColorNotInPalette_ShouldStillCycle()
        {
            // Arrange
            var service = CreateService();
            var palette = service.CurrentSettings.ColorPalette;
            
            // Set to a color not in the palette
            service.SetBrushColor("#999999");

            // Act
            var nextColor = service.GetNextColor();

            // Assert - Should default to first color (index -1 + 1 = 0)
            Assert.Equal(palette[0], nextColor);
        }

        [Fact]
        public void SetHotkey_ShouldPersistHotkeyConfiguration()
        {
            // Arrange
            var service = new AppSettingsService(_mockLogger.Object);
            var vks = new List<int> { 0xA0, 0x46 };  // Shift + F
            
            // Act
            service.SetHotkey(vks);
            
            // Assert - values should be set in memory
            Assert.Equal(vks, service.CurrentSettings.HotkeyVirtualKeys);
            
            // Create a new service instance to verify persistence
            var newService = new AppSettingsService(_mockLogger.Object);
            Assert.Equal(vks, newService.CurrentSettings.HotkeyVirtualKeys);
        }
    }
}
