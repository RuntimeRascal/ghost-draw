using GhostDraw.ViewModels;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GhostDraw.Tests;

public class SettingsViewModelTests
{
    private static SettingsViewModel CreateViewModel()
    {
        var mockLogger = new Mock<ILogger<AppSettingsService>>();
        var inMemoryStore = new InMemorySettingsStore();
        var appSettingsService = new AppSettingsService(mockLogger.Object, inMemoryStore);
        var loggingSettingsService = new LoggingSettingsService(appSettingsService);
        var loggerFactory = NullLoggerFactory.Instance;
        
        return new SettingsViewModel(appSettingsService, loggingSettingsService, loggerFactory);
    }

    [Fact]
    public void SettingsViewModel_Version_ShouldReturnValidVersionString()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var version = viewModel.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.StartsWith("v", version); // Version should start with 'v'
        Assert.Matches(@"^v\d+\.\d+\.\d+$", version); // Should match pattern v{Major}.{Minor}.{Build}
    }

    [Fact]
    public void SettingsViewModel_Version_ShouldContainOnlyNumbers()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var version = viewModel.Version;
        var versionWithoutPrefix = version.TrimStart('v');
        var parts = versionWithoutPrefix.Split('.');

        // Assert - Should have exactly 3 parts (Major.Minor.Build)
        Assert.Equal(3, parts.Length);
        Assert.All(parts, part => Assert.True(int.TryParse(part, out _)));
    }

    [Fact]
    public void SettingsViewModel_AppSettings_ShouldNotBeNull()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.AppSettings);
    }

    [Fact]
    public void SettingsViewModel_LoggingSettings_ShouldNotBeNull()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.LoggingSettings);
    }

    [Fact]
    public void SettingsViewModel_LoggerFactory_ShouldNotBeNull()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.LoggerFactory);
    }
}
