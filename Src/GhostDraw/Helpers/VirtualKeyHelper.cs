using System.Runtime.InteropServices;
using System.Text;

namespace GhostDraw.Helpers;

/// <summary>
/// Utility class for converting virtual key codes to user-friendly display names
/// using Windows API for localized key names
/// </summary>
public static class VirtualKeyHelper
{
    [DllImport("user32.dll")]
    private static extern int GetKeyNameText(int lParam, [Out] StringBuilder lpString, int nSize);

    [DllImport("user32.dll")]
    private static extern int MapVirtualKey(int uCode, uint uMapType);

    /// <summary>
    /// Gets the localized friendly name for a virtual key code using Windows API
    /// </summary>
    /// <param name="vkCode">Virtual key code</param>
    /// <returns>User-friendly key name</returns>
    public static string GetFriendlyName(int vkCode)
    {
        // Handle special cases
        string? specialName = GetSpecialKeyName(vkCode);
        if (specialName != null)
            return specialName;

        try
        {
            // Map VK to scan code
            int scanCode = MapVirtualKey(vkCode, 0); // MAPVK_VK_TO_VSC

            if (scanCode == 0)
                return GetFallbackName(vkCode);

            // Build lParam: scan code in bits 16-23, extended flag in bit 24
            int lParam = scanCode << 16;

            // Some keys need the extended flag
            if (IsExtendedKey(vkCode))
                lParam |= 0x01000000;

            // Get the key name from Windows
            var buffer = new StringBuilder(256);
            int result = GetKeyNameText(lParam, buffer, buffer.Capacity);

            if (result > 0)
                return buffer.ToString();
        }
        catch
        {
            // Fall through to fallback
        }

        return GetFallbackName(vkCode);
    }

    /// <summary>
    /// Gets special key names that should be normalized across left/right variants
    /// </summary>
    private static string? GetSpecialKeyName(int vkCode)
    {
        return vkCode switch
        {
            // Normalize modifiers (treat left/right as same)
            0xA2 or 0xA3 => "Ctrl",
            0xA4 or 0xA5 => "Alt",
            0xA0 or 0xA1 => "Shift",
            0x5B or 0x5C => "Win",

            // Other special keys
            0x1B => "Esc",
            0x20 => "Space",

            _ => null
        };
    }

    /// <summary>
    /// Gets a fallback name for keys that don't work with GetKeyNameText
    /// </summary>
    private static string GetFallbackName(int vkCode)
    {
        return vkCode switch
        {
            // Letters (A-Z)
            >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),

            // Numbers (0-9)
            >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),

            // Function keys (F1-F12)
            >= 0x70 and <= 0x7B => $"F{vkCode - 0x70 + 1}",

            // Common keys
            0x0D => "Enter",
            0x08 => "Backspace",
            0x09 => "Tab",
            0x2E => "Delete",
            0x2D => "Insert",

            // Default
            _ => $"Key{vkCode}"
        };
    }

    /// <summary>
    /// Determines if a virtual key code requires the extended flag for GetKeyNameText
    /// </summary>
    private static bool IsExtendedKey(int vkCode)
    {
        return vkCode switch
        {
            // Arrow keys
            0x25 or 0x26 or 0x27 or 0x28 => true,  // Left, Up, Right, Down

            // Navigation keys
            0x21 or 0x22 or 0x23 or 0x24 => true,  // PgUp, PgDn, End, Home
            0x2D or 0x2E => true,                   // Insert, Delete

            // Right-side modifiers
            0xA3 or 0xA5 => true,  // Right Ctrl, Right Alt

            // Numpad
            0x6F or 0x0D => true,  // Numpad Divide, Numpad Enter

            _ => false
        };
    }

    /// <summary>
    /// Converts a list of VK codes to a user-friendly display string
    /// </summary>
    /// <param name="virtualKeys">List of virtual key codes</param>
    /// <returns>Formatted combination string (e.g., "Ctrl + Alt + D")</returns>
    public static string GetCombinationDisplayName(List<int> virtualKeys)
    {
        if (virtualKeys == null || virtualKeys.Count == 0)
            return "None";

        // Get friendly names and remove duplicates
        var names = virtualKeys
            .Select(GetFriendlyName)
            .Distinct()
            .OrderBy(name => GetModifierOrder(name))  // Modifiers in standard order
            .ThenBy(name => name);

        return string.Join(" + ", names);
    }

    /// <summary>
    /// Gets the sort order for a key name (modifiers first in standard order, then others)
    /// </summary>
    private static int GetModifierOrder(string name)
    {
        return name switch
        {
            "Ctrl" => 0,
            "Alt" => 1,
            "Shift" => 2,
            "Win" => 3,
            _ => 100  // Non-modifiers last
        };
    }

    /// <summary>
    /// Check if a VK code is a modifier key
    /// </summary>
    /// <param name="vkCode">Virtual key code</param>
    /// <returns>True if the key is a modifier (Ctrl, Alt, Shift, Win)</returns>
    public static bool IsModifierKey(int vkCode)
    {
        return vkCode is
            (>= 0xA0 and <= 0xA5) or  // Shift, Ctrl, Alt (left and right)
            0x5B or 0x5C;              // Win keys (left and right)
    }
}
