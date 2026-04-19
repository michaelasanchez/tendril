using Microsoft.Playwright;
using System.Diagnostics;
using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Playwright;

public sealed class PlaywrightContextFactory : IAsyncDisposable
{
    private const string ChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowser? _cdpBrowser;
    private Process? _process;

    private static readonly BrowserNewContextOptions DesktopOptions = new()
    {
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public async Task<IBrowserContext> CreateContextAsync(ScraperDefinition def)
    {
        await _lock.WaitAsync();
        try
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

            if (!def.UseHeadlessBrowser)
            {
                if (_cdpBrowser == null || !_cdpBrowser.Contexts.Any())
                {
                    if (_cdpBrowser != null)
                    {
                        await _cdpBrowser.DisposeAsync();
                        _cdpBrowser = null;
                    }

                    if (_process != null && !_process.HasExited)
                    {
                        _process.Kill();
                        _process.Dispose();
                        _process = null;
                    }

                    return await ScrapeWithPersistentChrome(_playwright, def);
                }

                return _cdpBrowser.Contexts[0];
            }

            _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            return await _browser.NewContextAsync(DesktopOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
            _browser = null;
        }

        if (_cdpBrowser != null)
        {
            await _cdpBrowser.DisposeAsync();
            _cdpBrowser = null;
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

    private async Task<IBrowserContext> ScrapeWithPersistentChrome(
        IPlaywright playwright,
        ScraperDefinition def)
    {
        string userDataDir = $@"C:\temp\{def.Id}";

        _process = new Process();
        _process.StartInfo.FileName = ChromePath;
        _process.StartInfo.Arguments =
            $"--remote-debugging-port=9222 --user-data-dir=\"{userDataDir}\" " +
            $"--remote-allow-origins=* \"{def.BaseUrl}\"";
        _process.Start();

        await WaitForChromeAsync("http://127.0.0.1:9222");

        _cdpBrowser = await playwright.Chromium.ConnectOverCDPAsync("http://127.0.0.1:9222");

        return _cdpBrowser.Contexts[0];
    }

    private static async Task WaitForChromeAsync(string url, int timeoutMs = 10000)
    {
        using var http = new HttpClient();
        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                await http.GetAsync(url + "/json/version");
                return;
            }
            catch
            {
                await Task.Delay(200);
            }
        }

        throw new TimeoutException("Chrome did not become ready in time.");
    }
}