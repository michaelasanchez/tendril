using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class AttemptHistoryRepository(TendrilDbContext db) : IAttemptHistoryRepository
{
    public async Task Add(ScraperAttemptHistory attempt, CancellationToken cancellationToken = default)
    {
        db.AttemptHistory.Add(attempt);

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<ScraperAttemptHistory>> GetAttemptHistories(Guid scraperId, CancellationToken ct = default)
    {
        return db.AttemptHistory
            .AsNoTracking()
            .Where(a => a.ScraperDefinitionId == scraperId)
            .OrderByDescending(a => a.StartTimeUtc)
            .ToListAsync(ct);
    }
}
