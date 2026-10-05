using SirenaDesktop.Core.Interfaces;

namespace SirenaDesktop.Services.Platform;

public class FallbackPlatformTracker : IPlatformTracker
{
    public WindowInfo GetActiveWindowInfo()
    {
        return new WindowInfo("Unsupported Platform", "", "");
    }

    public int GetIdleSeconds()
    {
        return 0;
    }
}