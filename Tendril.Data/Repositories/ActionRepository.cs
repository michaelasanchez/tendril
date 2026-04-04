using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;

namespace Tendril.Data.Repositories;

public class ActionRepository : IActionRepository
{
    private readonly TendrilDbContext _db;

    public ActionRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScraperAction>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await _db.Actions
            .Where(x => x.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Actions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperAction selector, CancellationToken cancellationToken = default)
    {
        _db.Actions.Add(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperAction selector, CancellationToken cancellationToken = default)
    {
        _db.Actions.Update(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperAction selector, CancellationToken cancellationToken = default)
    {
        _db.Actions.Remove(selector);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
