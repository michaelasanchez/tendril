using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ScraperRepository(TendrilDbContext db) : IScraperRepository
{
    public async Task<List<ScraperDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ScraperDefinition>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Selectors.Where(z => !z.Disabled))
            .Include(s => s.ClassificationRules.Where(z => !z.Disabled))
            .Include(s => s.MappingRules.Where(z => !z.Disabled))
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdWithDisabledDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Selectors)
            .Include(s => s.ClassificationRules)
            .Include(s => s.MappingRules)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Selectors.Where(z => !z.Disabled))
            .Include(s => s.ClassificationRules.Where(z => !z.Disabled))
            .Include(s => s.MappingRules.Where(z => !z.Disabled))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Add(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Update(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Remove(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }
}
