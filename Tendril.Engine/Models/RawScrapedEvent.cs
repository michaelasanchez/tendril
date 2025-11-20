using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Engine.Models;

public class RawScrapedEvent
{
    public Dictionary<string, List<string>?> Fields { get; set; } = new();
}