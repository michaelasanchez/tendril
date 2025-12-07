namespace Tendril.Engine.Playwright;

using Microsoft.Playwright;

public static class PlaywrightContextFactory
{
    private static IPlaywright? _pw;

    public static async Task<IPage> CreatePageAsync()
    {
        _pw ??= await Playwright.CreateAsync();

        var browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = 1400,
                Height = 900
            }
        });
        return await context.NewPageAsync();
    }
}
