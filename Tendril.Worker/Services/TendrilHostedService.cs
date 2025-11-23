using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data;
using Tendril.Engine.Abstractions;
using Tendril.Worker.Scheduling;

namespace Tendril.Worker.Services;


public class TendrilHostedService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<TendrilHostedService> _logger;
    private readonly IConfiguration _config;

    private readonly int _pollIntervalSeconds;

    public TendrilHostedService(
        IServiceProvider provider,
        ILogger<TendrilHostedService> logger,
        IConfiguration config)
    {
        _provider = provider;
        _logger = logger;
        _config = config;

        _pollIntervalSeconds = _config.GetValue<int>("ScraperWorker:PollIntervalSeconds", 300);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scraper Worker running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScrapersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in worker loop.");
            }

            await IntervalScheduler.WaitAsync(_pollIntervalSeconds, stoppingToken);
        }
    }

    private async Task RunScrapersAsync(CancellationToken cancellationToken)
    {
        using var scope = _provider.CreateScope();

        var attemptRepo = scope.ServiceProvider.GetRequiredService<IAttemptHistoryRepository>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var rawRepo = scope.ServiceProvider.GetRequiredService<IRawEventRepository>();
        var scraperRepo = scope.ServiceProvider.GetRequiredService<IScraperRepository>();

        var mapper = scope.ServiceProvider.GetRequiredService<IEventMapper>();

        var executor = scope.ServiceProvider.GetRequiredService<IScrapeExecutor>();

        var scrapers = await scraperRepo.GetAllWithDetailsAsync(cancellationToken);

        foreach (var scraper in scrapers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Running scraper {Scraper}", scraper.Name);

            var start = DateTimeOffset.UtcNow;

            var result = await executor.RunScraperAsync(scraper, false, cancellationToken);

            var end = DateTimeOffset.UtcNow;

            // Log attempt
            var attempt = new ScraperAttemptHistory
            {
                Id = Guid.NewGuid(),
                ScraperDefinitionId = scraper.Id,
                StartTimeUtc = start,
                EndTimeUtc = end,
                Success = result.Success,
                ExtractedCount = result.RawEvents.Count,
                ErrorMessage = result.ErrorMessage
            };

            await attemptRepo.AddAsync(attempt, cancellationToken);

            if (result.Success)
            {
                scraper.LastSuccessUtc = end;
                scraper.State = ScraperState.Healthy;

                foreach (var raw in result.RawEvents)
                {
                    var rawEntity = new ScrapedEventRaw
                    {
                        Id = Guid.NewGuid(),
                        ScraperDefinitionId = scraper.Id,
                        ScrapedAtUtc = end,
                        RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
                    };

                    await rawRepo.AddAsync(rawEntity, cancellationToken);

                    // Map to consolidated Event
                    try
                    {
                        var mapped = mapper.Map(scraper, rawEntity);

                        await eventRepo.AddAsync(mapped, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to map raw event for scraper {ScraperId}", scraper.Id);
                        // optional: rawEntity mapping failure somewhere
                    }
                }

                await scraperRepo.UpdateAsync(scraper, cancellationToken);
            }
            else
            {
                scraper.LastFailureUtc = end;
                scraper.LastErrorMessage = result.ErrorMessage;
                scraper.State = scraper.State == ScraperState.Healthy
                    ? ScraperState.Warning
                    : ScraperState.Unhealthy;

                _logger.LogWarning(
                    "Scraper {Scraper} failed: {Error}",
                    scraper.Name,
                    result.ErrorMessage);
            }

            await scraperRepo.UpdateAsync(scraper, cancellationToken);
        }
    }
}
