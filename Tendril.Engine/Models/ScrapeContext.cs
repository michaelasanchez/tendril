using Microsoft.Playwright;

namespace Tendril.Engine.Models;

public class ScrapeContext
{
    public HttpClient? Client { get; set; }
    public IBrowserContext? Browser { get; set; }
}
