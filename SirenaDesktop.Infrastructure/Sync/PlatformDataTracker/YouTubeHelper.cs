using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Infrastructure.Sync.PlatformDataTracker;
public static class YouTubeHelper
{
    public static bool IsVideoWatchPage(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        return url.Contains("/watch", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/shorts/", StringComparison.OrdinalIgnoreCase)
               || url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExtractVideoTitle(string windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return null;
        }

        const string suffix = "- YouTube";
        var t = windowTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? windowTitle[..^suffix.Length].Trim()
            : windowTitle.Trim();

        if (t.Length == 0)
        {
            return null;
        }

        var lastDash = t.LastIndexOf(" - ", StringComparison.Ordinal);
        return lastDash > 0 ? t[..lastDash].Trim() : t;
    }
}