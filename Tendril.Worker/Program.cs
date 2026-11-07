using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using Tendril.Data;
using Tendril.Engine;
using Tendril.Engine.Playwright;
using Tendril.Worker;
using Tendril.Worker.Infrastructure;

// Only one worker may run: startup recovery assumes any task left in "Running" is an orphan.
// ProgramData is writable by every account, including a service account.
var lockPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tendril", "worker.lock");

using var instanceGuard = SingleInstanceGuard.TryAcquire(lockPath);
if (instanceGuard is null)
{
    Console.Error.WriteLine("Another Tendril worker is already running. Exiting.");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(); // no-op when run from a console

var cookieContainer = new CookieContainer();
builder.Services.AddSingleton(cookieContainer);

builder.Services.AddHttpClient("ScraperClient")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
    {
        // TicketWeb/Akamai often requires these for modern bot detection bypass
        //AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        CookieContainer = cookieContainer, // This is crucial for session persistence
        AutomaticDecompression = DecompressionMethods.All
    })
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        // Add these additional "Sec-CH" headers to look like a real Chromium browser
        client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
        client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    });

// AddDataServices registers TendrilDbContext (previously registered twice).
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddEngineServices();

builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.Configure<BrowserOptions>(builder.Configuration.GetSection("Browser"));

// Time to unwind a scraper and stop Chrome before the host gives up.
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(45));

builder.Services.AddHostedService<WorkerSessionHostedService>();
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.MapGet("/", () => "Tendril Background Worker is running...");

app.Run();

return 0;
