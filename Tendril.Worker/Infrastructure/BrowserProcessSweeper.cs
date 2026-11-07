using System.Diagnostics;
using System.Management;

namespace Tendril.Worker.Infrastructure;

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

    /// <summary>Total working set of all processes whose command line contains <paramref name="needle"/>.</summary>
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
