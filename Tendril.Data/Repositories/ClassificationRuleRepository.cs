using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ClassificationRuleRepository : IClassificationRuleRepository
{
    private readonly TendrilDbContext _db;

    public ClassificationRuleRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScraperClassificationRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await _db.ClassificationRules
            .Where(r => r.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperClassificationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ClassificationRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        _db.ClassificationRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        _db.ClassificationRules.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        _db.ClassificationRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
