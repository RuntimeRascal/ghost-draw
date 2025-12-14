using System.IO;
using GhostDraw.Helpers;
using Xunit;

namespace GhostDraw.Tests;

public class AppDataPathProviderTests
{
    [Fact]
    public void Fallback_UsesLocalAppDataAndCreatesDirectory()
    {
        // Arrange
        var tempLocalAppData = Path.Combine(Path.GetTempPath(), "GhostDrawLocalAppDataTest");
        var expectedBase = Path.Combine(tempLocalAppData, "GhostDraw");

        if (Directory.Exists(tempLocalAppData))
        {
            Directory.Delete(tempLocalAppData, recursive: true);
        }

        AppDataPathProvider.PackagedPathResolverOverride = null;
        AppDataPathProvider.LocalAppDataPathResolverOverride = () => tempLocalAppData;

        // Act
        var path = AppDataPathProvider.GetLocalAppDataDirectory();

        // Assert
        Assert.Equal(expectedBase, path);
        Assert.True(Directory.Exists(path));

        // Cleanup
        AppDataPathProvider.PackagedPathResolverOverride = null;
        AppDataPathProvider.LocalAppDataPathResolverOverride = null;

        if (Directory.Exists(tempLocalAppData))
        {
            Directory.Delete(tempLocalAppData, recursive: true);
        }
    }

    [Fact]
    public void PackagedOverride_IsUsedAndCreatesDirectory()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "GhostDrawPackagedTest");
        var expected = Path.Combine(tempRoot, "GhostDraw");

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }

        AppDataPathProvider.PackagedPathResolverOverride = () => tempRoot;
        AppDataPathProvider.LocalAppDataPathResolverOverride = null;

        // Act
        var path = AppDataPathProvider.GetLocalAppDataDirectory();

        // Assert
        Assert.Equal(expected, path);
        Assert.True(Directory.Exists(path));
        AppDataPathProvider.PackagedPathResolverOverride = null;
        AppDataPathProvider.LocalAppDataPathResolverOverride = null;

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
