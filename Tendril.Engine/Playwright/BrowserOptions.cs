namespace Tendril.Engine.Playwright;

/// <summary>
/// Bound from the "Browser" config section. Defaults match the previous hard-coded values.
/// Two processes on one machine (API and Worker) must use different DebugPort values.
/// </summary>
public sealed class BrowserOptions
{
    public string ChromePath { get; set; } = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    /// <summary>Persistent headful profiles live in {ProfileRoot}\{scraperId}.</summary>
    public string ProfileRoot { get; set; } = @"C:\temp";

    public int DebugPort { get; set; } = 9222;
}
