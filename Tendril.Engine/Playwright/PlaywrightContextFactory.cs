using Microsoft.Playwright;
using System.Diagnostics;

namespace Tendril.Engine.Playwright;

public static class PlaywrightContextFactory
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    private static readonly BrowserNewContextOptions DesktopOptions = new()
    {
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public static async Task<IBrowserContext> CreateContextAsync(bool useRealChrome)
    {
        if (_browser == null)
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            _browser = await _playwright.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");
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

    private static async Task<string> ScrapeWithPersistentChrome()
    {
        string userDataDir = @"C:\temp\meijer_scraping_profile";
        string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        string targetUrl = "https://www.meijergardens.org/calendar/";

        // 1. Launch the process
        var process = new Process();
        process.StartInfo.FileName = chromePath;
        process.StartInfo.Arguments = $"--remote-debugging-port=9222 --user-data-dir=\"{userDataDir}\" --remote-allow-origins=* \"{targetUrl}\"";
        process.Start();

        // 2. Wait for Chrome to actually wake up
        await Task.Delay(3000);

        // 3. Connect Playwright
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");

        // 4. Find the tab that was opened by the launch command
        var context = browser.Contexts[0];
        var page = context.Pages.FirstOrDefault(p => p.Url.Contains("meijergardens")) ?? context.Pages[0];

        // 5. Stealth Init
        await page.AddInitScriptAsync("delete Object.getPrototypeOf(navigator).webdriver;");

        // 6. Extraction
        string html = await page.ContentAsync();

        // Cleanup (Optionally kill process if you don't want it hanging around)
        process.Kill();

        return html;
    }
}