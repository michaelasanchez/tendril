namespace Tendril.Engine.Playwright;

using Microsoft.Playwright;

public static class PlaywrightContextFactory
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    private static readonly BrowserNewContextOptions DesktopOptions = new()
    {
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public static async Task<IBrowserContext> CreateContextAsync()
    {
        if (_browser == null)
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        return await _browser.NewContextAsync(DesktopOptions);
    }

    public static async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
    }
}