using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Engine.Models;

public class ScrapeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<RawScrapedEvent> RawEvents { get; set; } = new();
}
