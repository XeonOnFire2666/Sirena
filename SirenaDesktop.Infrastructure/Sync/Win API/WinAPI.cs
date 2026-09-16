using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using SirenaDesktop.Services;

namespace SirenaDesktop.Win_API;

public record WindowInfo(string AppName, string Domain, string Title, string Url = "");

public class WinAPI
{
    private readonly DayStatsService _statsService;

    public WinAPI(DayStatsService statsService)
    {
        _statsService = statsService;
    }

    public WindowInfo GetActiveWindowInfo()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return new WindowInfo("Idle", "", "");

        GetWindowThreadProcessId(handle, out var processId);

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var appName = process.ProcessName;     
            var title = process.MainWindowTitle;
            var domain = "";
            var url = "";

            if (appName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                appName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
                appName.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
                appName.Equals("zen", StringComparison.OrdinalIgnoreCase) ||
                appName.Equals("Arc", StringComparison.OrdinalIgnoreCase) ||
                appName.Equals("chromium", StringComparison.OrdinalIgnoreCase))
                (domain, url) = ExtractBrowserInfo(handle);

            return new WindowInfo(appName, domain, title, url);
        }
        catch
        {
            return new WindowInfo("Unknown", "", "");
        }
    }

    public int GetIdleSeconds()
    {
        var lastInput = new LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

        if (!GetLastInputInfo(ref lastInput)) return 0;

        var currentTick = (uint)Environment.TickCount;
        var idleTicks = currentTick - lastInput.dwTime;

        return (int)(idleTicks / 1000);
    }

    private (string Domain, string Url) ExtractBrowserInfo(IntPtr handle)
    {
        try
        {
            var root = AutomationElement.FromHandle(handle);
            if (root == null) return ("", "");

            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
            var addressEditBox = root.FindFirst(TreeScope.Descendants, condition);

            if (addressEditBox == null)
            {
                var nameCondition = new PropertyCondition(AutomationElement.NameProperty, "Адрес и строка поиска");
                addressEditBox = root.FindFirst(TreeScope.Descendants, nameCondition);
            }

            if (addressEditBox != null &&
                addressEditBox.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj))
            {
                var pattern = (ValuePattern)patternObj;
                var url = pattern.Current.Value;

                if (!string.IsNullOrEmpty(url))
                {
                    if (!url.StartsWith("http") && !url.StartsWith("https"))
                        url = "https://" + url;

                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return (uri.Host, url);
                }
            }
        }
        catch
        {
        }

        return ("", "");
    }

    #region WinAPI Imports

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    #endregion
}

public static class YouTubeHelper
{
    public static bool IsVideoWatchPage(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        return url.Contains("/watch", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/shorts/", StringComparison.OrdinalIgnoreCase)
               || url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExtractVideoTitle(string windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
            return null;

        const string suffix = "- YouTube";
        var t = windowTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? windowTitle[..^suffix.Length].Trim()
            : windowTitle.Trim();

        if (t.Length == 0)
            return null;

        var lastDash = t.LastIndexOf(" - ", StringComparison.Ordinal);
        return lastDash > 0 ? t[..lastDash].Trim() : t;
    }
}