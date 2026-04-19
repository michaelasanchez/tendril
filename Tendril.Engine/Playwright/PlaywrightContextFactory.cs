using Microsoft.Playwright;
using System.Diagnostics;
using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Playwright;

public static class PlaywrightContextFactory
{
    private const string ChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static Process? _process;

    private static readonly BrowserNewContextOptions DesktopOptions = new()
    {
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public static async Task<IBrowserContext> CreateContextAsync(ScraperDefinition def)
    {
        if (_browser == null)
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            if (!def.UseHeadlessBrowser)
            {
                return await ScrapeWithPersistentChrome(_playwright, def);
            }
            else
            {
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });
            }
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

        if (_process != null && !_process.HasExited)
        {
            _process.Kill();
            _process.Dispose();
            _process = null;
        }
    }

    private static async Task<IBrowserContext> ScrapeWithPersistentChrome(IPlaywright playwright, ScraperDefinition def)
    {
        string userDataDir = def.Id.ToString(); // @"C:\temp\meijer_scraping_profile";
        string targetUrl = def.BaseUrl; // "https://www.meijergardens.org/calendar/";

        // 1. Launch the process
        _process = new Process();
        _process.StartInfo.FileName = ChromePath;
        _process.StartInfo.Arguments = $"--remote-debugging-port=9222 --user-data-dir=\"{userDataDir}\" --remote-allow-origins=* \"{targetUrl}\"";
        _process.Start();

        // 2. Wait for Chrome to actually wake up
        await Task.Delay(3000);

        // 3. Connect Playwright
        var browser = await playwright.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");

        // 4. Find the tab that was opened by the launch command
        var context = browser.Contexts[0];

        return context;
    }
}