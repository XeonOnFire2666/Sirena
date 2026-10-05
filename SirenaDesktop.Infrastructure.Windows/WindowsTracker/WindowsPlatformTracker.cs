using SirenaDesktop.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Automation;

namespace SirenaDesktop.Services.Platform.Windows;

[SupportedOSPlatform("windows")]
public class WindowsPlatformTracker : IPlatformTracker
{
    private static readonly HashSet<string> SupportedBrowsers = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "zen",
        "Arc",
        "chromium"
    };

    public WindowInfo GetActiveWindowInfo()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return new WindowInfo("Idle", "", "");
        }

        GetWindowThreadProcessId(handle, out var processId);

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var appName = process.ProcessName;
            var title = process.MainWindowTitle;
            var domain = "";
            var url = "";

            if (SupportedBrowsers.Contains(appName))
            {
                (domain, url) = ExtractBrowserInfo(handle);
            }

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

        if (!GetLastInputInfo(ref lastInput))
        {
            return 0;
        }

        var currentTick = (uint)Environment.TickCount;
        var idleTicks = currentTick - lastInput.dwTime;

        return (int)(idleTicks / 1000);
    }

    private static (string Domain, string Url) ExtractBrowserInfo(IntPtr handle)
    {
        try
        {
            var root = AutomationElement.FromHandle(handle);
            if (root == null)
            {
                return ("", "");
            }

            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
            var addressEditBox = root.FindFirst(TreeScope.Descendants, condition);

            if (addressEditBox == null)
            {
                var nameCondition = new PropertyCondition(AutomationElement.NameProperty, "Address and search bar");
                addressEditBox = root.FindFirst(TreeScope.Descendants, nameCondition);
            }

            if (addressEditBox != null && addressEditBox.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj))
            {
                var pattern = (ValuePattern)patternObj;
                var url = pattern.Current.Value;

                if (!string.IsNullOrEmpty(url))
                {
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "https://" + url;
                    }

                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        return (uri.Host, url);
                    }
                }
            }
        }
        catch
        {
        }

        return ("", "");
    }

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
}