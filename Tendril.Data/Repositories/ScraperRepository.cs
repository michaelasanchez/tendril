using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ScraperRepository : IScraperRepository
{
    private readonly TendrilDbContext _db;

    public ScraperRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScraperDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Scrapers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Scrapers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Scrapers
            .Include(s => s.Selectors)
            .Include(s => s.MappingRules)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        _db.Scrapers.Add(scraper);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        _db.Scrapers.Update(scraper);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        _db.Scrapers.Remove(scraper);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Used by Worker to pull full scraper config for execution
    public async Task<List<ScraperDefinition>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Scrapers
            .Include(x => x.Selectors)
            .Include(x => x.MappingRules)
            .ToListAsync(cancellationToken);
    }
}
