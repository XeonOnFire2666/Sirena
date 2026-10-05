using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Core.Interfaces;
public record WindowInfo(string AppName, string Domain, string Title, string Url = "");

public interface IPlatformTracker
{
    WindowInfo GetActiveWindowInfo();
    int GetIdleSeconds();
}