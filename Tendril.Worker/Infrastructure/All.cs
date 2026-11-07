using System.Diagnostics;
using System.Management; // NuGet: System.Management (Windows only; remove this block on Linux)

namespace Tendril.Worker.Infrastructure;

// ============================================================ item 9
/// <summary>Cross-platform single-instance guard using an exclusive file lock.</summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly FileStream _stream;
    private SingleInstanceGuard(FileStream stream) => _stream = stream;

    public static SingleInstanceGuard? TryAcquire(string lockFilePath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(lockFilePath))!);
            var fs = new FileStream(lockFilePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            return new SingleInstanceGuard(fs);
        }
        catch (IOException)
        {
            return null; // someone else holds it
        }
    }

    public void Dispose() => _stream.Dispose(); // OS releases the lock even on crash
}

// ============================================================ items 11, 12
public static class BrowserPaths
{
    /// <summary>All Chrome profiles live under here, so orphans are identifiable.</summary>
    public static readonly string ProfileRoot =
        Path.Combine(Path.GetTempPath(), "tendril-chrome");

    public static string NewProfileDir()
    {
        var dir = Path.Combine(ProfileRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Call only at startup, after orphan Chrome has been killed.</summary>
    public static void DeleteAllProfiles(ILogger logger)
    {
        if (!Directory.Exists(ProfileRoot)) return;

        foreach (var dir in Directory.EnumerateDirectories(ProfileRoot))
        {
            try { Directory.Delete(dir, recursive: true); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete stale profile {Dir}", dir); }
        }
    }
}

// ============================================================ items 10, 12, 17
public static class BrowserProcessSweeper
{
    /// <summary>Kills (with children) every process whose command line contains <paramref name="needle"/>.</summary>
    public static int KillByCommandLineMatch(string needle, ILogger logger)
    {
        var killed = 0;
        foreach (var pid in FindPids(needle))
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                p.Kill(entireProcessTree: true);
                killed++;
            }
            catch { /* already gone */ }
        }

        if (killed > 0) logger.LogWarning("Killed {Count} orphaned browser process(es).", killed);
        return killed;
    }

    /// <summary>Total working set of all browser processes launched under the profile root.</summary>
    public static long GetWorkingSetMb(string needle)
    {
        long bytes = 0;
        foreach (var pid in FindPids(needle))
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                bytes += p.WorkingSet64;
            }
            catch { }
        }
        return bytes / (1024 * 1024);
    }

    private static List<int> FindPids(string needle)
    {
        var self = Environment.ProcessId;
        var result = new List<int>();

        foreach (var (pid, cmd) in EnumerateCommandLines())
        {
            if (pid != self && cmd.Contains(needle, StringComparison.OrdinalIgnoreCase))
                result.Add(pid);
        }
        return result;
    }

    private static IEnumerable<(int Pid, string Cmd)> EnumerateCommandLines()
    {
        if (OperatingSystem.IsLinux())
        {
            foreach (var dir in Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(Path.GetFileName(dir), out var pid)) continue;

                string cmd;
                try { cmd = File.ReadAllText(Path.Combine(dir, "cmdline")).Replace('\0', ' '); }
                catch { continue; } // process exited or not readable

                yield return (pid, cmd);
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            foreach (var row in EnumerateWindows()) yield return row;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static List<(int, string)> EnumerateWindows()
    {
        var rows = new List<(int, string)>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT ProcessId, CommandLine FROM Win32_Process " +
            "WHERE Name LIKE 'chrome%' OR Name LIKE 'msedge%' OR Name LIKE 'chromium%'");

        foreach (ManagementObject mo in searcher.Get())
        {
            var cmd = mo["CommandLine"] as string;
            if (cmd is null) continue;
            rows.Add((Convert.ToInt32(mo["ProcessId"]), cmd));
        }
        return rows;
    }
}

// ============================================================ items 10, 11, 13, 14
/// <summary>
/// Create one per task (register as Scoped; the worker's async scope disposes it).
/// Pass UserDataDirArg + SlimArgs to whatever launches Chrome.
/// On dispose: kills any Chrome using this profile, then deletes the profile.
/// </summary>
public sealed class BrowserProfileLease(ILogger<BrowserProfileLease> logger) : IAsyncDisposable
{
    public string ProfileDir { get; } = BrowserPaths.NewProfileDir();
    public string UserDataDirArg => $"--user-data-dir={ProfileDir}";

    public static readonly string[] SlimArgs =
    [
        "--disable-gpu",
        "--disable-extensions",
        "--disable-dev-shm-usage",
        "--disable-background-networking",
        "--disable-background-timer-throttling",
        "--no-first-run",
        "--no-default-browser-check",
        "--mute-audio",
        "--blink-settings=imagesEnabled=false" // remove if you need images
    ];

    public async ValueTask DisposeAsync()
    {
        // Don't trust graceful close; a cancelled navigation is exactly when it fails.
        BrowserProcessSweeper.KillByCommandLineMatch(ProfileDir, logger);

        // Windows holds file handles briefly after kill, so retry the delete.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (Directory.Exists(ProfileDir)) Directory.Delete(ProfileDir, recursive: true);
                return;
            }
            catch when (attempt < 5)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete browser profile {Dir}", ProfileDir);
            }
        }
    }
}

// Usage inside your ingestion/browser factory (library-specific part):
//
//   var lease = sp.GetRequiredService<BrowserProfileLease>();
//   var args  = BrowserProfileLease.SlimArgs.Append(lease.UserDataDirArg);
//   // PuppeteerSharp: new LaunchOptions { Args = args.ToArray(), ... }
//   // Playwright:     new BrowserTypeLaunchOptions { Args = args }
//   // then block media/fonts with request interception if you don't need them.