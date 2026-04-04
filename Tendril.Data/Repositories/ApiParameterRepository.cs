using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ApiParameterRepository(TendrilDbContext db) : IApiParameterRepository
{
    public async Task<List<ApiParameter>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default)
    {
        return await db.ApiParameters
            .Where(r => r.ScraperDefinitionId == scraperId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiParameter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.ApiParameters
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(ApiParameter rule, CancellationToken cancellationToken = default)
    {
        db.ApiParameters.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ApiParameter rule, CancellationToken cancellationToken = default)
    {
        db.ApiParameters.Update(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ApiParameter rule, CancellationToken cancellationToken = default)
    {
        db.ApiParameters.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
    }
}
