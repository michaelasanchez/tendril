using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class MappingRuleRepository(TendrilDbContext db) : IMappingRuleRepository
{
    public async Task<List<ScraperMappingRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await db.MappingRules
            .Where(r => r.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperMappingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.MappingRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        db.MappingRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        db.MappingRules.Update(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        db.MappingRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
    }
}
