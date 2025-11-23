using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Core.Domain.Enums;

public enum ScraperState
{
    Unknown,
    Healthy,
    Warning,
    Unhealthy,
    Blocked,
    Offline
}
