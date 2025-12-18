using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;

namespace Tendril.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IConfiguration config,
    IEventIngestionService ingestionService,
    IScraperRepository scrapers) : BackgroundService
{
    private readonly int _startHour =
        config.GetValue<int>("ScraperSchedule:DailyStartHour");

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            var nextRun = now.Date.AddHours(_startHour);

            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            logger.LogInformation(
                "Next scraper run scheduled for {NextRun}", nextRun);

            await Task.Delay(nextRun - now, ct);

            logger.LogInformation("Starting scraper run");

            try
            {
                foreach (var scraper in await scrapers.GetAllWithDetailsAsync(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    await ingestionService.Ingest(scraper, ct);
                }

                logger.LogInformation("Scraper run completed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scraper run failed");
            }
        }
    }
}
