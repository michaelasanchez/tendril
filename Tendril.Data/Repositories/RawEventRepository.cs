using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class RawEventRepository : IRawEventRepository
{
    private readonly TendrilDbContext _db;

    public RawEventRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ScrapedEventRaw rawEvent, CancellationToken cancellationToken = default)
    {
        _db.RawEvents.Add(rawEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScrapedEventRaw?> GetMostRecentForScraperAsync(
        Guid scraperId,
        CancellationToken cancellationToken = default)
    {
        return await _db.RawEvents
            .Where(x => x.ScraperDefinitionId == scraperId)
            .OrderByDescending(x => x.ScrapedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ScrapedEventRaw>> GetForScraperAsync(
        Guid scraperId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _db.RawEvents
            .Where(x => x.ScraperDefinitionId == scraperId)
            .OrderByDescending(x => x.ScrapedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
