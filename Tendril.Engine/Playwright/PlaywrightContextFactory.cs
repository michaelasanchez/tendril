using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Diagnostics;
using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Playwright;

public sealed class PlaywrightContextFactory(IOptions<BrowserOptions> options) : IAsyncDisposable
{
    // Existing persistent profiles live under ProfileRoot ({root}\{scraperId}), so logins survive.
    private string ChromePath => options.Value.ChromePath;
    private string ProfileRoot => options.Value.ProfileRoot;
    private int DebugPort => options.Value.DebugPort;
    private string DebugUrl => $"http://127.0.0.1:{DebugPort}";

    // Cold start of Chrome on an old machine can be slow.
    private static readonly TimeSpan ChromeStartTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan GracefulCloseTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(10);

    // Cache-only folders inside a Chrome profile. Safe to delete; cookies,
    // local storage and logins live elsewhere and are untouched.
    private static readonly string[] CacheDirs =
    [
        @"Default\Cache", @"Default\Code Cache", @"Default\GPUCache",
        "GrShaderCache", "ShaderCache", "GraphiteDawnCache"
    ];

    private static readonly BrowserNewContextOptions DesktopOptions = new()
    {
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;      // headless, launched and owned by Playwright
    private IBrowser? _cdpBrowser;   // connection to our own headful Chrome
    private Process? _process;       // our headful Chrome

    // ------------------------------------------------------------------ create

    public async Task<IBrowserContext> CreateContextAsync(ScraperDefinition def)
    {
        await _lock.WaitAsync();
        try
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

            if (!def.UseHeadlessBrowser)
            {
                var reusable = _cdpBrowser is { IsConnected: true }
                               && _cdpBrowser.Contexts.Count > 0
                               && _process is { HasExited: false };

                if (reusable) return _cdpBrowser!.Contexts[0];

                await StopChromeCoreAsync();
                return await LaunchPersistentChromeAsync(_playwright, def);
            }

            // A crashed headless browser is never null, so ??= alone would hand out
            // a dead browser forever. Relaunch when the connection is gone.
            if (_browser is { IsConnected: false })
            {
                var dead = _browser;
                _browser = null;
                await BestEffortAsync(() => dead.DisposeAsync().AsTask());
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

    // ------------------------------------------------------------------ lifecycle

    /// <summary>
    /// Call after every scraper. Stops the headful Chrome so the next scraper never
    /// inherits another scraper's profile and Chrome doesn't sit open between runs.
    /// The profile stays on disk, so sessions are kept.
    /// </summary>
    public async Task StopPersistentChromeAsync()
    {
        await _lock.WaitAsync();
        try { await StopChromeCoreAsync(); }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Call after every task. Also closes the headless browser and the Playwright
    /// driver process, so nothing accumulates over weeks of uptime.
    /// Idempotent; the next CreateContextAsync starts fresh.
    /// </summary>
    public async Task RecycleAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StopChromeCoreAsync();

            if (_browser is not null)
            {
                var browser = _browser;
                _browser = null;
                await BestEffortAsync(() => browser.CloseAsync());
                await BestEffortAsync(() => browser.DisposeAsync().AsTask());
            }

            if (_playwright is not null)
            {
                try { _playwright.Dispose(); } catch { /* driver already gone */ }
                _playwright = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync() => await RecycleAsync();

    // ------------------------------------------------------------------ headful Chrome

    private async Task<IBrowserContext> LaunchPersistentChromeAsync(
        IPlaywright playwright, ScraperDefinition def)
    {
        // If something already answers on our port, connecting would silently attach to the
        // wrong browser (for example the API's, or a leftover Chrome).
        if (await IsDebugPortAnsweringAsync())
            throw new InvalidOperationException(
                $"Something is already answering on debug port {DebugPort}. " +
                "Set Browser:DebugPort to a port no other process uses.");

        var userDataDir = Path.Combine(ProfileRoot, def.Id.ToString());
        Directory.CreateDirectory(userDataDir);
        TrimCaches(userDataDir);

        var psi = new ProcessStartInfo(ChromePath) { UseShellExecute = false };
        psi.ArgumentList.Add($"--remote-debugging-port={DebugPort}");
        psi.ArgumentList.Add($"--user-data-dir={userDataDir}");
        psi.ArgumentList.Add("--remote-allow-origins=*");
        psi.ArgumentList.Add("--no-first-run");
        psi.ArgumentList.Add("--no-default-browser-check");
        psi.ArgumentList.Add("--hide-crash-restore-bubble");
        psi.ArgumentList.Add(string.IsNullOrEmpty(def.BaseUrl) ? "about:blank" : def.BaseUrl);

        _process = Process.Start(psi)
                   ?? throw new InvalidOperationException("Chrome failed to start.");

        try
        {
            await WaitForChromeAsync(_process);

            _cdpBrowser = await playwright.Chromium.ConnectOverCDPAsync(DebugUrl);

            return _cdpBrowser.Contexts.FirstOrDefault()
                   ?? throw new InvalidOperationException("Chrome exposed no browser context.");
        }
        catch
        {
            // Never leave a half-started Chrome behind.
            await StopChromeCoreAsync();
            throw;
        }
    }

    /// <summary>Caller must hold _lock.</summary>
    private async Task StopChromeCoreAsync()
    {
        var browser = _cdpBrowser;
        var process = _process;
        _cdpBrowser = null;
        _process = null;

        if (browser is not null)
            await BestEffortAsync(() => browser.DisposeAsync().AsTask());

        if (process is null) return;

        try
        {
            if (!process.HasExited)
            {
                // Graceful first: lets Chrome flush cookies and session state to the
                // persistent profile. A straight Kill can lose recent logins.
                process.CloseMainWindow();

                using var cts = new CancellationTokenSource(GracefulCloseTimeout);
                try { await process.WaitForExitAsync(cts.Token); }
                catch (OperationCanceledException) { }
            }

            if (!process.HasExited)
                process.Kill(entireProcessTree: true); // renderers, GPU process, etc.
        }
        catch
        {
            // Process vanished underneath us, which is the outcome we wanted anyway.
        }
        finally
        {
            process.Dispose();
        }
    }

    private async Task WaitForChromeAsync(Process process)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < ChromeStartTimeout)
        {
            // If our process died, Chrome probably handed off to an already-running
            // instance using the same profile. Anything answering on the port is
            // then NOT the browser we launched, so fail instead of connecting to it.
            if (process.HasExited)
                throw new InvalidOperationException(
                    "Chrome exited during startup (profile already in use?).");

            try
            {
                using var response = await http.GetAsync($"{DebugUrl}/json/version");
                if (response.IsSuccessStatusCode) return;
            }
            catch
            {
                // not listening yet
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Chrome did not become ready in time.");
    }

    private async Task<bool> IsDebugPortAnsweringAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        try
        {
            using var response = await http.GetAsync($"{DebugUrl}/json/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static void TrimCaches(string userDataDir)
    {
        foreach (var relative in CacheDirs)
        {
            try
            {
                var path = Path.Combine(userDataDir, relative);
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            }
            catch
            {
                // in use or locked; it will be trimmed on a later launch
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Runs a cleanup step with a timeout and swallows failures, so one dead
    /// connection can't stop the later steps (especially killing the process).
    /// </summary>
    private static async Task BestEffortAsync(Func<Task> action)
    {
        try { await action().WaitAsync(DisposeTimeout); }
        catch { /* dead connection or timeout; callers escalate by killing the process */ }
    }
}