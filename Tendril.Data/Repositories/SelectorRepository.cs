using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Repositories;

public class SelectorRepository : ISelectorRepository
{
    private readonly TendrilDbContext _db;

    public SelectorRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScraperSelector>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await _db.Selectors
            .Where(x => x.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperSelector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Selectors
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperSelector selector, CancellationToken cancellationToken = default)
    {
        _db.Selectors.Add(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperSelector selector, CancellationToken cancellationToken = default)
    {
        _db.Selectors.Update(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperSelector selector, CancellationToken cancellationToken = default)
    {
        _db.Selectors.Remove(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
