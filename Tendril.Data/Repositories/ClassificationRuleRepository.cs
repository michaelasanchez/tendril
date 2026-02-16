using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ClassificationRuleRepository(TendrilDbContext db) : IClassificationRuleRepository
{
    public async Task<List<ScraperClassificationRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await db.ClassificationRules
            .Where(r => r.ScraperDefinitionId == scraperId)
            .Include(z => z.Assignments)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperClassificationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.ClassificationRules
            .Include(z => z.Assignments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        db.ClassificationRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        db.ClassificationRules.Update(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default)
    {
        db.ClassificationRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
    }
}
