using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class VenueRepository(TendrilDbContext db) : IVenueRepository
{
    public async Task<List<Venue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Venues
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Venues
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        db.Venues.Add(venue);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        db.Venues.Update(venue);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        db.Venues.Remove(venue);
        await db.SaveChangesAsync(cancellationToken);
    }
}
