using System;

namespace WindowSwitcher.Services.Models;

public record VirtualDesktopInfo(Guid Id, string Name,int WindowCount, bool IsCurrentDesktop);