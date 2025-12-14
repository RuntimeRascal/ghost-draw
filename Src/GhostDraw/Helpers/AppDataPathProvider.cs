using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostDraw.Helpers;

/// <summary>
/// Resolves the app data directory for both packaged (MSIX) and unpackaged runs.
/// </summary>
public static class AppDataPathProvider
{
    private const string AppFolderName = "GhostDraw";

    // Test hooks to allow deterministic paths without MSIX context.
    internal static Func<string?>? PackagedPathResolverOverride { get; set; }
    internal static Func<string>? LocalAppDataPathResolverOverride { get; set; }

    public static string GetLocalAppDataDirectory()
    {
        var packagedPath = TryGetPackagedPath();
        var localBase = LocalAppDataPathResolverOverride?.Invoke()
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var basePath = packagedPath ?? Path.Combine(localBase, AppFolderName);

        Directory.CreateDirectory(basePath);
        return basePath;
    }

    private static string? TryGetPackagedPath()
    {
        try
        {
            var overridden = PackagedPathResolverOverride?.Invoke();
            if (!string.IsNullOrWhiteSpace(overridden))
            {
                return Path.Combine(overridden, AppFolderName);
            }

            var packageFamilyName = GetCurrentPackageFamilyNameSafe();
            if (!string.IsNullOrWhiteSpace(packageFamilyName))
            {
                // Packaged apps store writable data under LocalCache
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages",
                    packageFamilyName,
                    "LocalCache");

                return Path.Combine(basePath, AppFolderName);
            }
        }
        catch
        {
            // Ignore and fall back to classic path
        }

        return null;
    }

    private static string? GetCurrentPackageFamilyNameSafe()
    {
        const int APPMODEL_ERROR_NO_PACKAGE = 15700; // 0x3D54
        const int ERROR_INSUFFICIENT_BUFFER = 122;   // 0x7A

        int length = 0;
        int rc = GetCurrentPackageFamilyName(ref length, null);
        if (rc == APPMODEL_ERROR_NO_PACKAGE)
        {
            return null;
        }

        if (rc != 0 && rc != ERROR_INSUFFICIENT_BUFFER)
        {
            return null;
        }

        var sb = new StringBuilder(length);
        rc = GetCurrentPackageFamilyName(ref length, sb);
        if (rc != 0)
        {
            return null;
        }

        return sb.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength, StringBuilder? packageFamilyName);
}
