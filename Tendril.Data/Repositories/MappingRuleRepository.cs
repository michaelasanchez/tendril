using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class MappingRuleRepository : IMappingRuleRepository
{
    private readonly TendrilDbContext _db;

    public MappingRuleRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScraperMappingRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await _db.MappingRules
            .Where(r => r.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperMappingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.MappingRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        _db.MappingRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        _db.MappingRules.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default)
    {
        _db.MappingRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
