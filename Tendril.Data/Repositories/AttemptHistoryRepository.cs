using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data;

namespace Tendril.Data.Repositories;

public class AttemptHistoryRepository : IAttemptHistoryRepository
{
    private readonly TendrilDbContext _db;

    public AttemptHistoryRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ScraperAttemptHistory attempt, CancellationToken cancellationToken = default)
    {
        _db.AttemptHistory.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
