//using SirenaDesktop.Core.Interfaces;
//using SirenaDesktop.Services.Platform;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace SirenaDesktop.Infrastructure.Sync.Win_API;
//public static class PlatformTrackerFactory
//{
//    public static IPlatformTracker Create()
//    {
//        if (OperatingSystem.IsWindows())
//        {
//            return new WindowsPlatformTracker();
//        }

//        return new FallbackPlatformTracker();
//    }
//}